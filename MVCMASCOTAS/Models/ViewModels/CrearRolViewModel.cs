// Models/ViewModels/CrearRolViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class CrearRolViewModel
    {
        [Required(ErrorMessage = "El nombre del rol es requerido")]
        [Display(Name = "Nombre del Rol")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string NombreRol { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true; // Valor por defecto true
    }
}