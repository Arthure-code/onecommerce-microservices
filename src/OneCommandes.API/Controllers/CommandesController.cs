using Microsoft.AspNetCore.Mvc;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Models;



namespace OneCommandes.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandesController : ControllerBase
    {

        private readonly IServiceBusHelper _serviceBusHelper;
        private readonly ILogger<CommandesController> _logger;

        public CommandesController(IServiceBusHelper serviceBusHelper, ILogger<CommandesController> logger)
        {
            _serviceBusHelper = serviceBusHelper;
            _logger = logger;
        }


        private static readonly List<Commande> _commandes = new()
        {
            new Commande
            {
                Id = 1,
                NumeroCommande = "ONE-CMD-100001",
                IdProduit = 3,
                NomProduit = "T-shirt imprimé noir",
                NumeroFideliteClient = "ONE-1001",
                Quantite = 2,
                PrixUnitaire = 30.50m,
                PrixTotal = 61.00m,
                DateCommande = DateTime.UtcNow.AddDays(-3),
                AdresseLivraison = "123 Rue Sainte-Catherine, Montréal"
            },
            new Commande
            {
                Id = 2,
                NumeroCommande = "ONE-CMD-100002",
                IdProduit = 4,
                NomProduit = "T-shirt gris femme",
                NumeroFideliteClient = "ONE-1002",
                Quantite = 1,
                PrixUnitaire = 10.20m,
                PrixTotal = 10.20m,
                DateCommande = DateTime.UtcNow.AddDays(-1),
                AdresseLivraison = "456 Boulevard Laurier, Québec"
            }
        };

        private static int _idCounter = _commandes.Count;
        private static readonly Random _random = new();

        // Le compteur et la liste sont partagés par toutes les requêtes du
        // service, donc l'identifiant est pris sans qu'un autre appel le double.
        private static int ProchainIdentifiant() => Interlocked.Increment(ref _idCounter);

        [HttpGet]
        public ActionResult<IEnumerable<Commande>> GetAll()
        {
            return Ok(_commandes);
        }

        [HttpPost]
        public async Task<ActionResult<Commande>> Create([FromBody] Commande commande)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Génération d'un numéro commande unique
            string numero;
            do
            {
                numero = $"ONE-CMD-{_random.Next(100000, 999999)}";
            } while (_commandes.Any(c => c.NumeroCommande == numero));

            commande.Id = ProchainIdentifiant();
            commande.NumeroCommande = numero;
            commande.DateCommande = DateTime.UtcNow;
            commande.PrixTotal = commande.Quantite * commande.PrixUnitaire;

            _commandes.Add(commande);

            // Envoyer la commande au service bus
            await _serviceBusHelper.EnvoyerMessage(commande).ConfigureAwait(false);

            _logger.LogInformation(
                "Commande {NumeroCommande} créée pour le produit {IdProduit} et transmise au Service Bus.",
                commande.NumeroCommande,
                commande.IdProduit);

            return CreatedAtAction(nameof(GetAll), new { id = commande.Id }, commande);
        }
    }
}
