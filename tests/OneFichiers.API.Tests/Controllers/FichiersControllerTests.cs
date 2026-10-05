using System.Text;
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
        private readonly Mock<IMagasinImages> _magasin;
        private readonly FichiersController _controller;
        private readonly DefaultHttpContext _contexte;
        private readonly string _nomAccepte;
        private readonly LienTeleversement _lienTeleversement;
        private readonly LienLecture _lienLecture;
        private readonly byte[] _contenu;

        public FichiersControllerTests()
        {
            _magasin = new Mock<IMagasinImages>();
            _contexte = new DefaultHttpContext();
            _controller = new FichiersController(_magasin.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _contexte }
            };

            _nomAccepte = "Image3.png";

            _lienTeleversement = new LienTeleversement(
                "https://stone.blob.core.windows.net/images/a1b2c3.png?sig=signature",
                "a1b2c3.png",
                DateTimeOffset.UtcNow.AddMinutes(15));

            _lienLecture = new LienLecture(
                "https://stone.blob.core.windows.net/images",
                "?sig=signature",
                DateTimeOffset.UtcNow.AddMinutes(30));

            _contenu = Encoding.UTF8.GetBytes("des octets d'image");
        }

        [Fact]
        public async Task DemanderLienTeleversement_RefuseUnCorpsAbsent()
        {
            // Given aucune demande

            // When
            ActionResult<LienTeleversement> resultat = await _controller.DemanderLienTeleversement(null);

            // Then rien n'est demandé au magasin
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
            var demande = new DemandeLien { NomFichier = nomFichier };

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
            var demande = new DemandeLien { NomFichier = nomFichier };

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
            _magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .ReturnsAsync(_lienTeleversement);

            // When
            ActionResult<LienTeleversement> resultat =
                await _controller.DemanderLienTeleversement(new DemandeLien { NomFichier = _nomAccepte });

            // Then le lien part tel quel, l'API ne porte aucun octet
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(_lienTeleversement, reponse.Value);
            _magasin.Verify(m => m.LienTeleversementAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DemanderLienTeleversement_NeReprendPasLeNomProposeMaisGardeSonExtension()
        {
            // Given un magasin qui signe ce qu'on lui passe
            string? nomSigne = null;
            _magasin
                .Setup(m => m.LienTeleversementAsync(It.IsAny<string>()))
                .Callback<string>(n => nomSigne = n)
                .ReturnsAsync(_lienTeleversement);

            // When deux visiteurs proposent le même nom
            await _controller.DemanderLienTeleversement(new DemandeLien { NomFichier = _nomAccepte });
            string? premier = nomSigne;
            await _controller.DemanderLienTeleversement(new DemandeLien { NomFichier = _nomAccepte });

            // Then aucun des deux n'écrase l'image de l'autre
            Assert.NotNull(premier);
            Assert.NotEqual(premier, nomSigne);
            Assert.NotEqual(_nomAccepte, nomSigne);
            Assert.EndsWith(".png", nomSigne, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderLienLecture_RendLaBaseEtLaSignature()
        {
            // Given un magasin qui signe une lecture
            _magasin.Setup(m => m.LienLectureAsync()).ReturnsAsync(_lienLecture);

            // When
            ActionResult<LienLecture> resultat = await _controller.DemanderLienLecture();

            // Then une seule signature sert à toutes les images d'une page
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Assert.Same(_lienLecture, reponse.Value);
            _magasin.Verify(m => m.LienLectureAsync(), Times.Once);
        }

        [Fact]
        public async Task Deposer_RendNonTrouveQuandLesOctetsVontDirectementAuConteneur()
        {
            // Given un magasin qui ne reçoit pas les octets
            _magasin.Setup(m => m.RecoitLesOctets).Returns(false);

            // When
            IActionResult resultat = await _controller.Deposer(_nomAccepte);

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
            IActionResult resultat = await _controller.Deposer(_nomAccepte);

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
            _magasin.Setup(m => m.RecoitLesOctets).Returns(true);
            _contexte.Request.Body = new MemoryStream(_contenu);
            _contexte.Request.ContentLength = _contenu.Length;

            // When
            IActionResult resultat = await _controller.Deposer(_nomAccepte);

            // Then
            Assert.IsType<NoContentResult>(resultat);
            _magasin.Verify(m => m.EnregistrerAsync(_nomAccepte, _contexte.Request.Body), Times.Once);
        }
    }
}
