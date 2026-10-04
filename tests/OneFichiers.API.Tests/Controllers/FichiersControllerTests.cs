using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneFichiers.API.Controllers;
using OneFichiers.API.Models;

namespace OneFichiers.API.Tests.Controllers
{
    public class FichiersControllerTests
    {
        // Un pixel transparent : le plus petit contenu d'image qui soit valide.
        private const string UnPngEnBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        private readonly FichiersController _controller;
        private readonly Fichier _fichierSansContenu;
        private readonly Fichier _fichierIllisible;

        public FichiersControllerTests()
        {
            _controller = new FichiersController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            _fichierSansContenu = new Fichier
            {
                NomFichier = "image.png",
                FichierBase64 = "   "
            };

            _fichierIllisible = new Fichier
            {
                NomFichier = "image.png",
                FichierBase64 = "ceci n'est pas du base64 !!!"
            };
        }

        [Fact]
        public async Task Upload_RefuseUnCorpsAbsent()
        {
            // Given aucune requête

            // When
            IActionResult resultat = await _controller.Upload(null!);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Fact]
        public async Task Upload_RefuseUnContenuVide()
        {
            // Given un fichier annoncé sans contenu

            // When
            IActionResult resultat = await _controller.Upload(_fichierSansContenu);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Fact]
        public async Task Upload_RefuseUnContenuQuiNEstPasDuBase64()
        {
            // Given un contenu qui ne se décode pas

            // When
            IActionResult resultat = await _controller.Upload(_fichierIllisible);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Theory]
        [InlineData("../../appsettings.json")]
        [InlineData("..\\..\\appsettings.json")]
        [InlineData("/etc/passwd")]
        [InlineData("C:\\Windows\\System32\\drivers\\etc\\hosts")]
        [InlineData("sous-dossier/image.png")]
        public async Task Upload_RefuseUnNomQuiDesigneUnAutreEndroitQueLeDossierDesImages(string nomFichier)
        {
            // Given un nom de fichier qui sort du dossier des images
            var fichier = new Fichier
            {
                NomFichier = nomFichier,
                FichierBase64 = UnPngEnBase64
            };

            // When
            IActionResult resultat = await _controller.Upload(fichier);

            // Then rien n'est écrit, et l'appelant lit pourquoi
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Theory]
        [InlineData("archive.zip")]
        [InlineData("script.exe")]
        [InlineData("page.html")]
        [InlineData("sans-extension")]
        public async Task Upload_RefuseCeQuiNEstPasUneImage(string nomFichier)
        {
            // Given un nom de fichier qui n'annonce pas une image
            var fichier = new Fichier
            {
                NomFichier = nomFichier,
                FichierBase64 = UnPngEnBase64
            };

            // When
            IActionResult resultat = await _controller.Upload(fichier);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat);
        }
    }
}
