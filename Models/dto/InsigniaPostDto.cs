namespace RankingCyY.Models.dto
{
    public class InsigniaPostDto
    {
        public required string Nombre { get; set; }
        public required string Requisitos { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}
