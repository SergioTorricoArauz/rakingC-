using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("password")]
        public string Password { get; set; }
        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
        [Column("puntos_generales")]
        public int PuntosGenerales { get; set; }
    }
}
