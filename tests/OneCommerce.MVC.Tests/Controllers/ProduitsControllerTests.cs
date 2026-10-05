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
        [Fact]
        public async Task Index_RendLeCatalogueEntierSansFiltre()
        {
            // Given un catalogue
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            List<Produit> catalogue = generateur.CreateMany<Produit>().ToList();
            produitService.Setup(s => s.GetProduits()).ReturnsAsync(catalogue);

            // When aucun filtre n'est demandé
            IActionResult resultat = await controleur.Index(null);

            // Then
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(catalogue, vue.Model);
        }

        [Fact]
        public async Task Index_NeGardeQueCeQuiPorteLeFiltreOuEstEnVedette()
        {
            // Given un produit qui porte le filtre, un en vedette, et un tiers
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            Produit recherche = generateur.Build<Produit>()
                .With(p => p.Nom, "Chandail raye")
                .With(p => p.Vedette, false)
                .Create();
            Produit vedette = generateur.Build<Produit>()
                .With(p => p.Nom, "Polo uni")
                .With(p => p.Vedette, true)
                .Create();
            Produit ignore = generateur.Build<Produit>()
                .With(p => p.Nom, "Casquette")
                .With(p => p.Vedette, false)
                .Create();

            produitService
                .Setup(s => s.GetProduits())
                .ReturnsAsync(new List<Produit> { recherche, vedette, ignore });

            // When le visiteur filtre sur une partie du nom, dans une autre casse
            IActionResult resultat = await controleur.Index("CHANDAIL");

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
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            // When
            IActionResult resultat = controleur.Create();

            // Then rien n'est demandé au catalogue
            Assert.IsType<ViewResult>(resultat);
            produitService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_RendLeFormulaireQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            Produit nouveau = generateur.Create<Produit>();
            controleur.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            IActionResult resultat = await controleur.Create(nouveau);

            // Then rien n'est ajouté
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(nouveau, vue.Model);
            produitService.Verify(s => s.AddProduit(It.IsAny<Produit>()), Times.Never);
        }

        [Fact]
        public async Task Create_RefuseUnProduitDontLImageNAPasEteDeposee()
        {
            // Given un produit sans nom d'image
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            Produit sansImage = generateur.Build<Produit>()
                .With(p => p.Image, string.Empty)
                .Create();

            // When
            IActionResult resultat = await controleur.Create(sansImage);

            // Then le visiteur lit pourquoi, et rien n'est ajouté
            Assert.IsType<ViewResult>(resultat);
            Assert.True(controleur.ModelState.ContainsKey(nameof(Produit.Image)));
            produitService.Verify(s => s.AddProduit(It.IsAny<Produit>()), Times.Never);
        }

        [Fact]
        public async Task Create_DonneLIdentifiantSuivantEtRevientAuCatalogue()
        {
            // Given un catalogue dont on connaît le plus grand identifiant
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            List<Produit> catalogue = generateur.CreateMany<Produit>().ToList();
            int plusGrand = catalogue.Max(p => p.Id!.Value);
            produitService.Setup(s => s.GetProduits()).ReturnsAsync(catalogue);

            // When
            IActionResult resultat = await controleur.Create(generateur.Create<Produit>());

            // Then il prend la place suivante
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            produitService.Verify(s => s.AddProduit(It.Is<Produit>(p => p.Id == plusGrand + 1)), Times.Once);
        }

        [Fact]
        public async Task Edit_RendLeProduitQuandLeModeleEstInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            Mock<IFichiersService> fichiersService = generateur.Freeze<Mock<IFichiersService>>();
            ProduitsController controleur = Controleur(generateur);

            Produit modification = generateur.Create<Produit>();
            controleur.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            IActionResult resultat = await controleur.Edit(modification);

            // Then la page revient, et rien n'est modifié
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Same(modification, vue.Model);
            produitService.Verify(s => s.UpdateProduit(It.IsAny<Produit>()), Times.Never);
            fichiersService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_EnvoieLaModificationEtRevientAuCatalogue()
        {
            // Given un service qui accepte la modification
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            Mock<IFichiersService> fichiersService = generateur.Freeze<Mock<IFichiersService>>();
            ProduitsController controleur = Controleur(generateur);

            Produit modification = generateur.Create<Produit>();
            produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync(modification);

            // When
            IActionResult resultat = await controleur.Edit(modification);

            // Then le produit part tel quel, et aucune image ne transite par ici
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            produitService.Verify(s => s.UpdateProduit(modification), Times.Once);
            fichiersService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_LeDitQuandLaModificationEchoue()
        {
            // Given un service qui ne modifie rien
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            produitService
                .Setup(s => s.UpdateProduit(It.IsAny<Produit>()))
                .ReturnsAsync((Produit?)null);

            // When
            IActionResult resultat = await controleur.Edit(generateur.Create<Produit>());

            // Then le visiteur lit pourquoi, au lieu d'être renvoyé au catalogue
            Assert.IsType<ViewResult>(resultat);
            Assert.False(controleur.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un service qui ne connaît pas l'identifiant
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When la page de modification est ouverte
            IActionResult resultat = await controleur.Edit(generateur.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        [Fact]
        public async Task LienImage_RendLeLienQueLeNavigateurUtilisera()
        {
            // Given un service de fichiers qui accorde un lien
            IFixture generateur = Generateur();
            Mock<IFichiersService> fichiersService = generateur.Freeze<Mock<IFichiersService>>();
            ProduitsController controleur = Controleur(generateur);

            LienTeleversement lien = generateur.Create<LienTeleversement>();
            string nomPropose = generateur.Create<string>();
            fichiersService
                .Setup(s => s.LienTeleversement(It.IsAny<string>()))
                .ReturnsAsync(lien);

            // When
            IActionResult resultat = await controleur.LienImage(nomPropose);

            // Then le navigateur reçoit où écrire et sous quel nom
            JsonResult json = Assert.IsType<JsonResult>(resultat);
            Assert.Same(lien, json.Value);
            fichiersService.Verify(s => s.LienTeleversement(nomPropose), Times.Once);
        }

        [Fact]
        public async Task LienImage_RefuseQuandLeServiceNAccordeAucunLien()
        {
            // Given un service de fichiers qui refuse le nom proposé
            IFixture generateur = Generateur();
            Mock<IFichiersService> fichiersService = generateur.Freeze<Mock<IFichiersService>>();
            ProduitsController controleur = Controleur(generateur);

            fichiersService
                .Setup(s => s.LienTeleversement(It.IsAny<string>()))
                .ReturnsAsync((LienTeleversement?)null);

            // When
            IActionResult resultat = await controleur.LienImage(generateur.Create<string>());

            // Then
            Assert.IsType<BadRequestResult>(resultat);
        }

        [Fact]
        public async Task Delete_RetireLeProduitEtRevientAuCatalogue()
        {
            // Given un service qui accepte la suppression
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            int identifiant = generateur.Create<int>();
            produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(true);

            // When
            IActionResult resultat = await controleur.DeleteConfirme(identifiant);

            // Then
            RedirectToActionResult redirection = Assert.IsType<RedirectToActionResult>(resultat);
            Assert.Equal(nameof(ProduitsController.Index), redirection.ActionName);
            produitService.Verify(s => s.DeleteProduit(identifiant), Times.Once);
        }

        [Fact]
        public async Task Delete_LeDitQuandLaSuppressionEchoue()
        {
            // Given un service qui refuse la suppression
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            produitService
                .Setup(s => s.DeleteProduit(It.IsAny<int>()))
                .ReturnsAsync(false);
            produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync(generateur.Create<Produit>());

            // When
            IActionResult resultat = await controleur.DeleteConfirme(generateur.Create<int>());

            // Then le visiteur relit la page de suppression, et lit pourquoi
            ViewResult vue = Assert.IsType<ViewResult>(resultat);
            Assert.Equal("Delete", vue.ViewName);
            Assert.False(controleur.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un service qui ne connaît pas l'identifiant
            IFixture generateur = Generateur();
            Mock<IProduitService> produitService = generateur.Freeze<Mock<IProduitService>>();
            ProduitsController controleur = Controleur(generateur);

            produitService
                .Setup(s => s.GetProduitById(It.IsAny<int>()))
                .ReturnsAsync((Produit?)null);

            // When la page de suppression est ouverte
            IActionResult resultat = await controleur.Delete(generateur.Create<int>());

            // Then
            Assert.IsType<NotFoundResult>(resultat);
        }

        private static IFixture Generateur() =>
            new Fixture().Customize(new AutoMoqCustomization());

        // Le contrôleur est bâti par son constructeur seul : laisser AutoFixture
        // remplir ses propriétés revient à lui demander un ViewDataDictionary,
        // qu'il ne sait pas construire.
        private static ProduitsController Controleur(IFixture generateur) =>
            generateur.Build<ProduitsController>().OmitAutoProperties().Create();
    }
}
