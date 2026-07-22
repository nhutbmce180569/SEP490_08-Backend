using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StayHub.Common.Localization;
using System.Text;
using VoucherAPI.Helpers;
using VoucherAPI.Mappers;
using VoucherAPI.Models;
using VoucherAPI.Repositories;
using VoucherAPI.Repositories.Implements;
using VoucherAPI.Services;
using VoucherAPI.Services.Implements;

namespace VoucherAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<StayHubVoucherDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<VoucherProfile>();
            cfg.AddProfile<CustomerVoucherProfile>();
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<AuthorizationHeaderHandler>();

        builder.Services.AddHttpClient<ITourValidationService, TourValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        .AddHttpMessageHandler<AuthorizationHeaderHandler>();

        builder.Services.AddHttpClient<IUserValidationService, UserValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        .AddHttpMessageHandler<AuthorizationHeaderHandler>();

        builder.Services.AddHttpClient<IBookingAnalyticsClient, BookingAnalyticsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        .AddHttpMessageHandler<AuthorizationHeaderHandler>();

        builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
        builder.Services.AddScoped<IUserVoucherRepository, UserVoucherRepository>();
        builder.Services.AddScoped<IVoucherService, VoucherService>();
        builder.Services.AddScoped<ICustomerVoucherService, CustomerVoucherService>();
        builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddHttpClient<INotificationInternalService, NotificationInternalService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        builder.Services.AddControllers().AddStayHubDataAnnotationsLocalization();
        builder.Services.AddStayHubLocalization();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo { Title = "Voucher API", Version = "v1" });

            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Paste JWT token here",
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
                    Array.Empty<string>()
                }
            });
        });

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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                ClockSkew = TimeSpan.Zero
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
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("FrontendDev");
        app.UseStayHubLocalization();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Auto-remap dummy seed UserVouchers to valid existing Customer IDs & ensure MinOrderAmount column exists
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<StayHubVoucherDbContext>();

                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM sys.columns 
                            WHERE object_id = OBJECT_ID('Vouchers') AND name = 'MinOrderAmount'
                        )
                        BEGIN
                            ALTER TABLE Vouchers ADD MinOrderAmount BIGINT NULL;
                        END
                    ");

                    dbContext.Database.ExecuteSqlRaw(@"
                        UPDATE Vouchers SET MinOrderAmount = 500000 WHERE Code = 'STUDENT2025' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                        UPDATE Vouchers SET MinOrderAmount = 1000000 WHERE Code = 'TET2025' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                        UPDATE Vouchers SET MinOrderAmount = 2000000 WHERE Code = 'HONEYMOON25' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                        UPDATE Vouchers SET MinOrderAmount = 1500000 WHERE Code = 'EARLYBIRD25' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                        UPDATE Vouchers SET MinOrderAmount = 300000 WHERE Code = 'SUMMER25' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                        UPDATE Vouchers SET MinOrderAmount = 1000000 WHERE Code = 'TOPCUST2026' AND (MinOrderAmount IS NULL OR MinOrderAmount = 0);
                    ");
                    Console.WriteLine("[VoucherAPI] Successfully verified MinOrderAmount column and seeded default values.");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"[VoucherAPI] DB Migration/Seed notice: {dbEx.Message}");
                }

                var userValService = scope.ServiceProvider.GetRequiredService<IUserValidationService>();

                var batchUsers = userValService.GetUsersBatchAsync(Enumerable.Range(1, 100).ToList()).GetAwaiter().GetResult();
                var validUserIds = batchUsers.Select(u => u.Id).ToList();

                if (validUserIds.Count > 0)
                {
                    var dummyUserVouchers = dbContext.UserVouchers.ToList();
                    int remapIdx = 0;
                    bool updated = false;
                    foreach (var uv in dummyUserVouchers)
                    {
                        if (!validUserIds.Contains(uv.UserId))
                        {
                            uv.UserId = validUserIds[remapIdx % validUserIds.Count];
                            remapIdx++;
                            updated = true;
                        }
                    }
                    if (updated)
                    {
                        dbContext.SaveChanges();
                        Console.WriteLine("[VoucherAPI] Successfully remapped dummy UserVouchers to valid customer IDs.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VoucherAPI] Remap warning: {ex.Message}");
            }
        }

        app.Run();
    }
}
