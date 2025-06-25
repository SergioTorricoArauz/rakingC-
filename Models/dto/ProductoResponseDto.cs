namespace RankingCyY.Models.dto
{
    public class ProductoResponseDto
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int CantidadMaximaClientes { get; set; }
        public int CantidadComprada { get; set; }
        public bool EstaDisponible { get; set; }
        public int Categoria { get; set; }
    }
}
