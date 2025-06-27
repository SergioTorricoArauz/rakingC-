using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductoController(AppDbContext context) : ControllerBase
    {
        // Metodo para obtener todos los productos disponibles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetProductos()
        {
            var productos = await context.Productos
                .Where(p => p.EstaDisponible)
                .Select(p => new ProductoResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    CantidadMaximaClientes = p.CantidadMaximaClientes,
                    CantidadComprada = p.CantidadComprada,
                    EstaDisponible = p.EstaDisponible,
                    Categoria = p.Categoria
                })
                .ToListAsync();
            if (productos == null || productos.Count == 0)
            {
                return NotFound("No se encontraron productos disponibles.");
            }
            return Ok(productos);
        }

        // Metodo para obtener un producto por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDto>> GetProducto(int id)
        {
            var producto = await context.Productos
                .Where(p => p.Id == id && p.EstaDisponible)
                .Select(p => new ProductoResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    CantidadMaximaClientes = p.CantidadMaximaClientes,
                    CantidadComprada = p.CantidadComprada,
                    EstaDisponible = p.EstaDisponible,
                    Categoria = p.Categoria
                })
                .FirstOrDefaultAsync();
            if (producto == null)
            {
                return NotFound($"Producto con ID {id} no encontrado o no disponible.");
            }
            return Ok(producto);
        }

        // Metodo para crear un nuevo producto
        [HttpPost]
        public async Task<ActionResult<ProductoResponseDto>> CreateProducto([FromBody] ProductoPostDto productoDto)
        {
            if (productoDto == null)
            {
                return BadRequest("El producto no puede ser nulo.");
            }

            var producto = new Productos
            {
                Nombre = productoDto.Nombre,
                Descripcion = productoDto.Descripcion,
                Precio = productoDto.Precio,
                CantidadMaximaClientes = productoDto.CantidadMaximaClientes,
                CantidadComprada = productoDto.CantidadComprada,
                EstaDisponible = productoDto.EstaDisponible,
                FechaCreacion = DateTime.UtcNow,
                Categoria = productoDto.Categoria
            };

            context.Productos.Add(producto);
            await context.SaveChangesAsync();

            var responseDto = new ProductoResponseDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CantidadMaximaClientes = producto.CantidadMaximaClientes,
                CantidadComprada = producto.CantidadComprada,
                EstaDisponible = producto.EstaDisponible,
                Categoria = producto.Categoria
            };

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, responseDto);
        }
    }
}
