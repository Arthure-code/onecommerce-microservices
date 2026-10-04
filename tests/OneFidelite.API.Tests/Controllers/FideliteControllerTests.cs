using Microsoft.AspNetCore.Mvc;
using OneFidelite.API.Controllers;
using OneFidelite.API.Models;

namespace OneFidelite.API.Tests.Controllers
{
    public class FideliteControllerTests
    {
        private static Fidelite UneFidelite(string courriel) => new Fidelite
        {
            NomClient = "Denis Girard",
            CourrielClient = courriel,
            Telephone = "514-555-0199"
        };

        private static string UnCourrielUnique() => $"client-{Guid.NewGuid():N}@example.com";

        [Fact]
        public void GetAllFidelites_RendLesCartes()
        {
            //Etant donné le contrôleur
            var controleur = new FideliteController();

            //Lorsque
            ActionResult<IEnumerable<Fidelite>> resultat = controleur.GetAllFidelites();

            //Alors
            var reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            var fidelites = Assert.IsAssignableFrom<IEnumerable<Fidelite>>(reponse.Value);
            Assert.NotEmpty(fidelites);
        }

        [Fact]
        public void GetFideliteByNumero_RendLaCarteDemandee()
        {
            //Etant donné un numéro qui existe
            var controleur = new FideliteController();

            //Lorsque
            ActionResult<Fidelite> resultat = controleur.GetFideliteByNumero("ONE-100001");

            //Alors
            var reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            var fidelite = Assert.IsType<Fidelite>(reponse.Value);
            Assert.Equal("Alice Dupont", fidelite.NomClient);
        }

        [Fact]
        public void GetFideliteByNumero_RendNonTrouveQuandLeNumeroNExistePas()
        {
            //Etant donné un numéro qu'aucune carte ne porte
            var controleur = new FideliteController();

            //Lorsque
            ActionResult<Fidelite> resultat = controleur.GetFideliteByNumero("ONE-000000");

            //Alors
            Assert.IsType<NotFoundResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_RefuseUneCarteInvalide()
        {
            //Etant donné un modèle que la validation a rejeté
            var controleur = new FideliteController();
            controleur.ModelState.AddModelError("CourrielClient", "Le courriel est obligatoire");

            //Lorsque
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(UneFidelite(UnCourrielUnique()));

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_DonneUnNumeroEtUneDate()
        {
            //Etant donné une carte complète
            var controleur = new FideliteController();

            //Lorsque
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(UneFidelite(UnCourrielUnique()));

            //Alors la carte revient avec ce que le service a posé
            var creee = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            var fidelite = Assert.IsType<Fidelite>(creee.Value);
            Assert.StartsWith("ONE-", fidelite.NumeroFidelite, StringComparison.Ordinal);
            Assert.Equal(DateTime.UtcNow.Date, fidelite.DateCreation.Date);
            Assert.True(fidelite.Id > 0);
        }

        [Fact]
        public void CreateFidelite_RefuseUnCourrielDejaInscrit()
        {
            //Etant donné un courriel déjà porté par une carte
            var controleur = new FideliteController();
            string courriel = UnCourrielUnique();
            controleur.CreateFidelite(UneFidelite(courriel));

            //Lorsqu'une seconde carte prétend au même courriel
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(UneFidelite(courriel));

            //Alors
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_IgnoreLaCasseDuCourriel()
        {
            //Etant donné un courriel déjà inscrit en minuscules
            var controleur = new FideliteController();
            string courriel = UnCourrielUnique();
            controleur.CreateFidelite(UneFidelite(courriel));

            //Lorsque le même courriel revient en majuscules
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(UneFidelite(courriel.ToUpperInvariant()));

            //Alors c'est le même client
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_DonneUnNumeroDifferentAChaqueCarte()
        {
            //Etant donné deux inscriptions de suite
            var controleur = new FideliteController();

            //Lorsque
            var premiere = (CreatedAtActionResult)controleur.CreateFidelite(UneFidelite(UnCourrielUnique())).Result!;
            var seconde = (CreatedAtActionResult)controleur.CreateFidelite(UneFidelite(UnCourrielUnique())).Result!;

            //Alors
            var une = Assert.IsType<Fidelite>(premiere.Value);
            var autre = Assert.IsType<Fidelite>(seconde.Value);
            Assert.NotEqual(une.NumeroFidelite, autre.NumeroFidelite);
            Assert.NotEqual(une.Id, autre.Id);
        }
    }
}
