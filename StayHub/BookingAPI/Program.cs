
using BookingAPI.Helpers;
using BookingAPI.Mappers;
using BookingAPI.Models;
using BookingAPI.Repositories;
using BookingAPI.Repositories.Implements;
using BookingAPI.Services;
using BookingAPI.Services.Implements;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
namespace BookingAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddDbContext<StayHubBookingDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfire(config =>
            config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();


            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<ITourApiClient, TourApiClient>(client =>
            {
                var tourApiBaseUrl = builder.Configuration["GatewayApi:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(tourApiBaseUrl))
                {
                    client.BaseAddress = new Uri(tourApiBaseUrl.TrimEnd('/') + "/");
                }
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<IVoucherApiClient, VoucherApiClient>(client =>
            {
                var gatewayBaseUrl = builder.Configuration["GatewayApi:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(gatewayBaseUrl))
                {
                    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
                }
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<ITicketService, TicketService>();
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<OrderProfile>();
                cfg.AddProfile<TicketProfile>();
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // 2. CẤU HÌNH SWAGGER CHUẨN (Tự động thêm Bearer)
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Booking API", Version = "v1" });

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
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[]{}
                    }
                });
            });
            // 3. CẤU HÌNH AUTHENTICATION TRIỆT ĐỂ
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            //Đăng ký JWT Authentication
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
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard();
            app.MapControllers();

            app.Run();
        }
    }
}
