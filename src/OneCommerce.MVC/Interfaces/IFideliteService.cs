using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Interfaces
{
    public interface IFideliteService
    {
        Task<Fidelite?> CreateFideliteAsync(Fidelite fidelite);

        Task<List<Fidelite>> GetFidelitesAsync();

        Task<Fidelite?> GetFideliteByNumeroAsync(string numero);
    }
}
