using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InsigniaController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // Obtener todas las insignias desde la base de datos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InsigniaResponseDto>>> GetInsignias()
        {
            var insignias = await _context.Insignias.ToListAsync();
            if (insignias == null || insignias.Count == 0)
            {
                return NotFound("No se encontraron insignias.");
            }
            var insigniasDto = insignias.Select(i => new InsigniaResponseDto
            {
                Id = i.Id,
                Nombre = i.Nombre,
                Requisitos = i.Requisitos,
                FechaInicio = i.FechaInicio,
                FechaFin = i.FechaFin
            }).ToList();
            return Ok(insigniasDto);
        }

        // Obtener una insignia por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<InsigniaResponseDto>> GetInsignia(int id)
        {
            var insignia = await _context.Insignias.FindAsync(id);
            if (insignia == null)
            {
                return NotFound($"Insignia con ID {id} no encontrada.");
            }
            var insigniaDto = new InsigniaResponseDto
            {
                Id = insignia.Id,
                Nombre = insignia.Nombre,
                Requisitos = insignia.Requisitos,
                FechaInicio = insignia.FechaInicio,
                FechaFin = insignia.FechaFin
            };
            return Ok(insigniaDto);
        }

        // Crear una nueva insignia
        [HttpPost("register")]
        public async Task<ActionResult<InsigniaResponseDto>> RegisterInsignia(InsigniaPostDto insigniaRequest)
        {
            if (string.IsNullOrWhiteSpace(insigniaRequest.Nombre) || string.IsNullOrWhiteSpace(insigniaRequest.Requisitos))
            {
                return BadRequest("El nombre y los requisitos son obligatorios.");
            }
            var insignia = new Insignias
            {
                Nombre = insigniaRequest.Nombre,
                Requisitos = insigniaRequest.Requisitos,
                FechaInicio = insigniaRequest.FechaInicio,
                FechaFin = insigniaRequest.FechaFin,
            };
            _context.Insignias.Add(insignia);
            await _context.SaveChangesAsync();
            var insigniaResponse = new InsigniaResponseDto
            {
                Id = insignia.Id,
                Nombre = insignia.Nombre,
                Requisitos = insignia.Requisitos,
                FechaInicio = insignia.FechaInicio,
                FechaFin = insignia.FechaFin
            };
            return CreatedAtAction(nameof(GetInsignia), new { id = insignia.Id }, insigniaResponse);
        }

        // Eliminar una insignia por ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInsignia(int id)
        {
            var insignia = await _context.Insignias.FindAsync(id);
            if (insignia == null)
            {
                return NotFound($"Insignia con ID {id} no encontrada.");
            }
            _context.Insignias.Remove(insignia);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
