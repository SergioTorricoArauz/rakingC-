using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;
using RankingCyY.Utils;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClienteController(AppDbContext context) : ControllerBase
    {
        // GET: Obtener todos los clientes con filtros funcionales
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteResponseDto>>> GetClientes(
            [FromQuery] string? sortBy = "puntos",
            [FromQuery] int? minPoints = null,
            [FromQuery] int? maxPoints = null,
            [FromQuery] bool? isSuperUser = null)
        {
            var clientes = await context.Clientes.ToListAsync();

            if (!clientes.Any())
                return NotFound("No se encontraron clientes.");

            var clientesFiltrados = clientes.AsEnumerable();

            if (minPoints.HasValue || maxPoints.HasValue)
            {
                clientesFiltrados = ClienteFilters.FilterByPointsRange(
                    clientesFiltrados, 
                    minPoints ?? 0, 
                    maxPoints ?? int.MaxValue);
            }

            if (isSuperUser.HasValue)
            {
                clientesFiltrados = ClienteFilters.FilterBySuperUser(clientesFiltrados, isSuperUser);
            }

            // 7. Pattern Matching para ordenar clientes
            clientesFiltrados = ClienteFilters.SortBy(clientesFiltrados, sortBy ?? "puntos");
            // 3. Inmutable List
            var clienteDtos = ClienteMappers.ToResponseDtos(clientesFiltrados);
            
            return Ok(clienteDtos);
        }

        // GET: Obtener cliente por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteResponseDto>> GetCliente(int id)
        {
            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound($"Cliente con ID {id} no encontrado.");

            var clienteDto = ClienteMappers.ToResponseDto(cliente);
            return Ok(clienteDto);
        }

        // GET: Obtener insignias de un cliente
        [HttpGet("{clienteId}/insignias")]
        public async Task<ActionResult<IEnumerable<InsigniaResponseDto>>> GetInsigniasCliente(int clienteId)
        {
            var clienteInsignias = await context.ClienteInsignias
                .Where(ci => ci.ClienteId == clienteId)
                .Include(ci => ci.Insignia)
                .ToListAsync();

            if (!clienteInsignias.Any())
                return NotFound("El cliente no tiene insignias.");

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

        // POST: Crear un nuevo cliente con validaciones funcionales puras
        [HttpPost("register")]
        public async Task<ActionResult<ClienteResponseDto>> PostCliente(ClientePostDto clienteDto)
        {
            // 4. Funcion pura para validar cliente
            var (isValid, errorMessage) = ClienteValidators.ValidateCliente(
                clienteDto.Nombre, 
                clienteDto.Email, 
                clienteDto.Password, 
                clienteDto.PuntosGenerales);

            if (!isValid)
                return BadRequest(errorMessage);

            bool emailExists = await context.Clientes
                // 2. Funcion Lambda para validar email
                .AnyAsync(c => c.Email.Equals(clienteDto.Email, StringComparison.CurrentCultureIgnoreCase));
            
            if (emailExists)
                return Conflict("El correo electrónico ya está registrado.");

            var cliente = ClienteMappers.FromPostDto(clienteDto);
            
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            
            var responseDto = ClienteMappers.ToResponseDto(cliente);
            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, responseDto);
        }

        // POST: Sumar puntos usando lógica funcional
        [HttpPost("{clienteId}/sumarPuntos")]
        public async Task<IActionResult> SumarPuntosGenerales(int clienteId, [FromBody] int puntos)
        {
            var cliente = await context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound("Cliente no encontrado.");

            var puntosConBonificacion = InsigniaRules.CalculatePuntosConBonificacion(puntos, cliente.IsSuperUser);
            cliente.PuntosGenerales += puntosConBonificacion;

            if (!cliente.IsSuperUser && InsigniaRules.MereceSuperUser(cliente.PuntosGenerales, cliente.FechaRegistro))
            {
                cliente.IsSuperUser = true;
            }

            await context.SaveChangesAsync();

            var responseDto = ClienteMappers.ToResponseDto(cliente);
            return Ok(responseDto);
        }

        // POST: Asignar insignias usando reglas funcionales puras
        [HttpPost("{clienteId}/asignarInsignias")]
        public async Task<IActionResult> AsignarInsignias(int clienteId)
        {
            var cliente = await context.Clientes.FindAsync(clienteId);
            if (cliente == null)
                return NotFound("Cliente no encontrado.");

            var insigniasElegibles = InsigniaRules.GetInsigniasElegibles(cliente.PuntosGenerales);
            var insigniasOtorgadas = new List<string>();

            foreach (var nombreInsignia in insigniasElegibles)
            {
                var insignia = await context.Insignias
                    .FirstOrDefaultAsync(i => i.Nombre == nombreInsignia);

                if (insignia != null &&
                    !await context.ClienteInsignias
                        .AnyAsync(ci => ci.ClienteId == clienteId && ci.InsigniaId == insignia.Id))
                {
                    var clienteInsignia = new ClienteInsignia
                    {
                        ClienteId = clienteId,
                        InsigniaId = insignia.Id,
                        FechaOtorgada = DateTime.UtcNow
                    };

                    context.ClienteInsignias.Add(clienteInsignia);
                    insigniasOtorgadas.Add(nombreInsignia);
                }
            }

            if (!insigniasOtorgadas.Any())
                return BadRequest("El cliente no cumple con los requisitos para nuevas insignias.");

            await context.SaveChangesAsync();
            return Ok($"Insignias otorgadas: {string.Join(", ", insigniasOtorgadas)}");
        }

        // GET: Endpoint para demostrar closures
        [HttpGet("filtros-avanzados")]
        public async Task<ActionResult<IEnumerable<ClienteResponseDto>>> GetClientesConFiltrosAvanzados(
            [FromQuery] int minPoints = 100,
            [FromQuery] string domain = "gmail.com")
        {
            var clientes = await context.Clientes.ToListAsync();

            // 6. Usar closures para filtros avanzados
            var pointsFilter = ClienteClosures.CreatePointsFilter(minPoints);
            var domainValidator = ClienteClosures.CreateEmailDomainValidator(domain);

            // 1. Usar closures para filtrar clientes
            var clientesFiltrados = clientes
                .Where(pointsFilter)
                .Where(c => domainValidator(c.Email))
                .ToList();

            var clienteDtos = ClienteMappers.ToResponseDtos(clientesFiltrados);
            return Ok(clienteDtos);
        }

        // DELETE: Eliminar cliente
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound($"Cliente con ID {id} no encontrado.");

            context.Clientes.Remove(cliente);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
