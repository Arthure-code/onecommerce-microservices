using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using OneCommandes.API.DTOs;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Models;

namespace OneCommandes.API.Services
{
    public class ServiceBusHelper : IServiceBusHelper
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IConfiguration _config;

        public ServiceBusHelper(ServiceBusClient serviceBusClient, IConfiguration config)
        {
            _serviceBusClient = serviceBusClient;
            _config = config;
        }

        public async Task EnvoyerMessage(Commande commande)
        {

            string queueProduits = _config["NomQueue:Produits"] ?? throw new ArgumentException("Nom de queue Produits manquant");
            string queueFidelites = _config["NomQueue:Fidelites"] ?? throw new ArgumentException("Nom de queue Fidelites manquant");
            string queueLivraisons = _config["NomQueue:Livraisons"] ?? throw new ArgumentException("Nom de queue Livraisons manquant");

            // Créer les messages

            var messageProduits = new QueueProduitsDto
            {
                IdProduit = commande.IdProduit!.Value,
                Quantite = commande.Quantite!.Value
            };

            var messageFidelites = new QueueFidelitesDto
            {
                NumeroFideliteClient = commande.NumeroFideliteClient,
                Quantite = commande.Quantite.Value,
                PrixTotal = commande.PrixTotal!.Value
            };

            var messageLivraisons = new QueueLivraisonsDto
            {
                NumeroCommande = commande.NumeroCommande,
                DateCommande = commande.DateCommande!.Value,
                AdresseLivraison = commande.AdresseLivraison
            };

            // Envoyer les messages
            await EnvoyerAsync(queueProduits, messageProduits);
            await EnvoyerAsync(queueFidelites, messageFidelites);
            await EnvoyerAsync(queueLivraisons, messageLivraisons);
        }

        private async Task EnvoyerAsync(string queue, object payload)
        {
            if (string.IsNullOrWhiteSpace(queue))
                throw new ArgumentException("Nom de queue Service Bus manquant");

            var json = JsonConvert.SerializeObject(payload);

            var svcMessage = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            var sender = _serviceBusClient.CreateSender(queue);
            await sender.SendMessageAsync(svcMessage);
        }
    }

}
