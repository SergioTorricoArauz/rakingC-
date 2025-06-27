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
        public async Task<ActionResult<IEnumerable<ProductosDescuentoResponse>>> GetDescuentos()
        {
            var descuentos = await context.ProductosDescuentos
                .Include(pd => pd.Producto)
                .Select(pd => new ProductosDescuentoResponse
                {
                    Id = pd.Id,
                    ProductoId = pd.ProductoId,
                    CantidadMaximaClientes = pd.CantidadMaximaClientes,
                    Descuento = pd.Descuento,
                    FechaInicio = pd.FechaInicio,
                    FechaFin = pd.FechaFin,
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
        public async Task<ActionResult<ProductosDescuentoResponse>> GetDescuentoById(int id)
        {
            var descuento = await context.ProductosDescuentos
                .Include(pd => pd.Producto)
                .Where(pd => pd.Id == id)
                .Select(pd => new ProductosDescuentoResponse
                {
                    Id = pd.Id,
                    ProductoId = pd.ProductoId,
                    CantidadMaximaClientes = pd.CantidadMaximaClientes,
                    Descuento = pd.Descuento,
                    FechaInicio = pd.FechaInicio,
                    FechaFin = pd.FechaFin,
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

        // Si tienes un método GetContext, necesita un atributo HTTP explícito y una ruta única
        [HttpGet("context")]
        public async Task<ActionResult<object>> GetContext()
        {
            return Ok(context);
        }

        // Metodo para crear un nuevo descuento
        [HttpPost]
        public async Task<ActionResult<ProductosDescuentoResponse>> CreateDescuento([FromBody] ProductosDescuentoPost request, AppDbContext context)
        {

            if (request == null)
            {
                return BadRequest("Los datos del descuento son inválidos.");
            }

            var descuento = new ProductosDescuento
            {
                ProductoId = request.ProductoId,
                CantidadMaximaClientes = request.CantidadMaximaClientes,
                Descuento = request.Descuento,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                // Asignar el producto relacionado
                Producto = await context.Productos.FindAsync(request.ProductoId)
            };
            context.ProductosDescuentos.Add(descuento);
            await context.SaveChangesAsync();

            var response = new ProductosDescuentoResponse
            {
                Id = descuento.Id,
                ProductoId = descuento.ProductoId,
                CantidadMaximaClientes = descuento.CantidadMaximaClientes,
                Descuento = descuento.Descuento,
                FechaInicio = descuento.FechaInicio,
                FechaFin = descuento.FechaFin,
            };
            return CreatedAtAction(nameof(GetDescuentoById), new { id = response.Id }, response);
        }
    }
}
