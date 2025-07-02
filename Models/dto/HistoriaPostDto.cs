namespace RankingCyY.Models.dto
{
    public class HistoriaPostDto
    {
        public int ClienteId { get; set; }
        public required string Descripcion { get; set; }
        public int DuracionHoras { get; set; } = 24;
        public bool PermiteComentarios { get; set; } = true;
        public List<IFormFile> Imagenes { get; set; } = [];
    }
}