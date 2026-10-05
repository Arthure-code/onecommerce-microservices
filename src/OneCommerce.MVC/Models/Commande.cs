using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OneCommerce.MVC.Models
{
    public class Commande
    {
        public int? Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty; 
        [Required(ErrorMessage = "Le produit commandé est obligatoire")]
        public int? IdProduit { get; set; }
        public string NomProduit { get; set; } = string.Empty;
        [DisplayName("Numéro de fidélité client")]
        public string NumeroFideliteClient { get; set; } = string.Empty;
        [DisplayName("Quantité commandée")]
        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(1, 150, ErrorMessage = "La quantité est comprise entre 1 et 150")]
        public int? Quantite { get; set; }
        public decimal? PrixUnitaire { get; set; }
        public decimal? PrixTotal { get; set; }
        public DateTime? DateCommande { get; set; }
        [DisplayName("Adresse de livraison")]
        public string AdresseLivraison { get; set; } = string.Empty;

        [JsonIgnore]
        public Produit? Produit { get; set; }
    }
}
