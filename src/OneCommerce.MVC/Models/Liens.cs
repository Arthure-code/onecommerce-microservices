namespace OneCommerce.MVC.Models
{
    // Lien d'écriture : le navigateur y dépose l'image lui-même.
    public record LienTeleversement(string Url, string Nom, DateTimeOffset Expiration);

    // Base de lecture et signature, partagées par toutes les images d'une page.
    public record LienLecture(string Base, string Signature, DateTimeOffset Expiration)
    {
        public string Pour(string? nomImage) => $"{Base}/{nomImage}{Signature}";
    }
}
