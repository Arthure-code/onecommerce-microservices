using OneCommandes.API.Interfaces;
using OneCommandes.API.Models;

namespace OneCommandes.API.Services
{
    // Utilisé quand aucune chaîne de connexion n'est configurée, pour qu'une
    // commande aboutisse sur un poste qui n'a pas de Service Bus sous la main.
    public class ServiceBusHorsLigne : IServiceBusHelper
    {
        private readonly ILogger<ServiceBusHorsLigne> _logger;

        public ServiceBusHorsLigne(ILogger<ServiceBusHorsLigne> logger)
        {
            _logger = logger;
        }

        public Task EnvoyerMessage(Commande commande)
        {
            _logger.LogInformation(
                "Commande {NumeroCommande} non transmise : aucune chaîne de connexion Service Bus configurée.",
                commande.NumeroCommande);

            return Task.CompletedTask;
        }
    }
}
