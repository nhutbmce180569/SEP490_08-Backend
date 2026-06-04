using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StayHub.Common.Extensions;
using StayHub.Common.Localization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using TourAPI.Helpers;
using TourAPI.Mappers;
using TourAPI.Models;
using TourAPI.Repositories;
using TourAPI.Repositories.Implements;
using TourAPI.Services;
using TourAPI.Services.Implements;

namespace TourAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<StayHubCatalogDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<CloudinarySettings   >(
                builder.Configuration.GetSection("CloudinarySettings")
            );

            builder.Services.AddSingleton<Cloudinary>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;

                var account = new Account(
                    config.CloudName,
                    config.ApiKey,
                    config.ApiSecret
                );

                return new Cloudinary(account);
            });

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<TourProfile>();
                cfg.AddProfile<TourItineraryProfile>();
                cfg.AddProfile<TourScheduleProfile>();
                cfg.AddProfile<TourScheduleStaffProfile>();
                cfg.AddProfile<TourScheduleTicketProfile>();
                cfg.AddProfile<TourScheduleItineraryProfile>();
                cfg.AddProfile<ReviewProfile>();
                cfg.AddProfile<ReviewReplyProfile>();
                cfg.AddProfile<WishlistProfile>();
            });

            builder.Services.AddScoped<CloudinaryService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<AuthorizationHeaderHandler>();

            builder.Services.AddHttpClient<ITourService, TourService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<ICategoryService, CategoryService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<ITicketTypeApiClient, TicketTypeApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var gatewayUrl = builder.Configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
                client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddScoped<ITourItineraryService, TourItineraryService>();
            builder.Services.AddScoped<ITourScheduleService, TourScheduleService>();
            builder.Services.AddScoped<ITourScheduleStaffService, TourScheduleStaffService>();
            builder.Services.AddScoped<ITourScheduleTicketService, TourScheduleTicketService>();
            builder.Services.AddScoped<ITourScheduleItineraryService, TourScheduleItineraryService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<ITourRepository, TourRepository>();
            builder.Services.AddScoped<ITourItineraryRepository, TourItineraryRepository>();
            builder.Services.AddScoped<ITourScheduleRepository, TourScheduleRepository>();
            builder.Services.AddScoped<ITourScheduleStaffRepository, TourScheduleStaffRepository>();
            builder.Services.AddScoped<ITourScheduleTicketRepository, TourScheduleTicketRepository>();
            builder.Services.AddScoped<ITourScheduleItineraryRepository, TourScheduleItineraryRepository>();
            builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IReviewReplyRepository, ReviewReplyRepository>();
            builder.Services.AddScoped<ICustomerEngagementRepository, CustomerEngagementRepository>();
            builder.Services.AddScoped<ICustomerAnalyticsService, CustomerAnalyticsService>();
            builder.Services.AddScoped<IPlatformCatalogRepository, PlatformCatalogRepository>();
            builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
            builder.Services.AddHttpClient<IPlatformAnalyticsClients, PlatformAnalyticsClients>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                var gatewayUrl = builder.Configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
                client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<IAuthAnalyticsClient, AuthAnalyticsClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                var gatewayUrl = builder.Configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
                client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<IBookingAnalyticsClient, BookingAnalyticsClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                var gatewayUrl = builder.Configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
                client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<IReviewService, ReviewService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddHttpClient<INotificationInternalService, NotificationInternalService>();
            //{
            //    client.Timeout = TimeSpan.FromSeconds(10);
            //}).AddHttpMessageHandler<AuthorizationHeaderHandler>();
            builder.Services.AddHttpClient<IOrderService, OrderService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
           .AddHttpMessageHandler<AuthorizationHeaderHandler>();
            // Add services to the container.

            builder.Services.AddControllers().AddStayHubDataAnnotationsLocalization();
            builder.Services.AddStayHubLocalization();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // Swagger with Bearer JWT security
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Tour API", Version = "v1" });

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
            builder.Services.AddStayHubCors(builder.Configuration, "FrontendDev");
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStayHubHttpScheme();
            app.UseCors("FrontendDev");
            app.UseStayHubLocalization();

           
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
