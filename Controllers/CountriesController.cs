using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi;
using KoTi.Models;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CountriesController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public CountriesController(KoTiDbContext context)
    {
        _context = context;
    }

    // GET: api/Countries
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Country>>> GetCountries()
    {
        return await _context.Countries.OrderBy(c => c.Order).ToListAsync();
    }

    // GET: api/Countries/{id}/Regions
    [HttpGet("{id}/Regions")]
    public async Task<ActionResult<IEnumerable<Region>>> GetRegionsForCountry(int id)
    {
        return await _context.Regions
            .Where(r => r.CountryId == id)
            .OrderBy(r => r.Order)
            .ToListAsync();
    }
}