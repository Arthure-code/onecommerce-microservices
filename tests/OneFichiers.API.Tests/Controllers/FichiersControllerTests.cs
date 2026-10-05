using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OneFichiers.API.Controllers;
using OneFichiers.API.Interfaces;
using OneFichiers.API.Models;

namespace OneFichiers.API.Tests.Controllers
{
    public class FichiersControllerTests
    {
        [Fact]
        public async Task DemanderLienTeleversement_RefuseUnCorpsAbsent()
        {
            // Given aucune demande
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            // When
            ActionResult<LienTeleversement> resultat = await controleur.DemanderLienTeleversement(null);

            // Then rien n'est signé
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("../../appsettings.json")]
        [InlineData(@"..\..\appsettings.json")]
        [InlineData("/etc/passwd")]
        [InlineData(@"C:\Windows\System32\drivers\etc\hosts")]
        [InlineData("sous-dossier/image.png")]
        public async Task DemanderLienTeleversement_RefuseUnNomQuiDesigneUnAutreEndroitQueLeDossierDesImages(string nomFichier)
        {
            // Given un nom de fichier qui sort du dossier des images
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            DemandeLien demande = generateur.Build<DemandeLien>()
                .With(d => d.NomFichier, nomFichier)
                .Create();

            // When
            ActionResult<LienTeleversement> resultat = await controleur.DemanderLienTeleversement(demande);

            // Then aucun lien n'est signé, et l'appelant lit pourquoi
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("archive.zip")]
        [InlineData("script.exe")]
        [InlineData("page.html")]
        [InlineData("sans-extension")]
        public async Task DemanderLienTeleversement_RefuseCeQuiNEstPasUneImage(string nomFichier)
        {
            // Given un nom de fichier qui n'annonce pas une image
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            DemandeLien demande = generateur.Build<DemandeLien>()
                .With(d => d.NomFichier, nomFichier)
                .Create();

            // When
            ActionResult<LienTeleversement> resultat = await controleur.DemanderLienTeleversement(demande);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DemanderLienTeleversement_RendLeLienQueLeMagasinASigne()
        {
            // Given un magasin qui signe le lien demandé
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            LienTeleversement lien = generateur.Create<LienTeleversement>();
            magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .ReturnsAsync(lien);

            // When
            ActionResult<LienTeleversement> resultat =
                await controleur.DemanderLienTeleversement(generateur.Create<DemandeLien>());

            // Then le lien part tel quel, l'API ne porte aucun octet
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(lien, reponse.Value);
            magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DemanderLienTeleversement_NeReprendPasLeNomProposeMaisGardeSonExtension()
        {
            // Given un magasin qui retient le nom qu'on lui passe
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            string? nomSigne = null;
            magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .Callback<string>(n => nomSigne = n)
                .ReturnsAsync(generateur.Create<LienTeleversement>());

            DemandeLien demande = generateur.Create<DemandeLien>();

            // When deux visiteurs proposent le même nom
            await controleur.DemanderLienTeleversement(demande);
            string? premier = nomSigne;
            await controleur.DemanderLienTeleversement(demande);

            // Then aucun des deux n'écrase l'image de l'autre
            Assert.NotNull(premier);
            Assert.NotEqual(premier, nomSigne);
            Assert.NotEqual(demande.NomFichier, nomSigne);
            Assert.EndsWith(".png", nomSigne, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderLienLecture_RendLaBaseEtLaSignature()
        {
            // Given un magasin qui signe une lecture
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            LienLecture lien = generateur.Create<LienLecture>();
            magasin.Setup(m => m.LienLectureAsync()).ReturnsAsync(lien);

            // When
            ActionResult<LienLecture> resultat = await controleur.DemanderLienLecture();

            // Then une seule signature sert à toutes les images d'une page
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(lien, reponse.Value);
            magasin.Verify(m => m.LienLectureAsync(), Times.Once);
        }

        [Fact]
        public async Task Deposer_RendNonTrouveQuandLesOctetsVontDirectementAuConteneur()
        {
            // Given un magasin qui ne reçoit pas les octets
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            magasin.Setup(m => m.RecoitLesOctets).Returns(false);

            // When
            IActionResult resultat = await controleur.Deposer(generateur.Create<DemandeLien>().NomFichier);

            // Then la route n'existe pas pour l'appelant
            Assert.IsType<NotFoundResult>(resultat);
            magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_RefuseUnContenuVide()
        {
            // Given un dépôt annoncé sans octets
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            var contexte = new DefaultHttpContext();
            FichiersController controleur = Controleur(generateur, contexte);

            magasin.Setup(m => m.RecoitLesOctets).Returns(true);
            contexte.Request.ContentLength = 0;

            // When
            IActionResult resultat = await controleur.Deposer(generateur.Create<DemandeLien>().NomFichier);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
            magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_RefuseUnNomQuiSortDuDossierDesImages()
        {
            // Given un magasin qui reçoit les octets
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            FichiersController controleur = Controleur(generateur, new DefaultHttpContext());

            magasin.Setup(m => m.RecoitLesOctets).Returns(true);

            // When le nom désigne un autre endroit
            IActionResult resultat = await controleur.Deposer("../../appsettings.json");

            // Then rien n'est écrit
            Assert.IsType<BadRequestObjectResult>(resultat);
            magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_ConfieLesOctetsAuMagasin()
        {
            // Given un dépôt qui porte des octets
            IFixture generateur = Generateur();
            Mock<IMagasinImages> magasin = generateur.Freeze<Mock<IMagasinImages>>();
            var contexte = new DefaultHttpContext();
            FichiersController controleur = Controleur(generateur, contexte);

            string nomFichier = generateur.Create<DemandeLien>().NomFichier;
            byte[] contenu = generateur.Create<byte[]>();

            magasin.Setup(m => m.RecoitLesOctets).Returns(true);
            contexte.Request.Body = new MemoryStream(contenu);
            contexte.Request.ContentLength = contenu.Length;

            // When
            IActionResult resultat = await controleur.Deposer(nomFichier);

            // Then
            Assert.IsType<NoContentResult>(resultat);
            magasin.Verify(m => m.EnregistrerAsync(nomFichier, contexte.Request.Body), Times.Once);
        }

        // Le nom proposé doit annoncer une image, sinon le contrôleur le refuse
        // avant même d'atteindre ce que le test vérifie.
        private static IFixture Generateur()
        {
            IFixture generateur = new Fixture().Customize(new AutoMoqCustomization());

            generateur.Customize<DemandeLien>(demande => demande
                .With(d => d.NomFichier, () => $"{Guid.NewGuid():N}.png"));

            return generateur;
        }

        private static FichiersController Controleur(IFixture generateur, HttpContext contexte)
        {
            FichiersController controleur = generateur.Build<FichiersController>()
                .OmitAutoProperties()
                .Create();

            controleur.ControllerContext = new ControllerContext { HttpContext = contexte };

            return controleur;
        }
    }
}
