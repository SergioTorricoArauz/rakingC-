namespace RankingCyY.Models.dto
{
    public class TemporadaPatchDto
    {
        public DateTime? Inicio { get; set; }
        public DateTime? Fin { get; set; }
        public string? Nombre { get; set; }

        public bool? EstaDisponible { get; set; }
    }
}
