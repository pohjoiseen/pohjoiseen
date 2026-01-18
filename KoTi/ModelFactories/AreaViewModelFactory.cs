using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class AreaViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<AreaViewModel, Area>
{
    public async Task<IList<string>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Areas.FindAsync(id);
        if (entity is null)
        {
            return [];
        }

        var result = new List<string>();
        // default is English, count it as existing if name is not empty
        if (!string.IsNullOrEmpty(entity.Name))
        {
            result.Add("en");
        }
        var localizations = (await dbContext.AreaLocalizations
            .Where(al => al.AreaId == id)
            .ToListAsync())
            .Select(p => p.Language)
            .Distinct()
            .ToList();
        result.AddRange(localizations);
        return result;
    }

    public async Task<AreaViewModel?> LoadAsync(int id, string language)
    {
        var entity = await dbContext.Areas
            .Include(a => a.Region)
            .ThenInclude(r => r.Country)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null)
        {
            return null;
        }

        var model = new AreaViewModel
        {
            Id = entity.Id,
            Language = language,
            ParentsDescription = $"{entity.Region.Country.Name} · {entity.Region.Name}",  
            Slug = entity.Slug,
            ExploreStatus = (int) entity.ExploreStatus,
            Lat = entity.Lat,
            Lng = entity.Lng,
            Zoom = entity.Zoom,
        };

        if (language == "en")
        {
            model.Title = entity.Name;
            model.Subtitle = entity.Alias;
            model.ContentMD = entity.Notes;
            model.Links = entity.Links.Split("\n");
            model.Draft = entity.IsPrivate;
        }
        else
        {
            var localization = await dbContext.AreaLocalizations
                .FirstOrDefaultAsync(al => al.AreaId == id && al.Language == language);
            if (localization is not null)
            {
                model.Title = localization.Name;
                model.Subtitle = localization.Alias;
                model.ContentMD = localization.Notes;
                model.Links = localization.Links.Split("\n");
                model.Draft = localization.IsPrivate;
            }
        }
        
        // load places in this area too
        var places = await dbContext.Places
            .Include(p => p.Localizations)
            .Where(p => p.AreaId == id)
            .OrderBy(p => p.Order)
            .ToListAsync();
        model.PlaceLinks = places.Select(p => new AreaViewModel.PlaceLink
        {
            Id = p.Id,
            Icon = p.Category,
            Title = language == "en"
                    ? (string.IsNullOrWhiteSpace(p.Name) ? p.Localizations.FirstOrDefault()?.Name ?? "unknown" : p.Name)
                    : p.Localizations.FirstOrDefault(l => l.Language == language)?.Name ?? p.Name,
            LanguageExists = language == "en" ? !string.IsNullOrWhiteSpace(p.Name) : p.Localizations.Any(l => l.Language == language)
        }).ToList();
        
        return model;
    }

    public async Task<Area?> SaveAsync(int id, string language, AreaViewModel model)
    {
        var entity = await dbContext.Areas
            .FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null)
        {
            return null;
        }
        
        entity.Slug = model.Slug;
        entity.ExploreStatus = (ExploreStatus) model.ExploreStatus;
        entity.Lat = model.Lat;
        entity.Lng = model.Lng;
        entity.Zoom = model.Zoom;
        if (language == "en")
        {
            entity.Name = model.Title;
            entity.Alias = model.Subtitle ?? "";
            entity.Notes = model.ContentMD;
            entity.Links = string.Join("\n", model.Links.Where(l => !string.IsNullOrWhiteSpace(l)));
            entity.IsPrivate = model.Draft;
        }
        else
        {
            var localization = await dbContext.AreaLocalizations
                .FirstOrDefaultAsync(al => al.AreaId == id && al.Language == language);
            if (localization is null)
            {
                localization = new AreaLocalization { AreaId = id, Language = language };
                dbContext.AreaLocalizations.Add(localization);
            }
            localization.Name = model.Title;
            localization.Alias = model.Subtitle ?? "";
            localization.Notes = model.ContentMD;
            localization.Links = string.Join("\n", model.Links.Where(l => !string.IsNullOrWhiteSpace(l)));
            localization.IsPrivate = model.Draft;
        }
        
        // reorder places, do not do anything else with them
        var placesById = await dbContext.Places
            .Where(p => p.AreaId == id)
            .ToDictionaryAsync(p => p.Id, p => p);
        foreach (var place in placesById.Values)
        {
            place.Order = 0;
        }
        for (int i = 0; i < model.PlaceLinks.Count; i++)
        {
            if (placesById.ContainsKey(model.PlaceLinks[i].Id))
            {
                placesById[model.PlaceLinks[i].Id].Order = i + 1;
            }
        }
        
        await dbContext.SaveChangesAsync();
        return entity;
    }
}