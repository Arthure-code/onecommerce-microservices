using OneCommerce.MVC.Interfaces;

namespace OneCommerce.MVC.Services
{
    public class FichiersServiceProxy : IFichiersService
    {
        private readonly HttpClient _httpClient;

        public FichiersServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> Upload(IFormFile file, string nomFichier)
        {
            if (file == null || file.Length == 0)
                return null;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            string base64String = Convert.ToBase64String(fileBytes);

            var fichier = new
            {
                NomFichier = nomFichier,
                FichierBase64 = base64String
            };

            var response = await _httpClient.PostAsJsonAsync("api/fichiers", fichier);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return result?["url"];
        }
    }
}
