using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("historia_imagenes")]
    public class HistoriaImagen
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("historia_id")]
        public int HistoriaId { get; set; }

        [Column("nombre_archivo")]
        public required string NombreArchivo { get; set; }

        [Column("ruta_archivo")]
        public required string RutaArchivo { get; set; }

        [Column("orden")]
        public int Orden { get; set; }

        [Column("fecha_subida")]
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        [ForeignKey("HistoriaId")]
        public Historia Historia { get; set; } = null!;
    }
}
