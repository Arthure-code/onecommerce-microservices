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
        private readonly IFixture _fixture;
        private readonly Mock<ICommandesService> _commandesService;
        private readonly Mock<IFideliteService> _fideliteService;
        private readonly Mock<IProduitService> _produitService;
        private readonly CommandesController _controller;

        public CommandesControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _commandesService = _fixture.Freeze<Mock<ICommandesService>>();
            _fideliteService = _fixture.Freeze<Mock<IFideliteService>>();
            _produitService = _fixture.Freeze<Mock<IProduitService>>();

            // Le contrôleur est bâti par son constructeur seul : laisser
            // AutoFixture remplir ses propriétés revient à lui demander un
            // ViewDataDictionary, qu'il ne sait pas construire.
            _controller = _fixture.Build<CommandesController>().OmitAutoProperties().Create();
        }

        [Fact]
        public async Task Index_RendLHistorique()
        {
            // Given un service qui connaît des commandes
            IEnumerable<Commande> historique = _fixture.CreateMany<Commande>();
            _commandesService.Setup(s => s.GetAllAsync()).ReturnsAsync(historique);

            // When
            IActionResult resultat = await _controller.Index();

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(historique, vue.Model);
        }

        [Fact]
        public async Task Create_PrepareLaCommandeAPartirDuCatalogue()
        {
            // Given un produit du catalogue
            Produit produit = _fixture.Create<Produit>();
            _produitService.Setup(s => s.GetProduitById(produit.Id!.Value)).ReturnsAsync(produit);

            // When la page de commande est ouverte
            IActionResult resultat = await _controller.Create(produit.Id!.Value);

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
            _produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When
            IActionResult resultat = await _controller.Create(_fixture.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            Commande commande = _fixture.Create<Commande>();
            Produit produit = _fixture.Create<Produit>();
            _produitService.Setup(s => s.GetProduitById(commande.IdProduit!.Value)).ReturnsAsync(produit);
            _controller.ModelState.AddModelError(nameof(Commande.AdresseLivraison), "L'adresse est obligatoire");

            // When
            IActionResult resultat = await _controller.Create(commande);

            // Then la page revient avec son produit, et rien n'est commandé
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(produit, Assert.IsType<Commande>(vue.Model).Produit);
            _commandesService.Verify(s => s.CreateAsync(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUnNumeroDeFideliteInconnu()
        {
            // Given un numéro qu'aucune carte ne porte
            Commande commande = _fixture.Create<Commande>();
            Fidelite carteIntrouvable = _fixture.Build<Fidelite>()
                .With(f => f.NumeroFidelite, string.Empty)
                .Create();

            _fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(It.IsAny<string>()))
                .ReturnsAsync(carteIntrouvable);
            _produitService
                .Setup(s => s.GetProduitById(commande.IdProduit!.Value))
                .ReturnsAsync(_fixture.Create<Produit>());

            // When
            IActionResult resultat = await _controller.Create(commande);

            // Then le visiteur lit pourquoi, et rien n'est commandé
            Assert.IsType<ViewResult>(resultat);
            Assert.True(_controller.ModelState.ContainsKey(nameof(Commande.NumeroFideliteClient)));
            _commandesService.Verify(s => s.CreateAsync(It.IsAny<Commande>()), Times.Never);
        }

        [Fact]
        public async Task Create_EnvoieLaCommandeEtRevientALHistorique()
        {
            // Given une carte connue et un service qui accepte la commande
            Commande commande = _fixture.Create<Commande>();
            _fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(commande.NumeroFideliteClient))
                .ReturnsAsync(_fixture.Create<Fidelite>());
            _commandesService
                .Setup(s => s.CreateAsync(It.IsAny<Commande>()))
                .ReturnsAsync(_fixture.Create<Commande>());

            // When
            IActionResult resultat = await _controller.Create(commande);

            // Then
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(CommandesController.Index), redirection.ActionName);
            _commandesService.Verify(s => s.CreateAsync(commande), Times.Once);
        }

        [Fact]
        public async Task Create_LeDitQuandLaCommandeEchoue()
        {
            // Given une carte connue et un service qui ne crée rien
            Commande commande = _fixture.Create<Commande>();
            _fideliteService
                .Setup(s => s.GetFideliteByNumeroAsync(commande.NumeroFideliteClient))
                .ReturnsAsync(_fixture.Create<Fidelite>());
            _commandesService
                .Setup(s => s.CreateAsync(It.IsAny<Commande>()))
                .ReturnsAsync((Commande?)null);

            // When
            IActionResult resultat = await _controller.Create(commande);

            // Then le visiteur reste sur sa commande, et lit pourquoi
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(commande, vue.Model);
            Assert.False(_controller.ModelState.IsValid);
        }
    }
}
