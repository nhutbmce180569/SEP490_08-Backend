using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;

namespace GatewayAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // 1. SỬA LẠI CẤU HÌNH CORS CHO SIGNALR
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173") 
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
            }

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            app.UseHttpsRedirection();

            // Áp dụng CORS vừa sửa
            app.UseCors("AllowFrontend");

            // 2. MIDDLEWARE CHECK REVOKE TOKEN (CẬP NHẬT CHO SIGNALR)
            app.Use(async (context, next) =>
            {
                string token = string.Empty;

                // TH1: Tìm token trong Header (Cho các API thông thường)
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
                // TH2: Tìm token trong Query String (Cho SignalR WebSockets)
                else if (context.Request.Query.ContainsKey("access_token"))
                {
                    token = context.Request.Query["access_token"].ToString();
                }

                // Nếu có token (từ Header hoặc Query String), tiến hành kiểm tra với Redis
                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();

                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadJwtToken(token);
                        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == "sub")?.Value;
                        var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;

                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(iatClaim))
                        {
                            var redis = context.RequestServices.GetService<IConnectionMultiplexer>();
                            if (redis != null)
                            {
                                var db = redis.GetDatabase();
                                var revokeTimestampStr = await db.StringGetAsync($"revoke_user_{userId}");

                                if (revokeTimestampStr.HasValue)
                                {
                                    long revokeTimestamp = long.Parse(revokeTimestampStr);
                                    long tokenIat = long.Parse(iatClaim);

                                    if (tokenIat <= revokeTimestamp)
                                    {
                                        context.Response.StatusCode = 401;
                                        context.Response.ContentType = "application/json";
                                        await context.Response.WriteAsJsonAsync(new { message = "Token has been invalidated due to a password change. Please log in again." });
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                await next();
            });

            app.UseAuthorization();

            // YARP tự động hỗ trợ WebSockets, cứ thế Map là nó đi qua
            app.MapReverseProxy();
            app.MapControllers();

            app.Run();
        }
    }
}