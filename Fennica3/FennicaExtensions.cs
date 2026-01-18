using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Controllers;
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
                    // this code runs also for KoTi, as it includes Fennica3 completely;
                    // make sure language is forced to default English if the controller is not from
                    // Fennica3 assembly
                    if (ctx.GetEndpoint()?.Metadata
                            .FirstOrDefault(m => m.GetType() == typeof(ControllerActionDescriptor)) is ControllerActionDescriptor controllerActionDescriptor &&
                        controllerActionDescriptor.ControllerTypeInfo.Assembly != typeof(Fennica3).Assembly)
                    {
                        return await Task.FromResult(new ProviderCultureResult("en-US"));
                    }
                    
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