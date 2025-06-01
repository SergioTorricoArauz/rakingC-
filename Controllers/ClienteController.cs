using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;
using System.Net.Mail;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        // Obtener todos los clientes desde la base de datos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteResponseDto>>> GetClientes()
        {
            var clientes = await _context.Clientes.ToListAsync();
            if (clientes == null || !clientes.Any())
            {
                return NotFound("No se encontraron clientes.");
            }
            var clienteDtos = clientes.Select(c => new ClienteResponseDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Email = c.Email,
                PuntosGenerales = c.PuntosGenerales,
                FechaRegistro = c.FechaRegistro
            }).ToList();
            return Ok(clienteDtos);
        }

        // Obtener un cliente por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteResponseDto>> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound($"Cliente con ID {id} no encontrado.");
            }
            var clienteDto = new ClienteResponseDto
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Email = cliente.Email,
                PuntosGenerales = cliente.PuntosGenerales,
                FechaRegistro = cliente.FechaRegistro
            };
            return Ok(clienteDto);
        }

        // Obtener insignias de un cliente
        [HttpGet("{clienteId}/insignias")]
        public async Task<ActionResult<IEnumerable<InsigniaResponseDto>>> GetInsigniasCliente(int clienteId)
        {
            var clienteInsignias = await _context.ClienteInsignias
                .Where(ci => ci.ClienteId == clienteId)
                .Include(ci => ci.Insignia)
                .ToListAsync();

            if (clienteInsignias == null || !clienteInsignias.Any())
            {
                return NotFound("El cliente no tiene insignias.");
            }

            var insigniasDto = clienteInsignias.Select(ci => new InsigniaResponseDto
            {
                Id = ci.Insignia.Id,
                Nombre = ci.Insignia.Nombre,
                Requisitos = ci.Insignia.Requisitos,
                FechaInicio = ci.Insignia.FechaInicio,
                FechaFin = ci.Insignia.FechaFin,
                FechaOtorgada = ci.FechaOtorgada
            }).ToList();

            return Ok(insigniasDto);
        }


        // Crear un nuevo cliente
        [HttpPost("register")]
        public async Task<ActionResult<IEnumerable<Cliente>>> PostCliente(ClientePostDto clienteDto)
        {
            // Validación de correo electrónico
            try
            {
                var mail = new MailAddress(clienteDto.Email);
            }
            catch
            {
                return BadRequest("El correo electrónico no es válido.");
            }

            // Validar si el correo ya existe
            bool emailExists = await _context.Clientes
                .AnyAsync(c => c.Email.ToLower() == clienteDto.Email.ToLower());
            if (emailExists)
            {
                return Conflict("El correo electrónico ya está registrado.");
            }

            var cliente = new Cliente
            {
                Nombre = clienteDto.Nombre,
                Email = clienteDto.Email,
                Password = clienteDto.Password,
                PuntosGenerales = clienteDto.PuntosGenerales,
                FechaRegistro = DateTime.UtcNow
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetClientes), new { id = cliente.Id }, cliente);
        }

        // Agregar puntos a un cliente
        [HttpPost("{clienteId}/sumarPuntos")]
        public async Task<IActionResult> SumarPuntosGenerales(int clienteId, [FromBody] int puntos)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);

            if (cliente == null)
            {
                return NotFound("Cliente no encontrado.");
            }

            // Sumar los puntos a los puntos generales
            cliente.PuntosGenerales += puntos;

            // Guardar los cambios
            await _context.SaveChangesAsync();

            return Ok(cliente);
        }

        [HttpPost("{clienteId}/asignarInsignias")]
        public async Task<IActionResult> AsignarInsignias(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound("Cliente no encontrado.");

            // Define las reglas de insignias en una lista
            var reglasInsignias = new List<(string Nombre, int PuntosMinimos)>
    {
        ("Cliente Plata", 100),
        ("Cliente Oro", 200),
        ("Cliente Premium", 300)
    };

            var insigniasOtorgadas = new List<string>();

            foreach (var regla in reglasInsignias)
            {
                // Solo asignar la insignia si el cliente cumple con los requisitos de puntos
                if (cliente.PuntosGenerales >= regla.PuntosMinimos)
                {
                    // Verificar si la insignia ya existe y si no ha sido otorgada previamente
                    var insignia = await _context.Insignias
                        .FirstOrDefaultAsync(i => i.Nombre == regla.Nombre);

                    if (insignia != null &&
                        !await _context.ClienteInsignias
                            .AnyAsync(ci => ci.ClienteId == clienteId && ci.InsigniaId == insignia.Id))
                    {
                        // Asignar la insignia
                        var clienteInsignia = new ClienteInsignia
                        {
                            ClienteId = clienteId,
                            InsigniaId = insignia.Id,
                            FechaOtorgada = DateTime.UtcNow // Usar UTC para evitar problemas de zona horaria
                        };

                        _context.ClienteInsignias.Add(clienteInsignia);
                        insigniasOtorgadas.Add(regla.Nombre); // Registrar la insignia otorgada
                    }
                }
            }

            // Si no se otorgaron nuevas insignias, retornar un error
            if (insigniasOtorgadas.Count == 0)
                return BadRequest("El cliente no cumple con los requisitos para nuevas insignias.");

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();
            return Ok($"Insignias otorgadas: {string.Join(", ", insigniasOtorgadas)}");
        }


        // Asiganr insignia de temporada a un cliente
        [HttpPost("{clienteId}/asignarInsigniaTemporada")]
        public async Task<IActionResult> AsignarInsigniaTemporada(int clienteId, int temporadaId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            var temporada = await _context.Temporadas.FindAsync(temporadaId);

            if (cliente == null || temporada == null)
            {
                return NotFound("Cliente o temporada no encontrados.");
            }

            // Verificar si la fecha actual está dentro del rango de la temporada
            if (DateTime.Now >= temporada.Inicio && DateTime.UtcNow <= temporada.Fin)
            {
                // Lógica para otorgar la insignia de temporada
                var insignia = await _context.Insignias.FirstOrDefaultAsync(i => i.Nombre == "Reto Especial de Verano");

                if (insignia != null && cliente.PuntosGenerales >= 100) // Ejemplo: 100 puntos durante la temporada
                {
                    var clienteInsignia = new ClienteInsignia
                    {
                        ClienteId = clienteId,
                        InsigniaId = insignia.Id,
                        FechaOtorgada = DateTime.UtcNow
                    };

                    _context.ClienteInsignias.Add(clienteInsignia);
                    await _context.SaveChangesAsync();

                    return Ok("Insignia de temporada otorgada.");
                }
            }

            return BadRequest("La insignia no puede ser otorgada fuera del rango de la temporada.");
        }


        // Eliminar cliente
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound($"Cliente con ID {id} no encontrado.");
            }
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent(); 
        }
    }
}
