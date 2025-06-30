using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CarritoController(AppDbContext context) : ControllerBase
    {

        // Crea un carrito vacío para el cliente si no tiene uno ACTIVO.
        [HttpPost("crear")]
        public async Task<IActionResult> CrearCarrito(CarritoPostDto dto)
        {
            var yaExiste = await context.Carrito.AnyAsync(c => c.ClienteId == dto.ClienteId && c.Estado == "ACTIVO");
            if (yaExiste)
                return BadRequest("Ya tienes un carrito activo.");

            var carrito = new Carrito
            {
                ClienteId = dto.ClienteId,
                Estado = "ACTIVO",
                FechaCreacion = DateTime.UtcNow,
                Total = 0
            };
            context.Carrito.Add(carrito);
            await context.SaveChangesAsync();
            return Ok(carrito.Id);
        }

        // Agregar productos al carrito
        [HttpPost("agregar-producto")]
        public async Task<IActionResult> AgregarProductoAlCarrito(CarritoArticuloPost dto)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .FirstOrDefaultAsync(c => c.Id == dto.CarritoId);

            if (carrito == null || carrito.Estado != "ACTIVO")
                return BadRequest("Carrito no encontrado o no activo.");

            var producto = await context.Productos
                .Include(p => p.ProductosDescuentos)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            if (producto == null || !producto.EstaDisponible)
                return BadRequest("Producto no disponible.");

            // Lógica para descuento vigente
            var descuento = producto.ProductosDescuentos
                .FirstOrDefault(d => d.FechaInicio <= DateTime.UtcNow && d.FechaFin >= DateTime.UtcNow);

            // Solo verificar límite de cupos si tiene descuento activo
            if (descuento != null)
            {
                if (producto.CantidadComprada + dto.Cantidad > producto.CantidadMaximaClientes)
                    return BadRequest("No hay suficientes cupos para este producto con descuento.");
            }

            decimal precioFinal = descuento != null
                ? producto.Precio - (producto.Precio * descuento.Descuento / 100)
                : producto.Precio;

            // Crear o actualizar articulo en el carrito
            var articulo = carrito.Articulos.FirstOrDefault(a => a.ProductoId == dto.ProductoId);
            if (articulo == null)
            {
                articulo = new CarritoArticulos
                {
                    CarritoId = carrito.Id,
                    ProductoId = producto.Id,
                    Cantidad = dto.Cantidad,
                    PrecioUnitario = precioFinal
                };
                context.CarritoArticulos.Add(articulo);
            }
            else
            {
                // Solo verificar límite de cupos para actualización si tiene descuento activo
                if (descuento != null && producto.CantidadComprada + articulo.Cantidad + dto.Cantidad > producto.CantidadMaximaClientes)
                    return BadRequest("No hay suficientes cupos para este producto con descuento.");
                    
                articulo.Cantidad += dto.Cantidad;
                articulo.PrecioUnitario = precioFinal;
            }

            // Recalcular total
            carrito.Total = carrito.Articulos.Sum(a => a.Cantidad * a.PrecioUnitario);

            await context.SaveChangesAsync();
            return Ok("Producto agregado al carrito.");
        }

        // Elimina un producto del carrito
        [HttpDelete("quitar-producto/{carritoId}/{productoId}")]
        public async Task<IActionResult> QuitarProducto(int carritoId, int productoId)
        {
            var articulo = await context.CarritoArticulos
                .FirstOrDefaultAsync(a => a.CarritoId == carritoId && a.ProductoId == productoId);

            if (articulo == null)
                return NotFound("Artículo no encontrado en el carrito.");

            context.CarritoArticulos.Remove(articulo);

            var carrito = await context.Carrito.Include(c => c.Articulos)
                .FirstOrDefaultAsync(c => c.Id == carritoId);
            if (carrito != null)
                carrito.Total = carrito.Articulos.Where(a => a.Id != articulo.Id)
                    .Sum(a => a.Cantidad * a.PrecioUnitario);

            await context.SaveChangesAsync();
            return Ok("Producto eliminado del carrito.");
        }

        // Obtener carrito y artículos
        [HttpGet("{clienteId}")]
        public async Task<IActionResult> ObtenerCarrito(int clienteId)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .ThenInclude(a => a.Producto)
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Estado == "ACTIVO");

            if (carrito == null)
                return NotFound("No tienes un carrito activo.");

            var carritoDto = new CarritoResponseDto
            {
                Id = carrito.Id,
                ClienteId = carrito.ClienteId,
                Estado = carrito.Estado,
                FechaCreacion = carrito.FechaCreacion,
                Total = carrito.Total,
                Articulos = carrito.Articulos.Select(a => new CarritoArticuloRespondeDto
                {
                    Id = a.Id,
                    ProductoId = a.ProductoId,
                    NombreProducto = a.Producto.Nombre,
                    Cantidad = a.Cantidad,
                    PrecioUnitario = a.PrecioUnitario,
                    SubTotal = a.Cantidad * a.PrecioUnitario
                }).ToList()
            };

            return Ok(carritoDto);
        }

        // Finalizar/Pagar carrito
        [HttpPost("finalizar/{carritoId}")]
        public async Task<IActionResult> FinalizarCarrito(int carritoId)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .ThenInclude(a => a.Producto)
                .FirstOrDefaultAsync(c => c.Id == carritoId && c.Estado == "ACTIVO");

            if (carrito == null)
                return NotFound("Carrito no encontrado o ya pagado.");

            // Validación final de cupos y disponibilidad
            foreach (var articulo in carrito.Articulos)
            {
                var producto = await context.Productos
                    .Include(p => p.ProductosDescuentos)
                    .FirstOrDefaultAsync(p => p.Id == articulo.ProductoId);

                if (producto == null || !producto.EstaDisponible)
                    return BadRequest($"El producto {articulo.Producto.Nombre} ya no está disponible.");

                // Verificar si hay descuento activo
                var descuento = producto.ProductosDescuentos
                    .FirstOrDefault(d => d.FechaInicio <= DateTime.UtcNow && d.FechaFin >= DateTime.UtcNow);

                // Validar límites según si tiene descuento o no
                if (descuento != null)
                {
                    if (producto.CantidadComprada + articulo.Cantidad > descuento.CantidadMaximaClientes)
                        return BadRequest($"No hay suficientes cupos para el producto {producto.Nombre} con descuento.");
                }

                // Actualizar cantidad comprada
                producto.CantidadComprada += articulo.Cantidad;

                // Si tiene descuento, usar el límite del descuento
                if (descuento != null)
                {
                    if (producto.CantidadComprada >= descuento.CantidadMaximaClientes)
                        producto.EstaDisponible = false;
                }
                // Si no tiene descuento, usar el límite del producto
                else if (producto.CantidadComprada >= producto.CantidadMaximaClientes)
                {
                    producto.EstaDisponible = false;
                }
            }

            carrito.Estado = "FINALIZADO";

            // Aquí puedes asignar puntos al cliente si quieres (por cada compra)
            // var cliente = await context.Clientes.FindAsync(carrito.ClienteId);
            // cliente.PuntosGenerales += lógica de puntos;

            await context.SaveChangesAsync();
            return Ok("Compra realizada con éxito.");
        }

        [HttpGet("historial/{clienteId}")]
        public async Task<IActionResult> HistorialCarritos(int clienteId)
        {
            var carritos = await context.Carrito
                .Where(c => c.ClienteId == clienteId && c.Estado != "ACTIVO")
                .Include(c => c.Articulos)
                .ThenInclude(a => a.Producto)
                .OrderByDescending(c => c.FechaCreacion)
                .Select(c => new CarritoResponseDto
                {
                    Id = c.Id,
                    ClienteId = c.ClienteId,
                    Estado = c.Estado,
                    FechaCreacion = c.FechaCreacion,
                    Total = c.Total,
                    Articulos = c.Articulos.Select(a => new CarritoArticuloRespondeDto
                    {
                        Id = a.Id,
                        ProductoId = a.ProductoId,
                        NombreProducto = a.Producto.Nombre,
                        Cantidad = a.Cantidad,
                        PrecioUnitario = a.PrecioUnitario,
                        SubTotal = a.Cantidad * a.PrecioUnitario
                    }).ToList()
                })
                .ToListAsync();

            return Ok(carritos);
        }

        [HttpPost("vaciar/{carritoId}")]
        public async Task<IActionResult> VaciarCarrito(int carritoId)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .FirstOrDefaultAsync(c => c.Id == carritoId && c.Estado == "ACTIVO");

            if (carrito == null)
                return NotFound("Carrito no encontrado o no activo.");

            context.CarritoArticulos.RemoveRange(carrito.Articulos);
            carrito.Total = 0;
            await context.SaveChangesAsync();

            return Ok("Carrito vaciado.");
        }



    }
}
