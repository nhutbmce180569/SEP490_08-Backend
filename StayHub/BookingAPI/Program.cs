
using BookingAPI.Helpers;
using BookingAPI.Mappers;
using BookingAPI.Mappings;
using BookingAPI.Models;
using StayHub.Common.Localization;
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

            builder.Services.AddControllers().AddStayHubDataAnnotationsLocalization();
            builder.Services.AddStayHubLocalization();
            builder.Services.AddDbContext<StayHubBookingDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfire(config =>
            config.UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();

            builder.Services.AddScoped<IEligibleScheduleService, EligibleScheduleService>();
            builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IOrderAnalyticsService, OrderAnalyticsService>();
            builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
            builder.Services.AddScoped<ICancellationRepository, CancellationRepository>();
            builder.Services.AddScoped<ICancellationService, CancellationService>();
            builder.Services.AddHttpClient<IContentApiClient, ContentApiClient>(client =>
            {
                var contentApiBaseUrl = builder.Configuration["ContentApi:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(contentApiBaseUrl))
                {
                    client.BaseAddress = new Uri(contentApiBaseUrl.TrimEnd('/') + "/");
                }
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();

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

            builder.Services.AddHttpClient("TourApiClient", client =>
            {
                var tourApiBaseUrl = builder.Configuration["GatewayApi:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(tourApiBaseUrl))
                {
                    client.BaseAddress = new Uri(tourApiBaseUrl.TrimEnd('/') + "/");
                }
            });
            builder.Services.AddHttpClient<IVoucherApiClient, VoucherApiClient>(client =>
            {
                var gatewayBaseUrl = builder.Configuration["GatewayApi:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(gatewayBaseUrl))
                {
                    client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
                }
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<INotificationInternalService, NotificationInternalService>(client =>
            {
                var systemApiBaseUrl = builder.Configuration["SystemApi:BaseUrl"]
                    ?? builder.Configuration["GatewayApi:BaseUrl"];

                if (!string.IsNullOrWhiteSpace(systemApiBaseUrl))
                {
                    client.BaseAddress = new Uri(systemApiBaseUrl.TrimEnd('/') + "/");
                }
            });
            builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
            {
                var authApiBaseUrl = builder.Configuration["AuthApi:BaseUrl"] ?? "https://localhost:7001";
                if (!string.IsNullOrWhiteSpace(authApiBaseUrl))
                {
                    client.BaseAddress = new Uri(authApiBaseUrl.TrimEnd('/') + "/");
                }
            });
            builder.Services.AddHttpClient("SocialApiClient", client =>
  {
      var gatewayBaseUrl = builder.Configuration["GatewayApi:BaseUrl"] ?? "https://localhost:7000";
      client.BaseAddress = new Uri(gatewayBaseUrl.TrimEnd('/') + "/");
  });
            builder.Services.AddScoped<ITicketRepository, TicketRepository>();
            builder.Services.AddScoped<ITicketService, TicketService>();
            builder.Services.AddHostedService<BookingAPI.BackgroundServices.OrderNotificationBackgroundService>();
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<OrderProfile>();
                cfg.AddProfile<TicketProfile>();
                cfg.AddProfile<CancellationProfile>();
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // Swagger with Bearer JWT security
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Booking API", Version = "v1" });

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Paste your JWT token here (do not include the Bearer prefix)",
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
            // JWT authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            // Register JWT authentication
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

                    ClockSkew = TimeSpan.Zero // No clock skew tolerance
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
            app.UseStayHubLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard();
            app.MapControllers();

            RecurringJob.AddOrUpdate<IBackgroundJobService>(
                "cancel-expired-unpaid-orders",
                service => service.CancelExpiredUnpaidOrdersAsync(),
                Cron.Minutely);

            app.Run();
        }
    }
}
