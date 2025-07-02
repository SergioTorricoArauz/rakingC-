using RankingCyY.Models;
using RankingCyY.Models.dto;

namespace RankingCyY.Utils
{
    /// <summary>
    /// Funciones puras para mapeo de entidades Cliente
    /// </summary>
    public static class ClienteMappers
    {
        // FUNCIÓN PURA: Cliente a ClienteResponseDto
        public static ClienteResponseDto ToResponseDto(Cliente cliente)
        {
            return new ClienteResponseDto
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Email = cliente.Email,
                PuntosGenerales = cliente.PuntosGenerales,
                FechaRegistro = cliente.FechaRegistro,
                IsSuperUser = cliente.IsSuperUser
            };
        }
        
        // FUNCIÓN PURA: Múltiples clientes a DTOs
        public static IEnumerable<ClienteResponseDto> ToResponseDtos(IEnumerable<Cliente> clientes)
        {
            return clientes.Select(ToResponseDto);
        }
        
        // FUNCIÓN PURA: ClientePostDto a Cliente
        public static Cliente FromPostDto(ClientePostDto dto)
        {
            return new Cliente
            {
                Nombre = dto.Nombre.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Password = dto.Password,
                PuntosGenerales = dto.PuntosGenerales,
                FechaRegistro = DateTime.UtcNow.Date,
                IsSuperUser = false
            };
        }
    }
}