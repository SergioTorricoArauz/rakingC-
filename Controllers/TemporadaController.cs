using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TemporadaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TemporadaController(AppDbContext context)
        {
            _context = context;
        }

        // Obtener todas las temporadas desde la base de datos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TemporadaResponseDto>>> GetTemporadas()
        {
            var temporadas = await _context.Temporadas.ToListAsync();
            if (temporadas == null || !temporadas.Any())
            {
                return NotFound("No se encontraron temporadas.");
            }

            var temporadaDtos = temporadas.Select(t => new TemporadaResponseDto
            {
                Id = t.Id,
                Inicio = t.Inicio,
                Fin = t.Fin,
                Nombre = t.Nombre
            }).ToList();
            return Ok(temporadaDtos);
        }

        // Obtener una temporada por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<TemporadaResponseDto>> GetTemporada(int id)
        {
            var temporada = await _context.Temporadas.FindAsync(id);
            if(temporada == null)
            {
              return NotFound($"Temporada con ID {id} no encontrada.");
            }

            var temporadaDto = new TemporadaResponseDto
            {
                Id = temporada.Id,
                Inicio = temporada.Inicio,
                Fin = temporada.Fin,
                Nombre = temporada.Nombre
            };
            return Ok(temporadaDto);
        }

        // Crear una nueva temporada
        [HttpPost("register")]
        public async Task<ActionResult<TemporadaResponseDto>> CreateTemporada([FromBody] TemporadaPostDto temporadaDto)
        {
            if (temporadaDto == null)
            {
                return BadRequest("Los datos de la temporada son inválidos.");
            }
            var nuevaTemporada = new Models.Temporadas
            {
                Inicio = temporadaDto.Inicio,
                Fin = temporadaDto.Fin,
                Nombre = temporadaDto.Nombre
            };
            _context.Temporadas.Add(nuevaTemporada);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTemporada), new { id = nuevaTemporada.Id }, temporadaDto);
        }

        // Eliminar una temporada por ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemporada(int id)
        {
            var temporada = await _context.Temporadas.FindAsync(id);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {id} no encontrada.");
            }
            _context.Temporadas.Remove(temporada);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
