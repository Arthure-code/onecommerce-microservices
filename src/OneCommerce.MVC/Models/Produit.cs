using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OneCommerce.MVC.Models
{
    public class Produit
    {
        [DisplayName("Identifiant")]
        public int? Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MaxLength(100, ErrorMessage = "La taille maximale est de 100 caractères")]
        [RegularExpression("^[a-zA-Z -]*$", ErrorMessage = "Les caractères spéciaux et les chiffres ne sont pas autorisés")]

        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est obligatoire")]
        [MaxLength(250, ErrorMessage = "La taille maximale est de 250 caractères")]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100000, ErrorMessage = "Le prix est compris entre 0 et 100 000")]
        public decimal Prix { get; set; }

        [Range(0, 150, ErrorMessage = "La quantité est comprise entre 0 et 150")]
        [DisplayName("Quantité")]
        public int Quantite { get; set; }

        public string? Image { get; set; }

        [DisplayName("Est produit vedette")]
        public bool Vedette { get; set; } = false;

        [JsonIgnore]
        public IFormFile? FichierImage { get; set; }

    }
}
