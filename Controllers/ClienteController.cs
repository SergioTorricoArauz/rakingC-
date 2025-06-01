using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Data.dto;
using RankingCyY.Models;
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
