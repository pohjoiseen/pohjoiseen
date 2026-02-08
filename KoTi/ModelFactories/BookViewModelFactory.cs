using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public class BookViewModelFactory(HolviDbContext dbContext) : IContentViewModelFactory<BookViewModel, Book>
{
    public async Task<IList<string>> GetAllLanguagesAsync(int id)
    {
        var entity = await dbContext.Books.FindAsync(id);
        if (entity is null)
        {
            return [];
        }

        return await dbContext.Books
            .Where(a => a.Name == entity.Name)
            .Select(a => a.Language).Distinct().ToListAsync();
    }

    public async Task<BookViewModel?> LoadAsync(int id, string language)
    {
        var entity = await dbContext.Books.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }

        var model = new BookViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Title = entity.Title,
            Language = language,
            ContentMD = entity.ContentMD,
            Draft = entity.Draft,
            TitlePictureId = entity.TitlePictureId,
            AllLanguages = await dbContext.Articles
                .Where(a => a.Name == entity.Name)
                .Select(a => a.Language).Distinct().ToListAsync(),
        };
        
        // load posts
        var posts = await dbContext.Posts
            .Where(p => p.BookId == id)
            .OrderBy(p => p.Order)
            .ToListAsync();
        model.Posts = posts.Select(p => new BookViewModel.PostChild
        {
            Id = p.Id,
            Title = p.Title,
            Draft = p.Draft,
        }).ToList();
        
        return model;
    }

    public async Task<Book?> SaveAsync(int id, string language, BookViewModel model)
    {
        var entity = await dbContext.Books.FindAsync(id);
        if (entity is null || entity.Language != language)
        {
            return null;
        }
        
        entity.Name = model.Name;
        entity.Title = model.Title;
        entity.Language = model.Language;
        entity.ContentMD = model.ContentMD ?? "";
        entity.Draft = model.Draft;
        entity.TitlePictureId = model.TitlePictureId;
        
        // reorder posts, do not do anything else with them
        var postsById = await dbContext.Posts
            .Where(p => p.BookId == id)
            .ToDictionaryAsync(p => p.Id, p => p);
        foreach (var place in postsById.Values)
        {
            place.Order = 0;
        }
        for (int i = 0; i < model.Posts.Count; i++)
        {
            if (postsById.ContainsKey(model.Posts[i].Id))
            {
                postsById[model.Posts[i].Id].Order = i + 1;
            }
        }
        
        await dbContext.SaveChangesAsync();
        return entity;
    }
}