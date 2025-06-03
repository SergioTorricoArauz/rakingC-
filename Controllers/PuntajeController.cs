using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
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

        // Obtener un puntaje por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PuntajeResponseDto>> GetPuntaje(int id)
        {
            var puntaje = await _context.Puntajes
                .Include(p => p.Cliente)
                .Include(p => p.Temporada)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (puntaje == null)
            {
                return NotFound($"Puntaje con ID {id} no encontrado.");
            }
            var puntajeDto = new PuntajeResponseDto
            {
                Id = puntaje.Id,
                Puntos = puntaje.Puntos,
                ClienteNombre = puntaje.Cliente.Nombre,
                TemporadaNombre = puntaje.Temporada.Nombre
            };
            return Ok(puntajeDto);
        }

        // Obtener el ranking de puntajes por temporada
        [HttpGet("ranking/temporada/{temporadaId}")]
        public async Task<ActionResult<IEnumerable<PuntajeResponseDto>>> GetRankingPorTemporada(int temporadaId)
        {
            var ranking = await _context.Puntajes
                .Where(p => p.TemporadaId == temporadaId)
                .GroupBy(p => p.ClienteId)
                .Select(g => new
                {
                    ClienteId = g.Key,
                    PuntosTotales = g.Sum(p => p.Puntos)
                })
                .OrderByDescending(r => r.PuntosTotales)
                .Join(
                    _context.Clientes,
                    r => r.ClienteId,
                    cliente => cliente.Id,
                    (r, cliente) => new { r, cliente }
                )
                .Join(
                    _context.Temporadas,
                    r => temporadaId,
                    temporada => temporada.Id,
                    (r, temporada) => new PuntajeResponseDto
                    {
                        Id = r.r.ClienteId,
                        ClienteNombre = r.cliente.Nombre,
                        Puntos = r.r.PuntosTotales,
                        TemporadaNombre = temporada.Nombre
                    })
                .ToListAsync();

            if (ranking == null || !ranking.Any())
            {
                return NotFound("No se encontraron puntajes para la temporada especificada.");
            }

            return Ok(ranking);
        }



        // Crear un nuevo puntaje de cliente con temporada
        [HttpPost("register-cliente-temporada")]
        public async Task<ActionResult<PuntajeResponseDto>> RegisterPuntaje(PuntajePostDto puntajeRequest)
        {
            if (puntajeRequest == null)
            {
                return BadRequest("Los datos del puntaje son inválidos.");
            }
            var cliente = await _context.Clientes.FindAsync(puntajeRequest.ClienteId);
            if (cliente == null)
            {
                return NotFound($"Cliente con ID {puntajeRequest.ClienteId} no encontrado.");
            }
            var temporada = await _context.Temporadas.FindAsync(puntajeRequest.TemporadaId);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {puntajeRequest.TemporadaId} no encontrada.");
            }
            var nuevoPuntaje = new Puntajes
            {
                Puntos = puntajeRequest.Puntos,
                Cliente = cliente,
                Temporada = temporada
            };
            _context.Puntajes.Add(nuevoPuntaje);
            await _context.SaveChangesAsync();
            var puntajeDto = new PuntajeResponseDto
            {
                Id = nuevoPuntaje.Id,
                Puntos = nuevoPuntaje.Puntos,
                ClienteNombre = cliente.Nombre,
                TemporadaNombre = temporada.Nombre
            };
            return CreatedAtAction(nameof(GetPuntaje), new { id = nuevoPuntaje.Id }, puntajeDto);
        }
        }
}
