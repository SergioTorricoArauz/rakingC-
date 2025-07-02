using RankingCyY.Models;

namespace RankingCyY.Utils
{
    /// Funciones que demuestran closures
    public static class ClienteClosures
    {
        // CLOSURE: Funci�n que retorna otra funci�n con estado capturado
        public static Func<Cliente, bool> CreatePointsFilter(int minPoints)
        {
            return cliente => cliente.PuntosGenerales >= minPoints;
        }
        
        // CLOSURE: Funci�n para crear validadores personalizados
        public static Func<string, bool> CreateEmailDomainValidator(string allowedDomain)
        {
            return email => email.EndsWith($"@{allowedDomain}", StringComparison.OrdinalIgnoreCase);
        }
        
        // CLOSURE: Contador de clientes con estado
        public static Func<Cliente, bool> CreateCountingFilter(int maxCount)
        {
            int currentCount = 0;
            
            return cliente =>
            {
                if (currentCount >= maxCount)
                    return false;
                    
                currentCount++;
                return true;
            };
        }
        
        // CLOSURE: Funci�n de configuraci�n din�mica
        public static Func<IEnumerable<Cliente>, IEnumerable<Cliente>> CreateCustomSorter(bool ascending, string field)
        {
            return clientes => field.ToLower() switch
            {
                "puntos" => ascending 
                    ? clientes.OrderBy(c => c.PuntosGenerales)
                    : clientes.OrderByDescending(c => c.PuntosGenerales),
                "nombre" => ascending
                    ? clientes.OrderBy(c => c.Nombre)
                    : clientes.OrderByDescending(c => c.Nombre),
                _ => clientes
            };
        }
    }
}