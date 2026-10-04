using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using OneCommerce.MVC.Interfaces;
using OneCommerce.MVC.Controllers;
using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Tests
{
    public class CommandesControllerTests
{
    private readonly Mock<ICommandesService> _mockCommandesService;
    private readonly Mock<IFideliteService> _mockFideliteService;
    private readonly Mock<IProduitService> _mockProduitService;
    private readonly CommandesController _controller;

    public CommandesControllerTests()
    {
        _mockCommandesService = new Mock<ICommandesService>();
        _mockFideliteService = new Mock<IFideliteService>();
        _mockProduitService = new Mock<IProduitService>();

        _controller = new CommandesController(
            _mockCommandesService.Object,
            _mockFideliteService.Object,
            _mockProduitService.Object
        );
    }

    [Fact]
    public async Task Create_Post_ModelStateInvalid_ReturnsViewWithProduit()
    {
        // Etant donné
        var commande = new Commande { IdProduit = 1 };
        _controller.ModelState.AddModelError("NomProduit", "Required");

        var produit = new Produit { Id = 1, Nom = "Test Produit", Prix = 10 };
        _mockProduitService.Setup(p => p.GetProduitById(1))
            .ReturnsAsync(produit);

        // Lorsque
        var result = await _controller.Create(commande);

        // Alors
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Commande>(viewResult.Model);
        Assert.Equal(produit, model.Produit);
    }

    [Fact]
    public async Task Create_Post_FideliteNotFound_ReturnsViewWithModelError()
    {
        // Etant donné
        var commande = new Commande { IdProduit = 1, NumeroFideliteClient = "ONE-9999" };

        var produit = new Produit { Id = 1, Nom = "Produit", Prix = 20 };
        _mockProduitService.Setup(p => p.GetProduitById(1))
            .ReturnsAsync(produit);

        _mockFideliteService.Setup(f => f.GetFideliteByNumeroAsync("ONE-9999"))
            .ReturnsAsync(new Fidelite { NumeroFidelite = "" }); // fidélité introuvable

        // Lorsque
        var result = await _controller.Create(commande);

        // Alors
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Commande>(viewResult.Model);
        Assert.Equal(produit, model.Produit);
        Assert.True(_controller.ModelState.ContainsKey("NumeroFideliteClient"));
    }

    [Fact]
    public async Task Create_Post_SuccessfulCreation_RedirectsToIndex()
    {
        // Etant donné
        var commande = new Commande { IdProduit = 1, NumeroFideliteClient = "ONE-1001" };

        _mockFideliteService.Setup(f => f.GetFideliteByNumeroAsync("ONE-1001"))
            .ReturnsAsync(new Fidelite { NumeroFidelite = "ONE-1001" });

        _mockCommandesService.Setup(c => c.CreateAsync(commande))
            .ReturnsAsync(new Commande { Id = 99, NumeroCommande = "ONE-CMD-123456" });

        // Lorsque
        var result = await _controller.Create(commande);

        // Alors
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Create_Post_CreationFails_ReturnsViewWithModelError()
    {
        // Etant donné
        var commande = new Commande { IdProduit = 1, NumeroFideliteClient = "ONE-1001" };

        _mockFideliteService.Setup(f => f.GetFideliteByNumeroAsync("ONE-1001"))
            .ReturnsAsync(new Fidelite { NumeroFidelite = "ONE-1001" });

        _mockCommandesService.Setup(c => c.CreateAsync(commande))
            .ReturnsAsync((Commande?)null); // simulate failure

        var produit = new Produit { Id = 1, Nom = "Produit", Prix = 15 };
        _mockProduitService.Setup(p => p.GetProduitById(1))
            .ReturnsAsync(produit);

        // Lorsque
        var result = await _controller.Create(commande);

        // Alors
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.True(_controller.ModelState.ContainsKey(""));
    }
}
}
