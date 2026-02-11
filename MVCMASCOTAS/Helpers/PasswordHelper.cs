using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para hash seguro de contraseñas con PBKDF2 (más seguro que SHA256)
    /// Utiliza múltiples iteraciones para resistir ataques de fuerza bruta
    /// </summary>
    public static class PasswordHelper
    {
        // ⚡ NUEVO: Número de iteraciones para PBKDF2 (más iteraciones = más seguro pero más lento)
        private const int PBKDF2_ITERATIONS = 10000;
        private const int HASH_SIZE = 32; // 256 bits
        private const int SALT_SIZE = 32; // 256 bits

        #region GENERACIÓN DE SALT

        /// <summary>
        /// Genera un salt aleatorio criptográficamente seguro de 32 bytes
        /// </summary>
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        #endregion

        #region HASHING DE CONTRASEÑAS

        /// <summary>
        /// Genera el hash de una contraseña usando PBKDF2 con un salt específico
        /// ⚡ MÁS SEGURO: Usa PBKDF2 en lugar de SHA256 simple
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="salt">Salt en Base64</param>
        /// <returns>Hash de la contraseña en Base64</returns>
        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password), "La contraseña no puede estar vacía");

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt), "El salt no puede estar vacío");

            try
            {
                // Convertir salt de Base64 a bytes
                byte[] saltBytes = Convert.FromBase64String(salt);

                // Usar PBKDF2 para generar el hash
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256))
                {
                    byte[] hashBytes = pbkdf2.GetBytes(HASH_SIZE);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("El salt no tiene un formato Base64 válido", nameof(salt), ex);
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Genera el hash de una contraseña usando PBKDF2 (versión alternativa más explícita)
        /// </summary>
        public static string HashPasswordSecure(string password, string salt, int iterations = PBKDF2_ITERATIONS)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            if (iterations < 1000)
                throw new ArgumentException("El número de iteraciones debe ser al menos 1000", nameof(iterations));

            byte[] saltBytes = Convert.FromBase64String(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HASH_SIZE);
                return Convert.ToBase64String(hashBytes);
            }
        }

        #endregion

        #region VERIFICACIÓN DE CONTRASEÑAS

        /// <summary>
        /// Verifica si una contraseña coincide con un hash almacenado
        /// </summary>
        /// <param name="password">Contraseña ingresada por el usuario</param>
        /// <param name="storedHash">Hash almacenado en la base de datos</param>
        /// <param name="salt">Salt almacenado en la base de datos</param>
        /// <returns>True si la contraseña es correcta</returns>
        public static bool VerifyPassword(string password, string storedHash, string salt)
        {
            // Validaciones de seguridad
            if (string.IsNullOrEmpty(password))
                return false;

            if (string.IsNullOrEmpty(storedHash))
                return false;

            if (string.IsNullOrEmpty(salt))
                return false;

            try
            {
                // Generar hash de la contraseña ingresada
                string hashOfInput = HashPassword(password, salt);

                // Comparación segura contra timing attacks
                return SlowEquals(hashOfInput, storedHash);
            }
            catch (Exception ex)
            {
                // Log del error pero no exponer detalles
                System.Diagnostics.Debug.WriteLine($"Error en verificación de contraseña: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Comparación segura de strings para evitar timing attacks
        /// </summary>
        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            // Si las longitudes son diferentes, ya sabemos que no son iguales
            // pero seguimos comparando para evitar timing attacks
            uint diff = (uint)a.Length ^ (uint)b.Length;

            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }

        #endregion

        #region VALIDACIÓN DE CONTRASEÑAS

        /// <summary>
        /// Valida que una contraseña cumpla con los requisitos mínimos de seguridad
        /// Requiere: mayúsculas, minúsculas y números (caracteres especiales opcionales)
        /// Para validación más estricta, usar IsPasswordValidAdvanced()
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <param name="minLength">Longitud mínima (por defecto 8)</param>
        /// <returns>True si cumple los requisitos</returns>
        public static bool IsPasswordValid(string password, int minLength = 8)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < minLength)
                return false;

            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))
                    hasUpperCase = true;
                else if (char.IsLower(c))
                    hasLowerCase = true;
                else if (char.IsDigit(c))
                    hasDigit = true;

                // ✅ OPTIMIZACIÓN: Si ya tenemos todos los tipos, podemos salir
                if (hasUpperCase && hasLowerCase && hasDigit)
                    return true;
            }

            // ⚡ REQUISITOS: Debe tener al menos mayúscula, minúscula y número
            // NOTA: Para requisitos incluyendo caracteres especiales, usar IsPasswordValidAdvanced()
            return hasUpperCase && hasLowerCase && hasDigit;
        }

        /// <summary>
        /// ⚡ NUEVO: Validación avanzada con requisitos personalizados
        /// </summary>
        public static bool IsPasswordValidAdvanced(string password,
            int minLength = 8,
            bool requireUpperCase = true,
            bool requireLowerCase = true,
            bool requireDigit = true,
            bool requireSpecialChar = false)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < minLength)
                return false;

            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;
            bool hasSpecialChar = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpperCase = true;
                else if (char.IsLower(c)) hasLowerCase = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (!char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) hasSpecialChar = true;
            }

            // Verificar requisitos opcionales
            if (requireUpperCase && !hasUpperCase) return false;
            if (requireLowerCase && !hasLowerCase) return false;
            if (requireDigit && !hasDigit) return false;
            if (requireSpecialChar && !hasSpecialChar) return false;

            return true;
        }

        /// <summary>
        /// ⚡ NUEVO: Evalúa la fortaleza de una contraseña (0-5)
        /// </summary>
        public static int GetPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int strength = 0;

            // Longitud
            if (password.Length >= 8) strength++;
            if (password.Length >= 12) strength++;

            // Complejidad
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

            if (hasUpper && hasLower) strength++;
            if (hasDigit) strength++;
            if (hasSpecial) strength++;

            return Math.Min(strength, 5); // Máximo 5
        }

        /// <summary>
        /// Obtiene el mensaje de error de validación de contraseña
        /// </summary>
        public static string GetPasswordValidationMessage(int minLength = 8)
        {
            return $"La contraseña debe tener al menos {minLength} caracteres e incluir mayúsculas, minúsculas y números.";
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene un mensaje descriptivo de la fortaleza de la contraseña
        /// </summary>
        public static string GetPasswordStrengthMessage(string password)
        {
            int strength = GetPasswordStrength(password);
            switch (strength)
            {
                case 0:
                case 1:
                    return "Muy débil";
                case 2:
                    return "Débil";
                case 3:
                    return "Moderada";
                case 4:
                    return "Fuerte";
                case 5:
                    return "Muy fuerte";
                default:
                    return "Desconocida";
            }
        }

        #endregion

        #region UTILIDADES

        /// <summary>
        /// ⚡ NUEVO: Genera una contraseña aleatoria segura
        /// </summary>
        public static string GenerateRandomPassword(int length = 12)
        {
            if (length < 8)
                throw new ArgumentException("La longitud mínima debe ser 8", nameof(length));

            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%&*-_=+";

            string allChars = upperChars + lowerChars + digitChars + specialChars;

            using (var rng = RandomNumberGenerator.Create())
            {
                var result = new StringBuilder(length);
                byte[] randomBytes = new byte[4];

                // Asegurar que tenga al menos uno de cada tipo
                result.Append(GetRandomChar(upperChars, rng, randomBytes));
                result.Append(GetRandomChar(lowerChars, rng, randomBytes));
                result.Append(GetRandomChar(digitChars, rng, randomBytes));
                result.Append(GetRandomChar(specialChars, rng, randomBytes));

                // Rellenar el resto aleatoriamente
                for (int i = 4; i < length; i++)
                {
                    result.Append(GetRandomChar(allChars, rng, randomBytes));
                }

                // Mezclar aleatoriamente
                return ShuffleString(result.ToString(), rng, randomBytes);
            }
        }

        private static char GetRandomChar(string chars, RandomNumberGenerator rng, byte[] buffer)
        {
            rng.GetBytes(buffer);
            uint randomNumber = BitConverter.ToUInt32(buffer, 0);
            return chars[(int)(randomNumber % chars.Length)];
        }

        private static string ShuffleString(string input, RandomNumberGenerator rng, byte[] buffer)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;

            while (n > 1)
            {
                rng.GetBytes(buffer);
                int k = (int)(BitConverter.ToUInt32(buffer, 0) % n);
                n--;
                char temp = array[k];
                array[k] = array[n];
                array[n] = temp;
            }

            return new string(array);
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si una contraseña ha sido comprometida (común)
        /// Lista de contraseñas más comunes para rechazar
        /// </summary>
        public static bool IsPasswordCommon(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Top 50 contraseñas más comunes
            string[] commonPasswords = new string[]
            {
                "123456", "password", "123456789", "12345678", "12345",
                "1234567", "1234567890", "qwerty", "abc123", "111111",
                "123123", "admin", "letmein", "welcome", "monkey",
                "dragon", "master", "sunshine", "password1", "football",
                "iloveyou", "admin123", "welcome123", "Password1",
                "123", "1234", "root", "toor", "test", "guest"
            };

            return commonPasswords.Any(p =>
                string.Equals(p, password, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}