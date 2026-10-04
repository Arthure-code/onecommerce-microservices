using OneCommandes.API.Models;

namespace OneCommandes.API.Interfaces
{
    public interface IServiceBusHelper
    {
        public Task EnvoyerMessage(Commande commande);
    }
}
