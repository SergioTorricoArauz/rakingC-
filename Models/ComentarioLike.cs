using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("comentario_likes")]
    public class ComentarioLike
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("comentario_id")]
        public int ComentarioId { get; set; }

        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [Column("fecha_like")]
        public DateTime FechaLike { get; set; } = DateTime.UtcNow;

        [ForeignKey("ComentarioId")]
        public HistoriaComentario Comentario { get; set; } = null!;

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; } = null!;
    }
}
