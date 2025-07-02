using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("historia_comentarios")]
    public class HistoriaComentario
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("historia_id")]
        public int HistoriaId { get; set; }

        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [Column("comentario")]
        public required string Comentario { get; set; }

        [Column("fecha_comentario")]
        public DateTime FechaComentario { get; set; } = DateTime.UtcNow;

        [Column("likes")]
        public int Likes { get; set; } = 0;

        [ForeignKey("HistoriaId")]
        public Historia Historia { get; set; } = null!;

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;

        // Relación con likes
        public ICollection<ComentarioLike> ComentarioLikes { get; set; } = [];
    }
}
