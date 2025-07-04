﻿namespace RankingCyY.Models.dto
{
    public class ProductoPostDto
    {
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int CantidadMaximaClientes { get; set; }
        public int CantidadComprada { get; set; }
        public bool EstaDisponible { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public required int Categoria { get; set; }
    }
}
