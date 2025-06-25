namespace RankingCyY.Models.dto
{
    public class ClienteResponseDto
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public required string Email { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int PuntosGenerales { get; set; }
        public bool IsSuperUser { get; set; }
    }
}
