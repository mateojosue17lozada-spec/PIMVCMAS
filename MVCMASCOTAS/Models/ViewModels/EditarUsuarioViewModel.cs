using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class EditarUsuarioViewModel
    {
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [StringLength(13, ErrorMessage = "La cédula no puede exceder 13 caracteres")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        [Display(Name = "Ciudad")]
        public string Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; }

        [Display(Name = "Usuario Activo")]
        public bool Activo { get; set; }

        [Display(Name = "Email Confirmado")]
        public bool EmailConfirmado { get; set; }

        [Display(Name = "Teléfono Confirmado")]
        public bool TelefonoConfirmado { get; set; }

        public string ImagenPerfilBase64 { get; set; }

        [Display(Name = "Nueva Imagen de Perfil")]
        public HttpPostedFileBase NuevaImagenPerfil { get; set; }

        // Nuevas propiedades para roles
        [Display(Name = "Roles del Usuario")]
        public List<int> RolesSeleccionados { get; set; }

        public List<RolViewModel> RolesDisponibles { get; set; }

        [Display(Name = "Fecha de Registro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? FechaRegistro { get; set; }

        [Display(Name = "Último Acceso")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UltimoAcceso { get; set; }

        public EditarUsuarioViewModel()
        {
            RolesSeleccionados = new List<int>();
            RolesDisponibles = new List<RolViewModel>();
        }
    }

    public class RolViewModel
    {
        public int RolId { get; set; }
        public string NombreRol { get; set; }
        public string Descripcion { get; set; }
        public bool Seleccionado { get; set; }
    }
}