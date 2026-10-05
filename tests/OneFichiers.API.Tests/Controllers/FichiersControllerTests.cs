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
        private readonly IFixture _fixture;
        private readonly Mock<IMagasinImages> _magasin;
        private readonly FichiersController _controller;
        private readonly DefaultHttpContext _contexte;

        public FichiersControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Le nom proposé doit annoncer une image, sinon le contrôleur le
            // refuse avant même d'atteindre ce que le test vérifie.
            _fixture.Customize<DemandeLien>(demande => demande
                .With(d => d.NomFichier, () => $"{_fixture.Create<string>().Replace("-", "")}.png"));

            _magasin = _fixture.Freeze<Mock<IMagasinImages>>();
            _contexte = new DefaultHttpContext();
            _controller = _fixture.Build<FichiersController>()
                .OmitAutoProperties()
                .Create();
            _controller.ControllerContext = new ControllerContext { HttpContext = _contexte };
        }

        [Fact]
        public async Task DemanderLienTeleversement_RefuseUnCorpsAbsent()
        {
            // Given aucune demande

            // When
            ActionResult<LienTeleversement> resultat = await _controller.DemanderLienTeleversement(null);

            // Then rien n'est signé
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            _magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
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
            DemandeLien demande = _fixture.Build<DemandeLien>()
                .With(d => d.NomFichier, nomFichier)
                .Create();

            // When
            ActionResult<LienTeleversement> resultat = await _controller.DemanderLienTeleversement(demande);

            // Then aucun lien n'est signé, et l'appelant lit pourquoi
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            _magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("archive.zip")]
        [InlineData("script.exe")]
        [InlineData("page.html")]
        [InlineData("sans-extension")]
        public async Task DemanderLienTeleversement_RefuseCeQuiNEstPasUneImage(string nomFichier)
        {
            // Given un nom de fichier qui n'annonce pas une image
            DemandeLien demande = _fixture.Build<DemandeLien>()
                .With(d => d.NomFichier, nomFichier)
                .Create();

            // When
            ActionResult<LienTeleversement> resultat = await _controller.DemanderLienTeleversement(demande);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            _magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DemanderLienTeleversement_RendLeLienQueLeMagasinASigne()
        {
            // Given un magasin qui signe le lien demandé
            LienTeleversement lien = _fixture.Create<LienTeleversement>();
            _magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .ReturnsAsync(lien);

            // When
            ActionResult<LienTeleversement> resultat =
                await _controller.DemanderLienTeleversement(_fixture.Create<DemandeLien>());

            // Then le lien part tel quel, l'API ne porte aucun octet
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(lien, reponse.Value);
            _magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DemanderLienTeleversement_NeReprendPasLeNomProposeMaisGardeSonExtension()
        {
            // Given un magasin qui retient le nom qu'on lui passe
            string? nomSigne = null;
            _magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .Callback<string>(n => nomSigne = n)
                .ReturnsAsync(_fixture.Create<LienTeleversement>());

            DemandeLien demande = _fixture.Create<DemandeLien>();

            // When deux visiteurs proposent le même nom
            await _controller.DemanderLienTeleversement(demande);
            string? premier = nomSigne;
            await _controller.DemanderLienTeleversement(demande);

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
            LienLecture lien = _fixture.Create<LienLecture>();
            _magasin.Setup(m => m.LienLectureAsync()).ReturnsAsync(lien);

            // When
            ActionResult<LienLecture> resultat = await _controller.DemanderLienLecture();

            // Then une seule signature sert à toutes les images d'une page
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(lien, reponse.Value);
            _magasin.Verify(m => m.LienLectureAsync(), Times.Once);
        }

        [Fact]
        public async Task Deposer_RendNonTrouveQuandLesOctetsVontDirectementAuConteneur()
        {
            // Given un magasin qui ne reçoit pas les octets
            _magasin.Setup(m => m.RecoitLesOctets).Returns(false);

            // When
            IActionResult resultat = await _controller.Deposer(_fixture.Create<DemandeLien>().NomFichier);

            // Then la route n'existe pas pour l'appelant
            Assert.IsType<NotFoundResult>(resultat);
            _magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_RefuseUnContenuVide()
        {
            // Given un dépôt annoncé sans octets
            _magasin.Setup(m => m.RecoitLesOctets).Returns(true);
            _contexte.Request.ContentLength = 0;

            // When
            IActionResult resultat = await _controller.Deposer(_fixture.Create<DemandeLien>().NomFichier);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
            _magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_RefuseUnNomQuiSortDuDossierDesImages()
        {
            // Given un magasin qui reçoit les octets
            _magasin.Setup(m => m.RecoitLesOctets).Returns(true);

            // When le nom désigne un autre endroit
            IActionResult resultat = await _controller.Deposer("../../appsettings.json");

            // Then rien n'est écrit
            Assert.IsType<BadRequestObjectResult>(resultat);
            _magasin.Verify(m => m.EnregistrerAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        }

        [Fact]
        public async Task Deposer_ConfieLesOctetsAuMagasin()
        {
            // Given un dépôt qui porte des octets
            string nomFichier = _fixture.Create<DemandeLien>().NomFichier;
            byte[] contenu = _fixture.Create<byte[]>();

            _magasin.Setup(m => m.RecoitLesOctets).Returns(true);
            _contexte.Request.Body = new MemoryStream(contenu);
            _contexte.Request.ContentLength = contenu.Length;

            // When
            IActionResult resultat = await _controller.Deposer(nomFichier);

            // Then
            Assert.IsType<NoContentResult>(resultat);
            _magasin.Verify(m => m.EnregistrerAsync(nomFichier, _contexte.Request.Body), Times.Once);
        }
    }
}
