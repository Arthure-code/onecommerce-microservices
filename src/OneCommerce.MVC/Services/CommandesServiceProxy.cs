using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Services
{
    public class CommandesServiceProxy : ICommandesService
    {
        private readonly HttpClient _http;

        public CommandesServiceProxy(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<Commande>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<IEnumerable<Commande>>("api/commandes")
                   ?? Enumerable.Empty<Commande>();
        }

        public async Task<Commande?> CreateAsync(Commande commande)
        {
            var response = await _http.PostAsJsonAsync("api/commandes", commande);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Commande>();
            }
            return null;
        }
    }
}
