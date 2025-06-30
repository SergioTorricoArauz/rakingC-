using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("carrito_articulos")]
    public class CarritoArticulos
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("carrito_id")]
        public int CarritoId { get; set; }
        [Column("producto_id")]
        public int ProductoId { get; set; }
        [Column("cantidad")]
        public int Cantidad { get; set; }
        [Column("preciounitario")]
        public decimal PrecioUnitario { get; set; } = decimal.Zero;

        [ForeignKey("CarritoId")]
        public Carrito Carrito { get; set; } = null!;

        [ForeignKey("ProductoId")]
        public Productos Producto { get; set; } = null!;
    }
}
