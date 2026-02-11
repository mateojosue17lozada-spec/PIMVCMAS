using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "El teléfono debe contener solo números (10 dígitos)")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La cédula es requerida")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "La cédula debe tener 10 dígitos")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "La cédula debe contener solo números (10 dígitos)")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        [Display(Name = "Ciudad")]
        public string Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "La contraseña debe contener al menos: una mayúscula, una minúscula y un número")]
        [PasswordStrength(ErrorMessage = "Esta contraseña es demasiado común. Elija una más segura.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Foto de Perfil (Opcional)")]
        [DataType(DataType.Upload)]
        [FileSize(5 * 1024 * 1024, ErrorMessage = "La imagen no debe exceder 5 MB")]
        [FileTypes("jpg,jpeg,png,gif", ErrorMessage = "Solo se permiten imágenes JPG, PNG o GIF")]
        [ValidImage(ErrorMessage = "El archivo no es una imagen válida o está corrupto")]
        public HttpPostedFileBase ImagenPerfil { get; set; }

        [Required(ErrorMessage = "Debe aceptar los términos y condiciones")]
        [Display(Name = "Acepto los términos y condiciones")]
        [MustBeTrue(ErrorMessage = "Debe aceptar los términos y condiciones")]
        public bool AceptaTerminos { get; set; }

        // ⚡ NUEVO: Campo para validar que no es un bot
        [Display(Name = "¿Cuánto es 2 + 2?")]
        [Required(ErrorMessage = "Debe responder la pregunta de seguridad")]
        [Range(4, 4, ErrorMessage = "Respuesta incorrecta")]
        public int PreguntaSeguridad { get; set; } = 0;
    }

    // ⚡ MEJORADO: Clase auxiliar para validar que el checkbox esté marcado
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value is bool && (bool)value;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Debe marcar la casilla '{name}' para continuar.";
        }
    }

    // ⚡ NUEVO: Atributo para validar fortaleza de contraseña
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PasswordStrengthAttribute : ValidationAttribute
    {
        private static readonly string[] CommonPasswords = {
            "123456", "password", "12345678", "qwerty", "abc123",
            "password1", "12345", "123456789", "letmein", "welcome",
            "admin", "1234567", "1234567890", "123123", "111111",
            "sunshine", "iloveyou", "monkey", "football", "admin123",
            "welcome123", "Password1", "Password123", "1234", "123",
            "test", "guest", "root", "toor", "superman"
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("La contraseña es requerida");

            string password = value.ToString();

            if (string.IsNullOrEmpty(password))
                return ValidationResult.Success;

            // Verificar si es una contraseña común
            if (CommonPasswords.Contains(password.ToLower()))
            {
                return new ValidationResult("Esta contraseña es demasiado común. Por seguridad, elija una contraseña más única.");
            }

            // Verificar secuencias simples
            if (IsSimpleSequence(password))
            {
                return new ValidationResult("La contraseña contiene secuencias simples (ej: 12345, abcde). Use una combinación más aleatoria.");
            }

            // Verificar si es demasiado similar al nombre de usuario
            var model = validationContext.ObjectInstance as RegisterViewModel;
            if (model != null && !string.IsNullOrEmpty(model.NombreCompleto))
            {
                string[] nameParts = model.NombreCompleto.ToLower().Split(' ');
                foreach (var part in nameParts)
                {
                    if (part.Length > 3 && password.ToLower().Contains(part))
                    {
                        return new ValidationResult("La contraseña no debe contener partes de su nombre. Por seguridad, use una contraseña completamente diferente.");
                    }
                }
            }

            return ValidationResult.Success;
        }

        private bool IsSimpleSequence(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 3)
                return false;

            password = password.ToLower();

            // Verificar secuencias numéricas
            if (IsNumericSequence(password))
                return true;

            // Verificar secuencias del teclado
            string[] keyboardSequences = {
                "qwerty", "asdfgh", "zxcvbn", "123456", "abcdef",
                "qazwsx", "edcrfv", "tgbnhy", "yhnujm", "ikolp"
            };

            foreach (var seq in keyboardSequences)
            {
                if (password.Contains(seq))
                    return true;
            }

            return false;
        }

        private bool IsNumericSequence(string str)
        {
            if (string.IsNullOrEmpty(str) || !str.All(char.IsDigit))
                return false;

            // Verificar si es una secuencia ascendente o descendente
            bool ascending = true;
            bool descending = true;

            for (int i = 1; i < str.Length; i++)
            {
                int current = str[i] - '0';
                int previous = str[i - 1] - '0';

                if (current != previous + 1)
                    ascending = false;

                if (current != previous - 1)
                    descending = false;
            }

            return ascending || descending;
        }
    }

    // ⚡ NUEVO: Atributo para validar tamaño de archivo
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FileSizeAttribute : ValidationAttribute
    {
        private readonly long _maxSize;

        public FileSizeAttribute(long maxSize)
        {
            _maxSize = maxSize;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var file = value as HttpPostedFileBase;
            if (file == null)
                return ValidationResult.Success;

            if (file.ContentLength > _maxSize)
            {
                double sizeInMB = _maxSize / (1024.0 * 1024.0);
                return new ValidationResult($"El archivo '{file.FileName}' excede el tamaño máximo de {sizeInMB:0.#} MB.");
            }

            return ValidationResult.Success;
        }
    }

    // ⚡ NUEVO: Atributo para validar tipos de archivo
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FileTypesAttribute : ValidationAttribute
    {
        private readonly string[] _allowedTypes;

        public FileTypesAttribute(string allowedTypes)
        {
            _allowedTypes = allowedTypes.Split(',');
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var file = value as HttpPostedFileBase;
            if (file == null)
                return ValidationResult.Success;

            string extension = System.IO.Path.GetExtension(file.FileName)?.ToLower()?.TrimStart('.') ?? "";

            if (string.IsNullOrEmpty(extension))
                return new ValidationResult("El archivo no tiene una extensión válida.");

            if (!_allowedTypes.Contains(extension))
            {
                string allowed = string.Join(", ", _allowedTypes);
                return new ValidationResult($"Tipo de archivo no permitido. Formatos aceptados: {allowed}");
            }

            // Verificar content type también
            string[] validContentTypes = {
                "image/jpeg", "image/jpg", "image/png", "image/gif",
                "image/x-png", "image/pjpeg"
            };

            if (!validContentTypes.Contains(file.ContentType.ToLower()))
            {
                return new ValidationResult($"Tipo de contenido no válido. El archivo parece no ser una imagen válida.");
            }

            return ValidationResult.Success;
        }
    }

    // ⚡ NUEVO: Atributo para validar email único (se verifica en el controlador)
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UniqueEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            string email = value.ToString();

            // Esta validación se hará en el controlador con la base de datos
            // Aquí solo validamos el formato
            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(email))
            {
                return new ValidationResult("Formato de email inválido.");
            }

            return ValidationResult.Success;
        }
    }

    // ⚡ NUEVO: Atributo para validar cédula única (se verifica en el controlador)
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UniqueCedulaAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            string cedula = value.ToString();

            if (string.IsNullOrEmpty(cedula))
                return ValidationResult.Success;

            // Validar formato de cédula ecuatoriana
            if (cedula.Length != 10 || !cedula.All(char.IsDigit))
            {
                return new ValidationResult("La cédula debe tener 10 dígitos numéricos.");
            }

            // Validar provincia (primeros dos dígitos)
            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24)
            {
                return new ValidationResult("Los dos primeros dígitos de la cédula no corresponden a una provincia válida de Ecuador (01-24).");
            }

            // Validar tercer dígito (0-6 para personas naturales)
            int tercerDigito = int.Parse(cedula.Substring(2, 1));
            if (tercerDigito > 6)
            {
                return new ValidationResult("El tercer dígito de la cédula no es válido para personas naturales.");
            }

            return ValidationResult.Success;
        }
    }

    // ⚡ NUEVO: Atributo para validar que la imagen es realmente una imagen
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ValidImageAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var file = value as HttpPostedFileBase;
            if (file == null || file.ContentLength == 0)
                return ValidationResult.Success;

            try
            {
                // Verificar cabeceras de imagen
                using (var image = System.Drawing.Image.FromStream(file.InputStream))
                {
                    // Verificar dimensiones mínimas y máximas
                    if (image.Width < 100 || image.Height < 100)
                    {
                        return new ValidationResult("La imagen es demasiado pequeña. Mínimo 100x100 píxeles.");
                    }

                    if (image.Width > 4000 || image.Height > 4000)
                    {
                        return new ValidationResult("La imagen es demasiado grande. Máximo 4000x4000 píxeles.");
                    }

                    // Verificar relación de aspecto
                    double aspectRatio = (double)image.Width / image.Height;
                    if (aspectRatio < 0.5 || aspectRatio > 2.0)
                    {
                        return new ValidationResult("La imagen tiene una relación de aspecto extrema. Use una imagen más cuadrada.");
                    }

                    file.InputStream.Position = 0; // Resetear stream para futuras lecturas
                }
            }
            catch (ArgumentException)
            {
                return new ValidationResult("El archivo no es una imagen válida o está corrupto.");
            }
            catch (Exception ex)
            {
                return new ValidationResult($"Error al validar la imagen: {ex.Message}");
            }

            return ValidationResult.Success;
        }
    }
}