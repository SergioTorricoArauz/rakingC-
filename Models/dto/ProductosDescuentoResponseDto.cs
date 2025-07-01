namespace RankingCyY.Models.dto
{
    public class ProductosDescuentoResponseDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public int CantidadMaximaClientes { get; set; }
        public decimal Descuento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int CantidadComprada { get; set; } = 0;
        public string Nombre { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public decimal Precio { get; set; }
        public bool EstaDisponible { get; set; }
        public int Categoria { get; set; }
        public decimal PrecioConDescuento => Precio - (Precio * Descuento / 100);
    }
}
