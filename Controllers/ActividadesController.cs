using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActividadesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActividadesController(AppDbContext context)
        {
            _context = context;
        }

        // Endpoint para obtener todas las actividades
        [HttpGet("actividades")]
        public async Task<ActionResult<IEnumerable<ActividadRequestDto>>> GetActividades()
        {
            var actividades = await _context.Actividades.ToListAsync();
            if (actividades == null || !actividades.Any())
            {
                return NotFound("No se encontraron actividades.");
            }
            return Ok(actividades);
        }

        // Endpoint para registrar una actividad
        [HttpPost("register")]
        public async Task<IActionResult> ParticiparEnActividad([FromBody] ActividadRequestDto actividadRequest)
        {
            if (actividadRequest == null || string.IsNullOrEmpty(actividadRequest.Actividad) || actividadRequest.PuntosActividad == null)
            {
                return BadRequest("Datos de actividad inválidos.");
            }
            var actividad = new Actividades
            {
                Actividad = actividadRequest.Actividad,
                PuntosActividad = actividadRequest.PuntosActividad.Value,
                FechaActividad = DateTime.UtcNow.Date
            };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();
            return Ok("Actividad registrada correctamente.");
        }

        // Endpoint para registrar la participación de clientes en una actividad
        [HttpPost("participar")]
        public async Task<IActionResult> ParticiparEnActividad([FromBody] ActividadClienteDto actividadRequest)
        {
            // Validación de existencia de la actividad y el cliente
            var actividad = await ObtenerActividadPorId(actividadRequest.ActividadId);
            if (actividad == null)
                return NotFound("Actividad no encontrada.");

            var cliente = await ObtenerClientePorId(actividadRequest.ClienteId);
            if (cliente == null)
                return NotFound("Cliente no encontrado.");

            if (await ClienteYaParticipoEnActividad(actividadRequest.ClienteId, actividadRequest.ActividadId))
                return BadRequest("El cliente ya ha participado en esta actividad.");

            await RegistrarParticipacionEnActividad(actividadRequest.ClienteId, actividadRequest.ActividadId);

            await ActualizarPuntosCliente(cliente);

            await ActualizarPuntajeEnTemporada(actividadRequest.ClienteId, actividad.PuntosActividad);

            await _context.SaveChangesAsync();

            return Ok($"Cliente {cliente.Nombre} ha participado en la actividad y ha ganado {actividad.PuntosActividad} puntos.");
        }

        private async Task<Actividades> ObtenerActividadPorId(int actividadId)
        {
            return await _context.Actividades.FindAsync(actividadId);
        }

        private async Task<Cliente> ObtenerClientePorId(int clienteId)
        {
            return await _context.Clientes.FindAsync(clienteId);
        }

        private async Task<bool> ClienteYaParticipoEnActividad(int clienteId, int actividadId)
        {
            return await _context.ClienteActividades
                .AnyAsync(ca => ca.ClienteId == clienteId && ca.ActividadId == actividadId);
        }

        private async Task RegistrarParticipacionEnActividad(int clienteId, int actividadId)
        {
            var clienteActividad = new ClienteActividad
            {
                ClienteId = clienteId,
                ActividadId = actividadId
            };
            _context.ClienteActividades.Add(clienteActividad);
        }

        private async Task ActualizarPuntosCliente(Cliente cliente)
        {
            cliente.PuntosGenerales += 2;
        }

        private async Task ActualizarPuntajeEnTemporada(int clienteId, int puntosActividad)
        {
            var temporadaActiva = await _context.Temporadas
                .Where(t => t.EstaDisponible)
                .FirstOrDefaultAsync();

            if (temporadaActiva == null)
                return;

            var puntajeExistente = await _context.Puntajes
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.TemporadaId == temporadaActiva.Id);

            if (puntajeExistente != null)
            {
                puntajeExistente.Puntos += puntosActividad;
            }
            else
            {
                var nuevoPuntaje = new Puntajes
                {
                    ClienteId = clienteId,
                    TemporadaId = temporadaActiva.Id,
                    Puntos = puntosActividad
                };
                _context.Puntajes.Add(nuevoPuntaje);
            }
        }


    }
}
