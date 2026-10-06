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
        [Fact]
        public void GetProduits_RendLeCatalogue()
        {
            // Given le contrôleur et son catalogue
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);

            // When
            ActionResult<IEnumerable<Produit>> resultat = controleur.GetProduits();

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            IEnumerable<Produit> produits = Assert.IsType<IEnumerable<Produit>>(reponse.Value, exactMatch: false);
            Assert.NotEmpty(produits);
        }

        [Fact]
        public void GetProduit_RendLeProduitDemande()
        {
            // Given un produit que le catalogue porte
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            Produit ajoute = ProduitAbsentDuCatalogue(generateur, controleur);
            controleur.AddProduit(ajoute);

            // When
            ActionResult<Produit> resultat = controleur.GetProduit(ajoute.Id!.Value);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(ajoute.Id, produit.Id);
        }

        [Fact]
        public void GetProduit_RendNonTrouveQuandLIdentifiantNExistePas()
        {
            // Given un identifiant absent du catalogue
            ProduitsController controleur = Controleur(Generateur());

            // When
            ActionResult<Produit> resultat = controleur.GetProduit(IdentifiantAbsent(controleur));

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RefuseUnProduitInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            controleur.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            ActionResult<Produit> resultat = controleur.AddProduit(generateur.Create<Produit>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RendLAdresseOuRelireLeProduit()
        {
            // Given un produit complet
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            Produit ajoute = ProduitAbsentDuCatalogue(generateur, controleur);

            // When
            ActionResult<Produit> resultat = controleur.AddProduit(ajoute);

            // Then
            CreatedAtActionResult cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(cree.Value);
            Assert.Equal(ajoute.Nom, produit.Nom);
            Assert.Equal(ajoute.Id, cree.RouteValues!["id"]);
        }

        [Fact]
        public void UpdateProduit_ReecritLeProduitAvecCeQuiEstEnvoye()
        {
            // Given un produit du catalogue, et une modification complète
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            Produit ajoute = ProduitAbsentDuCatalogue(generateur, controleur);
            controleur.AddProduit(ajoute);

            Produit modification = generateur.Build<Produit>()
                .With(p => p.Vedette, true)
                .Create();

            // When
            ActionResult<Produit> resultat = controleur.UpdateProduit(ajoute.Id!.Value, modification);

            // Then
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            Produit produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(modification.Nom, produit.Nom);
            Assert.Equal(modification.Prix, produit.Prix);
            Assert.Equal(modification.Quantite, produit.Quantite);
            Assert.Equal(modification.Image, produit.Image);
            Assert.True(produit.Vedette);
        }

        [Fact]
        public void UpdateProduit_RefuseUneImageAbsente()
        {
            // Given une modification dont l'image est vide
            Produit modification = Generateur().Build<Produit>()
                .With(p => p.Image, string.Empty)
                .Create();

            // When le modèle est validé, comme le fait la liaison HTTP
            List<ValidationResult> erreurs = Valider(modification);

            // Then la requête n'atteint pas le contrôleur
            Assert.Contains(erreurs, e => e.MemberNames.Contains(nameof(Produit.Image)));
        }

        [Fact]
        public void UpdateProduit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un identifiant absent du catalogue
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);

            // When
            ActionResult<Produit> resultat =
                controleur.UpdateProduit(IdentifiantAbsent(controleur), generateur.Create<Produit>());

            // Then
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void UpdateProduit_RefuseUnProduitInvalide()
        {
            // Given un modèle que la validation a rejeté
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            controleur.ModelState.AddModelError(nameof(Produit.Nom), "Le nom est obligatoire");

            // When
            ActionResult<Produit> resultat =
                controleur.UpdateProduit(IdentifiantAbsent(controleur), generateur.Create<Produit>());

            // Then
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Fact]
        public void DeleteProduit_RetireLeProduitDuCatalogue()
        {
            // Given un produit que le catalogue porte
            IFixture generateur = Generateur();
            ProduitsController controleur = Controleur(generateur);
            Produit ajoute = ProduitAbsentDuCatalogue(generateur, controleur);
            controleur.AddProduit(ajoute);

            // When
            IActionResult resultat = controleur.DeleteProduit(ajoute.Id!.Value);

            // Then il ne s'y trouve plus
            Assert.IsType<NoContentResult>(resultat);
            Assert.IsType<NotFoundObjectResult>(controleur.GetProduit(ajoute.Id!.Value).Result);
        }

        [Fact]
        public void DeleteProduit_RendNonTrouveQuandLeProduitNExistePas()
        {
            // Given un identifiant absent du catalogue
            ProduitsController controleur = Controleur(Generateur());

            // When
            IActionResult resultat = controleur.DeleteProduit(IdentifiantAbsent(controleur));

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
            Produit produit = Generateur().Build<Produit>()
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
            Produit produit = Generateur().Create<Produit>();

            // When le modèle est validé
            List<ValidationResult> erreurs = Valider(produit);

            // Then
            Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Image)));
        }

        // Le modèle n'accepte qu'un nom sans chiffre et une image .png ou .jpg,
        // donc le générateur rend d'emblée des produits que sa propre validation
        // accepte. Tout le reste est tiré au hasard.
        private static IFixture Generateur()
        {
            IFixture generateur = new Fixture().Customize(new AutoMoqCustomization());

            generateur.Customize<Produit>(produit => produit
                .With(p => p.Nom, Lettres)
                .With(p => p.Image, () => $"{Lettres()}.png"));

            return generateur;
        }

        private static ProduitsController Controleur(IFixture generateur) =>
            generateur.Build<ProduitsController>().OmitAutoProperties().Create();

        private static string Lettres() =>
            new(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray());

        // Le catalogue que le contrôleur garde est statique : un identifiant
        // tiré au hasard pourrait être celui d'un produit qu'un autre test a
        // déjà ajouté.
        private static Produit ProduitAbsentDuCatalogue(IFixture generateur, ProduitsController controleur) =>
            generateur.Build<Produit>()
                .With(p => p.Id, IdentifiantAbsent(controleur))
                .Create();

        private static int IdentifiantAbsent(ProduitsController controleur) =>
            Catalogue(controleur).Max(p => p.Id!.Value) + 1;

        private static IEnumerable<Produit> Catalogue(ProduitsController controleur)
        {
            OkObjectResult reponse = Assert.IsType<OkObjectResult>(controleur.GetProduits().Result);

            return Assert.IsType<IEnumerable<Produit>>(reponse.Value, exactMatch: false);
        }

        private static List<ValidationResult> Valider(Produit produit)
        {
            var resultats = new List<ValidationResult>();
            Validator.TryValidateObject(produit, new ValidationContext(produit), resultats, true);

            return resultats;
        }
    }
}
