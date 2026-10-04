using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Services
{
    public class FideliteServiceProxy : IFideliteService
    {
        private readonly HttpClient _httpClient;

        public FideliteServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Fidelite?> CreateFideliteAsync(Fidelite fidelite)
        {
            var response = await _httpClient.PostAsJsonAsync("api/fidelite", fidelite);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<Fidelite>();
        }

        public async Task<Fidelite?> GetFideliteByNumeroAsync(string numero)
        {
            var response = await _httpClient.GetAsync("api/fidelite/"+numero);
            if (!response.IsSuccessStatusCode) return new Fidelite();
            return await response.Content.ReadFromJsonAsync<Fidelite>() ?? new Fidelite();
        }

        public async Task<List<Fidelite>> GetFidelitesAsync()
        {
            var response = await _httpClient.GetAsync("api/fidelite");
            if (!response.IsSuccessStatusCode) return new List<Fidelite>();
            return await response.Content.ReadFromJsonAsync<List<Fidelite>>() ?? new List<Fidelite>();
        }
    }
}
