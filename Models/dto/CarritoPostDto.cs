namespace RankingCyY.Models.dto
{
    public class CarritoPostDto
    {
        public int ClienteId { get; set; }
        public required string Estado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public decimal Total { get; set; } = 0.0m;
    }
}
