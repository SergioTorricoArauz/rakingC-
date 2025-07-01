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
        // Metodo para obtener productos con paginación y filtros
        [HttpGet]
        public async Task<IActionResult> GetProductos(
            [FromQuery] int? categoria,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (page <= 0 || pageSize <= 0) return BadRequest("Parámetros de paginación inválidos.");

            var now = DateTime.UtcNow;
            var query = context.Productos.AsNoTracking().Where(p => p.EstaDisponible);

            if (categoria is not null)
                query = query.Where(p => p.Categoria == categoria);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Nombre.Contains(search) || p.Descripcion.Contains(search));

            var total = await query.CountAsync(ct);

            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductoResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    CantidadMaximaClientes = p.ProductosDescuentos
                        .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                        .OrderByDescending(d => d.Descuento)
                        .Select(d => d.CantidadMaximaClientes)
                        .FirstOrDefault() != 0 
                            ? p.ProductosDescuentos
                                .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                                .OrderByDescending(d => d.Descuento)
                                .Select(d => d.CantidadMaximaClientes)
                                .FirstOrDefault()
                            : p.CantidadMaximaClientes,
                    CantidadComprada = p.CantidadComprada,
                    EstaDisponible = p.EstaDisponible,
                    Categoria = p.Categoria
                })
                .ToListAsync(ct);

            return Ok(new { total, productos });
        }


        // Metodo para obtener un producto por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDto>> GetProducto(int id)
        {
            var now = DateTime.UtcNow;
            var producto = await context.Productos
                .Where(p => p.Id == id && p.EstaDisponible)
                .Select(p => new ProductoResponseDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    CantidadMaximaClientes = p.ProductosDescuentos
                        .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                        .OrderByDescending(d => d.Descuento)
                        .Select(d => d.CantidadMaximaClientes)
                        .FirstOrDefault() != 0 
                            ? p.ProductosDescuentos
                                .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                                .OrderByDescending(d => d.Descuento)
                                .Select(d => d.CantidadMaximaClientes)
                                .FirstOrDefault()
                            : p.CantidadMaximaClientes,
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

        // Obtener productos disponibles y con descuento activo
        [HttpGet("con-descuento")]
        public async Task<IActionResult> GetProductosConDescuento()
        {
            var now = DateTime.UtcNow;
            var productos = await context.Productos
                .Where(p => p.EstaDisponible && p.ProductosDescuentos.Any(d => d.FechaInicio <= now && d.FechaFin >= now))
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.Precio,
                    CantidadMaximaClientes = p.ProductosDescuentos
                        .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                        .OrderByDescending(d => d.Descuento)
                        .Select(d => d.CantidadMaximaClientes)
                        .FirstOrDefault(),
                    DescuentoActivo = p.ProductosDescuentos
                        .Where(d => d.FechaInicio <= now && d.FechaFin >= now)
                        .OrderByDescending(d => d.Descuento)
                        .Select(d => d.Descuento)
                        .FirstOrDefault()
                })
                .ToListAsync();
            return Ok(productos);
        }


        // Metodo para crear un nuevo producto
        [HttpPost]
        public async Task<ActionResult<ProductoResponseDto>> CreateProducto([FromBody] ProductoPostDto productoDto)
        {
            if (productoDto == null)
            {
                return BadRequest("El producto no puede ser nulo.");
            }

            if (string.IsNullOrWhiteSpace(productoDto.Nombre))
                return BadRequest("El nombre del producto es requerido.");

            if (string.IsNullOrWhiteSpace(productoDto.Descripcion))
                return BadRequest("La descripción del producto es requerida.");

            if (productoDto.Precio < 0)
                return BadRequest("El precio no puede ser negativo.");

            if (productoDto.Categoria <= 0 || productoDto.Categoria > 3)
                return BadRequest("Categoría inválida. Debe ser 1 (IMPRESIONES), 2 (SESIONES) o 3 (CONTRATOS).");

            if (await context.Productos.AnyAsync(p => p.Nombre == productoDto.Nombre && p.Categoria == productoDto.Categoria))
                return BadRequest("Ya existe un producto con ese nombre en esa categoría.");

            try
            {
                var producto = new Productos
                {
                    Nombre = productoDto.Nombre.Trim(),
                    Descripcion = productoDto.Descripcion.Trim(),
                    Precio = productoDto.Precio,
                    CantidadMaximaClientes = productoDto.CantidadMaximaClientes,
                    CantidadComprada = 0,
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
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error al crear el producto en la base de datos." + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound($"Producto con ID {id} no encontrado.");
            }
            context.Productos.Remove(producto);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
