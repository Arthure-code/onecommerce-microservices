namespace OneCommandes.API.DTOs
{
    public class queueLivraisonsDTO
    {
        public string NumeroCommande { get; set; } = string.Empty;
        public DateTime DateCommande { get; set; }
        public string AdresseLivraison { get; set; } = string.Empty;
    }
}
