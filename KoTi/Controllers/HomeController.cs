using Holvi;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HomeController : ControllerBase
{
    private readonly HolviDbContext _context;

    public HomeController(HolviDbContext context)
    {
        _context = context;
    }

    // GET: api/Home/Stats
    [HttpGet("Stats")]
    public async Task<ActionResult<Stats>> GetStats()
    {
        return new Stats
        {
            TotalPictures = await _context.Pictures.CountAsync(),
            TotalPicturesWithNoLocation = await _context.Pictures.Where(p => p.PlaceId == null).CountAsync(),
            TotalPlaces = await _context.Places.CountAsync(),
            TotalPosts = await _context.Posts.CountAsync(),
        };
    }
}