using RankingCyY.Models;

namespace RankingCyY.Utils
{
    /// <summary>
    /// Funciones recursivas para operaciones con clientes
    /// </summary>
    public static class ClienteRecursiveFunctions
    {
        // FUNCIÓN RECURSIVA: Calcular factorial de puntos (ejemplo educativo)
        public static long CalculatePointsFactorial(int points)
        {
            if (points <= 1) return 1;
            return points * CalculatePointsFactorial(points - 1);
        }
        
        // FUNCIÓN RECURSIVA: Calcular jerarquía de clientes por referidos
        public static int CalculateClientHierarchyDepth(Cliente cliente, IEnumerable<Cliente> todosClientes, int currentDepth = 0)
        {
            var referidos = todosClientes.Where(c => c.Email.Contains(cliente.Nombre.ToLower())).ToList();
            
            if (!referidos.Any())
                return currentDepth;
            
            return referidos.Max(referido => 
                CalculateClientHierarchyDepth(referido, todosClientes, currentDepth + 1));
        }
        
        // FUNCIÓN RECURSIVA: Suma recursiva de puntos de una lista
        public static int SumPointsRecursively(IEnumerable<Cliente> clientes)
        {
            if (!clientes.Any())
                return 0;
            
            var first = clientes.First();
            var rest = clientes.Skip(1);
            
            return first.PuntosGenerales + SumPointsRecursively(rest);
        }
    }
}