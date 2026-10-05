namespace OneCommandes.API.DTOs
{
    public class QueueFidelitesDto
    {
        public string NumeroFideliteClient { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixTotal { get; set; }
    }
}
