using System.Text.Json.Serialization;

namespace RankingCyY.Models.dto
{
    public class InsigniaResponseDto
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Requisitos { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? FechaInicio { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? FechaFin { get; set; }

        public DateTime? FechaOtorgada { get; set; }
    }
}
