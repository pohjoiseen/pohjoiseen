//
// Fennica3 blog front-end ASP.NET MVC application.  See README.md.
//
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Fennica3;
using Holvi;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHolviServices(builder.Configuration);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews();
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = true;
});
builder.Services.Configure<WebEncoderOptions>(options =>
{
    // avoid escaping all the cyrillic in templates
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // culture is received from "language" route parameter
    var supportedCultures = Fennica3.Fennica3.Languages.Select(l => new CultureInfo(l)).ToList();
    options.DefaultRequestCulture = new RequestCulture(supportedCultures[0], supportedCultures[0]);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider> { 
        new CustomRequestCultureProvider(async ctx =>
        {
            ctx.Request.RouteValues.TryGetValue("language", out var language);
            language ??= Fennica3.Fennica3.Languages[0];

            return await Task.FromResult(new ProviderCultureResult(language as string));
        })
    };
});
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365 * 2);
});
builder.Services.AddScoped<Helpers>();
builder.Services.AddScoped<ContentFormatter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRequestLocalization();
app.UseStatusCodePagesWithReExecute("/status-code/{0}");

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();