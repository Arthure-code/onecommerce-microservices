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
                List<Produit> produits = await _produitService.GetProduits();

                produit.Id = produits.Max(p => p.Id) + 1;

                var extension = Path.GetExtension(produit.FichierImage?.FileName);

                produit.Image = $"Image{produit.Id}{extension}";

                await _produitService.AddProduit(produit);

                await _fichiersService.Upload(produit.FichierImage, produit.Image);

                return RedirectToAction(nameof(Index));
            }

            return View(produit);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            Produit produit = await _produitService.GetProduitById(id);

            if (produit == null)
                return NotFound();

            return View(produit);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Produit produit)
        {
            

            return View(produit);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            Produit produit = await _produitService.GetProduitById(id);

            if (produit == null)
                return NotFound();

            return View(produit);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Produit produit)
        {
           

            return RedirectToAction(nameof(Index));
          
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