//
// Fennica3 blog front-end ASP.NET MVC application.  See README.md.
//
using Fennica3;
using Holvi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHolviServices(builder.Configuration);
builder.Services.AddFennicaServices(builder.Configuration);
builder.Services.AddControllersWithViews();

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