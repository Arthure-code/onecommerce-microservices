using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OneProduit.API.Models
{
    public class Produit
    {
        [DisplayName("Identifiant")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MaxLength(100, ErrorMessage = "La taille maximale est de 100 caractères")]
        [RegularExpression("^[a-zA-Z -]*$", ErrorMessage = "Les caractères spéciaux et les chiffres ne sont pas autorisés")]

        public string Nom { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        [MaxLength(250, ErrorMessage = "La taille maximale est de 250 caractères")]
        public string Description { get; set; }

        [Required(ErrorMessage = "La description est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix ne peut etre négatif")]
        [RegularExpression(@"^\d*$|(?=^.*$)^\d+\,\d{0,2}$", ErrorMessage = "Vous ne pouvez entrer plus de deux decimal")]
        public decimal Prix { get; set; }

        [Range(0, 150, ErrorMessage = "La quantité est comprise entre 0 et 150")]
        [DisplayName("Quantité")]
        public int Quantite { get; set; }

        [Required(ErrorMessage = "L'image est obligatoire")]
        [FileExtensions(Extensions = (".png , .jpg"), ErrorMessage = "L'image doit avoir l'extension .png ou .jpg")]
        public string Image { get; set; }

        [DisplayName("Est produit vedette")]
        public bool Vedette { get; set; } = false;

    }
}
