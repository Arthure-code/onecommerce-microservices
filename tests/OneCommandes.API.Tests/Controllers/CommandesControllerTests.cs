using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OneCommandes.API.Controllers;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Models;

namespace OneCommandes.API.Tests.Controllers
{
    public class CommandesControllerTests
    {
        private readonly Mock<IServiceBusHelper> _serviceBusHelper;
        private readonly Mock<ILogger<CommandesController>> _logger;
        private readonly CommandesController _controller;
        private readonly Commande _commande;
        private readonly Commande _autreCommande;

        public CommandesControllerTests()
        {
            _serviceBusHelper = new Mock<IServiceBusHelper>();
            _logger = new Mock<ILogger<CommandesController>>();
            _controller = new CommandesController(_serviceBusHelper.Object, _logger.Object);

            _commande = new Commande
            {
                IdProduit = 3,
                NomProduit = "T-shirt imprimé noir",
                NumeroFideliteClient = "ONE-100001",
                Quantite = 2,
                PrixUnitaire = 30.50m,
                AdresseLivraison = "123 Rue Sainte-Catherine, Montréal"
            };

            _autreCommande = new Commande
            {
                IdProduit = 4,
                NomProduit = "T-shirt gris femme",
                NumeroFideliteClient = "ONE-100002",
                Quantite = 1,
                PrixUnitaire = 10.20m,
                AdresseLivraison = "456 Boulevard Laurier, Québec"
            };
        }

        [Fact]
        public void GetAll_RendLesCommandes()
        {
            // Given un contrôleur dont la messagerie ne sera pas sollicitée

            // When
            ActionResult<IEnumerable<Commande>> resultat = _controller.GetAll();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Commande> commandes = Assert.IsAssignableFrom<IEnumerable<Commande>>(reponse.Value);
            Assert.NotEmpty(commandes);
            _serviceBusHelper.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUneCommandeInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError("NomProduit", "Le nom du produit est obligatoire");

            // When
            ActionResult<Commande> resultat = await _controller.Create(_commande);

            // Then rien n'est créé, et rien ne part sur la file
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            _serviceBusHelper.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneUnNumeroUneDateEtUnTotal()
        {
            // Given une commande complète

            // When
            ActionResult<Commande> resultat = await _controller.Create(_commande);

            // Then la commande revient avec ce que le service a posé
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Commande commande = Assert.IsType<Commande>(cree.Value);
            Assert.StartsWith("ONE-CMD-", commande.NumeroCommande, StringComparison.Ordinal);
            Assert.Equal(61.00m, commande.PrixTotal);
            Assert.Equal(DateTime.UtcNow.Date, commande.DateCommande.Date);
        }

        [Fact]
        public async Task Create_TransmetLaCommandeAuServiceBus()
        {
            // Given une messagerie qui accepte le message
            _serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // When
            await _controller.Create(_commande);

            // Then elle part une fois, avec le numéro que le service a donné
            _serviceBusHelper.Verify(
                m => m.EnvoyerMessage(It.Is<Commande>(c => c.NumeroCommande.StartsWith("ONE-CMD-", StringComparison.Ordinal))),
                Times.Once);
            _serviceBusHelper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_TransmetLaCommandeTelleQuEnregistree()
        {
            // Given une messagerie qui retient ce qu'on lui confie
            Commande? transmise = null;
            _serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .Callback<Commande>(c => transmise = c)
                .Returns(Task.CompletedTask);

            // When
            ActionResult<Commande> resultat = await _controller.Create(_commande);

            // Then ce qui part sur la file est ce qui revient à l'appelant
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Commande rendue = Assert.IsType<Commande>(cree.Value);
            Assert.NotNull(transmise);
            Assert.Equal(rendue.NumeroCommande, transmise!.NumeroCommande);
            Assert.Equal(rendue.PrixTotal, transmise.PrixTotal);
        }

        [Fact]
        public async Task Create_RemonteLEchecDeLaMessagerie()
        {
            // Given une messagerie indisponible
            _serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .ThrowsAsync(new InvalidOperationException("Service Bus injoignable"));

            // When la commande est créée

            // Then l'appelant n'obtient pas un succès silencieux
            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(_commande));
        }

        [Fact]
        public async Task Create_DonneUnNumeroDifferentAChaqueCommande()
        {
            // Given deux commandes de suite

            // When
            CreatedAtActionResult premiere = Assert.IsType<CreatedAtActionResult>((await _controller.Create(_commande)).Result);
            CreatedAtActionResult seconde = Assert.IsType<CreatedAtActionResult>((await _controller.Create(_autreCommande)).Result);

            // Then
            Commande une = Assert.IsType<Commande>(premiere.Value);
            Commande autre = Assert.IsType<Commande>(seconde.Value);
            Assert.NotEqual(une.NumeroCommande, autre.NumeroCommande);
            Assert.NotEqual(une.Id, autre.Id);
        }
    }
}
