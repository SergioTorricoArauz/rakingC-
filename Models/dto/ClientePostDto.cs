using System.ComponentModel.DataAnnotations.Schema;

namespace RankingCyY.Models.dto
{

    public class ClientePostDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int PuntosGenerales { get; set; }
    }
}
