﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RankingCyY.Data;
using RankingCyY.Models;
using RankingCyY.Models.dto;
using RankingCyY.Hubs;
using RankingCyY.Services;

namespace RankingCyY.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CarritoController(AppDbContext context, IHubContext<CarritoHub> hubContext, IPuntosService puntosService) : ControllerBase
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

            // Notificar al cliente específico sobre el nuevo carrito
            await hubContext.Clients.Group($"Carrito_{dto.ClienteId}")
                .SendAsync("CarritoCreado", carrito.Id);

            return Ok(carrito.Id);
        }

        // Agregar productos al carrito
        [HttpPost("agregar-producto")]
        public async Task<IActionResult> AgregarProductoAlCarrito(CarritoArticuloPost dto)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .ThenInclude(a => a.Producto)
                .FirstOrDefaultAsync(c => c.Id == dto.CarritoId);

            if (carrito == null || carrito.Estado != "ACTIVO")
                return BadRequest("Carrito no encontrado o no activo.");

            var producto = await context.Productos
                .Include(p => p.ProductosDescuentos)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductoId);

            if (producto == null || !producto.EstaDisponible)
                return BadRequest("Producto no disponible.");

            var descuento = producto.ProductosDescuentos
                .FirstOrDefault(d => d.FechaInicio <= DateTime.UtcNow && d.FechaFin >= DateTime.UtcNow);

            if (descuento != null)
            {
                if (producto.CantidadComprada + dto.Cantidad > descuento.CantidadMaximaClientes)
                    return BadRequest("No hay suficientes cupos para este producto con descuento.");
            }

            decimal precioFinal = descuento != null
                ? producto.Precio - (producto.Precio * descuento.Descuento / 100)
                : producto.Precio;

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
                if (descuento != null && producto.CantidadComprada + articulo.Cantidad + dto.Cantidad > descuento.CantidadMaximaClientes)
                    return BadRequest("No hay suficientes cupos para este producto con descuento.");

                articulo.Cantidad += dto.Cantidad;
                articulo.PrecioUnitario = precioFinal;
            }

            carrito.Total = carrito.Articulos.Sum(a => a.Cantidad * a.PrecioUnitario);

            await context.SaveChangesAsync();

            // Crear el carrito actualizado para enviar via SignalR
            var carritoActualizado = await ObtenerCarritoDto(carrito.ClienteId);
            
            // Notificar al cliente específico sobre la actualización del carrito
            await hubContext.Clients.Group($"Carrito_{carrito.ClienteId}")
                .SendAsync("CarritoActualizado", carritoActualizado);

            return Ok("Producto agregado al carrito.");
        }


        // Elimina un producto del carrito
        [HttpDelete("quitar-producto/{carritoId}/{productoId}")]
        public async Task<IActionResult> QuitarProducto(int carritoId, int productoId)
        {
            var articulo = await context.CarritoArticulos
                .Include(a => a.Carrito)
                .FirstOrDefaultAsync(a => a.CarritoId == carritoId && a.ProductoId == productoId);

            if (articulo == null)
                return NotFound("Artículo no encontrado en el carrito.");

            var clienteId = articulo.Carrito.ClienteId;
            context.CarritoArticulos.Remove(articulo);

            var carrito = await context.Carrito.Include(c => c.Articulos)
                .FirstOrDefaultAsync(c => c.Id == carritoId);
            if (carrito != null)
                carrito.Total = carrito.Articulos.Where(a => a.Id != articulo.Id)
                    .Sum(a => a.Cantidad * a.PrecioUnitario);

            await context.SaveChangesAsync();

            // Crear el carrito actualizado para enviar via SignalR
            var carritoActualizado = await ObtenerCarritoDto(clienteId);
            
            // Notificar al cliente específico sobre la actualización del carrito
            await hubContext.Clients.Group($"Carrito_{clienteId}")
                .SendAsync("CarritoActualizado", carritoActualizado);

            return Ok("Producto eliminado del carrito.");
        }

        // Obtener carrito y artículos de clientes
        [HttpGet("{clienteId}")]
        public async Task<IActionResult> ObtenerCarrito(int clienteId)
        {
            var carritoDto = await ObtenerCarritoDto(clienteId);
            if (carritoDto == null)
                return NotFound("No tienes un carrito activo.");

            return Ok(carritoDto);
        }

        // Método helper para obtener el DTO del carrito
        private async Task<CarritoResponseDto?> ObtenerCarritoDto(int clienteId)
        {
            var carrito = await context.Carrito
                .Include(c => c.Articulos)
                .ThenInclude(a => a.Producto)
                .FirstOrDefaultAsync(c => c.ClienteId == clienteId && c.Estado == "ACTIVO");

            if (carrito == null)
                return null;

            return new CarritoResponseDto
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

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                foreach (var articulo in carrito.Articulos)
                {
                    var producto = await context.Productos
                        .Include(p => p.ProductosDescuentos)
                        .FirstOrDefaultAsync(p => p.Id == articulo.ProductoId);

                    if (producto == null || !producto.EstaDisponible)
                        return BadRequest($"El producto {articulo.Producto.Nombre} ya no está disponible.");

                    var descuento = producto.ProductosDescuentos
                        .FirstOrDefault(d => d.FechaInicio <= DateTime.UtcNow && d.FechaFin >= DateTime.UtcNow);

                    if (descuento != null)
                    {
                        if (producto.CantidadComprada + articulo.Cantidad > descuento.CantidadMaximaClientes)
                            return BadRequest($"No hay suficientes cupos para el producto {producto.Nombre} con descuento.");
                    }

                    producto.CantidadComprada += articulo.Cantidad;

                    if (descuento != null)
                    {
                        if (producto.CantidadComprada >= descuento.CantidadMaximaClientes)
                            producto.EstaDisponible = false;
                    }
                    else if (producto.CantidadComprada >= producto.CantidadMaximaClientes)
                    {
                        producto.EstaDisponible = false;
                    }
                }

                carrito.Estado = "FINALIZADO";

                await puntosService.AsignarPuntosPorCompraAsync(carrito.ClienteId, carrito.Articulos.ToList());

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notificar via SignalR
                await hubContext.Clients.Group($"Carrito_{carrito.ClienteId}")
                    .SendAsync("CarritoFinalizado", carritoId);

                return Ok("Compra realizada con éxito.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error al finalizar la compra: {ex.Message}");
            }
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

            // Crear el carrito vacío para enviar via SignalR
            var carritoVacio = await ObtenerCarritoDto(carrito.ClienteId);
            
            // Notificar al cliente específico sobre el vaciado del carrito
            await hubContext.Clients.Group($"Carrito_{carrito.ClienteId}")
                .SendAsync("CarritoVaciado", carritoVacio);

            return Ok("Carrito vaciado.");
        }
    }
}
