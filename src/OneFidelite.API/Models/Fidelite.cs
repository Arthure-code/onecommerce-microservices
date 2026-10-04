using System.ComponentModel.DataAnnotations;

namespace OneFidelite.API.Models
{
    public class Fidelite
    {
        public int Id { get; set; } // interne
        public string NumeroFidelite { get; set; } = string.Empty;

        [Required]
        public string NomClient { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CourrielClient { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Telephone { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

       public int Points { get; set; } = 0; // par défaut 0 points
    }
}
