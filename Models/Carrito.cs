using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("carrito")]
    public class Carrito
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("cliente_id")]
        public int ClienteId { get; set; } // ACTIVO, INACTIVO, CANCELADO, FINALIZADO
        [Column("estado")]
        public required string Estado { get; set; }
        [Column("fechacreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("total")]
        public decimal Total { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;

        // Relación inversa con CarritoArticulos
        public ICollection<CarritoArticulos> Articulos { get; set; } = [];
    }
}
