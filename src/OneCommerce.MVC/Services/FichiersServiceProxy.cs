using Microsoft.Extensions.Caching.Memory;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Services
{
    public class FichiersServiceProxy : IFichiersService
    {
        private const string CleLienLecture = "lien-lecture-images";

        // La signature de lecture est demandée une fois et sert à toutes les
        // images de toutes les pages jusqu'à son échéance.
        private static readonly TimeSpan MargeAvantEcheance = TimeSpan.FromMinutes(2);

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public FichiersServiceProxy(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<LienTeleversement?> LienTeleversement(string nomPropose)
        {
            var response = await _httpClient.PostAsJsonAsync("api/fichiers/televersement", new { NomFichier = nomPropose });

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LienTeleversement>();
        }

        public async Task<LienLecture?> LienLecture()
        {
            if (_cache.TryGetValue(CleLienLecture, out LienLecture? enCache))
                return enCache;

            var response = await _httpClient.GetAsync("api/fichiers/lecture");

            if (!response.IsSuccessStatusCode)
                return null;

            LienLecture? lien = await response.Content.ReadFromJsonAsync<LienLecture>();

            if (lien != null)
            {
                DateTimeOffset fin = lien.Expiration - MargeAvantEcheance;

                if (fin > DateTimeOffset.UtcNow)
                    _cache.Set(CleLienLecture, lien, fin);
            }

            return lien;
        }
    }
}
