using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class ArticleViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<ArticleViewModel, Article>
{
    public async Task<IList<string>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Articles.FindAsync(id);
        if (entity is null)
        {
            return [];
        }

        return await dbContext.Articles
            .Where(a => a.Name == entity.Name)
            .Select(a => a.Language).Distinct().ToListAsync();
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
                .Select(a => a.Language).Distinct().ToListAsync()
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
}