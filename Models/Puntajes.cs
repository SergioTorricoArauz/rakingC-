using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("puntajes")]
    public class Puntajes
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("puntos")]
        public int Puntos { get; set; }

        [Column("id_cliente")]
        public int ClienteId { get; set; }

        [Column("id_temporada")]
        public int TemporadaId { get; set; }

        public Temporadas Temporada { get; set; }

        public Cliente Cliente { get; set; }
    }
}
