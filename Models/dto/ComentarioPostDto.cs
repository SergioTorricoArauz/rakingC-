namespace RankingCyY.Models.dto
{
    public class ComentarioPostDto
    {
        public int HistoriaId { get; set; }
        public int ClienteId { get; set; }
        public required string Comentario { get; set; }
    }
}