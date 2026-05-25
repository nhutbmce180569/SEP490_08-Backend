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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
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

            app.UseCors("AllowAll");

            app.Use(async (context, next) =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();

                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
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

            app.MapReverseProxy();
            app.MapControllers();

            app.Run();
        }
    }
}