using Microsoft.AspNetCore.Mvc;
using OneFichiers.API.Models;

namespace OneFichiers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FichiersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload([FromBody] Fichier fichier)
    {
        if (fichier == null || string.IsNullOrWhiteSpace(fichier.FichierBase64))
        {
            return BadRequest("Le fichier est vide ou invalide.");
        }

        try
        {
            // Conversion base64 -> bytes
            byte[] imageBytes = Convert.FromBase64String(fichier.FichierBase64);

            // Dossier de destination : wwwroot/images
            string imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            // Nom complet du fichier
            string filePath = Path.Combine(imagesPath, fichier.NomFichier);

            // Écriture sur disque
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

            // Retourne l’URL d’accès à l’image
            var fileUrl = $"{Request.Scheme}://{Request.Host}/images/{fichier.NomFichier}";

            return Ok(new { message = "Fichier enregistré avec succès", url = fileUrl });
        }
        catch (FormatException)
        {
            return BadRequest("Le contenu base64 est invalide.");
        }
        
    }
}
