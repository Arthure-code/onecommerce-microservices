using Microsoft.AspNetCore.Mvc;
using OneFidelite.API.Controllers;
using OneFidelite.API.Models;

namespace OneFidelite.API.Tests.Controllers
{
    public class FideliteControllerTests
    {
        private readonly FideliteController _controller;
        private readonly string _numeroAuFichier;
        private readonly string _numeroAbsentDuFichier;
        private readonly Fidelite _inscription;
        private readonly Fidelite _autreInscription;
        private readonly Fidelite _memeCourrielEnMajuscules;

        public FideliteControllerTests()
        {
            _controller = new FideliteController();

            _numeroAuFichier = "ONE-100001";
            _numeroAbsentDuFichier = "ONE-000000";

            _inscription = new Fidelite
            {
                NomClient = "Denis Girard",
                CourrielClient = "denis.girard@example.com",
                Telephone = "514-555-0199"
            };

            _autreInscription = new Fidelite
            {
                NomClient = "Hélène Roy",
                CourrielClient = "helene.roy@example.com",
                Telephone = "418-555-0144"
            };

            _memeCourrielEnMajuscules = new Fidelite
            {
                NomClient = "Denis Girard",
                CourrielClient = "DENIS.GIRARD@EXAMPLE.COM",
                Telephone = "514-555-0199"
            };
        }

        [Fact]
        public void GetAllFidelites_RendLesCartes()
        {
            // Given le contrôleur et son fichier de clients

            // When
            ActionResult<IEnumerable<Fidelite>> resultat = _controller.GetAllFidelites();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Fidelite> fidelites = Assert.IsAssignableFrom<IEnumerable<Fidelite>>(reponse.Value);
            Assert.NotEmpty(fidelites);
        }

        [Fact]
        public void GetFideliteByNumero_RendLaCarteDemandee()
        {
            // Given un numéro que le fichier porte

            // When
            ActionResult<Fidelite> resultat = _controller.GetFideliteByNumero(_numeroAuFichier);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Fidelite fidelite = Assert.IsType<Fidelite>(reponse.Value);
            Assert.Equal(_numeroAuFichier, fidelite.NumeroFidelite);
        }

        [Fact]
        public void GetFideliteByNumero_RendNonTrouveQuandLeNumeroNExistePas()
        {
            // Given un numéro qu'aucune carte ne porte

            // When
            ActionResult<Fidelite> resultat = _controller.GetFideliteByNumero(_numeroAbsentDuFichier);

            // Then
            Assert.IsType<NotFoundResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_RefuseUneCarteInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Fidelite.CourrielClient), "Le courriel est obligatoire");

            // When
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(_inscription);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_DonneUnNumeroUneDateEtUnIdentifiant()
        {
            // Given une inscription complète

            // When
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(_autreInscription);

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
            _controller.CreateFidelite(_inscription);

            // When le même courriel revient en majuscules
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(_memeCourrielEnMajuscules);

            // Then c'est le même client, et il n'est pas inscrit deux fois
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }
    }
}
