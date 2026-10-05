using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OneFichiers.API.Interfaces;
using OneFichiers.API.Models;

namespace OneFichiers.API.Services
{
    public class MagasinBlob : IMagasinImages
    {
        private static readonly TimeSpan DureeEcriture = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DureeLecture = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DureeDelegation = TimeSpan.FromHours(1);
        private static readonly TimeSpan MargeAvantEcheance = TimeSpan.FromMinutes(5);

        private readonly BlobServiceClient _service;
        private readonly BlobContainerClient _conteneur;
        private readonly SemaphoreSlim _verrou = new(1, 1);

        private UserDelegationKey? _cle;
        private DateTimeOffset _cleUtilisableJusqua;

        public MagasinBlob(BlobServiceClient service, BlobContainerClient conteneur)
        {
            _service = service;
            _conteneur = conteneur;
        }

        public bool RecoitLesOctets => false;

        public async Task<LienTeleversement> LienTeleversementAsync(string nomFichier)
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(DureeEcriture);

            var demande = new BlobSasBuilder
            {
                BlobContainerName = _conteneur.Name,
                BlobName = nomFichier,
                Resource = "b",
                ExpiresOn = expiration
            };
            demande.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            string signature = await Signer(demande);
            BlobClient blob = _conteneur.GetBlobClient(nomFichier);

            return new LienTeleversement($"{blob.Uri}?{signature}", nomFichier, expiration);
        }

        public async Task<LienLecture> LienLectureAsync()
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow.Add(DureeLecture);

            var demande = new BlobSasBuilder
            {
                BlobContainerName = _conteneur.Name,
                Resource = "c",
                ExpiresOn = expiration
            };
            demande.SetPermissions(BlobContainerSasPermissions.Read);

            string signature = await Signer(demande);

            return new LienLecture(_conteneur.Uri.ToString(), "?" + signature, expiration);
        }

        public Task EnregistrerAsync(string nomFichier, Stream contenu)
        {
            throw new NotSupportedException("Les octets vont directement au conteneur, par le lien signé.");
        }

        private async Task<string> Signer(BlobSasBuilder demande)
        {
            UserDelegationKey cle = await CleDeDelegation();

            return demande.ToSasQueryParameters(cle, _service.AccountName).ToString();
        }

        // La clé de délégation vient de l'identité de l'application, pas d'une
        // clé de compte. Elle est demandée une fois l'heure, pas à chaque lien.
        private async Task<UserDelegationKey> CleDeDelegation()
        {
            if (_cle != null && _cleUtilisableJusqua > DateTimeOffset.UtcNow)
            {
                return _cle;
            }

            await _verrou.WaitAsync();

            try
            {
                if (_cle == null || _cleUtilisableJusqua <= DateTimeOffset.UtcNow)
                {
                    DateTimeOffset fin = DateTimeOffset.UtcNow.Add(DureeDelegation);

                    _cle = await _service.GetUserDelegationKeyAsync(
                        new BlobGetUserDelegationKeyOptions(fin) { StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5) });
                    _cleUtilisableJusqua = fin - MargeAvantEcheance;
                }
            }
            finally
            {
                _verrou.Release();
            }

            return _cle;
        }
    }
}
