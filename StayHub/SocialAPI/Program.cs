using FluentValidation;
using FluentValidation.AspNetCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using SocialAPI.DTOs;
using SocialAPI.Helper;
using SocialAPI.Hubs;
using SocialAPI.Models;
using SocialAPI.Repositories;
using SocialAPI.Repositories.Implements;
using SocialAPI.Services;
using SocialAPI.Services.Implements;
using SocialAPI.Helpers;
using StayHub.Common.Localization;
using StackExchange.Redis;
using System.Text;

namespace SocialAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddHttpClient();
            builder.Services.AddDbContext<StayHubSocialDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers().AddOData(options =>
                options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
                       .AddRouteComponents("api", GetEdmModel())
            ).AddStayHubDataAnnotationsLocalization();
            builder.Services.AddStayHubLocalization();

            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IChatNotificationService, ChatNotificationService>();

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddEndpointsApiExplorer();

            // CẤU HÌNH SWAGGER CHUẨN
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Social API", Version = "v1" });
                option.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Dán Token vào đây (Không cần gõ chữ Bearer)",
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
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        },
                        new string[]{}
                    }
                });
            });

            // CẤU HÌNH AUTHENTICATION TRIỆT ĐỂ
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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

                // THÊM ĐOẠN BẮT TOKEN CHO SIGNALR (WEBSOCKET CHUNG TOÀN CỤC)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // ============================================================
            // 🚨 SỬA LỖI DI CRASH: Đăng ký IAuthApiClient vào Container
            // ============================================================
            builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["AuthApi:BaseUrl"] ?? "https://localhost:7001/");
            });

            // Cấu hình Cloudinary & Redis
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

            // Đăng ký các dịch vụ hệ thống khác
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IMomentRepository, MomentRepository>();
            builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();
            builder.Services.AddScoped<IContentModerator, ContentModerator>();
            builder.Services.AddScoped<IMomentService, MomentService>();
            builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();
            builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
            builder.Services.AddHttpClient<IBookingApiClient, BookingApiClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["BookingApi:BaseUrl"]
                    ?? "https://localhost:7002/");
            });
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            builder.Services.AddSignalR();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSignalR", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("google-services.json")
                });
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowSignalR");
            app.UseStayHubLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ============================================================
            // MAPPING CÁC CỔNG ENDPOINT HUB SIGNALR TOÀN CỤC VÀ CỤC BỘ
            // ============================================================
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapHub<FriendshipHub>("/hubs/friendship");
            app.MapHub<TrackingHub>("/hubs/tracking");
            app.MapHub<NotificationHub>("/hubs/global-chat"); // Định vị cổng Hub thông báo riêng biệt của phân hệ mình

            app.Run();
        }

        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<MomentResponseDto>("Moments");
            return builder.GetEdmModel();
        }
    }
}