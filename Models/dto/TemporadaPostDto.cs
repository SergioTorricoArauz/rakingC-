namespace RankingCyY.Models.dto
{
    public class TemporadaPostDto
    {
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public required string Nombre { get; set; }
        public bool? EstaDisponible { get; set; }
    }
}
