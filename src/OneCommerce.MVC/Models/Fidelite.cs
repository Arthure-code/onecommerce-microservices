using System.ComponentModel.DataAnnotations;

namespace OneCommerce.MVC.Models
{
    public class Fidelite
    {
        public string? NumeroFidelite { get; set; } = string.Empty;

        [Required]
        public string NomClient { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CourrielClient { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Telephone { get; set; } = string.Empty;

        public int Points { get; set; } = 0; // par défaut 0 points
    }
}
