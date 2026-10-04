using Microsoft.AspNetCore.Mvc;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Controllers
{
    public class FideliteController : Controller
    {
        private readonly IFideliteService _fideliteService;

        public FideliteController(IFideliteService fideliteService)
        {
            _fideliteService = fideliteService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var fidelites = await _fideliteService.GetFidelitesAsync();
            return View(fidelites);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Fidelite());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Fidelite model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var fideliteCree = await _fideliteService.CreateFideliteAsync(model);

            if (fideliteCree == null)
            {
                ModelState.AddModelError("", "Impossible de créer la fidélité. Courriel déjà utilisé ?");
                return View(model);
            }

            ViewBag.NumeroFidelite = fideliteCree.NumeroFidelite;
            return View(model);
        }
    }
}
