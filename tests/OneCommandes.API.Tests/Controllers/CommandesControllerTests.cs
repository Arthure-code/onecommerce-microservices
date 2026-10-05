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
        [Fact]
        public void GetAll_RendLesCommandes()
        {
            // Given un contrôleur dont la messagerie ne sera pas sollicitée
            IFixture generateur = Generateur();
            Mock<IServiceBusHelper> serviceBusHelper = generateur.Freeze<Mock<IServiceBusHelper>>();
            CommandesController controleur = Controleur(generateur);

            // When
            ActionResult<IEnumerable<Commande>> resultat = controleur.GetAll();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Commande> commandes = Assert.IsType<IEnumerable<Commande>>(reponse.Value, exactMatch: false);
            Assert.NotEmpty(commandes);
            serviceBusHelper.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUneCommandeInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            Mock<IServiceBusHelper> serviceBusHelper = generateur.Freeze<Mock<IServiceBusHelper>>();
            CommandesController controleur = Controleur(generateur);
            controleur.ModelState.AddModelError(nameof(Commande.NomProduit), "Le nom du produit est obligatoire");

            // When
            ActionResult<Commande> resultat = await controleur.Create(generateur.Create<Commande>());

            // Then rien n'est créé, et rien ne part sur la file
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            serviceBusHelper.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneUnNumeroUneDateEtUnTotal()
        {
            // Given une commande dont la quantité et le prix sont connus
            IFixture generateur = Generateur();
            CommandesController controleur = Controleur(generateur);

            Commande commande = generateur.Build<Commande>()
                .With(c => c.Quantite, 2)
                .With(c => c.PrixUnitaire, 30.50m)
                .Create();

            // When
            ActionResult<Commande> resultat = await controleur.Create(commande);

            // Then la commande revient avec ce que le service a posé
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Commande rendue = Assert.IsType<Commande>(cree.Value);
            Assert.StartsWith("ONE-CMD-", rendue.NumeroCommande, StringComparison.Ordinal);
            Assert.Equal(61.00m, rendue.PrixTotal);
            Assert.Equal(DateTime.UtcNow.Date, rendue.DateCommande!.Value.Date);
        }

        [Fact]
        public async Task Create_TransmetLaCommandeAuServiceBus()
        {
            // Given une messagerie qui accepte le message
            IFixture generateur = Generateur();
            Mock<IServiceBusHelper> serviceBusHelper = generateur.Freeze<Mock<IServiceBusHelper>>();
            CommandesController controleur = Controleur(generateur);

            serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .Returns(Task.CompletedTask);

            // When
            await controleur.Create(generateur.Create<Commande>());

            // Then elle part une fois, avec le numéro que le service a donné
            serviceBusHelper.Verify(
                m => m.EnvoyerMessage(It.Is<Commande>(c => c.NumeroCommande.StartsWith("ONE-CMD-", StringComparison.Ordinal))),
                Times.Once);
            serviceBusHelper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_TransmetLaCommandeTelleQuEnregistree()
        {
            // Given une messagerie qui retient ce qu'on lui confie
            IFixture generateur = Generateur();
            Mock<IServiceBusHelper> serviceBusHelper = generateur.Freeze<Mock<IServiceBusHelper>>();
            CommandesController controleur = Controleur(generateur);

            Commande? transmise = null;
            serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .Callback<Commande>(c => transmise = c)
                .Returns(Task.CompletedTask);

            // When
            ActionResult<Commande> resultat = await controleur.Create(generateur.Create<Commande>());

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
            IFixture generateur = Generateur();
            Mock<IServiceBusHelper> serviceBusHelper = generateur.Freeze<Mock<IServiceBusHelper>>();
            CommandesController controleur = Controleur(generateur);

            serviceBusHelper
                .Setup(m => m.EnvoyerMessage(It.IsAny<Commande>()))
                .ThrowsAsync(new InvalidOperationException("Service Bus injoignable"));

            // When la commande est créée

            // Then l'appelant n'obtient pas un succès silencieux
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controleur.Create(generateur.Create<Commande>()));
        }

        [Fact]
        public async Task Create_DonneUnNumeroDifferentAChaqueCommande()
        {
            // Given deux commandes de suite
            IFixture generateur = Generateur();
            CommandesController controleur = Controleur(generateur);

            // When
            CreatedAtActionResult premiere =
                Assert.IsType<CreatedAtActionResult>((await controleur.Create(generateur.Create<Commande>())).Result);
            CreatedAtActionResult seconde =
                Assert.IsType<CreatedAtActionResult>((await controleur.Create(generateur.Create<Commande>())).Result);

            // Then
            Commande une = Assert.IsType<Commande>(premiere.Value);
            Commande autre = Assert.IsType<Commande>(seconde.Value);
            Assert.NotEqual(une.NumeroCommande, autre.NumeroCommande);
            Assert.NotEqual(une.Id, autre.Id);
        }

        private static IFixture Generateur() =>
            new Fixture().Customize(new AutoMoqCustomization());

        // Le contrôleur est bâti par son constructeur seul, qui reçoit la
        // messagerie et le journal du générateur.
        private static CommandesController Controleur(IFixture generateur) =>
            generateur.Build<CommandesController>().OmitAutoProperties().Create();
    }
}
