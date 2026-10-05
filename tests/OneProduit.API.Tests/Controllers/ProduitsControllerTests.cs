using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using OneProduit.API.Controllers;
using OneProduit.API.Models;

namespace OneProduit.API.Tests.Controllers
{
    public class ProduitsControllerTests
    {
        // Le catalogue du contrôleur est statique, et la fixture de chaque
        // test repart de un : sans compteur partagé, deux tests ajoutent des
        // produits qui portent le même identifiant.
        private static int _dernierIdentifiant = 1000;

        private readonly IFixture _fixture;
        private readonly ProduitsController _controller;

        public ProduitsControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Le modèle n'accepte qu'un nom sans chiffre et une image .png ou
            // .jpg, donc la fixture rend d'emblée des produits que sa propre
            // validation accepte. Tout le reste est tiré par le générateur.
            _fixture.Customize<Produit>(produit => produit
                .With(p => p.Id, () => Interlocked.Increment(ref _dernierIdentifiant))
                .With(p => p.Nom, Lettres)
                .With(p => p.Image, () => $"{Lettres()}.png"));

            _controller = _fixture.Build<ProduitsController>().OmitAutoProperties().Create();
        }

        private static string Lettres() =>
            new(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray());

        private static List<ValidationResult> Valider(Produit produit)
        {
            var resultats = new List<ValidationResult>();
            Validator.TryValidateObject(produit, new ValidationContext(produit), resultats, true);

            return resultats;
        }

        private IEnumerable<Produit> Catalogue()
        {
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(_controller.GetProduits().Result);

            return Assert.IsType<IEnumerable<Produit>>(reponse.Value, exactMatch: false);
        }

        private int IdentifiantAbsentDuCatalogue() => Catalogue().Max(p => p.Id!.Value) + 1;

        [Fact]
        public void GetProduits_RendLeCatalogue()
        {
            // Given le contrôleur et son catalogue

            // When
            ActionResult<IEnumerable<Produit>> resultat = _controller.GetProduits();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Produit> produits = Assert.IsType<IEnumerable<Produit>>(reponse.Value, exactMatch: false);
            Assert.NotEmpty(produits);
        }

        [Fact]
        public void GetProduit_RendLeProduitDemande()
        {
            // Given un produit que le catalogue porte
            Produit ajoute = _fixture.Create<Produit>();
            _controller.AddProduit(ajoute);

            // When
            ActionResult<Produit> resultat = _controller.GetProduit(ajoute.Id!.Value);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(ajoute.Id, produit.Id);
        }

        [Fact]
        public void GetProduit_RendNonTrouveQuandLIdentifiantNExistePas()
        {
            // Given un identifiant absent du catalogue

            // When
            ActionResult<Produit> resultat = _controller.GetProduit(IdentifiantAbsentDuCatalogue());

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RefuseUnProduitInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            ActionResult<Produit> resultat = _controller.AddProduit(_fixture.Create<Produit>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RendLAdresseOuRelireLeProduit()
        {
            // Given un produit complet
            Produit ajoute = _fixture.Create<Produit>();

            // When
            ActionResult<Produit> resultat = _controller.AddProduit(ajoute);

            // Then
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(cree.Value);
            Assert.Equal(ajoute.Nom, produit.Nom);
            Assert.Equal(ajoute.Id, cree.RouteValues!["id"]);
        }

        [Fact]
        public void UpdateProduit_ReecritLeProduitSansToucherALImage()
        {
            // Given un produit du catalogue, et une modification sans image
            Produit ajoute = _fixture.Create<Produit>();
            _controller.AddProduit(ajoute);

            Produit modification = _fixture.Build<Produit>()
                .With(p => p.Image, string.Empty)
                .With(p => p.Vedette, true)
                .Create();

            // When
            ActionResult<Produit> resultat = _controller.UpdateProduit(ajoute.Id!.Value, modification);

            // Then les champs changent et l'image reste celle d'avant
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(modification.Nom, produit.Nom);
            Assert.Equal(modification.Prix, produit.Prix);
            Assert.Equal(modification.Quantite, produit.Quantite);
            Assert.True(produit.Vedette);
            Assert.Equal(ajoute.Image, produit.Image);
        }

        [Fact]
        public void UpdateProduit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un identifiant absent du catalogue

            // When
            ActionResult<Produit> resultat =
                _controller.UpdateProduit(IdentifiantAbsentDuCatalogue(), _fixture.Create<Produit>());

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void UpdateProduit_RefuseUnProduitInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            ActionResult<Produit> resultat =
                _controller.UpdateProduit(_fixture.Create<Produit>().Id!.Value, _fixture.Create<Produit>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void DeleteProduit_RetireLeProduitDuCatalogue()
        {
            // Given un produit que le catalogue porte
            Produit ajoute = _fixture.Create<Produit>();
            _controller.AddProduit(ajoute);

            // When
            IActionResult resultat = _controller.DeleteProduit(ajoute.Id!.Value);

            // Then il ne s'y trouve plus
            Assert.IsType<NoContentResult>(resultat);
            Assert.IsType<NotFoundObjectResult>(_controller.GetProduit(ajoute.Id!.Value).Result);
        }

        [Fact]
        public void DeleteProduit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un identifiant absent du catalogue

            // When
            IActionResult resultat = _controller.DeleteProduit(IdentifiantAbsentDuCatalogue());

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat);
        }

        [Theory]
        [InlineData("fr-CA")]
        [InlineData("en-CA")]
        [InlineData("")]
        public void UnPrixAvecDesCentimesEstAccepteQuelleQueSoitLaCultureDuServeur(string culture)
        {
            // Given un prix à centimes, et un serveur dont la culture n'est pas
            // celle du poste
            Produit produit = _fixture.Build<Produit>()
                .With(p => p.Prix, 30.50m)
                .Create();

            CultureInfo precedente = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            try
            {
                // When le modèle est validé
                List<ValidationResult> erreurs = Valider(produit);

                // Then aucune erreur ne porte sur le prix
                Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Prix)));
            }
            finally
            {
                CultureInfo.CurrentCulture = precedente;
            }
        }

        [Fact]
        public void UneImagePngEstAcceptee()
        {
            // Given un produit dont l'image porte une extension connue
            Produit produit = _fixture.Create<Produit>();

            // When le modèle est validé
            List<ValidationResult> erreurs = Valider(produit);

            // Then
            Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Image)));
        }
    }
}
