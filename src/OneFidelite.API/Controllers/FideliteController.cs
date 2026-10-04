using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneFidelite.API.Models;

namespace OneFidelite.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FideliteController : ControllerBase
    {
        private static readonly List<Fidelite> _fidelites = new()
        {
            new Fidelite
            {
                Id = 1,
                NumeroFidelite = "ONE-100001",
                NomClient = "Alice Dupont",
                CourrielClient = "alice.dupont@example.com",
                Telephone = "514-123-4567",
                DateCreation = DateTime.UtcNow.AddDays(-10),
                Points = 150
            },
            new Fidelite
            {
                Id = 2,
                NumeroFidelite = "ONE-100002",
                NomClient = "Bob Martin",
                CourrielClient = "bob.martin@example.com",
                Telephone = "418-987-6543",
                DateCreation = DateTime.UtcNow.AddDays(-5),
                Points = 75
            },
            new Fidelite
            {
                Id = 3,
                NumeroFidelite = "ONE-100003",
                NomClient = "Caroline Tremblay",
                CourrielClient = "caroline.tremblay@example.com",
                Telephone = "450-555-1212",
                DateCreation = DateTime.UtcNow.AddDays(-2),
                Points = 200
            }
        };

        private static readonly Random _random = new();

        [HttpPost]
        public ActionResult<Fidelite> CreateFidelite([FromBody] Fidelite fidelite)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifie l'unicité du courriel
            if (_fidelites.Any(f => f.CourrielClient.Equals(fidelite.CourrielClient, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("Un client avec ce courriel existe déjà.");
            }

            // Génération d’un numéro fidélité unique : ONE-xxxx
            string numero;
            do
            {
                numero = $"ONE-{_random.Next(100000, 999999)}";
            } while (_fidelites.Any(f => f.NumeroFidelite == numero));

            fidelite.NumeroFidelite = numero;
            fidelite.Id = _fidelites.Count > 0 ? _fidelites.Max(f => f.Id) + 1 : 1;
            fidelite.DateCreation = DateTime.UtcNow;

            _fidelites.Add(fidelite);

            return CreatedAtAction(nameof(GetFideliteByNumero), new { numero = fidelite.NumeroFidelite }, fidelite);
        }

        [HttpGet("{numero}")]
        public ActionResult<Fidelite> GetFideliteByNumero(string numero)
        {
            var fidelite = _fidelites.FirstOrDefault(f => f.NumeroFidelite == numero);
            if (fidelite == null) return NotFound();
            return Ok(fidelite);
        }

        [HttpGet]
        public ActionResult<IEnumerable<Fidelite>> GetAllFidelites()
        {
            return Ok(_fidelites);
        }

    }
}
