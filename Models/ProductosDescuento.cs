using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("productos_descuento")]
    public class ProductosDescuento
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("producto_id")]
        public required int ProductoId { get; set; }
        [Column("cantidad_maxima_clientes")]
        public int CantidadMaximaClientes { get; set; }
        [Column("descuento")]
        public decimal Descuento { get; set; }
        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; }
        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; }
        [Column("cantidad_vendida_con_descuento")]
        public int CantidadComprada { get; set; } = 0;

        // Relación con Productos
        public required Productos Producto { get; set; }

    }
}
