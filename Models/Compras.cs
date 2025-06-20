using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("compras")]
    public class Compras
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("puntos_compra")]
        public int PuntosCompras { get; set; }
        [Column("fecha_compra")]
        public DateTime FechaCompra { get; set; }
        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }
    }
}
