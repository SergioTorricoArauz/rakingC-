using RankingCyY.Models;

namespace RankingCyY.Utils
{
    /// Funciones puras para filtrado y ordenamiento de clientes
    public static class ClienteFilters
    {
        public static IEnumerable<Cliente> FilterByPointsRange(IEnumerable<Cliente> clientes, int minPoints, int maxPoints)
        {
            return clientes.Where(c => c.PuntosGenerales >= minPoints && c.PuntosGenerales <= maxPoints);
        }
        
        // FUNCIÓN PURA: Filtrar por SuperUser
        public static IEnumerable<Cliente> FilterBySuperUser(IEnumerable<Cliente> clientes, bool? isSuperUser)
        {
            return isSuperUser.HasValue 
                ? clientes.Where(c => c.IsSuperUser == isSuperUser.Value)
                : clientes;
        }
        
        // FUNCIÓN PURA: Filtrar por fecha de registro
        public static IEnumerable<Cliente> FilterByRegistrationDate(IEnumerable<Cliente> clientes, DateTime? fromDate, DateTime? toDate)
        {
            var query = clientes.AsEnumerable();
            
            if (fromDate.HasValue)
                query = query.Where(c => c.FechaRegistro >= fromDate.Value);
                
            if (toDate.HasValue)
                query = query.Where(c => c.FechaRegistro <= toDate.Value);
                
            return query;
        }
        
        // FUNCIÓN PURA: Ordenamiento usando pattern matching
        public static IEnumerable<Cliente> SortBy(IEnumerable<Cliente> clientes, string sortBy) =>
            sortBy.ToLower() switch
            {
                "nombre" => clientes.OrderBy(c => c.Nombre),
                "puntos" => clientes.OrderByDescending(c => c.PuntosGenerales),
                "fecha" => clientes.OrderByDescending(c => c.FechaRegistro),
                "email" => clientes.OrderBy(c => c.Email),
                _ => clientes.OrderBy(c => c.Id)
            };
    }
}