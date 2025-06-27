namespace RankingCyY.Models.dto
{
    public class ProductosDescuentoPost
    {
        public required int ProductoId { get; set; }
        public required int CantidadMaximaClientes { get; set; }
        public required decimal Descuento { get; set; }
        public required DateTime FechaInicio { get; set; }
        public required DateTime FechaFin { get; set; }

    }
}
