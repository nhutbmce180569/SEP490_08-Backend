using AuthAPI.BackgroundServices;
using AuthAPI.DTOs;
using AuthAPI.Helpers;
using AuthAPI.Helpers.Implements;
using AuthAPI.Mappers;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AuthAPI.Repositories.Implements;
using AuthAPI.Services;
using AuthAPI.Services.Implements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add DbContext
            builder.Services.AddDbContext<StayHubIdentityDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // 2. SWAGGER CONFIGURATION (Automatically handles Bearer token)
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Paste your Token here (Do not include the 'Bearer' prefix)",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[]{}
                    }
                });
            });

            // === 1. CORS CONFIGURATION FOR FRONTEND APP ===
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // 3. STRICT AUTHENTICATION CONFIGURATION
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            // Register JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<StayHubIdentityDbContext>();

                        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
                        var tokenSecurityStamp = context.Principal?.FindFirst("SecurityStamp")?.Value;

                        if (userIdClaim == null || string.IsNullOrEmpty(tokenSecurityStamp))
                        {
                            context.Fail("Invalid token payload or missing security credentials.");
                            return;
                        }

                        int userId = int.Parse(userIdClaim.Value);

                        var currentUserInfo = await dbContext.Users
                            .Where(u => u.Id == userId)
                            .Select(u => new { u.SecurityStamp, u.Status })
                            .FirstOrDefaultAsync();

                        // Deny access if user is deleted, deactivated, or SecurityStamp does not match
                        if (currentUserInfo == null ||
                            currentUserInfo.Status != "Active" ||
                            currentUserInfo.SecurityStamp != tokenSecurityStamp)
                        {
                            context.Fail("Session expired due to security changes or account deactivation. Please log in again.");
                        }
                    },

                    // === 2. RETURN STANDARDIZED JSON ERROR PAYLOAD FOR FRONTEND ===
                    OnChallenge = async context =>
                    {
                        // Suppress the default plain browser challenge response
                        context.HandleResponse();

                        // Extract the failure message set during OnTokenValidated (if any)
                        var errorMessage = context.AuthenticateFailure?.Message
                                           ?? "Invalid or expired login session.";

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Unauthorized",
                            message = errorMessage
                        });

                        await context.Response.WriteAsync(jsonResponse);
                    }
                };
            });

            builder.Services.AddHttpClient();
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

            // BackgroundService
            builder.Services.AddHostedService<TokenCleanupService>();

            // Mapper
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<UserProfile>());

            // Helper
            builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
            builder.Services.AddScoped<IJwtHelper, JwtHelper>();

            // Đọc chuỗi kết nối từ file appsettings.json của AuthAPI
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
            }
            // =======================================

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowReactApp");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}