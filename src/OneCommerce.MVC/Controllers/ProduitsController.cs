using Microsoft.AspNetCore.Mvc;
using OneCommerce.MVC.Models;
using System.Diagnostics;
using OneCommerce.MVC.Interfaces;


namespace OneCommerce.MVC.Controllers
{
    public class ProduitsController : Controller
    {

        private readonly IProduitService _produitService;
        private readonly IFichiersService _fichiersService;

        public ProduitsController(IProduitService produitService, IFichiersService fichiersService)
        {
            _produitService = produitService;
            _fichiersService = fichiersService;
        }

        public async Task<IActionResult> Index(string? filtre)
        {

            List<Produit> produits = await _produitService.GetProduits();

            if (filtre != null)
                produits = produits.Where(p=>p.Nom.Contains(filtre,StringComparison.InvariantCultureIgnoreCase) || p.Vedette).ToList();

            return View(produits);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Produit produit)
        {
            if(ModelState.IsValid)
            {
                if (produit.FichierImage == null)
                {
                    ModelState.AddModelError(nameof(Produit.FichierImage), "L'image est obligatoire.");
                    return View(produit);
                }

                List<Produit> produits = await _produitService.GetProduits();

                produit.Id = produits.Count > 0 ? produits.Max(p => p.Id) + 1 : 1;

                string extension = Path.GetExtension(produit.FichierImage.FileName);

                produit.Image = $"Image{produit.Id}{extension}";

                await _produitService.AddProduit(produit);

                await _fichiersService.Upload(produit.FichierImage, produit.Image);

                return RedirectToAction(nameof(Index));
            }

            return View(produit);
        }

        [HttpGet]
        public Task<IActionResult> Edit(int id) => AfficherProduit(id, "Edit");

        [HttpPost]
        public async Task<IActionResult> Edit(Produit produit)
        {
            if (!ModelState.IsValid)
            {
                return View(produit);
            }

            // Une nouvelle image remplace l'ancienne ; sans dépôt, le produit
            // garde celle qu'il avait.
            if (produit.FichierImage != null)
            {
                string extension = Path.GetExtension(produit.FichierImage.FileName);
                produit.Image = $"Image{produit.Id}{extension}";

                await _fichiersService.Upload(produit.FichierImage, produit.Image);
            }

            Produit? modifie = await _produitService.UpdateProduit(produit);

            if (modifie == null)
            {
                ModelState.AddModelError(string.Empty, "La modification du produit a échoué.");
                return View(produit);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public Task<IActionResult> Delete(int id) => AfficherProduit(id, "Delete");

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirme(int id)
        {
            bool supprime = await _produitService.DeleteProduit(id);

            if (!supprime)
            {
                ModelState.AddModelError(string.Empty, "La suppression du produit a échoué.");

                return await AfficherProduit(id, "Delete");
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> AfficherProduit(int id, string vue)
        {
            Produit? produit = await _produitService.GetProduitById(id);

            if (produit == null)
                return NotFound();

            return View(vue, produit);
        }





        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}