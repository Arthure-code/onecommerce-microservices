using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using OneProduit.API.Controllers;
using OneProduit.API.Models;

namespace OneProduit.API.Tests.Controllers
{
    public class ProduitsControllerTests
    {
        private static Produit UnProduit(decimal prix = 19.99m) => new Produit
        {
            Id = 99,
            Nom = "Polo blanc",
            Description = "Polo blanc homme taille L",
            Prix = prix,
            Quantite = 3,
            Image = "image1.png",
            Vedette = false
        };

        private static IList<ValidationResult> Valider(Produit produit)
        {
            var resultats = new List<ValidationResult>();
            Validator.TryValidateObject(produit, new ValidationContext(produit), resultats, true);

            return resultats;
        }

        [Fact]
        public void GetProduits_RendLeCatalogue()
        {
            //Etant donné le contrôleur
            var controleur = new ProduitsController();

            //Lorsque
            ActionResult<IEnumerable<Produit>> resultat = controleur.GetProduits();

            //Alors
            var reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            var produits = Assert.IsAssignableFrom<IEnumerable<Produit>>(reponse.Value);
            Assert.NotEmpty(produits);
        }

        [Fact]
        public void GetProduit_RendLeProduitDemande()
        {
            //Etant donné un identifiant du catalogue
            var controleur = new ProduitsController();

            //Lorsque
            ActionResult<Produit> resultat = controleur.GetProduit(3);

            //Alors
            var reponse = Assert.IsType<OkObjectResult>(resultat.Result);
            var produit = Assert.IsType<Produit>(reponse.Value);
            Assert.Equal(3, produit.Id);
        }

        [Fact]
        public void GetProduit_RendNonTrouveQuandLIdentifiantNExistePas()
        {
            //Etant donné un identifiant absent du catalogue
            var controleur = new ProduitsController();

            //Lorsque
            ActionResult<Produit> resultat = controleur.GetProduit(9999);

            //Alors
            Assert.IsType<NotFoundObjectResult>(resultat.Result);
        }

        [Fact]
        public void AddProduit_RefuseUnProduitInvalide()
        {
            //Etant donné un modèle que la validation a rejeté
            var controleur = new ProduitsController();
            controleur.ModelState.AddModelError("Nom", "Le nom est obligatoire");

            //Lorsque
            ActionResult<Produit> resultat = controleur.AddProduit(UnProduit());

            //Alors
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
        }

        [Theory]
        [InlineData(30.50)]
        [InlineData(10.20)]
        [InlineData(25)]
        public void UnPrixAvecDesCentimesEstAccepte(decimal prix)
        {
            //Etant donné un prix tel que le catalogue en contient
            Produit produit = UnProduit(prix);

            //Lorsque le modèle est validé
            IList<ValidationResult> erreurs = Valider(produit);

            //Alors aucune erreur ne porte sur le prix
            Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Prix)));
        }


        [Theory]
        [InlineData("fr-CA")]
        [InlineData("en-CA")]
        [InlineData("")]
        public void UnPrixAvecDesCentimesEstAccepteQuelleQueSoitLaCulture(string culture)
        {
            //Etant donné un serveur dont la culture n'est pas celle du poste
            var precedente = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);

            try
            {
                //Lorsque le modèle est validé
                IList<ValidationResult> erreurs = Valider(UnProduit(30.50m));

                //Alors le prix passe
                Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Prix)));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = precedente;
            }
        }

        [Fact]
        public void UneImagePngEstAcceptee()
        {
            //Etant donné une image telle que le catalogue en contient
            Produit produit = UnProduit();

            //Lorsque le modèle est validé
            IList<ValidationResult> erreurs = Valider(produit);

            //Alors aucune erreur ne porte sur l'image
            Assert.DoesNotContain(erreurs, e => e.MemberNames.Contains(nameof(Produit.Image)));
        }
    }
}
