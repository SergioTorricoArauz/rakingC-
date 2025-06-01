using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("clientes_insignias")]
    public class ClienteInsignia
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("id_cliente")]
        public int ClienteId { get; set; }
        [Column("id_insignia")]
        public int InsigniaId { get; set; }
        [Column("fecha_otorgada")]
        public DateTime FechaOtorgada { get; set; }

        // Relación con Cliente
        public Cliente Cliente { get; set; }
        // Relación con Insignia
        public Insignias Insignia { get; set; }
    }

}
