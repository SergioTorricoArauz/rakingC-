using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("productos")]
    public class Productos
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public required string Nombre { get; set; }

        [Column("descripcion")]
        public required string Descripcion { get; set; }

        [Column("precio", TypeName ="numeric(10,2)")]
        public decimal Precio { get; set; }

        [Column("cantidad_maxima_clientes")]
        public int CantidadMaximaClientes { get; set; }

        [Column("cantidad_comprada")]
        public int CantidadComprada { get; set; }

        [Column("esta_disponible")]
        public bool EstaDisponible { get; set; }

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; }
        [Column("categoria")]
        public int Categoria { get; set; } // 1 = IMPRESIONES, 2 = SESIONES, 3 = CONTRATOS

        // Relación inversa con CarritoArticulos
        public ICollection<CarritoArticulos> CarritoArticulos { get; set; } = [];

        // Relación inversa con ProductosDescuento
        public ICollection<ProductosDescuento> ProductosDescuentos { get; set; } = [];
    }
}
