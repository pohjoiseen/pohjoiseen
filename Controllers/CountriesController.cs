using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi;
using KoTi.RequestModels;
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

    // GET: api/Countries/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Country>> GetCountry(int id)
    {
        var country = await _context.Countries.FindAsync(id);
        if (country == null)
        {
            return NotFound();
        }

        return country;
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

    // PUT: api/Countries/{id}/Regions/Order
    [HttpPut("{id}/Regions/Order")]
    public async Task<IActionResult> ReorderRegions(int id, IdsDTO dto)
    {
        var regionsById = await _context.Regions
            .Where(r => r.CountryId == id)
            .ToDictionaryAsync(r => r.Id, r => r);
        for (int i = 0; i < dto.Ids.Length; i++)
        {
            if (!regionsById.ContainsKey(dto.Ids[i]))
            {
                return BadRequest();
            }

            regionsById[dto.Ids[i]].Order = i + 1;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}