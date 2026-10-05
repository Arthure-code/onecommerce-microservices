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
            if (!ModelState.IsValid)
            {
                return View(produit);
            }

            if (string.IsNullOrWhiteSpace(produit.Image))
            {
                ModelState.AddModelError(nameof(Produit.Image), "L'image est obligatoire.");

                return View(produit);
            }

            List<Produit> produits = await _produitService.GetProduits();

            produit.Id = produits.Count > 0 ? produits.Max(p => p.Id) + 1 : 1;

            await _produitService.AddProduit(produit);

            return RedirectToAction(nameof(Index));
        }

        // Le navigateur demande ici où déposer son image. Il y écrit lui-même,
        // puis renvoie le nom avec le formulaire : l'octet ne passe pas par ici.
        [HttpPost]
        public async Task<IActionResult> LienImage(string nomPropose)
        {
            LienTeleversement? lien = await _fichiersService.LienTeleversement(nomPropose);

            if (lien == null)
            {
                return BadRequest();
            }

            return Json(lien);
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