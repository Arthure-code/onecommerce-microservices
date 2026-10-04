using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Interfaces
{
    public interface IProduitService
    {
        Task<List<Produit>> GetProduits();
        Task<Produit?> GetProduitById(int id);
        Task<Produit?> AddProduit(Produit produit);
    }
}
