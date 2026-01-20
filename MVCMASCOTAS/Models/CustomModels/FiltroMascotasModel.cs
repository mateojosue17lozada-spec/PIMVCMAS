using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.CustomModels
{
    public class FiltroMascotasModel
    {
        [Display(Name = "Buscar")]
        public string Busqueda { get; set; }

        [Display(Name = "Especie")]
        public string Especie { get; set; }

        [Display(Name = "Raza")]
        public string Raza { get; set; }

        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [Display(Name = "Tamaño")]
        public string Tamanio { get; set; }

        [Display(Name = "Categoría")]
        public string Categoria { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Edad")]
        public string EdadAproximada { get; set; }

        [Display(Name = "Esterilizado")]
        public bool? Esterilizado { get; set; }

        [Display(Name = "Tipo Especial")]
        public string TipoEspecial { get; set; }

        [Display(Name = "Ordenar Por")]
        public string OrdenarPor { get; set; }

        // Paginación
        public int Pagina { get; set; } = 1;
        public int ElementosPorPagina { get; set; } = 12;

        // Filtros adicionales para admin
        public bool? Activo { get; set; }
        public int? VeterinarioAsignado { get; set; }
        public int? RescatistaId { get; set; }
    }
}