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
        [Fact]
        public async Task Index_RendLesCartesDuService()
        {
            // Given un service qui connaît des cartes
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            FideliteController controleur = Controleur(generateur);

            List<Fidelite> cartes = generateur.CreateMany<Fidelite>().ToList();
            fideliteService.Setup(s => s.GetFidelitesAsync()).ReturnsAsync(cartes);

            // When
            IActionResult resultat = await controleur.Index();

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(cartes, vue.Model);
        }

        [Fact]
        public void Create_OuvreUnFormulaireVide()
        {
            // Given la page d'inscription
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            FideliteController controleur = Controleur(generateur);

            // When
            IActionResult resultat = controleur.Create();

            // Then rien n'est demandé au service
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.IsType<Fidelite>(vue.Model);
            fideliteService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            FideliteController controleur = Controleur(generateur);

            Fidelite inscription = generateur.Create<Fidelite>();
            controleur.ModelState.AddModelError(nameof(Fidelite.CourrielClient), "Le courriel est obligatoire");

            // When
            IActionResult resultat = await controleur.Create(inscription);

            // Then rien n'est inscrit
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(inscription, vue.Model);
            fideliteService.Verify(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()), Times.Never);
        }

        [Fact]
        public async Task Create_MontreLeNumeroQuandLInscriptionAboutit()
        {
            // Given un service qui accepte l'inscription
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            FideliteController controleur = Controleur(generateur);

            Fidelite inscription = generateur.Create<Fidelite>();
            Fidelite carteCreee = generateur.Create<Fidelite>();
            fideliteService
                .Setup(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()))
                .ReturnsAsync(carteCreee);

            // When
            IActionResult resultat = await controleur.Create(inscription);

            // Then le visiteur repart avec son numéro
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal(carteCreee.NumeroFidelite, vue.ViewData["NumeroFidelite"]);
            fideliteService.Verify(s => s.CreateFideliteAsync(inscription), Times.Once);
        }

        [Fact]
        public async Task Create_LeDitQuandLInscriptionEchoue()
        {
            // Given un service qui refuse l'inscription
            IFixture generateur = Generateur();
            Mock<IFideliteService> fideliteService = generateur.Freeze<Mock<IFideliteService>>();
            FideliteController controleur = Controleur(generateur);

            fideliteService
                .Setup(s => s.CreateFideliteAsync(It.IsAny<Fidelite>()))
                .ReturnsAsync((Fidelite?)null);

            // When
            IActionResult resultat = await controleur.Create(generateur.Create<Fidelite>());

            // Then le visiteur lit pourquoi, et aucun numéro n'est affiché
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.False(controleur.ModelState.IsValid);
            Assert.Null(vue.ViewData["NumeroFidelite"]);
        }

        private static IFixture Generateur() =>
            new Fixture().Customize(new AutoMoqCustomization());

        // Le contrôleur est bâti par son constructeur seul : laisser AutoFixture
        // remplir ses propriétés revient à lui demander un ViewDataDictionary,
        // qu'il ne sait pas construire.
        private static FideliteController Controleur(IFixture generateur) =>
            generateur.Build<FideliteController>().OmitAutoProperties().Create();
    }
}
