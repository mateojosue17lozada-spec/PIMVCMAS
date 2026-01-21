using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class MascotaViewModel
    {
        public int MascotaId { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La especie es requerida")]
        [Display(Name = "Especie")]
        public string Especie { get; set; }

        [StringLength(100)]
        [Display(Name = "Raza")]
        public string Raza { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [StringLength(50)]
        [Display(Name = "Edad Aproximada")]
        public string EdadAproximada { get; set; }

        [Display(Name = "Tamaño")]
        public string Tamanio { get; set; }

        [StringLength(100)]
        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Categoría")]
        public string Categoria { get; set; }

        [Display(Name = "Tipo Especial")]
        public string TipoEspecial { get; set; }

        [Display(Name = "Descripción General")]
        [DataType(DataType.MultilineText)]
        public string DescripcionGeneral { get; set; }

        [Display(Name = "Características de Comportamiento")]
        [DataType(DataType.MultilineText)]
        public string CaracteristicasComportamiento { get; set; }

        [Display(Name = "Historia de Rescate")]
        [DataType(DataType.MultilineText)]
        public string HistoriaRescate { get; set; }

        [Display(Name = "Imagen Principal")]
        public HttpPostedFileBase ImagenPrincipalFile { get; set; }

        public byte[] ImagenPrincipal { get; set; }

        [Display(Name = "Esterilizado")]
        public bool Esterilizado { get; set; }

        [StringLength(50)]
        [Display(Name = "Microchip")]
        public string Microchip { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        public DateTime FechaIngreso { get; set; }

        // Para mostrar en vistas
        public string ImagenBase64 { get; set; }
        public string VeterinarioNombre { get; set; }
        public string RescatistaNombre { get; set; }
    }
}