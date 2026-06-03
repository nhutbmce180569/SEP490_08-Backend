using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StayHub.Common.Resources;

namespace StayHub.Common.Localization;

public static class LocalizationExtensions
{
    public static IServiceCollection AddStayHubLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        services.TryAddScoped<ValidationLocalizer>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("vi")
            };

            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new LanguageHeaderRequestCultureProvider());
            options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
            options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
        });

        return services;
    }

    /// <summary>
    /// Localizes DataAnnotations <c>ErrorMessage</c> values via <see cref="ValidationMessages"/> (.resx).
    /// Chain after <c>AddControllers()</c> / <c>AddOData()</c>.
    /// </summary>
    public static IMvcBuilder AddStayHubDataAnnotationsLocalization(this IMvcBuilder mvcBuilder)
    {
        return mvcBuilder.AddDataAnnotationsLocalization(options =>
        {
            options.DataAnnotationLocalizerProvider = (_, factory) =>
                factory.Create(typeof(ValidationMessages));
        });
    }

    public static IApplicationBuilder UseStayHubLocalization(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        return app.UseRequestLocalization(options);
    }
}
