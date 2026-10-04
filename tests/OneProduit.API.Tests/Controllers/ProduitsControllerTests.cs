using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OneProduit.API.Controllers;
using OneProduit.API.Models;

namespace OneProduit.API.Tests.Controllers
{
    public class ProduitsControllerTests
    {
        private readonly ProduitsController _controller;
        private readonly Produit _produit;
        private readonly int _identifiantAuCatalogue;
        private readonly int _identifiantAbsentDuCatalogue;

        public ProduitsControllerTests()
        {
            _controller = new ProduitsController();

            _produit = new Produit
            {
                Id = 99,
                Nom = "Polo blanc",
                Description = "Polo blanc homme taille L",
                Prix = 30.50m,
                Quantite = 3,
                Image = "image1.png",
                Vedette = false
            };

            _identifiantAuCatalogue = 3;
            _identifiantAbsentDuCatalogue = 9999;
        }

        private static IList<ValidationResult> Valider(Produit produit)
        {
            var resultats = new List<ValidationResult>();
            Validator.TryValidateObject(produit, new ValidationContext(produit), resultats, true);

            return resultats;
        }

        [Fact]
        public void GetProduits_RendLeCatalogue()
        {
            // Given le contrôleur et son catalogue

            // When
            ActionResult<IEnumerable<Produit>> resultat = _controller.GetProduits();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Produit> produits = Assert.IsAssignableFrom<IEnumerable<Produit>>(reponse.Value);
            Assert.NotEmpty(produits);
        }

        [Fact]
        public void GetProduit_RendLeProduitDemande()
        {
            // Given un identifiant que le catalogue porte

            // When
            ActionResult<Produit> resultat = _controller.GetProduit(_identifiantAuCatalogue);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(_identifiantAuCatalogue, produit.Id);
        }

        [Fact]
        public void GetProduit_RendNonTrouveQuandLIdentifiantNExistePas()
        {
            // Given un identifiant absent du catalogue

            // When
            ActionResult<Produit> resultat = _controller.GetProduit(_identifiantAbsentDuCatalogue);

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RefuseUnProduitInvalide()
        {
            // Given un modèle que la validation a rejeté
            _controller.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            ActionResult<Produit> resultat = _controller.AddProduit(_produit);

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RendLAdresseOuRelireLeProduit()
        {
            // Given un produit complet

            // When
            ActionResult<Produit> resultat = _controller.AddProduit(_produit);

            // Then
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(cree.Value);
            Assert.Equal(_produit.Nom, produit.Nom);
            Assert.Equal(_produit.Id, cree.RouteValues!["id"]);
        }

        [Theory]
        [InlineData("fr-CA")]
        [InlineData("en-CA")]
        [InlineData("")]
        public void UnPrixAvecDesCentimesEstAccepteQuelleQueSoitLaCultureDuServeur(string culture)
        {
            // Given un serveur dont la culture n'est pas celle du poste
            CultureInfo precedente = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            try
            {
                // When le modèle est validé
                IList<ValidationResult> erreurs = Valider(_produit);

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

            // When le modèle est validé
            IList<ValidationResult> erreurs = Valider(_produit);

            // Then
            Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Image)));
        }
    }
}
