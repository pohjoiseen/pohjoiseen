using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;

namespace Fennica3;

public static class FennicaExtensions
{
    public static IServiceCollection AddFennicaServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<RouteOptions>(options =>
        {
            options.AppendTrailingSlash = true;
        });
        services.Configure<WebEncoderOptions>(options =>
        {
            // avoid escaping all the cyrillic in templates
            options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
        });
        services.Configure<RequestLocalizationOptions>(options =>
        {
            // culture is received from "language" route parameter
            var supportedCultures = Fennica3.Languages.Select(l => new CultureInfo(l)).ToList();
            options.DefaultRequestCulture = new RequestCulture(supportedCultures[0], supportedCultures[0]);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders = new List<IRequestCultureProvider> { 
                new CustomRequestCultureProvider(async ctx =>
                {
                    ctx.Request.RouteValues.TryGetValue("language", out var language);
                    language ??= Fennica3.Languages[0];

                    return await Task.FromResult(new ProviderCultureResult(language as string));
                })
            };
        });
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365 * 2);
        });
        
        services.AddScoped<Helpers>();
        services.AddScoped<ContentFormatter>();
        return services;
    }
}