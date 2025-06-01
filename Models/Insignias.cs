using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RankingCyY.Models
{
    [Table ("insignias")]
    public class Insignias
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("nombre")]
        public required string Nombre { get; set; }
        [Column("requisitos")]
        public required string Requisitos { get; set; }

        [Column("fecha_inicio")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? FechaInicio { get; set; }

        [Column("fecha_fin")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? FechaFin { get; set; }
    }
}
