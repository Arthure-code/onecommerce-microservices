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
        private static Commande UneCommande() => new Commande
        {
            IdProduit = 3,
            NomProduit = "T-shirt imprimé noir",
            NumeroFideliteClient = "ONE-1001",
            Quantite = 2,
            PrixUnitaire = 30.50m,
            AdresseLivraison = "123 Rue Sainte-Catherine, Montréal"
        };

        [Fact]
        public void GetAll_RendLesCommandes()
        {
            //Etant donné un service de messagerie qui n'est jamais appelé
            var messagerie = new Mock<IServiceBusHelper>();
            var journal = new Mock<ILogger<CommandesController>>();
            var controleur = new CommandesController(messagerie.Object, journal.Object);

            //Lorsque
            ActionResult<IEnumerable<Commande>> resultat = controleur.GetAll();

            //Alors
            var reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            var commandes = Assert.IsAssignableFrom<IEnumerable<Commande>>(reponse.Value);
            Assert.NotEmpty(commandes);
            messagerie.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUneCommandeInvalide()
        {
            //Etant donné un modèle que la validation a rejeté
            var messagerie = new Mock<IServiceBusHelper>();
            var journal = new Mock<ILogger<CommandesController>>();
            var controleur = new CommandesController(messagerie.Object, journal.Object);
            controleur.ModelState.AddModelError("NomProduit", "Le nom du produit est obligatoire");

            //Lorsque
            ActionResult<Commande> resultat = await controleur.Create(UneCommande());

            //Alors rien n'est créé, et rien ne part sur la file
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            messagerie.Verify(m => m.EnvoyerMessage(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneUnNumeroUneDateEtUnTotal()
        {
            //Etant donné une commande complète
            var messagerie = new Mock<IServiceBusHelper>();
            var journal = new Mock<ILogger<CommandesController>>();
            var controleur = new CommandesController(messagerie.Object, journal.Object);

            //Lorsque
            ActionResult<Commande> resultat = await controleur.Create(UneCommande());

            //Alors la commande revient avec ce que le service a posé
            var cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            var commande = Assert.IsType<Commande>(cree.Value);
            Assert.StartsWith("ONE-CMD-", commande.NumeroCommande, StringComparison.Ordinal);
            Assert.Equal(61.00m, commande.PrixTotal);
            Assert.Equal(DateTime.UtcNow.Date, commande.DateCommande.Date);
        }

        [Fact]
        public async Task Create_TransmetLaCommandeAuServiceBus()
        {
            //Etant donné une commande complète
            var messagerie = new Mock<IServiceBusHelper>();
            var journal = new Mock<ILogger<CommandesController>>();
            var controleur = new CommandesController(messagerie.Object, journal.Object);
            Commande envoyee = UneCommande();

            //Lorsque
            await controleur.Create(envoyee);

            //Alors elle part sur la file, une fois, avec son numéro
            messagerie.Verify(m => m.EnvoyerMessage(It.Is<Commande>(c => c.NumeroCommande.StartsWith("ONE-CMD-", StringComparison.Ordinal))), Times.Once);
        }

        [Fact]
        public async Task Create_DonneUnNumeroDifferentAChaqueCommande()
        {
            //Etant donné deux commandes de suite
            var messagerie = new Mock<IServiceBusHelper>();
            var journal = new Mock<ILogger<CommandesController>>();
            var controleur = new CommandesController(messagerie.Object, journal.Object);

            //Lorsque
            var premiere = (CreatedAtActionResult)(await controleur.Create(UneCommande())).Result!;
            var seconde = (CreatedAtActionResult)(await controleur.Create(UneCommande())).Result!;

            //Alors
            var une = Assert.IsType<Commande>(premiere.Value);
            var autre = Assert.IsType<Commande>(seconde.Value);
            Assert.NotEqual(une.NumeroCommande, autre.NumeroCommande);
            Assert.NotEqual(une.Id, autre.Id);
        }
    }
}
