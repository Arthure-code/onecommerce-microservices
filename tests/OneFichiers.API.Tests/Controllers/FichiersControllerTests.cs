using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneFichiers.API.Controllers;
using OneFichiers.API.Models;

namespace OneFichiers.API.Tests.Controllers
{
    public class FichiersControllerTests
    {
        // Un pixel transparent, le plus petit contenu valide qui soit.
        private const string UnPngEnBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        private static FichiersController UnControleur()
        {
            return new FichiersController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task Upload_RefuseUnCorpsAbsent()
        {
            //Etant donné aucune requête
            FichiersController controleur = UnControleur();

            //Lorsque
            IActionResult resultat = await controleur.Upload(null!);

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Fact]
        public async Task Upload_RefuseUnContenuVide()
        {
            //Etant donné un fichier sans contenu
            FichiersController controleur = UnControleur();

            //Lorsque
            IActionResult resultat = await controleur.Upload(new Fichier
            {
                NomFichier = "image.png",
                FichierBase64 = "   "
            });

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Theory]
        [InlineData("../../appsettings.json")]
        [InlineData("..\\..\\appsettings.json")]
        [InlineData("/etc/passwd")]
        [InlineData("C:\\Windows\\System32\\drivers\\etc\\hosts")]
        [InlineData("sous-dossier/image.png")]
        public async Task Upload_RefuseUnNomQuiSortDuDossierDesImages(string nomFichier)
        {
            //Etant donné un nom de fichier qui désigne un autre endroit
            FichiersController controleur = UnControleur();

            //Lorsque
            IActionResult resultat = await controleur.Upload(new Fichier
            {
                NomFichier = nomFichier,
                FichierBase64 = UnPngEnBase64
            });

            //Alors rien n'est écrit, et l'appelant lit pourquoi
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Theory]
        [InlineData("archive.zip")]
        [InlineData("script.exe")]
        [InlineData("page.html")]
        [InlineData("sans-extension")]
        public async Task Upload_RefuseCeQuiNEstPasUneImage(string nomFichier)
        {
            //Etant donné un nom de fichier qui n'est pas une image
            FichiersController controleur = UnControleur();

            //Lorsque
            IActionResult resultat = await controleur.Upload(new Fichier
            {
                NomFichier = nomFichier,
                FichierBase64 = UnPngEnBase64
            });

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat);
        }

        [Fact]
        public async Task Upload_RefuseUnContenuQuiNEstPasDuBase64()
        {
            //Etant donné un contenu qui ne se décode pas
            FichiersController controleur = UnControleur();

            //Lorsque
            IActionResult resultat = await controleur.Upload(new Fichier
            {
                NomFichier = "image.png",
                FichierBase64 = "ceci n'est pas du base64 !!!"
            });

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat);
        }
    }
}
