using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Interfaces
{
    public interface ICommandesService
    {
        Task<IEnumerable<Commande>> GetAllAsync();
        Task<Commande?> CreateAsync(Commande commande);
    }
}
