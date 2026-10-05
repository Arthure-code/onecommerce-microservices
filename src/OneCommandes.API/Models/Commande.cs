using System.ComponentModel.DataAnnotations;

namespace OneCommandes.API.Models
{
    public class Commande
    {
        public int? Id { get; set; }
        public string NumeroCommande { get; set; } = string.Empty; 
        [Required(ErrorMessage = "Le produit commandé est obligatoire")]
        public int? IdProduit { get; set; }
        public string NomProduit { get; set; } = string.Empty;
        public string NumeroFideliteClient { get; set; } = string.Empty;
        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(1, 150, ErrorMessage = "La quantité est comprise entre 1 et 150")]
        public int? Quantite { get; set; }
        [Required(ErrorMessage = "Le prix unitaire est obligatoire")]
        [Range(0, 100000, ErrorMessage = "Le prix est compris entre 0 et 100 000")]
        public decimal? PrixUnitaire { get; set; }
        public decimal? PrixTotal { get; set; }
        public DateTime? DateCommande { get; set; }
        public string AdresseLivraison { get; set; } = string.Empty;
    }
}
