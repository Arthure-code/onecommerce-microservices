using Microsoft.AspNetCore.Mvc;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Controllers
{
    public class CommandesController : Controller
    {
        private readonly ICommandesService _commandesService;
        private readonly IFideliteService _fideliteService;
        private readonly IProduitService _produitService;

        public CommandesController(ICommandesService commandesService, 
            IFideliteService fideliteService,
            IProduitService produitService)
        {
            _commandesService = commandesService;
            _fideliteService = fideliteService;
            _produitService = produitService;
        }

        public async Task<IActionResult> Index()
        {
            var commandes = await _commandesService.GetAllAsync();
            return View(commandes);
        }

        public async Task<IActionResult> Create(int id)
        {
            Produit? produit = await _produitService.GetProduitById(id);

            if (produit?.Id == null)
            {
                return NotFound();
            }

            var commande = new Commande
            {
                IdProduit = produit.Id.Value,
                Produit = produit,
                PrixUnitaire = produit.Prix
            };

            return View(commande);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Commande commande)
        {
            if (!ModelState.IsValid)
            {
                commande.Produit = await _produitService.GetProduitById(commande.IdProduit ?? 0);
                return View(commande);
            }

            Fidelite? fidelite = await _fideliteService.GetFideliteByNumeroAsync(commande.NumeroFideliteClient);

            if (string.IsNullOrEmpty(fidelite?.NumeroFidelite))
            {
                commande.Produit = await _produitService.GetProduitById(commande.IdProduit ?? 0);

                ModelState.AddModelError("NumeroFideliteClient", "Ce numéro de fidélité n'existe pas.");
                return View(commande);
            }

            var created = await _commandesService.CreateAsync(commande);
            if (created != null)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "Erreur lors de la création de la commande.");
            return View(commande);
        }
    }
}
