using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneCommerce.MVC.Controllers;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Tests.Controllers
{
    public class CommandesControllerTests
    {
        [Fact]
        public async Task Index_RendLHistorique()
        {
            // Given un service qui connaît des commandes
            IFixture generateur = Generateur();
            Mock<ICommandesService> commandesService = generateur.Freeze<Mock<ICommandesService>>();
            CommandesController controleur = Controleur(generateur);

            IEnumerable<Commande> historique = generateur.CreateMany<Commande>();
            commandesService.Setup(s => s.GetAllAsync()).ReturnsAsync(historique);

            // When
            IActionResult resultat = await controleur.Index();

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(historique, vue.Model);
        }

        [Fact]
        public async Task Create_PrepareLaCommandeAPartirDuCatalogue()
        {
            // Given un produit du catalogue
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            CommandesController controleur = Controleur(generateur);

            Produit produit = generateur.Create<Produit>();
            produitService.Setup(s => s.GetProduitById(produit.Id!.Value)).ReturnsAsync(produit);

            // When la page de commande est ouverte
            IActionResult resultat = await controleur.Create(produit.Id!.Value);

            // Then le prix vient du catalogue, pas du visiteur
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Commande commande = Assert.IsType<Commande>(vue.Model);
            Assert.Equal(produit.Id, commande.IdProduit);
            Assert.Equal(produit.Prix, commande.PrixUnitaire);
            Assert.Same(produit, commande.Produit);
        }

        [Fact]
        public async Task Create_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un identifiant absent du catalogue
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            CommandesController controleur = Controleur(generateur);

            produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When
            IActionResult resultat = await controleur.Create(generateur.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            Mock<ICommandesService> commandesService = generateur.Freeze<Mock<ICommandesService>>();
            CommandesController controleur = Controleur(generateur);

            Commande commande = generateur.Create<Commande>();
            Produit produit = generateur.Create<Produit>();
            produitService.Setup(s => s.GetProduitById(commande.IdProduit!.Value)).ReturnsAsync(produit);
            controleur.ModelState.AddModelError(nameof(Commande.AdresseLivraison), "L'adresse est obligatoire");

            // When
            IActionResult resultat = await controleur.Create(commande);

            // Then la page revient avec son produit, et rien n'est commandé
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(produit, Assert.IsType<Commande>(vue.Model).Produit);
            commandesService.Verify(s => s.CreateAsync(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUnNumeroDeFideliteInconnu()
        {
            // Given un numéro qu'aucune carte ne porte
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            Mock<ICommandesService> commandesService = generateur.Freeze<Mock<ICommandesService>>();
            CommandesController controleur = Controleur(generateur);

            Commande commande = generateur.Create<Commande>();
            Fidelite carteIntrouvable = generateur.Build<Fidelite>()
                .With(f => f.NumeroFidelite, string.Empty)
                .Create();

            fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(It.IsAny<string>()))
                .ReturnsAsync(carteIntrouvable);
            produitService
                .Setup(s => s.GetProduitById(commande.IdProduit!.Value))
                .ReturnsAsync(generateur.Create<Produit>());

            // When
            IActionResult resultat = await controleur.Create(commande);

            // Then le visiteur lit pourquoi, et rien n'est commandé
            Assert.IsType<ViewResult>(resultat);
            Assert.True(controleur.ModelState.ContainsKey(nameof(Commande.NumeroFideliteClient)));
            commandesService.Verify(s => s.CreateAsync(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_EnvoieLaCommandeEtRevientALHistorique()
        {
            // Given une carte connue et un service qui accepte la commande
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            Mock<ICommandesService> commandesService = generateur.Freeze<Mock<ICommandesService>>();
            CommandesController controleur = Controleur(generateur);

            Commande commande = generateur.Create<Commande>();
            fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(commande.NumeroFideliteClient))
                .ReturnsAsync(generateur.Create<Fidelite>());
            commandesService
                .Setup(s => s.CreateAsync(It.IsAny<Commande>()))
                .ReturnsAsync(generateur.Create<Commande>());

            // When
            IActionResult resultat = await controleur.Create(commande);

            // Then
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(CommandesController.Index), redirection.ActionName);
            commandesService.Verify(s => s.CreateAsync(commande), Times.Once);
        }

        [Fact]
        public async Task Create_LeDitQuandLaCommandeEchoue()
        {
            // Given une carte connue et un service qui ne crée rien
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            Mock<ICommandesService> commandesService = generateur.Freeze<Mock<ICommandesService>>();
            CommandesController controleur = Controleur(generateur);

            Commande commande = generateur.Create<Commande>();
            fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(commande.NumeroFideliteClient))
                .ReturnsAsync(generateur.Create<Fidelite>());
            commandesService
                .Setup(s => s.CreateAsync(It.IsAny<Commande>()))
                .ReturnsAsync((Commande?)null);

            // When
            IActionResult resultat = await controleur.Create(commande);

            // Then le visiteur reste sur sa commande, et lit pourquoi
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(commande, vue.Model);
            Assert.False(controleur.ModelState.IsValid);
        }

        private static IFixture Generateur() =>
            new Fixture().Customize(new AutoMoqCustomization());

        // Le contrôleur est bâti par son constructeur seul : laisser AutoFixture
        // remplir ses propriétés revient à lui demander un ViewDataDictionary,
        // qu'il ne sait pas construire.
        private static CommandesController Controleur(IFixture generateur) =>
            generateur.Build<CommandesController>().OmitAutoProperties().Create();
    }
}
