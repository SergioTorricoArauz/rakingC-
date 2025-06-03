using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("cliente_actividad")]
    public class ClienteActividad
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        public Cliente Cliente { get; set; }

        [Column("id_actividad")]
        public int ActividadId { get; set; }

        public Actividades Actividad { get; set; }
    }
}
