using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneCommerce.MVC.Controllers;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Tests.Controllers
{
    public class ProduitsControllerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IProduitService> _produitService;
        private readonly Mock<IFichiersService> _fichiersService;
        private readonly ProduitsController _controller;

        public ProduitsControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _produitService = _fixture.Freeze<Mock<IProduitService>>();
            _fichiersService = _fixture.Freeze<Mock<IFichiersService>>();

            // Le contrôleur est bâti par son constructeur seul : laisser
            // AutoFixture remplir ses propriétés revient à lui demander un
            // ViewDataDictionary, qu'il ne sait pas construire.
            _controller = _fixture.Build<ProduitsController>().OmitAutoProperties().Create();
        }

        [Fact]
        public async Task Edit_RendLeProduitQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            Produit modification = _fixture.Create<Produit>();
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            IActionResult resultat = await _controller.Edit(modification);

            // Then la page revient, et rien n'est modifié
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(modification, vue.Model);
            _produitService.Verify(s => s.UpdateProduit(It.IsAny<Produit>()), Times.Never);
            _fichiersService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_EnvoieLaModificationEtRevientAuCatalogue()
        {
            // Given un service qui accepte la modification
            Produit modification = _fixture.Create<Produit>();
            _produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync(modification);

            // When
            IActionResult resultat = await _controller.Edit(modification);

            // Then le produit part tel quel, et aucune image ne transite par ici
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            _produitService.Verify(s => s.UpdateProduit(modification), Times.Once);
            _fichiersService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_LeDitQuandLaModificationEchoue()
        {
            // Given un service qui ne modifie rien
            _produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync((Produit?)null);

            // When
            IActionResult resultat = await _controller.Edit(_fixture.Create<Produit>());

            // Then le visiteur lit pourquoi, au lieu d'être renvoyé au catalogue
            Assert.IsType<ViewResult>(resultat);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un service qui ne connaît pas l'identifiant
            _produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When la page de modification est ouverte
            IActionResult resultat = await _controller.Edit(_fixture.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task LienImage_RendLeLienQueLeNavigateurUtilisera()
        {
            // Given un service de fichiers qui accorde un lien
            LienTeleversement lien = _fixture.Create<LienTeleversement>();
            string nomPropose = _fixture.Create<string>();
            _fichiersService
                .Setup(s => s.LienTeleversement(It.IsAny<string>()))
                .ReturnsAsync(lien);

            // When
            IActionResult resultat = await _controller.LienImage(nomPropose);

            // Then le navigateur reçoit où écrire et sous quel nom
            JsonResult json = Assert.IsType<JsonResult>(resultat);
            Assert.Same(lien, json.Value);
            _fichiersService.Verify(s => s.LienTeleversement(nomPropose), Times.Once);
        }

        [Fact]
        public async Task LienImage_RefuseQuandLeServiceNAccordeAucunLien()
        {
            // Given un service de fichiers qui refuse le nom proposé
            _fichiersService
                .Setup(s => s.LienTeleversement(It.IsAny<string>()))
                .ReturnsAsync((LienTeleversement?)null);

            // When
            IActionResult resultat = await _controller.LienImage(_fixture.Create<string>());

            // Then
            Assert.IsType<BadRequestResult>(resultat);
        }

        [Fact]
        public async Task Delete_RetireLeProduitEtRevientAuCatalogue()
        {
            // Given un service qui accepte la suppression
            int identifiant = _fixture.Create<int>();
            _produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(true);

            // When
            IActionResult resultat = await _controller.DeleteConfirme(identifiant);

            // Then
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            _produitService.Verify(s => s.DeleteProduit(identifiant), Times.Once);
        }

        [Fact]
        public async Task Delete_LeDitQuandLaSuppressionEchoue()
        {
            // Given un service qui refuse la suppression
            _produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(false);
            _produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync(_fixture.Create<Produit>());

            // When
            IActionResult resultat = await _controller.DeleteConfirme(_fixture.Create<int>());

            // Then le visiteur relit la page de suppression, et lit pourquoi
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("Delete", vue.ViewName);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un service qui ne connaît pas l'identifiant
            _produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When la page de suppression est ouverte
            IActionResult resultat = await _controller.Delete(_fixture.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task Index_RendLeCatalogueEntierSansFiltre()
        {
            // Given un catalogue
            List<Produit> catalogue = _fixture.CreateMany<Produit>().ToList();
            _produitService.Setup(s => s.GetProduits()).ReturnsAsync(catalogue);

            // When aucun filtre n'est demandé
            IActionResult resultat = await _controller.Index(null);

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(catalogue, vue.Model);
        }

        [Fact]
        public async Task Index_NeGardeQueCeQuiPorteLeFiltreOuEstEnVedette()
        {
            // Given un produit qui porte le filtre, un en vedette, et un tiers
            Produit recherche = _fixture.Build<Produit>()
                .With(p => p.Nom, "Chandail raye")
                .With(p => p.Vedette, false)
                .Create();
            Produit vedette = _fixture.Build<Produit>()
                .With(p => p.Nom, "Polo uni")
                .With(p => p.Vedette, true)
                .Create();
            Produit ignore = _fixture.Build<Produit>()
                .With(p => p.Nom, "Casquette")
                .With(p => p.Vedette, false)
                .Create();

            _produitService
                .Setup(s => s.GetProduits())
                .ReturnsAsync(new List<Produit> { recherche, vedette, ignore });

            // When le visiteur filtre sur une partie du nom, dans une autre casse
            IActionResult resultat = await _controller.Index("CHANDAIL");

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            List<Produit> retenus = Assert.IsType<List<Produit>>(vue.Model);
            Assert.Contains(recherche, retenus);
            Assert.Contains(vedette, retenus);
            Assert.DoesNotContain(ignore, retenus);
        }

        [Fact]
        public void Create_OuvreUnFormulaireVide()
        {
            // Given la page d'ajout

            // When
            IActionResult resultat = _controller.Create();

            // Then rien n'est demandé au catalogue
            Assert.IsType<ViewResult>(resultat);
            _produitService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            Produit nouveau = _fixture.Create<Produit>();
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            IActionResult resultat = await _controller.Create(nouveau);

            // Then rien n'est ajouté
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(nouveau, vue.Model);
            _produitService.Verify(s => s.AddProduit(It.IsAny<Produit>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUnProduitDontLImageNAPasEteDeposee()
        {
            // Given un produit sans nom d'image
            Produit sansImage = _fixture.Build<Produit>()
                .With(p => p.Image, string.Empty)
                .Create();

            // When
            IActionResult resultat = await _controller.Create(sansImage);

            // Then le visiteur lit pourquoi, et rien n'est ajouté
            Assert.IsType<ViewResult>(resultat);
            Assert.True(_controller.ModelState.ContainsKey(nameof(Produit.Image)));
            _produitService.Verify(s => s.AddProduit(It.IsAny<Produit>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneLIdentifiantSuivantEtRevientAuCatalogue()
        {
            // Given un catalogue dont on connaît le plus grand identifiant
            List<Produit> catalogue = _fixture.CreateMany<Produit>().ToList();
            int plusGrand = catalogue.Max(p => p.Id!.Value);
            _produitService.Setup(s => s.GetProduits()).ReturnsAsync(catalogue);

            Produit nouveau = _fixture.Create<Produit>();

            // When
            IActionResult resultat = await _controller.Create(nouveau);

            // Then il prend la place suivante
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            _produitService.Verify(s => s.AddProduit(It.Is<Produit>(p => p.Id == plusGrand + 1)), Times.Once);
        }
    }
}
