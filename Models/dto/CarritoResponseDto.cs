namespace RankingCyY.Models.dto
{
    public class CarritoResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string? Estado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; } = 0.0m;

        public List<CarritoArticuloRespondeDto> Articulos { get; set; } = [];
    }
}
