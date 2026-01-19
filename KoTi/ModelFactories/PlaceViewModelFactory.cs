using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class PlaceViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<PlaceViewModel, Place>
{
    public async Task<IList<string>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Places.FindAsync(id);
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
        var localizations = (await dbContext.PlaceLocalizations
            .Where(al => al.PlaceId == id)
            .ToListAsync())
            .Select(p => p.Language)
            .Distinct()
            .ToList();
        result.AddRange(localizations);
        return result;
    }

    public async Task<PlaceViewModel?> LoadAsync(int id, string language)
    {
        var entity = await dbContext.Places.FindAsync(id);
        if (entity is null)
        {
            return null;
        }

        var breadcrumbs = await GetBreadcrumbs(entity);
        
        var model = new PlaceViewModel
        {
            Id = entity.Id,
            Language = language,
            IsLeaf = entity.IsLeaf,
            ParentsTitles = breadcrumbs.Select(p => GetTitle(p, language)).ToList(),
            ParentsIds = breadcrumbs.Select(p => p.Id).ToList(),
            Name = entity.Name,
            Icon = entity.Icon ?? "star",
            ExploreStatus = (int) entity.ExploreStatus,
            TitlePictureId = entity.TitlePictureId,
            TitleImageOffsetY = entity.TitleImageOffsetY,
            Lat = entity.Lat,
            Lng = entity.Lng,
            Zoom = entity.Zoom,
            Rating = entity.Rating
        };

        if (language == "en")
        {
            model.Title = entity.Title ?? "";
            model.Subtitle = entity.Subtitle;
            model.Description = entity.Description;
            model.ContentMD = entity.ContentMD ?? "";
            model.Links = entity.Links?.Split("\n") ?? [];
            model.Meta = entity.Meta ?? new PlaceMeta();
            model.Draft = entity.Draft;
        }
        else
        {
            var localization = await dbContext.PlaceLocalizations
                .FirstOrDefaultAsync(pl => pl.PlaceId == id && pl.Language == language);
            if (localization is not null)
            {
                model.Title = localization.Title;
                model.Subtitle = localization.Subtitle;
                model.Description = localization.Description;
                model.ContentMD = localization.ContentMD;
                model.Links = localization.Links.Split("\n");
                model.Meta = localization.Meta ?? new PlaceMeta();
                model.Draft = localization.Draft;
            }
        }
        
        // load children
        var places = await dbContext.Places
            .Include(p => p.Localizations)
            .Where(p => p.ParentId == id)
            .OrderBy(p => p.Order)
            .ToListAsync();
        model.Children = places.Select(p => new PlaceViewModel.PlaceChild
        {
            Id = p.Id,
            IsLeaf = p.IsLeaf,
            Icon = p.Icon ?? "star",
            Title = GetTitle(p, language),
            LanguageExists = language == "en" ? !string.IsNullOrWhiteSpace(p.Name) : p.Localizations.Any(l => l.Language == language),
            Draft = language == "en" ? p.Draft : p.Localizations.Any(l => l.Language == language && l.Draft) || true
        }).ToList();
        
        return model;
    }

    public async Task<Place?> SaveAsync(int id, string language, PlaceViewModel model)
    {
        var entity = await dbContext.Places
            .FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null)
        {
            return null;
        }
        
        entity.IsLeaf = model.IsLeaf;
        entity.Name = model.Name;
        entity.Icon = model.Icon;
        entity.ExploreStatus = (ExploreStatus) model.ExploreStatus;
        entity.TitlePictureId = model.TitlePictureId;
        entity.TitleImageOffsetY = model.TitleImageOffsetY;
        entity.Lat = model.Lat;
        entity.Lng = model.Lng;
        entity.Zoom = model.Zoom;
        entity.Rating = model.Rating;
        if (language == "en")
        {
            entity.Title = model.Title;
            entity.Subtitle = model.Subtitle ?? "";
            entity.Description = model.Description ?? "";
            entity.ContentMD = model.ContentMD;
            entity.Meta = model.Meta;
            entity.Links = string.Join("\n", model.Links.Where(l => !string.IsNullOrWhiteSpace(l)));
            entity.Draft = model.Draft;
        }
        else
        {
            var localization = await dbContext.PlaceLocalizations
                .FirstOrDefaultAsync(pl => pl.PlaceId == id && pl.Language == language);
            if (localization is null)
            {
                localization = new PlaceLocalization { PlaceId = id, Language = language };
                dbContext.PlaceLocalizations.Add(localization);
            }
            localization.Title = model.Title;
            localization.Subtitle = model.Subtitle ?? "";
            localization.Description = model.Description ?? "";
            localization.ContentMD = model.ContentMD ?? "";
            localization.Meta = model.Meta;
            localization.Links = string.Join("\n", model.Links.Where(l => !string.IsNullOrWhiteSpace(l)));
            localization.Draft = model.Draft;
        }
        
        // reorder places, do not do anything else with them
        var placesById = await dbContext.Places
            .Where(p => p.ParentId == id)
            .ToDictionaryAsync(p => p.Id, p => p);
        foreach (var place in placesById.Values)
        {
            place.Order = 0;
        }
        for (int i = 0; i < model.Children.Count; i++)
        {
            if (placesById.ContainsKey(model.Children[i].Id))
            {
                placesById[model.Children[i].Id].Order = i + 1;
            }
        }
        
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private async Task<IList<Place>> GetBreadcrumbs(Place place)
    {
        var result = new List<Place>();
        while (place.ParentId != null)
        {
            place = (await dbContext.Places
                .Include(p => p.Localizations)
                .FirstOrDefaultAsync(p => p.Id == place.ParentId))!;
            result.Add(place);
        }
        result.Reverse();
        return result;
    }
    
    private string GetTitle(Place place, string language)
    {
        if (language == "en")
        {
            return string.IsNullOrWhiteSpace(place.Title)
                ? place.Localizations.FirstOrDefault()?.Title ?? "unknown"
                : place.Title;
        }
        return place.Localizations.FirstOrDefault(l => l.Language == language)?.Title ?? place.Title ?? "unknown";
    }
}