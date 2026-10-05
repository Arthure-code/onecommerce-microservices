namespace OneFichiers.API.Models
{
    // Lien d'écriture : l'appelant y dépose les octets lui-même.
    public record LienTeleversement(string Url, string Nom, DateTimeOffset Expiration);

    // Base de lecture et signature, partagées par toutes les images d'une page.
    public record LienLecture(string Base, string Signature, DateTimeOffset Expiration);
}
