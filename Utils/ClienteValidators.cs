using System.Net.Mail;

namespace RankingCyY.Utils
{
    /// Funciones puras para validación de datos de clientes
    public static class ClienteValidators
    {
        // FUNCIÓN PURA: Validación de email
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            
            try
            {
                var mail = new MailAddress(email);
                return mail.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        // FUNCIÓN PURA: Validación de puntos
        public static bool IsValidPoints(int points)
        {
            return points >= 0 && points <= 50000;
        }
        
        // FUNCIÓN PURA: Validación de nombre
        public static bool IsValidName(string nombre)
        {
            return !string.IsNullOrWhiteSpace(nombre) && 
                   nombre.Length >= 2 && 
                   nombre.Length <= 100;
        }
        
        // FUNCIÓN PURA: Validación de password
        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && 
                   password.Length >= 6;
        }
        
        // FUNCIÓN PURA: Validación completa usando composición
        public static (bool IsValid, string ErrorMessage) ValidateCliente(string nombre, string email, string password, int puntos)
        {
            if (!IsValidName(nombre))
                return (false, "El nombre debe tener entre 2 y 100 caracteres.");
            
            if (!IsValidEmail(email))
                return (false, "El correo electrónico no es válido.");
            
            if (!IsValidPassword(password))
                return (false, "La contraseña debe tener al menos 6 caracteres.");
            
            if (!IsValidPoints(puntos))
                return (false, "Los puntos deben estar entre 0 y 50000.");
            
            return (true, string.Empty);
        }
    }
}