using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class EditProfileViewModel
    {
        // ======================
        // PROPIEDADES EDITABLES
        // ======================

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string Telefono { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        [Display(Name = "Ciudad")]
        public string Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; }

        // ======================
        // PROPIEDADES DE SOLO LECTURA (para mostrar)
        // ======================

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } // SOLO PARA MOSTRAR, no se edita

        [Display(Name = "Cédula")]
        [StringLength(13, ErrorMessage = "La cédula no puede exceder 13 caracteres")]
        public string Cedula { get; set; } // SOLO PARA MOSTRAR, no se edita

        [Display(Name = "Fecha de Registro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime? FechaRegistro { get; set; } // SOLO PARA MOSTRAR

        // ======================
        // PROPIEDADES DE ARCHIVO
        // ======================

        [Display(Name = "Nueva Imagen de Perfil")]
        public HttpPostedFileBase NuevaImagenPerfil { get; set; }

        // ======================
        // PROPIEDADES ADICIONALES (para la vista)
        // ======================

        [Display(Name = "Imagen Actual")]
        public string ImagenPerfilBase64 { get; set; }

        [Display(Name = "¿Activo?")]
        public bool? Activo { get; set; } // SOLO LECTURA

        [Display(Name = "Email Confirmado")]
        public bool? EmailConfirmado { get; set; } // SOLO LECTURA

        [Display(Name = "Teléfono Confirmado")]
        public bool? TelefonoConfirmado { get; set; } // SOLO LECTURA

        // ======================
        // CONSTRUCTORES
        // ======================

        public EditProfileViewModel()
        {
            // Valores por defecto
            Activo = true;
            EmailConfirmado = false;
            TelefonoConfirmado = false;
        }

        // ======================
        // MÉTODOS DE AYUDA
        // ======================

        /// <summary>
        /// Mapea desde un modelo Usuarios al ViewModel
        /// </summary>
        public static EditProfileViewModel FromUsuario(Usuarios usuario)
        {
            if (usuario == null)
                return null;

            return new EditProfileViewModel
            {
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Cedula = usuario.Cedula,
                Direccion = usuario.Direccion,
                Ciudad = usuario.Ciudad,
                Provincia = usuario.Provincia,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo,
                EmailConfirmado = usuario.EmailConfirmado,
                TelefonoConfirmado = usuario.TelefonoConfirmado,

                // Convertir imagen a base64 si existe
                ImagenPerfilBase64 = usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0
                    ? Convert.ToBase64String(usuario.ImagenPerfil)
                    : null
            };
        }

        /// <summary>
        /// Actualiza un modelo Usuarios con los datos del ViewModel (solo propiedades editables)
        /// </summary>
        public void UpdateUsuario(Usuarios usuario)
        {
            if (usuario == null)
                return;

            // Solo actualizar propiedades editables
            usuario.NombreCompleto = NombreCompleto;
            usuario.Telefono = Telefono;
            usuario.Direccion = Direccion;
            usuario.Ciudad = Ciudad;
            usuario.Provincia = Provincia;

            // NOTA: Email, Cedula, FechaRegistro, Activo, etc. NO se actualizan
            // ya que son propiedades de solo lectura en el ViewModel
        }
    }
}