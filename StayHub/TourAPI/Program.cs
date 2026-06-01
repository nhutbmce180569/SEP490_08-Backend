using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
            builder.Services.AddScoped<ITourItineraryService, TourItineraryService>();
            builder.Services.AddScoped<ITourScheduleService, TourScheduleService>();
            //builder.Services.AddScoped<ITourScheduleStaffService, TourScheduleStaffService>();
            builder.Services.AddScoped<ITourScheduleTicketService, TourScheduleTicketService>();
            builder.Services.AddScoped<ITourScheduleItineraryService, TourScheduleItineraryService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<ITourRepository, TourRepository>();
            builder.Services.AddScoped<ITourItineraryRepository, TourItineraryRepository>();
            builder.Services.AddScoped<ITourScheduleRepository, TourScheduleRepository>();
            // builder.Services.AddScoped<ITourScheduleStaffRepository, TourScheduleStaffRepository>();
            builder.Services.AddScoped<ITourScheduleTicketRepository, TourScheduleTicketRepository>();
            builder.Services.AddScoped<ITourScheduleItineraryRepository, TourScheduleItineraryRepository>();
            builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IReviewReplyRepository, ReviewReplyRepository>();
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

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            // 2. CẤU HÌNH SWAGGER CHUẨN (Tự động thêm Bearer)
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "Tour API", Version = "v1" });

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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendDev", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    // Nếu request có cookie/session thì mới thêm:
                    // .AllowCredentials();
                });
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("FrontendDev");   // ← THÊM DÒNG NÀY

           
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
