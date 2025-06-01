namespace RankingCyY.Models.dto
{
    public class PuntajeResponseDto
    {
        public int Id { get; set; }
        public int Puntos { get; set; }
        public string ClienteNombre { get; set; }  // Nombre del Cliente
        public string TemporadaNombre { get; set; } // Nombre de la Temporada
    }
}
