namespace RankingCyY.Models.dto
{
    public class CarritoArticuloPost
    {
        public required int CarritoId { get; set; }
        public required int ProductoId { get; set; }
        public required int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } = decimal.Zero;
    }
}
