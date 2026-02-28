using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class PostViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<PostViewModel, Post>
{
    public async Task<IDictionary<string, int>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Posts.FindAsync(id);
        if (entity is null)
        {
            return new Dictionary<string, int>();
        }

        return await dbContext.Posts
            .Where(p => p.Name == entity.Name && p.Date == entity.Date)
            .ToDictionaryAsync(p => p.Language, p => p.Id);
    }

    public async Task<PostViewModel?> LoadAsync(int id, string language)
    {
        var entity = await dbContext.Posts.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }

        var model = new PostViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Date = entity.Date,
            Title = entity.Title,
            Language = language,
            Description = entity.Description,
            DateDescription = entity.DateDescription,
            LocationDescription = entity.LocationDescription,
            Address = entity.Address,
            PublicTransport = entity.PublicTransport,
            TitlePictureId = entity.TitlePictureId,
            TitleImageCaption = entity.TitleImageCaption,
            TitleImageInText = entity.TitleImageInText ?? false,
            TitleImageOffsetY = entity.TitleImageOffsetY,
            CoatsOfArms = entity.CoatsOfArms ?? new List<Post.CoatOfArms>(),
            Geo = entity.Geo ?? new List<Post.GeoPoint>(),
            ContentMD = entity.ContentMD,
            Draft = entity.Draft,
            Mini = entity.Mini,
            BookId = entity.BookId,
            AllLanguages = await dbContext.Posts
                .Where(p => p.Name == entity.Name && p.Date == entity.Date)
                .ToDictionaryAsync(p => p.Language, p => p.Id)
        };
        return model;
    }

    public async Task<Post?> SaveAsync(int id, string language, PostViewModel model)
    {
        var entity = await dbContext.Posts.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }
        
        entity.Name = model.Name;
        entity.Date = model.Date;
        entity.Title = model.Title;
        entity.Language = model.Language;
        entity.Description = model.Description ?? "";
        entity.DateDescription = model.DateDescription;
        entity.LocationDescription = model.LocationDescription;
        entity.Address = model.Address;
        entity.PublicTransport = model.PublicTransport;
        entity.TitlePictureId = model.TitlePictureId;
        entity.TitleImageCaption = model.TitleImageCaption;
        entity.TitleImageInText = model.TitleImageInText;
        entity.TitleImageOffsetY = model.TitleImageOffsetY;
        entity.CoatsOfArms = model.CoatsOfArms;
        entity.Geo = model.Geo;
        entity.ContentMD = model.ContentMD ?? "";
        entity.BookId = model.BookId;
        entity.Draft = model.Draft;
        entity.Mini = model.Mini;
        await dbContext.SaveChangesAsync();
        return entity;
    }
    
    public async Task<int> CopyToLanguageAsync(int id, string language, string targetLanguage)
    {
        var entity = await dbContext.Posts.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            throw new Exception("Original entity not found");
        }

        var newEntity = new Post
        {
            ContentMD = entity.ContentMD,
            Draft = true,
            Language = targetLanguage,
            Name = entity.Name,
            Date = entity.Date,
            Title = entity.Title,
            Mini = entity.Mini,
            Description = entity.Description,
            Address = entity.Address,
            PublicTransport = entity.PublicTransport,
            DateDescription = entity.DateDescription,
            LocationDescription = entity.LocationDescription,
            TitlePictureId = entity.TitlePictureId,
            TitleImageCaption = entity.TitleImageCaption,
            TitleImageInText = entity.TitleImageInText,
            TitleImageOffsetY = entity.TitleImageOffsetY,
            CoatsOfArms = entity.CoatsOfArms?.Select(coa => new Post.CoatOfArms
            {
                Size = coa.Size,
                Url = coa.Url,
            }).ToList(),
            Geo = entity.Geo?.Select(g => new Post.GeoPoint
            {
                Title = g.Title,
                Subtitle = g.Subtitle,
                Lat = g.Lat,
                Lng = g.Lng,
                Zoom = g.Zoom,
                Icon = g.Icon,
                Description = g.Description,
                Anchor = g.Anchor,
                Maps = g.Maps,
                TitleImage = g.TitleImage,
                Links = g.Links,
            }).ToList()
        };
        
        dbContext.Posts.Add(newEntity);
        await dbContext.SaveChangesAsync();
        return newEntity.Id;
    }
}