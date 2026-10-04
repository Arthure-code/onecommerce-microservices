using Microsoft.AspNetCore.Mvc;
using OneFichiers.API.Models;

namespace OneFichiers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FichiersController : ControllerBase
{
    private static readonly string[] ExtensionsAcceptees = [".png", ".jpg", ".jpeg"];

    [HttpPost]
    public async Task<IActionResult> Upload([FromBody] Fichier fichier)
    {
        if (fichier == null || string.IsNullOrWhiteSpace(fichier.FichierBase64))
        {
            return BadRequest("Le fichier est vide ou invalide.");
        }

        string? nomFichier = NomDeFichierAccepte(fichier.NomFichier);
        if (nomFichier == null)
        {
            return BadRequest("Le nom de fichier est invalide. Il doit être un nom simple se terminant par .png, .jpg ou .jpeg.");
        }

        try
        {
            byte[] imageBytes = Convert.FromBase64String(fichier.FichierBase64);

            string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            await System.IO.File.WriteAllBytesAsync(Path.Combine(imagesPath, nomFichier), imageBytes);

            var fileUrl = $"{Request.Scheme}://{Request.Host}/images/{nomFichier}";

            return Ok(new { message = "Fichier enregistré avec succès", url = fileUrl });
        }
        catch (FormatException)
        {
            return BadRequest("Le contenu base64 est invalide.");
        }
    }

    // Le nom arrive du corps de la requête. Sans ce filtre, un appelant qui
    // envoie ../../appsettings.json écrit où il veut sur le disque.
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
