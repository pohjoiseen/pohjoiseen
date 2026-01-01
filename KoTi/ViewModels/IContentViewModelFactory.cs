using Holvi.Models;

namespace KoTi.ViewModels;

public interface IContentViewModelFactory<TViewModel, TEntity> 
    where TViewModel : AbstractContentViewModel where TEntity : IContentEntity
{
    Task<IList<string>> GetAllLanguagesAsync(int id);
    Task<TViewModel?> LoadAsync(int id, string language);
    Task<TEntity?> SaveAsync(int id, string language, TViewModel model);
}