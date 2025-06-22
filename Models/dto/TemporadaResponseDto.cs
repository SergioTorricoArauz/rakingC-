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
                // Usa la hora de Bolivia para evitar desfases de UTC
                var bolivia = TimeZoneInfo.FindSystemTimeZoneById("SA Western Standard Time");
                var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bolivia).Date;

                if (EstaDisponible == true) return "Activa"; // Conversión explícita de bool?
                if (Fin < hoy) return "Finalizada";
                if (Inicio >= hoy) return "Pendiente";   // ← ahora incluye el mismo día
                return "Inactiva";
            }
        }
    }
}
