using System.Text;
using AIAPI.BackgroundServices;
using AIAPI.Clients;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Recommender;
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
using StayHub.Common.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GatewaySettings>(builder.Configuration.GetSection(GatewaySettings.SectionName));
builder.Services.Configure<MlSettings>(builder.Configuration.GetSection(MlSettings.SectionName));
builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection(WeatherSettings.SectionName));

builder.Services.Configure<RecommenderSettings>(builder.Configuration.GetSection(RecommenderSettings.SectionName));

builder.Services.AddDbContext<StayHubAiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationHeaderHandler>();

builder.Services.AddHttpClient<IGatewayCatalogClient, GatewayCatalogClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthorizationHeaderHandler>();

builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});

builder.Services.AddHttpClient("Wikidata", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.BaseAddress = new Uri("https://www.wikidata.org/");
});

builder.Services.AddSingleton<ICatalogStore, CatalogStore>();
builder.Services.AddSingleton<IRagKnowledgeIndex, RagKnowledgeIndex>();
builder.Services.AddSingleton<ISystemKnowledgeIndex, SystemKnowledgeIndex>();
builder.Services.AddSingleton<IDimensionWeightProvider, DimensionWeightProvider>();
builder.Services.AddScoped<IAiCultureAccessor, AiCultureAccessor>();
builder.Services.AddScoped<IAiLocalizedCopy, AiLocalizedCopy>();
builder.Services.AddScoped<IKnowledgeLocalizationService, KnowledgeLocalizationService>();
builder.Services.AddScoped<TourScoringEngine>();
builder.Services.AddScoped<DimensionWeightCalibrator>();
builder.Services.AddScoped<TourRanker>();
builder.Services.AddSingleton<IMlModelRegistry, MlModelRegistry>();
builder.Services.AddSingleton<QueryEntityExtractor>();

builder.Services.AddScoped<ICatalogSyncService, CatalogSyncService>();
builder.Services.AddScoped<IModelTrainingService, ModelTrainingService>();
builder.Services.AddScoped<ITourSemanticSearchService, TourSemanticSearchService>();
builder.Services.AddScoped<ITourRecommendationService, TourRecommendationService>();
builder.Services.AddScoped<ITourAssistantService, TourAssistantService>();
builder.Services.AddScoped<ICulturalKnowledgeService, CulturalKnowledgeService>();
builder.Services.AddScoped<IPersonalizedTourRecommendationService, PersonalizedTourRecommendationService>();
builder.Services.AddScoped<IRecommenderEvaluationService, RecommenderEvaluationService>();
builder.Services.AddScoped<IGroundTruthLabelService, GroundTruthLabelService>();
builder.Services.AddScoped<IUserStudyService, UserStudyService>();
builder.Services.AddScoped<IUserStudyPilotSeeder, UserStudyPilotSeeder>();
builder.Services.AddScoped<IPaperExportService, PaperExportService>();
builder.Services.AddScoped<IInterRaterAgreementService, InterRaterAgreementService>();
builder.Services.AddSingleton<IEvaluationResultsExporter, EvaluationResultsExporter>();

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Services.AddHostedService<TourAiWarmupBackgroundService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

builder.Services.AddControllers().AddStayHubDataAnnotationsLocalization();
builder.Services.AddStayHubLocalization();
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
app.UseStayHubLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
