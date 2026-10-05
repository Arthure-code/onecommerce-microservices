using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneCommandes.API.Controllers;
using OneCommandes.API.Interfaces;
using OneCommandes.API.Models;

namespace OneCommandes.API.Tests.Controllers
{
    public class CommandesControllerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IServiceBusHelper> _serviceBusHelper;
        private readonly CommandesController _controller;

        public CommandesControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _serviceBusHelper = _fixture.Freeze<Mock<IServiceBusHelper>>();

            // Le contrôleur est bâti par son constructeur seul, qui reçoit la
            // messagerie et le journal depuis la fixture.
            _controller = _fixture.Build<CommandesController>().OmitAutoProperties().Create();
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
            _controller.ModelState.AddModelError(nameof(Commande.NomProduit), "Le nom du produit est obligatoire");

            // When
            ActionResult<Commande> resultat = await _controller.Create(_fixture.Create<Commande>());

            // Then rien n'est créé, et rien ne part sur la file
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            _serviceBusHelper.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneUnNumeroUneDateEtUnTotal()
        {
            // Given une commande dont la quantité et le prix sont connus
            Commande commande = _fixture.Build<Commande>()
                .With(c => c.Quantite, 2)
                .With(c => c.PrixUnitaire, 30.50m)
                .Create();

            // When
            ActionResult<Commande> resultat = await _controller.Create(commande);

            // Then la commande revient avec ce que le service a posé
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Commande rendue = Assert.IsType<Commande>(cree.Value);
            Assert.StartsWith("ONE-CMD-", rendue.NumeroCommande, StringComparison.Ordinal);
            Assert.Equal(61.00m, rendue.PrixTotal);
            Assert.Equal(DateTime.UtcNow.Date, rendue.DateCommande.Date);
        }

        [Fact]
        public async Task Create_TransmetLaCommandeAuServiceBus()
        {
            // Given une messagerie qui accepte le message
            _serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .Returns(Task.CompletedTask);

            // When
            await _controller.Create(_fixture.Create<Commande>());

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
            ActionResult<Commande> resultat = await _controller.Create(_fixture.Create<Commande>());

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
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.Create(_fixture.Create<Commande>()));
        }

        [Fact]
        public async Task Create_DonneUnNumeroDifferentAChaqueCommande()
        {
            // Given deux commandes de suite

            // When
            CreatedAtActionResult premiere =
                Assert.IsType<CreatedAtActionResult>((await _controller.Create(_fixture.Create<Commande>())).Result);
            CreatedAtActionResult seconde =
                Assert.IsType<CreatedAtActionResult>((await _controller.Create(_fixture.Create<Commande>())).Result);

            // Then
            Commande une = Assert.IsType<Commande>(premiere.Value);
            Commande autre = Assert.IsType<Commande>(seconde.Value);
            Assert.NotEqual(une.NumeroCommande, autre.NumeroCommande);
            Assert.NotEqual(une.Id, autre.Id);
        }
    }
}
