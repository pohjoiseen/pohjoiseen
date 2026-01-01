using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class HomeController : Controller
{
    [HttpGet("Blank")]
    public IActionResult Blank() => Ok("");
}