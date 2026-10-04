using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneCommerce.MVC.Controllers;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Tests.Controllers
{
    public class ProduitsControllerTests
    {
        private readonly Mock<IProduitService> _produitService;
        private readonly Mock<IFichiersService> _fichiersService;
        private readonly ProduitsController _controller;
        private readonly Produit _produit;
        private readonly Produit _produitModifie;
        private readonly Mock<IFormFile> _image;

        public ProduitsControllerTests()
        {
            _produitService = new Mock<IProduitService>();
            _fichiersService = new Mock<IFichiersService>();
            _controller = new ProduitsController(_produitService.Object, _fichiersService.Object);

            _produit = new Produit
            {
                Id = 3,
                Nom = "T-shirt imprime noir",
                Description = "T-shirt noir imprime homme",
                Prix = 30.50m,
                Quantite = 4,
                Image = "image3.png"
            };

            _produitModifie = new Produit
            {
                Id = 3,
                Nom = "T-shirt imprime noir revisite",
                Description = "T-shirt noir imprime homme, nouvelle coupe",
                Prix = 34.00m,
                Quantite = 7,
                Image = "image3.png"
            };

            _image = new Mock<IFormFile>();
            _image.Setup(f => f.FileName).Returns("nouvelle-image.png");
        }

        [Fact]
        public async Task Edit_RendLeProduitQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            IActionResult resultat = await _controller.Edit(_produitModifie);

            // Then la page revient, et rien n'est modifié
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(_produitModifie, vue.Model);
            _produitService.Verify(s => s.UpdateProduit(It.IsAny<Produit>()), Times.Never);
            _fichiersService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_EnvoieLaModificationEtRevientAuCatalogue()
        {
            // Given un service qui accepte la modification
            _produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync(_produitModifie);

            // When aucune nouvelle image n'est déposée
            IActionResult resultat = await _controller.Edit(_produitModifie);

            // Then le produit part tel quel, et l'image n'est pas touchée
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            _produitService.Verify(s => s.UpdateProduit(_produitModifie), Times.Once);
            _fichiersService.Verify(s => s.Upload(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Edit_DeposeLaNouvelleImageAvantDeModifier()
        {
            // Given une nouvelle image jointe au formulaire
            _produitModifie.FichierImage = _image.Object;
            _produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync(_produitModifie);

            // When
            await _controller.Edit(_produitModifie);

            // Then l'image part sous le nom que le produit portera
            _fichiersService.Verify(s => s.Upload(_image.Object, "Image3.png"), Times.Once);
            _produitService.Verify(s => s.UpdateProduit(It.Is<Produit>(p => p.Image == "Image3.png")), Times.Once);
        }

        [Fact]
        public async Task Edit_LeDitQuandLaModificationEchoue()
        {
            // Given un service qui ne modifie rien
            _produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync((Produit?)null);

            // When
            IActionResult resultat = await _controller.Edit(_produitModifie);

            // Then le visiteur lit pourquoi, au lieu d'être renvoyé au catalogue
            Assert.IsType<ViewResult>(resultat);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_RetireLeProduitEtRevientAuCatalogue()
        {
            // Given un service qui accepte la suppression
            _produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(true);

            // When
            IActionResult resultat = await _controller.Delete(_produit);

            // Then
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            _produitService.Verify(s => s.DeleteProduit(3), Times.Once);
        }

        [Fact]
        public async Task Delete_LeDitQuandLaSuppressionEchoue()
        {
            // Given un service qui refuse la suppression
            _produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(false);

            // When
            IActionResult resultat = await _controller.Delete(_produit);

            // Then le visiteur lit pourquoi
            Assert.IsType<ViewResult>(resultat);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_RendNonTrouveSansIdentifiant()
        {
            // Given un produit sans identifiant
            var sansIdentifiant = new Produit { Nom = "Inconnu", Description = "Sans identifiant" };

            // When
            IActionResult resultat = await _controller.Delete(sansIdentifiant);

            // Then rien n'est demandé au service
            Assert.IsType<NotFoundResult>(resultat);
            _produitService.Verify(s => s.DeleteProduit(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Edit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un service qui ne connaît pas l'identifiant
            _produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When la page de modification est ouverte
            IActionResult resultat = await _controller.Edit(99);

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }
    }
}
