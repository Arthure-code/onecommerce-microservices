using OneFichiers.API.Models;

namespace OneFichiers.API.Interfaces
{
    public interface IMagasinImages
    {
        // Vrai quand le magasin reçoit lui-même les octets, faute de pouvoir
        // signer une URL vers un stockage externe.
        bool RecoitLesOctets { get; }

        Task<LienTeleversement> LienTeleversementAsync(string nomFichier);

        Task<LienLecture> LienLectureAsync();

        Task EnregistrerAsync(string nomFichier, Stream contenu);
    }
}
