using System.Text;
using AIAPI.BackgroundServices;
using AIAPI.Clients;
using AIAPI.Helpers;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Services;
using AIAPI.Services.Implements;
using AIAPI.Settings;
using AIAPI.Validations;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GatewaySettings>(builder.Configuration.GetSection(GatewaySettings.SectionName));
builder.Services.Configure<MlSettings>(builder.Configuration.GetSection(MlSettings.SectionName));

builder.Services.AddDbContext<StayHubAiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationHeaderHandler>();

builder.Services.AddHttpClient<IGatewayCatalogClient, GatewayCatalogClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthorizationHeaderHandler>();

builder.Services.AddSingleton<ICatalogStore, CatalogStore>();
builder.Services.AddSingleton<IMlModelRegistry, MlModelRegistry>();
builder.Services.AddSingleton<QueryEntityExtractor>();

builder.Services.AddScoped<ICatalogSyncService, CatalogSyncService>();
builder.Services.AddScoped<IModelTrainingService, ModelTrainingService>();
builder.Services.AddScoped<ITourSemanticSearchService, TourSemanticSearchService>();
builder.Services.AddScoped<ITourRecommendationService, TourRecommendationService>();
builder.Services.AddScoped<ITourAssistantService, TourAssistantService>();

builder.Services.AddHostedService<TourAiWarmupBackgroundService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "StayHub AI API", Version = "v1" });
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

var jwtKey = builder.Configuration["Jwt:Key"]!;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
