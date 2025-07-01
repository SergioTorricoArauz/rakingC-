using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductosDescuentoController(AppDbContext context) : ControllerBase
    {
        // Metodo para obtener los descuentos de los productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductosDescuentoResponseDto>>> GetDescuentos()
        {
            var descuentos = await context.ProductosDescuentos
                .Include(pd => pd.Producto)
                .Select(pd => new ProductosDescuentoResponseDto
                {
                    Id = pd.Id,
                    ProductoId = pd.ProductoId,
                    CantidadMaximaClientes = pd.CantidadMaximaClientes,
                    Descuento = pd.Descuento,
                    FechaInicio = pd.FechaInicio,
                    FechaFin = pd.FechaFin,
                    CantidadComprada = pd.CantidadComprada,
                    // Datos del producto desde la relación
                    Nombre = pd.Producto.Nombre,
                    Descripcion = pd.Producto.Descripcion,
                    Precio = pd.Producto.Precio,
                    EstaDisponible = pd.Producto.EstaDisponible,
                    Categoria = pd.Producto.Categoria
                })
                .ToListAsync();

            return Ok(descuentos);
        }

        // Metodo para obtener un descuento por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductosDescuentoResponseDto>> GetDescuentoById(int id)
        {
            var descuento = await context.ProductosDescuentos
                .Include(pd => pd.Producto)
                .Where(pd => pd.Id == id)
                .Select(pd => new ProductosDescuentoResponseDto
                {
                    Id = pd.Id,
                    ProductoId = pd.ProductoId,
                    CantidadMaximaClientes = pd.CantidadMaximaClientes,
                    Descuento = pd.Descuento,
                    FechaInicio = pd.FechaInicio,
                    FechaFin = pd.FechaFin,
                    CantidadComprada = pd.CantidadComprada,
                    // Datos del producto desde la relación
                    Nombre = pd.Producto.Nombre,
                    Descripcion = pd.Producto.Descripcion,
                    Precio = pd.Producto.Precio,
                    EstaDisponible = pd.Producto.EstaDisponible,
                    Categoria = pd.Producto.Categoria,
                })
                .FirstOrDefaultAsync();

            if (descuento == null)
            {
                return NotFound();
            }

            return Ok(descuento);
        }

        // Corregir el método GetContext (Problema 1)
        [HttpGet("context")]
        public ActionResult<object> GetContext() // Removido async ya que no usa await
        {
            return Ok("Context information"); // Cambiar por información útil o eliminar el método
        }

        // Metodo para crear un nuevo descuento
        [HttpPost]
        public async Task<ActionResult<ProductosDescuentoResponseDto>> CreateDescuento([FromBody] ProductosDescuentoPostDto request)
        {
            if (request == null)
            {
                return BadRequest("Los datos del descuento son inválidos.");
            }

            // Validar que el producto existe
            var producto = await context.Productos.FindAsync(request.ProductoId);
            if (producto == null)
            {
                return BadRequest("El producto especificado no existe.");
            }

            // Validaciones adicionales
            if (request.Descuento <= 0 || request.Descuento > 100)
            {
                return BadRequest("El descuento debe estar entre 1 y 100.");
            }

            if (request.FechaInicio >= request.FechaFin)
            {
                return BadRequest("La fecha de inicio debe ser anterior a la fecha de fin.");
            }

            try
            {
                var descuento = new ProductosDescuento
                {
                    ProductoId = request.ProductoId,
                    CantidadMaximaClientes = request.CantidadMaximaClientes,
                    Descuento = request.Descuento,
                    FechaInicio = request.FechaInicio.ToUniversalTime(), // Convertir a UTC
                    FechaFin = request.FechaFin.ToUniversalTime(), // Convertir a UTC
                    CantidadComprada = 0, // Siempre iniciar en 0
                    Producto = producto
                };

                context.ProductosDescuentos.Add(descuento);
                await context.SaveChangesAsync();

                var response = new ProductosDescuentoResponseDto
                {
                    Id = descuento.Id,
                    ProductoId = descuento.ProductoId,
                    CantidadMaximaClientes = descuento.CantidadMaximaClientes,
                    Descuento = descuento.Descuento,
                    FechaInicio = descuento.FechaInicio,
                    FechaFin = descuento.FechaFin,
                    CantidadComprada = descuento.CantidadComprada,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio,
                    EstaDisponible = producto.EstaDisponible,
                    Categoria = producto.Categoria
                };

                return CreatedAtAction(nameof(GetDescuentoById), new { id = response.Id }, response);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, "Error al crear el descuento en la base de datos.");
            }
        }
    }
}
