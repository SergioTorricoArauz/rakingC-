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
                Nombre = t.Nombre,
                EstaDisponible = t.EstaDisponible
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
                Nombre = temporada.Nombre,
                EstaDisponible = temporada.EstaDisponible
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

        // Asignar insignias a los 3 primeros clientes de una temporada específica
        [HttpPost("asignarInsignias/temporada/{temporadaId}")]
        public async Task<IActionResult> AsignarInsigniasTemporada(int temporadaId)
        {
            // Obtener el ranking de puntajes por temporada
            var ranking = await GetRankingPorTemporadaHelper(temporadaId);

            // Si no hay clientes en el ranking
            if (!ranking.Any())
            {
                return NotFound("No hay clientes en la temporada especificada.");
            }

            // Obtener las insignias de temporada
            var insignias = await _context.Insignias
                .Where(i => i.Nombre.StartsWith("Temporada Top"))
                .ToListAsync();

            // Verificar que existan las insignias
            if (!insignias.Any())
            {
                return NotFound("No se encontraron insignias de temporada.");
            }

            var insigniasOtorgadas = new List<string>();

            // Asignar las insignias a los tres primeros clientes
            for (int i = 0; i < 3; i++)  // Solo asignamos a los primeros 3 clientes
            {
                var puntaje = ranking[i];
                Insignias insignia = null;

                // Asignar la insignia correspondiente según el puesto en el ranking
                if (i == 0)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 1");
                }
                else if (i == 1)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 2");
                }
                else if (i == 2)
                {
                    insignia = insignias.FirstOrDefault(i => i.Nombre == "Temporada Top 3");
                }

                // Verificar si la insignia se encuentra y asignarla si el cliente no la tiene ya
                if (insignia != null && !await _context.ClienteInsignias.AnyAsync(ci => ci.ClienteId == puntaje.Id && ci.InsigniaId == insignia.Id))
                {
                    // Asignar la insignia
                    _context.ClienteInsignias.Add(new ClienteInsignia
                    {
                        ClienteId = puntaje.Id,
                        InsigniaId = insignia.Id,
                        FechaOtorgada = DateTime.UtcNow.Date  // Fecha de otorgamiento
                    });

                    insigniasOtorgadas.Add(insignia.Nombre);  // Guardar el nombre de la insignia otorgada
                }
            }

            // Desactivar la temporada solo si no está ya desactivada utilizando el método de actualización
            var temporada = await _context.Temporadas.FindAsync(temporadaId);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {temporadaId} no encontrada.");
            }

            // Actualizar el estado de la temporada a no disponible
            if (temporada.EstaDisponible)
            {
                temporada.EstaDisponible = false;  // Desactivar la temporada
            }
            else
            {
                return BadRequest("La temporada ya está desactivada.");
            }

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();

            return Ok($"Insignias otorgadas: {string.Join(", ", insigniasOtorgadas)}");
        }


        // Método auxiliar para obtener el ranking por temporada
        private async Task<List<PuntajeResponseDto>> GetRankingPorTemporadaHelper(int temporadaId)
        {
            // Obtener el nombre de la temporada antes de la consulta del ranking
            var temporada = await _context.Temporadas
                .Where(t => t.Id == temporadaId)
                .Select(t => t.Nombre)
                .FirstOrDefaultAsync();

            // Consultar el ranking con el nombre de la temporada
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
                    (r, cliente) => new PuntajeResponseDto
                    {
                        Id = r.ClienteId,  // Asignar el ID del Cliente
                        ClienteNombre = cliente.Nombre,  // Nombre del cliente
                        Puntos = r.PuntosTotales,  // Puntos totales por temporada
                        TemporadaNombre = temporada  // Usar el nombre de la temporada ya obtenido
                    })
                .ToListAsync();

            return ranking;
        }

        // Actualizar una temporada por ID (PATCH)
        [HttpPatch("{temporadaId}")]
        public async Task<IActionResult> UpdateTemporada(int temporadaId, [FromBody] TemporadaPatchDto temporadaPatchDto)
        {
            // Buscar la temporada
            var temporada = await _context.Temporadas.FindAsync(temporadaId);
            if (temporada == null)
            {
                return NotFound($"Temporada con ID {temporadaId} no encontrada.");
            }

            // Solo actualizamos los campos que se proporcionan
            if (temporadaPatchDto.EstaDisponible.HasValue)
            {
                temporada.EstaDisponible = temporadaPatchDto.EstaDisponible.Value;  // Actualiza solo si el valor es proporcionado
            }

            if (temporadaPatchDto.Inicio.HasValue)
            {
                temporada.Inicio = temporadaPatchDto.Inicio.Value;  // Actualiza solo si el valor es proporcionado
            }

            if (temporadaPatchDto.Fin.HasValue)
            {
                temporada.Fin = temporadaPatchDto.Fin.Value;  // Actualiza solo si el valor es proporcionado
            }

            if (!string.IsNullOrEmpty(temporadaPatchDto.Nombre))
            {
                temporada.Nombre = temporadaPatchDto.Nombre;  // Actualiza solo si el valor es proporcionado
            }

            // Guardar los cambios en la base de datos
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
