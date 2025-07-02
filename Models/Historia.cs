using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("historias")]
    public class Historia
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [Column("descripcion")]
        public required string Descripcion { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("fecha_expiracion")]
        public DateTime FechaExpiracion { get; set; }

        [Column("esta_activa")]
        public bool EstaActiva { get; set; } = true;

        [Column("permite_comentarios")]
        public bool PermiteComentarios { get; set; } = true;

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;

        // Relaciones
        public ICollection<HistoriaImagen> Imagenes { get; set; } = [];
        public ICollection<HistoriaComentario> Comentarios { get; set; } = [];
    }
}
