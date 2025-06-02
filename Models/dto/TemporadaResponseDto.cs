namespace RankingCyY.Models.dto
{
    public class TemporadaResponseDto
    {
        public int Id { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public required string Nombre { get; set; }
        public bool? EstaDisponible { get; set; }
    }
}
