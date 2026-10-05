using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using OneFidelite.API.Controllers;
using OneFidelite.API.Models;

namespace OneFidelite.API.Tests.Controllers
{
    public class FideliteControllerTests
    {
        private readonly IFixture _fixture;
        private readonly FideliteController _controller;

        public FideliteControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _controller = _fixture.Build<FideliteController>().OmitAutoProperties().Create();
        }

        private IEnumerable<Fidelite> Fichier()
        {
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(_controller.GetAllFidelites().Result);

            return Assert.IsType<IEnumerable<Fidelite>>(reponse.Value, exactMatch: false);
        }

        [Fact]
        public void GetAllFidelites_RendLesCartes()
        {
            // Given le contrôleur et son fichier de clients

            // When
            ActionResult<IEnumerable<Fidelite>> resultat = _controller.GetAllFidelites();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Fidelite> fidelites = Assert.IsType<IEnumerable<Fidelite>>(reponse.Value, exactMatch: false);
            Assert.NotEmpty(fidelites);
        }

        [Fact]
        public void GetFideliteByNumero_RendLaCarteDemandee()
        {
            // Given un numéro que le fichier porte
            string numero = Fichier().First().NumeroFidelite;

            // When
            ActionResult<Fidelite> resultat = _controller.GetFideliteByNumero(numero);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Fidelite fidelite = Assert.IsType<Fidelite>(reponse.Value);
            Assert.Equal(numero, fidelite.NumeroFidelite);
        }

        [Fact]
        public void GetFideliteByNumero_RendNonTrouveQuandLeNumeroNExistePas()
        {
            // Given un numéro qu'aucune carte ne porte

            // When
            ActionResult<Fidelite> resultat = _controller.GetFideliteByNumero(_fixture.Create<string>());

            // Then
            Assert.IsType<NotFoundResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_RefuseUneCarteInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Fidelite.CourrielClient), "Le courriel est obligatoire");

            // When
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(_fixture.Create<Fidelite>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void CreateFidelite_DonneUnNumeroUneDateEtUnIdentifiant()
        {
            // Given une inscription complète

            // When
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(_fixture.Create<Fidelite>());

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
            Fidelite inscription = _fixture.Create<Fidelite>();
            _controller.CreateFidelite(inscription);

            Fidelite memeCourrielEnMajuscules = _fixture.Build<Fidelite>()
                .With(f => f.CourrielClient, inscription.CourrielClient.ToUpperInvariant())
                .Create();

            // When le même courriel revient en majuscules
            ActionResult<Fidelite> resultat = _controller.CreateFidelite(memeCourrielEnMajuscules);

            // Then c'est le même client, et il n'est pas inscrit deux fois
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }
    }
}
