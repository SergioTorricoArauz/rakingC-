using Microsoft.EntityFrameworkCore;
using RankingCyY.Data;
using RankingCyY.Models;

namespace RankingCyY.Services
{
    public interface IPuntosService
    {
        Task AsignarPuntosPorCompraAsync(int clienteId, List<CarritoArticulos> articulos);
        int ObtenerPuntosPorCategoria(int categoria);
    }

    public class PuntosService : IPuntosService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PuntosService> _logger;

        // Configuración de puntos por categoría
        private readonly Dictionary<int, int> _puntosPorCategoria = new()
        {
            { 1, 5 },   // IMPRESIONES = 5 puntos
            { 2, 5 },   // SESIONES = 5 puntos  
            { 3, 10 }   // CONTRATOS = 10 puntos
        };

        public PuntosService(AppDbContext context, ILogger<PuntosService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AsignarPuntosPorCompraAsync(int clienteId, List<CarritoArticulos> articulos)
        {
            try
            {
                // Verificar si hay una temporada activa
                var temporadaActiva = await _context.Temporadas
                    .FirstOrDefaultAsync(t => t.EstaDisponible);

                if (temporadaActiva == null)
                {
                    _logger.LogInformation("No hay temporada activa, no se asignan puntos por compra para cliente {ClienteId}", clienteId);
                    return;
                }

                // Verificar si el cliente existe
                var cliente = await _context.Clientes.FindAsync(clienteId);
                if (cliente == null)
                {
                    _logger.LogError("Cliente con ID {ClienteId} no encontrado", clienteId);
                    return;
                }

                int puntosTotal = 0;

                // Calcular puntos por cada artículo comprado
                foreach (var articulo in articulos)
                {
                    var producto = await _context.Productos.FindAsync(articulo.ProductoId);
                    if (producto == null) continue;

                    int puntosProducto = ObtenerPuntosPorCategoria(producto.Categoria);
                    int puntosArticulo = puntosProducto * articulo.Cantidad;
                    puntosTotal += puntosArticulo;

                    _logger.LogInformation("Producto {ProductoNombre} (Categoría {Categoria}): {Cantidad} x {PuntosUnitarios} = {PuntosArticulo} puntos",
                        producto.Nombre, producto.Categoria, articulo.Cantidad, puntosProducto, puntosArticulo);
                }

                if (puntosTotal > 0)
                {
                    // Crear o actualizar registro de puntaje
                    var puntajeExistente = await _context.Puntajes
                        .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.TemporadaId == temporadaActiva.Id);

                    if (puntajeExistente != null)
                    {
                        puntajeExistente.Puntos += puntosTotal;
                        _logger.LogInformation("Actualizados {PuntosTotal} puntos para cliente {ClienteId} en temporada {TemporadaId}. Total: {PuntosNuevoTotal}",
                            puntosTotal, clienteId, temporadaActiva.Id, puntajeExistente.Puntos);
                    }
                    else
                    {
                        var nuevoPuntaje = new Puntajes
                        {
                            ClienteId = clienteId,
                            TemporadaId = temporadaActiva.Id,
                            Puntos = puntosTotal
                        };
                        _context.Puntajes.Add(nuevoPuntaje);
                        _logger.LogInformation("Creado nuevo registro de {PuntosTotal} puntos para cliente {ClienteId} en temporada {TemporadaId}",
                            puntosTotal, clienteId, temporadaActiva.Id);
                    }

                    // Actualizar puntos generales del cliente
                    cliente.PuntosGenerales += puntosTotal;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Puntos asignados exitosamente: {PuntosTotal} puntos para cliente {ClienteId}", puntosTotal, clienteId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar puntos por compra para cliente {ClienteId}", clienteId);
                throw;
            }
        }

        public int ObtenerPuntosPorCategoria(int categoria)
        {
            return _puntosPorCategoria.TryGetValue(categoria, out int puntos) ? puntos : 0;
        }
    }
}