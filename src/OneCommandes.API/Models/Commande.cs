namespace OneCommandes.API.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty; 
        public int IdProduit { get; set; }
        public string NomProduit { get; set; } = string.Empty;
        public string NumeroFideliteClient { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal PrixTotal { get; set; }
        public DateTime DateCommande { get; set; }
        public string AdresseLivraison { get; set; } = string.Empty;
    }
}
