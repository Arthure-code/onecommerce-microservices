using OneFichiers.API.Interfaces;
using OneFichiers.API.Models;

namespace OneFichiers.API.Services
{
    // Utilisé quand aucun compte de stockage n'est configuré, pour qu'un
    // dépôt d'image aboutisse sur un poste qui n'a pas d'abonnement Azure.
    public class MagasinDisque : IMagasinImages
    {
        private readonly IHttpContextAccessor _contexte;

        public MagasinDisque(IHttpContextAccessor contexte)
        {
            _contexte = contexte;
        }

        public bool RecoitLesOctets => true;

        public Task<LienTeleversement> LienTeleversementAsync(string nomFichier)
        {
            return Task.FromResult(
                new LienTeleversement($"{Racine()}/api/fichiers/{nomFichier}", nomFichier, DateTimeOffset.UtcNow.AddMinutes(15)));
        }

        public Task<LienLecture> LienLectureAsync()
        {
            return Task.FromResult(
                new LienLecture($"{Racine()}/images", string.Empty, DateTimeOffset.UtcNow.AddMinutes(30)));
        }

        public async Task EnregistrerAsync(string nomFichier, Stream contenu)
        {
            string dossier = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(dossier);

            await using FileStream fichier = File.Create(Path.Combine(dossier, nomFichier));
            await contenu.CopyToAsync(fichier);
        }

        private string Racine()
        {
            HttpRequest requete = _contexte.HttpContext!.Request;

            return $"{requete.Scheme}://{requete.Host}";
        }
    }
}
