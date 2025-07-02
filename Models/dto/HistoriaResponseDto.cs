namespace RankingCyY.Models.dto
{
    public class HistoriaResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool EstaActiva { get; set; }
        public bool PermiteComentarios { get; set; }
        public bool PuedeComentarAun { get; set; }
        public List<HistoriaImagenDto> Imagenes { get; set; } = [];
        public List<HistoriaComentarioDto> Comentarios { get; set; } = [];
    }

    public class HistoriaImagenDto
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = null!;
        public string Url { get; set; } = null!;
        public int Orden { get; set; }
    }

    public class HistoriaComentarioDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = null!;
        public string Comentario { get; set; } = null!;
        public DateTime FechaComentario { get; set; }
        public int Likes { get; set; }
        public bool YaLeDiLike { get; set; }
    }
}