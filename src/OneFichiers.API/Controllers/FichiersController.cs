using Microsoft.AspNetCore.Mvc;
using OneFichiers.API.Interfaces;
using OneFichiers.API.Models;

namespace OneFichiers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FichiersController : ControllerBase
{
    private static readonly string[] ExtensionsAcceptees = [".png", ".jpg", ".jpeg"];

    private readonly IMagasinImages _magasin;

    public FichiersController(IMagasinImages magasin)
    {
        _magasin = magasin;
    }

    [HttpPost("televersement")]
    public ActionResult<LienTeleversement> DemanderLienTeleversement([FromBody] DemandeLien? demande)
    {
        string? nomPropose = NomDeFichierAccepte(demande?.NomFichier);
        if (nomPropose == null)
        {
            return BadRequest("Le nom de fichier est invalide. Il doit être un nom simple se terminant par .png, .jpg ou .jpeg.");
        }

        // Le nom déposé est tiré ici, pas repris de l'appelant : deux visiteurs
        // qui envoient photo.png n'écrasent pas l'image l'un de l'autre.
        string nomDepose = $"{Guid.NewGuid():N}{Path.GetExtension(nomPropose)}";

        return Ok(_magasin.LienTeleversement(nomDepose));
    }

    [HttpGet("lecture")]
    public ActionResult<LienLecture> DemanderLienLecture()
    {
        return Ok(_magasin.LienLecture());
    }

    // Le repli sur disque reçoit les octets ici. Avec un compte de stockage,
    // ils vont directement au conteneur et cette route n'existe pas.
    [HttpPut("{nomFichier}")]
    public async Task<IActionResult> Deposer(string nomFichier)
    {
        if (!_magasin.RecoitLesOctets)
        {
            return NotFound();
        }

        string? nomAccepte = NomDeFichierAccepte(nomFichier);
        if (nomAccepte == null)
        {
            return BadRequest("Le nom de fichier est invalide. Il doit être un nom simple se terminant par .png, .jpg ou .jpeg.");
        }

        if (Request.ContentLength is null or 0)
        {
            return BadRequest("Le fichier est vide.");
        }

        await _magasin.EnregistrerAsync(nomAccepte, Request.Body);

        return NoContent();
    }

    // Le nom arrive de l'appelant. Sans ce filtre, un nom comme
    // ../../appsettings.json désigne un autre endroit que le dossier des images.
    private static string? NomDeFichierAccepte(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            return null;
        }

        string nomSeul = Path.GetFileName(nom);

        if (nomSeul != nom || nomSeul.Length == 0)
        {
            return null;
        }

        if (nomSeul.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        string extension = Path.GetExtension(nomSeul);

        return ExtensionsAcceptees.Contains(extension, StringComparer.OrdinalIgnoreCase) ? nomSeul : null;
    }
}
