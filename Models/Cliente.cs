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
        public required string Nombre { get; set; }
        [Column("email")]
        public required string Email { get; set; }
        [Column("password")]
        public required string Password { get; set; }
        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
        [Column("puntos_generales")]
        public int PuntosGenerales { get; set; }
        [Column("is_super_user")]
        public bool IsSuperUser { get; set; }

        // Relación muchos a muchos con Insignias
        public ICollection<ClienteInsignia> ClienteInsignias { get; set; }

        // Relación uno a muchos con ClienteActividades
        public ICollection<ClienteActividad> ClienteActividades { get; set; }
    }
}
