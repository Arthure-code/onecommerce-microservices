using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneCommerce.MVC.Controllers;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Tests.Controllers
{
    public class FideliteControllerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IFideliteService> _fideliteService;
        private readonly FideliteController _controller;

        public FideliteControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fideliteService = _fixture.Freeze<Mock<IFideliteService>>();

            // Le contrôleur est bâti par son constructeur seul : laisser
            // AutoFixture remplir ses propriétés revient à lui demander un
            // ViewDataDictionary, qu'il ne sait pas construire.
            _controller = _fixture.Build<FideliteController>().OmitAutoProperties().Create();
        }

        [Fact]
        public async Task Index_RendLesCartesDuService()
        {
            // Given un service qui connaît des cartes
            List<Fidelite> cartes = _fixture.CreateMany<Fidelite>().ToList();
            _fideliteService.Setup(s => s.GetFidelitesAsync()).ReturnsAsync(cartes);

            // When
            IActionResult resultat = await _controller.Index();

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(cartes, vue.Model);
        }

        [Fact]
        public void Create_OuvreUnFormulaireVide()
        {
            // Given la page d'inscription

            // When
            IActionResult resultat = _controller.Create();

            // Then rien n'est demandé au service
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.IsType<Fidelite>(vue.Model);
            _fideliteService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            Fidelite inscription = _fixture.Create<Fidelite>();
            _controller.ModelState.AddModelError(nameof(Fidelite.CourrielClient), "Le courriel est obligatoire");

            // When
            IActionResult resultat = await _controller.Create(inscription);

            // Then rien n'est inscrit
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(inscription, vue.Model);
            _fideliteService.Verify(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()), Times.Never);
        }

        [Fact]
        public async Task Create_MontreLeNumeroQuandLInscriptionAboutit()
        {
            // Given un service qui accepte l'inscription
            Fidelite inscription = _fixture.Create<Fidelite>();
            Fidelite carteCreee = _fixture.Create<Fidelite>();
            _fideliteService
                .Setup(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()))
                .ReturnsAsync(carteCreee);

            // When
            IActionResult resultat = await _controller.Create(inscription);

            // Then le visiteur repart avec son numéro
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal(carteCreee.NumeroFidelite, vue.ViewData["NumeroFidelite"]);
            _fideliteService.Verify(s => s.CreateFideliteAsync(inscription), Times.Once);
        }

        [Fact]
        public async Task Create_LeDitQuandLInscriptionEchoue()
        {
            // Given un service qui refuse l'inscription
            _fideliteService
                .Setup(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()))
                .ReturnsAsync((Fidelite?)null);

            // When
            IActionResult resultat = await _controller.Create(_fixture.Create<Fidelite>());

            // Then le visiteur lit pourquoi, et aucun numéro n'est affiché
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Null(vue.ViewData["NumeroFidelite"]);
        }
    }
}
