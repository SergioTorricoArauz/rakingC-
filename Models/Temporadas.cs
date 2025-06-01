using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("temporadas")]
    public class Temporadas
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("inicio")]
        public DateTime Inicio { get; set; }
        [Column("fin")]
        public DateTime Fin { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; }
    }
}
