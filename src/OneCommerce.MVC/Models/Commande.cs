using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OneCommerce.MVC.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty; 
        public int IdProduit { get; set; }
        public string NomProduit { get; set; } = string.Empty;
        [DisplayName("Numéro de fidélité client")]
        public string NumeroFideliteClient { get; set; } = string.Empty;
        [DisplayName("Quantité commandée")]
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal PrixTotal { get; set; }
        public DateTime DateCommande { get; set; }
        [DisplayName("Adresse de livraison")]
        public string AdresseLivraison { get; set; } = string.Empty;

        [JsonIgnore]
        public Produit? Produit { get; set; }
    }
}
