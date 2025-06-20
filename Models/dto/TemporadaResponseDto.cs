namespace RankingCyY.Models.dto
{
    public class TemporadaResponseDto
    {
        public int Id { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public required string Nombre { get; set; }
        public bool? EstaDisponible { get; set; }

        // Propiedad calculada para el estado legible
        public string Estado
        {
            get
            {
                var hoy = DateTime.UtcNow.Date;
                if (EstaDisponible == true)
                    return "Activa";
                if (Fin < hoy)
                    return "Finalizada";
                if (Inicio > hoy)
                    return "Pendiente";
                return "Inactiva";
            }
        }
    }
}
