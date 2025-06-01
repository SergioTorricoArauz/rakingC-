using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PuntajeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PuntajeController(AppDbContext context)
        {
            _context = context;
        }

        // Obtener los puntajes ordenado desde el mas alto hasta el mas bajo
        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<PuntajeResponseDto>>> GetPuntajes()
        {
            var puntajes = await _context.Puntajes
                .OrderByDescending(p => p.Puntos)
                .ToListAsync();
            if (puntajes == null || !puntajes.Any())
            {
                return NotFound("No se encontraron puntajes.");
            }
            var puntajesDto = puntajes.Select(p => new PuntajeResponseDto
            {
                Id = p.Id,
                Puntos = p.Puntos,
                ClienteId = p.ClienteId,
                TemporadaId = p.TemporadaId
            }).ToList();
            return Ok(puntajesDto);
        }

        // Obtener un puntaje por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PuntajeResponseDto>> GetPuntaje(int id)
        {
            var puntaje = await _context.Puntajes.FindAsync(id);
            if (puntaje == null)
            {
                return NotFound($"Puntaje con ID {id} no encontrado.");
            }
            var puntajeDto = new PuntajeResponseDto
            {
                Id = puntaje.Id,
                Puntos = puntaje.Puntos,
                ClienteId = puntaje.ClienteId,
                TemporadaId = puntaje.TemporadaId
            };
            return Ok(puntajeDto);
        }

        // Crear un nuevo puntaje
        [HttpPost("register")]
        public async Task<ActionResult<PuntajeResponseDto>> RegisterPuntaje(PuntajePostDto puntajeRequest)
        {
            if (puntajeRequest.Puntos <= 0 || puntajeRequest.ClienteId <= 0 || puntajeRequest.TemporadaId <= 0)
            {
                return BadRequest("Los puntos, ClienteId y TemporadaId son obligatorios y deben ser mayores a cero.");
            }
            var nuevoPuntaje = new Models.Puntajes
            {
                Puntos = puntajeRequest.Puntos,
                ClienteId = puntajeRequest.ClienteId,
                TemporadaId = puntajeRequest.TemporadaId
            };
            _context.Puntajes.Add(nuevoPuntaje);
            await _context.SaveChangesAsync();
            var puntajeDto = new PuntajeResponseDto
            {
                Id = nuevoPuntaje.Id,
                Puntos = nuevoPuntaje.Puntos,
                ClienteId = nuevoPuntaje.ClienteId,
                TemporadaId = nuevoPuntaje.TemporadaId
            };
            return CreatedAtAction(nameof(GetPuntaje), new { id = nuevoPuntaje.Id }, puntajeDto);
        }
        }
}
