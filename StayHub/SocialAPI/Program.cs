using FluentValidation;

using FluentValidation.AspNetCore;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
// Thêm các thư viện cần thiết để nhận diện được Services và Repositories
using SocialAPI.DTOs;
using SocialAPI.Helper;
using SocialAPI.Hubs;
using SocialAPI.Models;
using SocialAPI.Repositories;
using SocialAPI.Repositories.Implements;
using SocialAPI.Services;
using SocialAPI.Services.Implements;
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
            builder.Services.AddHttpClient(); // Đã được cấu hình để Inject HttpClient trong Service
            builder.Services.AddDbContext<StayHubSocialDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers().AddOData(options =>
                options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100)
                       .AddRouteComponents("api", GetEdmModel())
            ).AddStayHubDataAnnotationsLocalization();
            builder.Services.AddStayHubLocalization();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            // 2. CẤU HÌNH SWAGGER CHUẨN (Tự động thêm Bearer)
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Social API", Version = "v1" });

                // DÒNG NÀY SẼ CỨU SWAGGER KHỎI LỖI XUNG ĐỘT VỚI ODATA:
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

            // 3. CẤU HÌNH AUTHENTICATION TRIỆT ĐỂ
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            // Đăng ký JWT Authentication
            // Đăng ký JWT Authentication
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

                    ClockSkew = TimeSpan.Zero // Không cho thời gian trễ
                };

                // ==========================================================
                // THÊM ĐOẠN NÀY ĐỂ BẮT TOKEN CHO SIGNALR (WEBSOCKET)
                // ==========================================================
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Lấy token từ query string
                        var accessToken = context.Request.Query["access_token"];

                        // Lấy path của request
                        var path = context.HttpContext.Request.Path;

                        // 🚨 FIX LỖI: Chỉ cần check bắt đầu bằng "/hubs" là nhận hết mọi Hub
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            // Gắn token vào context để hệ thống xác thực
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // ============================================================
            // 4. ĐĂNG KÝ DEPENDENCY INJECTION (DI) CHO CỤM CHỨC NĂNG 
            // ============================================================

            // 4.1 Cấu hình Cloudinary
            builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

            // 2. Đăng ký Location Service
            builder.Services.AddScoped<ILocationService, LocationService>();
            // 4.2 Đăng ký Repositories & Services
            builder.Services.AddScoped<IMomentRepository, MomentRepository>();
            builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();

            builder.Services.AddScoped<IMomentService, MomentService>();

            builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();
            builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
            // 4.3 Đăng ký AutoMapper (Cách dùng Lambda Action an toàn nhất)
            builder.Services.AddAutoMapper(cfg =>
            {
                // Lưu ý: Nếu file Profile của bạn tên là "MomentProfile" thì đổi tên ở đây nhé
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
            // ============================================================

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.MapHub<ChatHub>("/hubs/chat");
            app.UseHttpsRedirection();

            app.UseCors("AllowSignalR");
            app.UseStayHubLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<FriendshipHub>("/hubs/friendship");
            app.MapHub<TrackingHub>("/hubs/tracking");

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