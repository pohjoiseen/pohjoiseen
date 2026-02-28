using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class ArticleViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<ArticleViewModel, Article>
{
    public async Task<IDictionary<string, int>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Articles.FindAsync(id);
        if (entity is null)
        {
            return new Dictionary<string, int>();
        }

        return await dbContext.Articles
            .Where(a => a.Name == entity.Name)
            .ToDictionaryAsync(a => a.Language, a => a.Id);
    }

    public async Task<ArticleViewModel?> LoadAsync(int id, string language)
    {
        var entity = await dbContext.Articles.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }

        var model = new ArticleViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Title = entity.Title,
            Language = language,
            ContentMD = entity.ContentMD,
            Draft = entity.Draft,
            AllLanguages = await dbContext.Articles
                .Where(a => a.Name == entity.Name)
                .ToDictionaryAsync(a => a.Language, a => a.Id)
        };
        return model;
    }

    public async Task<Article?> SaveAsync(int id, string language, ArticleViewModel model)
    {
        var entity = await dbContext.Articles.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }
        
        entity.Name = model.Name;
        entity.Title = model.Title;
        entity.Language = model.Language;
        entity.ContentMD = model.ContentMD ?? "";
        entity.Draft = model.Draft;
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<int> CopyToLanguageAsync(int id, string language, string targetLanguage)
    {
        var entity = await dbContext.Articles.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            throw new Exception("Original entity not found");
        }

        var newEntity = new Article
        {
            ContentMD = entity.ContentMD,
            Draft = true,
            Language = targetLanguage,
            Name = entity.Name,
            Title = entity.Title,
        };
        dbContext.Articles.Add(newEntity);
        await dbContext.SaveChangesAsync();
        return newEntity.Id;
    }
}