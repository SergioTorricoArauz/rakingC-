namespace RankingCyY.Models.dto
{
    public class ClienteResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int PuntosGenerales { get; set; }
        public bool IsSuperUser { get; set; }
    }
}
