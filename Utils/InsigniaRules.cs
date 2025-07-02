namespace RankingCyY.Utils
{
    /// <summary>
    /// Funciones puras para l�gica de negocio de insignias
    /// </summary>
    public static class InsigniaRules
    {
        // Record para reglas de insignias (IMMUTABLE)
        public record InsigniaRule(string Nombre, int PuntosMinimos);
        
        // FUNCI�N PURA: Obtener reglas de insignias
        public static IReadOnlyList<InsigniaRule> GetInsigniaRules()
        {
            return new List<InsigniaRule>
            {
                new("Cliente Premium", 300),
                new("Cliente Oro", 200),
                new("Cliente Plata", 100)
            }.AsReadOnly();
        }
        
        // FUNCI�N PURA: Determinar insignias elegibles
        public static IEnumerable<string> GetInsigniasElegibles(int puntosGenerales)
        {
            return GetInsigniaRules()
                .Where(regla => puntosGenerales >= regla.PuntosMinimos)
                .Select(regla => regla.Nombre);
        }
        
        // FUNCI�N PURA: Calcular puntos con bonificaci�n
        public static int CalculatePuntosConBonificacion(int puntosBase, bool esSuperUser)
        {
            return esSuperUser ? puntosBase * 2 : puntosBase;
        }
        
        // FUNCI�N PURA: Determinar si cliente merece upgrade a SuperUser
        public static bool MereceSuperUser(int puntosGenerales, DateTime fechaRegistro)
        {
            var diasRegistrado = (DateTime.UtcNow.Date - fechaRegistro.Date).Days;
            return puntosGenerales >= 1000 && diasRegistrado >= 30;
        }
    }
}