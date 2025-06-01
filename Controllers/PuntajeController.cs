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
        /*
        // Obtener los puntajes ordenado desde el mas alto hasta el mas bajo
        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<PuntajeResponseDto>>> GetPuntajes()
        {
            var puntajes = await _context.Puntajes
                .Include(p => p.Cliente)  // Incluir Cliente para obtener su nombre
                .Include(p => p.Temporada)  // Incluir Temporada para obtener su nombre
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
                ClienteNombre = p.Cliente.Nombre,  // Asignar nombre del cliente
                TemporadaNombre = p.Temporada.Nombre  // Asignar nombre de la temporada
            }).ToList();

            return Ok(puntajesDto);
        }
        */

        // Obtener un puntaje por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PuntajeResponseDto>> GetPuntaje(int id)
        {
            var puntaje = await _context.Puntajes
                .Include(p => p.Cliente)  // Incluir Cliente para obtener su nombre
                .Include(p => p.Temporada)  // Incluir Temporada para obtener su nombre
                .FirstOrDefaultAsync(p => p.Id == id);
            if (puntaje == null)
            {
                return NotFound($"Puntaje con ID {id} no encontrado.");
            }
            var puntajeDto = new PuntajeResponseDto
            {
                Id = puntaje.Id,
                Puntos = puntaje.Puntos,
                ClienteNombre = puntaje.Cliente.Nombre,  // Asignar nombre del cliente
                TemporadaNombre = puntaje.Temporada.Nombre  // Asignar nombre de la temporada
            };
            return Ok(puntajeDto);
        }

        /*
        // Obtener Puntajes por temporada
        [HttpGet("temporada/{temporadaId}/puntajes")]
        public async Task<ActionResult<IEnumerable<PuntajeResponseDto>>> GetPuntajesPorTemporada(int temporadaId)
        {
            // Filtrar los puntajes por temporada específica
            var puntajes = await _context.Puntajes
                .Where(p => p.TemporadaId == temporadaId)  // Filtrar por temporada
                .Include(p => p.Cliente)  // Incluir Cliente para obtener su nombre
                .Include(p => p.Temporada)  // Incluir Temporada para obtener su nombre
                .OrderByDescending(p => p.Puntos)  // Ordenar por puntos de mayor a menor
                .ToListAsync();

            if (puntajes == null || !puntajes.Any())
            {
                return NotFound("No se encontraron puntajes para la temporada especificada.");
            }

            // Convertir los puntajes a DTO
            var puntajesDto = puntajes.Select(p => new PuntajeResponseDto
            {
                Id = p.Id,
                Puntos = p.Puntos,
                ClienteNombre = p.Cliente.Nombre,  // Agregar nombre del cliente
                TemporadaNombre = p.Temporada.Nombre  // Agregar nombre de la temporada
            }).ToList();

            return Ok(puntajesDto);
        }
        */

        // Obtener el ranking de puntajes por temporada
        [HttpGet("ranking/temporada/{temporadaId}")]
        public async Task<ActionResult<IEnumerable<PuntajeResponseDto>>> GetRankingPorTemporada(int temporadaId)
        {
            // Consultar los puntajes acumulados de todos los clientes en la temporada específica
            var ranking = await _context.Puntajes
                .Where(p => p.TemporadaId == temporadaId)  // Filtrar por la temporada
                .GroupBy(p => p.ClienteId)  // Agrupar por cliente
                .Select(g => new
                {
                    ClienteId = g.Key,  // Obtener el Id del Cliente
                    PuntosTotales = g.Sum(p => p.Puntos)  // Sumar los puntos por cliente
                })
                .OrderByDescending(r => r.PuntosTotales)  // Ordenar por los puntos totales, de mayor a menor
                .Join(
                    _context.Clientes,  // Unir con los clientes para obtener sus nombres
                    r => r.ClienteId,   // Relacionar ClienteId
                    cliente => cliente.Id,  // Relacionar ClienteId
                    (r, cliente) => new { r, cliente }  // Crear un nuevo objeto con puntaje y nombre del cliente
                )
                .Join(
                    _context.Temporadas,  // Unir con las temporadas para obtener el nombre de la temporada
                    r => temporadaId,    // Relacionar TemporadaId
                    temporada => temporada.Id,  // Relacionar TemporadaId
                    (r, temporada) => new PuntajeResponseDto
                    {
                        Id = r.r.ClienteId,  // Asignar el ID del Cliente
                        ClienteNombre = r.cliente.Nombre,  // Nombre del cliente
                        Puntos = r.r.PuntosTotales,  // Puntos totales por temporada
                        TemporadaNombre = temporada.Nombre  // Nombre de la temporada
                    })
                .ToListAsync();

            // Si no hay puntajes, devolver un mensaje
            if (ranking == null || !ranking.Any())
            {
                return NotFound("No se encontraron puntajes para la temporada especificada.");
            }

            return Ok(ranking);
        }



        // Crear un nuevo puntaje
        [HttpPost("register")]
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
                ClienteNombre = cliente.Nombre,  // Asignar nombre del cliente
                TemporadaNombre = temporada.Nombre  // Asignar nombre de la temporada
            };
            return CreatedAtAction(nameof(GetPuntaje), new { id = nuevoPuntaje.Id }, puntajeDto);
        }
        }
}
