using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using OneFidelite.API.Controllers;
using OneFidelite.API.Models;

namespace OneFidelite.API.Tests.Controllers
{
    public class FideliteControllerTests
    {
        [Fact]
        public void GetAllFidelites_RendLesCartes()
        {
            // Given le contrôleur et son fichier de clients
            FideliteController controleur = Controleur(Generateur());

            // When
            ActionResult<IEnumerable<Fidelite>> resultat = controleur.GetAllFidelites();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Fidelite> fidelites = Assert.IsType<IEnumerable<Fidelite>>(reponse.Value, exactMatch: false);
            Assert.NotEmpty(fidelites);
        }

        [Fact]
        public void GetFideliteByNumero_RendLaCarteDemandee()
        {
            // Given un numéro que le fichier porte
            FideliteController controleur = Controleur(Generateur());
            string numero = Fichier(controleur).First().NumeroFidelite;

            // When
            ActionResult<Fidelite> resultat = controleur.GetFideliteByNumero(numero);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Fidelite fidelite = Assert.IsType<Fidelite>(reponse.Value);
            Assert.Equal(numero, fidelite.NumeroFidelite);
        }

        [Fact]
        public void GetFideliteByNumero_RendNonTrouveQuandLeNumeroNExistePas()
        {
            // Given un numéro qu'aucune carte ne porte
            IFixture generateur = Generateur();
            FideliteController controleur = Controleur(generateur);

            // When
            ActionResult<Fidelite> resultat = controleur.GetFideliteByNumero(generateur.Create<string>());

            // Then
            Assert.IsType<NotFoundResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_RefuseUneCarteInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            FideliteController controleur = Controleur(generateur);
            controleur.ModelState.AddModelError(nameof(Fidelite.CourrielClient), "Le courriel est obligatoire");

            // When
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(generateur.Create<Fidelite>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_DonneUnNumeroUneDateEtUnIdentifiant()
        {
            // Given une inscription complète
            IFixture generateur = Generateur();
            FideliteController controleur = Controleur(generateur);

            // When
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(generateur.Create<Fidelite>());

            // Then la carte revient avec ce que le service a posé
            CreatedAtActionResult creee = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Fidelite fidelite = Assert.IsType<Fidelite>(creee.Value);
            Assert.StartsWith("ONE-", fidelite.NumeroFidelite, StringComparison.Ordinal);
            Assert.Equal(DateTime.UtcNow.Date, fidelite.DateCreation.Date);
            Assert.True(fidelite.Id > 0);
        }

        [Fact]
        public void CreateFidelite_RefuseUnCourrielDejaInscritQuelleQueSoitSaCasse()
        {
            // Given un courriel déjà porté par une carte
            IFixture generateur = Generateur();
            FideliteController controleur = Controleur(generateur);

            Fidelite inscription = generateur.Create<Fidelite>();
            controleur.CreateFidelite(inscription);

            Fidelite memeCourrielEnMajuscules = generateur.Build<Fidelite>()
                .With(f => f.CourrielClient, inscription.CourrielClient.ToUpperInvariant())
                .Create();

            // When le même courriel revient en majuscules
            ActionResult<Fidelite> resultat = controleur.CreateFidelite(memeCourrielEnMajuscules);

            // Then c'est le même client, et il n'est pas inscrit deux fois
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }

        private static IFixture Generateur() =>
            new Fixture().Customize(new AutoMoqCustomization());

        private static FideliteController Controleur(IFixture generateur) =>
            generateur.Build<FideliteController>().OmitAutoProperties().Create();

        private static IEnumerable<Fidelite> Fichier(FideliteController controleur)
        {
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(controleur.GetAllFidelites().Result);

            return Assert.IsType<IEnumerable<Fidelite>>(reponse.Value, exactMatch: false);
        }
    }
}
