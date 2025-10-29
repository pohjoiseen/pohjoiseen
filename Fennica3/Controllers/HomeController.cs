using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Fennica3.ViewModels;

namespace Fennica3.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Blog", "Blog", new { language = Fennica3.Languages[0] });
    }
    
    [HttpGet("/500")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            LayoutParams = new LayoutParams
            {
                Title = "Error",
                Language = CultureInfo.CurrentCulture.Name
            }
        });
    }
    
    [HttpGet("/status-code/{code}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult StatusCode(int code)
    {
        return View(new StatusCodeModel
        {
            StatusCode = code,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            LayoutParams = new LayoutParams
            {
                Title = code.ToString(),
                Language = CultureInfo.CurrentCulture.Name
            }
        });
    }
}