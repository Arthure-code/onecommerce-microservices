using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using OneFichiers.API.Interfaces;
using OneFichiers.API.Models;

namespace OneFichiers.API.Services
{
    public class MagasinBlob : IMagasinImages
    {
        private static readonly TimeSpan DureeEcriture = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DureeLecture = TimeSpan.FromMinutes(30);

        private readonly BlobContainerClient _conteneur;

        public MagasinBlob(BlobContainerClient conteneur)
        {
            _conteneur = conteneur;
        }

        public bool RecoitLesOctets => false;

        public LienTeleversement LienTeleversement(string nomFichier)
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(DureeEcriture);

            BlobClient blob = _conteneur.GetBlobClient(nomFichier);
            Uri url = blob.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, expiration);

            return new LienTeleversement(url.ToString(), nomFichier, expiration);
        }

        public LienLecture LienLecture()
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(DureeLecture);

            Uri url = _conteneur.GenerateSasUri(BlobContainerSasPermissions.Read, expiration);

            return new LienLecture(_conteneur.Uri.ToString(), "?" + url.Query.TrimStart('?'), expiration);
        }

        public Task EnregistrerAsync(string nomFichier, Stream contenu)
        {
            throw new NotSupportedException("Les octets vont directement au conteneur, par le lien signé.");
        }
    }
}
