using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
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
                Nombre = t.Nombre,
                EstaDisponible = t.EstaDisponible
            }).ToList();
            return Ok(temporadaDtos);
        }

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
                Nombre = temporada.Nombre,
                EstaDisponible = temporada.EstaDisponible
            };
            return Ok(temporadaDto);
        }

        [HttpGet("participa/{clienteId}/temporada/{temporadaId}")]
        public async Task<ActionResult<bool>> ClienteParticipaEnTemporada(int clienteId, int temporadaId)
        {
            var existe = await _context.Puntajes
                .AnyAsync(p => p.ClienteId == clienteId && p.TemporadaId == temporadaId);

            return Ok(existe);
        }

        [HttpPost("register")]
        public async Task<ActionResult<TemporadaResponseDto>> CreateTemporada([FromBody] TemporadaPostDto temporadaDto)
        {
            if (temporadaDto == null)
            {
                return BadRequest("Los datos de la temporada son inválidos.");
            }

            // Buscar si ya existe una temporada activa
            var temporadaActiva = await _context.Temporadas
                .FirstOrDefaultAsync(t => t.EstaDisponible);

            // Buscar la última temporada creada (por fecha de fin más reciente)
            var ultimaTemporada = await _context.Temporadas
                .OrderByDescending(t => t.Fin)
                .FirstOrDefaultAsync();

            // Si hay una temporada previa, validar que la nueva temporada inicie después de la última que terminó
            if (ultimaTemporada != null && temporadaDto.Inicio <= ultimaTemporada.Fin)
            {
                return BadRequest("La fecha de inicio de la nueva temporada debe ser posterior a la fecha de fin de la última temporada creada.");
            }

            // Si hay una temporada activa, la nueva se crea como inactiva
            bool esActiva = temporadaActiva == null;

            var nuevaTemporada = new Models.Temporadas
            {
                Inicio = temporadaDto.Inicio,
                Fin = temporadaDto.Fin,
                Nombre = temporadaDto.Nombre,
                EstaDisponible = esActiva
            };
            _context.Temporadas.Add(nuevaTemporada);
            await _context.SaveChangesAsync();

            var response = new TemporadaResponseDto
            {
                Id = nuevaTemporada.Id,
                Inicio = nuevaTemporada.Inicio,
                Fin = nuevaTemporada.Fin,
                Nombre = nuevaTemporada.Nombre,
                EstaDisponible = nuevaTemporada.EstaDisponible
            };

            return CreatedAtAction(nameof(GetTemporada), new { id = nuevaTemporada.Id }, response);
        }

        //Finaliza la temporada
        [HttpPost("asignarInsignias/temporada/{temporadaId}")]
        public async Task<IActionResult> AsignarInsigniasTemporada(int temporadaId)
        {
            var ranking = await GetRankingPorTemporadaHelper(temporadaId);

            if (ranking.Count == 0)
            {
                return NotFound("No hay clientes en la temporada especificada.");
            }

            var insignias = await _context.Insignias
                .Where(i => i.Nombre.StartsWith("Temporada Top"))
                .ToListAsync();

            if (!insignias.Any())
            {
                return NotFound("No se encontraron insignias de temporada.");
            }

            var insigniasOtorgadas = new List<string>();

            for (int i = 0; i < 3; i++)
            {
                var puntaje = ranking[i];
                Insignias? insignia = null;

                if (i == 0)
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 1");
                else if (i == 1)
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 2");
                else if (i == 2)
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 3");

                if (insignia != null && !await _context.ClienteInsignias.AnyAsync(ci => ci.ClienteId == puntaje.Id && ci.InsigniaId == insignia.Id))
                {
                    _context.ClienteInsignias.Add(new ClienteInsignia
                    {
                        ClienteId = puntaje.Id,
                        InsigniaId = insignia.Id,
                        FechaOtorgada = DateTime.UtcNow.Date
                    });

                    insigniasOtorgadas.Add(insignia.Nombre);
                }
            }

            var temporada = await _context.Temporadas.FindAsync(temporadaId);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {temporadaId} no encontrada.");
            }

            // Solo finaliza automáticamente si es la temporada activa y la fecha de fin ya se cumplió
            if (temporada.EstaDisponible && DateTime.UtcNow.Date >= temporada.Fin.Date)
            {
                temporada.EstaDisponible = false;
                await _context.SaveChangesAsync();
                return Ok($"Insignias otorgadas: {string.Join(", ", insigniasOtorgadas)}. La temporada activa ha finalizado automáticamente.");
            }

            await _context.SaveChangesAsync();
            return Ok($"Insignias otorgadas: {string.Join(", ", insigniasOtorgadas)}");
        }


        // Método auxiliar para obtener el ranking por temporada
        private async Task<List<PuntajeResponseDto>> GetRankingPorTemporadaHelper(int temporadaId)
        {
            var temporada = await _context.Temporadas
                .Where(t => t.Id == temporadaId)
                .Select(t => t.Nombre)
                .FirstOrDefaultAsync();

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
                    (r, cliente) => new PuntajeResponseDto
                    {
                        Id = r.ClienteId,
                        ClienteNombre = cliente.Nombre,
                        Puntos = r.PuntosTotales,
                        TemporadaNombre = temporada
                    })
                .ToListAsync();

            return ranking;
        }

        // Actualizar una temporada por ID (PATCH)
        [HttpPatch("{temporadaId}")]
        public async Task<IActionResult> UpdateTemporada(int temporadaId, [FromBody] TemporadaPatchDto temporadaPatchDto)
        {
            var temporada = await _context.Temporadas.FindAsync(temporadaId);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {temporadaId} no encontrada.");
            }
            if (temporadaPatchDto.EstaDisponible.HasValue)
            {
                temporada.EstaDisponible = temporadaPatchDto.EstaDisponible.Value;
            }

            if (temporadaPatchDto.Inicio.HasValue)
            {
                temporada.Inicio = temporadaPatchDto.Inicio.Value;
            }

            if (temporadaPatchDto.Fin.HasValue)
            {
                temporada.Fin = temporadaPatchDto.Fin.Value;
            }

            if (!string.IsNullOrEmpty(temporadaPatchDto.Nombre))
            {
                temporada.Nombre = temporadaPatchDto.Nombre;
            }

            await _context.SaveChangesAsync();

            return Ok("Temporada actualizada exitosamente.");
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
