using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Services
{
    public class ProduitServiceProxy : IProduitService
    {
        private readonly HttpClient _httpClient;

        public ProduitServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Produit>> GetProduits()
        {
            return await _httpClient.GetFromJsonAsync<List<Produit>>("api/produits")
                   ?? new List<Produit>();
        }

        public async Task<Produit?> GetProduitById(int id)
        {
            return await _httpClient.GetFromJsonAsync<Produit>($"api/produits/{id}");
        }

        public async Task<Produit?> AddProduit(Produit produit)
        {
            var response = await _httpClient.PostAsJsonAsync("api/produits", produit);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Produit>();
            }
            return null;
        }
    }

}
