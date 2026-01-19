using System;
using System.Security.Cryptography;
using System.Text;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para hash seguro de contraseñas con SHA256 y Salt
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Genera un salt aleatorio de 32 bytes
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Genera el hash de una contraseña con un salt específico
        /// </summary>
        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            using (var sha256 = SHA256.Create())
            {
                // Combinar password + salt
                string combined = password + salt;
                byte[] combinedBytes = Encoding.UTF8.GetBytes(combined);

                // Generar hash
                byte[] hashBytes = sha256.ComputeHash(combinedBytes);

                // Convertir a Base64
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Verifica si una contraseña coincide con un hash almacenado
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string salt)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (string.IsNullOrEmpty(storedHash))
                return false;

            if (string.IsNullOrEmpty(salt))
                return false;

            string hashOfInput = HashPassword(password, salt);
            return hashOfInput.Equals(storedHash);
        }

        /// <summary>
        /// Valida que una contraseña cumpla con los requisitos mínimos
        /// </summary>
        public static bool IsPasswordValid(string password, int minLength = 8)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < minLength)
                return false;

            // Debe contener al menos una letra
            bool hasLetter = false;
            // Debe contener al menos un número
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsLetter(c))
                    hasLetter = true;
                if (char.IsDigit(c))
                    hasDigit = true;
            }

            return hasLetter && hasDigit;
        }

        /// <summary>
        /// Obtiene el mensaje de error de validación de contraseña
        /// </summary>
        public static string GetPasswordValidationMessage(int minLength = 8)
        {
            return $"La contraseña debe tener al menos {minLength} caracteres, incluir letras y números.";
        }
    }
}