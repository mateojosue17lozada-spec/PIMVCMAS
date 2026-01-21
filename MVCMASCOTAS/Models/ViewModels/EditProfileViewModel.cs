using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
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

        [Display(Name = "Nueva Imagen de Perfil")]
        public HttpPostedFileBase NuevaImagenPerfil { get; set; }
    }
}