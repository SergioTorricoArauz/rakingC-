using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("actividades")]
    public class Actividades
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("actividad")]
        public string Actividad { get; set; }
        [Column("puntos_actividad")]
        public int PuntosActividad { get; set; }
        [Column("fecha_actividad")]
        public DateTime FechaActividad { get; set; }

        public ICollection<ClienteActividad> ClienteActividades { get; set; }
    }
}
