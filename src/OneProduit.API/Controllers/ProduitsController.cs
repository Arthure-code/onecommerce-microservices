using Microsoft.AspNetCore.Mvc;
using OneProduit.API.Models;

namespace OneProduit.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProduitsController : ControllerBase
    {
        // Liste statique simulant une base de données
        private static List<Produit> _produits = new List<Produit>
        {
            new Produit { Id = 3, Nom = "T-shirt imprime noir", Description = "T-shirt noir imprime homme", Prix = 30.50m, Quantite = 4, Image = "image3.png", Vedette = false },
            new Produit { Id = 4, Nom = "T-shirt gris femme", Description = "T-shirt gris femme taille M", Prix = 10.20m, Quantite = 2, Image = "image4.png", Vedette = false },
            new Produit { Id = 5, Nom = "T-shirt multicolore femme", Description = "T-shirt multicolore femme taille M", Prix = 25m, Quantite = 6, Image = "image5.png", Vedette = false },
            new Produit { Id = 6, Nom = "T-shirt Homme Treillis", Description = "T-shirt Tee Chemise Homme Treillis Bloc", Prix = 12m, Quantite = 3, Image = "image6.png", Vedette = false },
            new Produit { Id = 7, Nom = "T-shirt imprime blanc", Description = "T-shirt blanc imprime homme", Prix = 20.40m, Quantite = 4, Image = "image7.png", Vedette = false },
            new Produit { Id = 8, Nom = "T-shirt Tee Femme", Description = "T-shirt Tee Femme Graphic Papillon", Prix = 14m, Quantite = 9, Image = "image8.png", Vedette = false },
            new Produit { Id = 9, Nom = "T-shirt Femme Graphic 3D", Description = "T-shirt Tee Femme Chat Graphic 3D du quotidien", Prix = 11m, Quantite = 3, Image = "image9.png", Vedette = false },
            new Produit { Id = 2, Nom = "Polo Homme noir", Description = "Polo noir homme taille L", Prix = 15.00m, Quantite = 2, Image = "image2.png", Vedette = false },
            new Produit { Id = 10, Nom = "Tshirt multi femme", Description = "Tshirt multi femme", Prix = 3m, Quantite = 3, Image = "image1.png", Vedette = false },
            new Produit { Id = 11, Nom = "produit demo", Description = "produit demo", Prix = 1m, Quantite = 1, Image = "image6.png", Vedette = true },
        };

        // GET api/produits
        [HttpGet]
        public ActionResult<IEnumerable<Produit>> GetProduits()
        {
            return Ok(_produits);
        }

        // GET api/produits/{id}
        [HttpGet("{id}")]
        public ActionResult<Produit> GetProduit(int id)
        {
            var produit = _produits.FirstOrDefault(p => p.Id == id);
            if (produit == null)
                return NotFound($"Aucun produit trouvé avec l'ID {id}");

            return Ok(produit);
        }
        // POST api/produits
        [HttpPost]
        public ActionResult<Produit> AddProduit([FromBody] Produit produit)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _produits.Add(produit);

            return CreatedAtAction(nameof(GetProduit), new { id = produit.Id }, produit);
        }
    }
}

