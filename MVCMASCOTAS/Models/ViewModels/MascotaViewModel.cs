using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Models.ViewModels
{
    /// <summary>
    /// ViewModel completo para gestión de mascotas
    /// VERSIÓN CORREGIDA - Con todas las propiedades necesarias para la vista
    /// </summary>
    public class MascotaViewModel
    {
        public int MascotaId { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        [Display(Name = "Nombre de la Mascota")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La especie es requerida")]
        [Display(Name = "Especie")]
        public string Especie { get; set; }

        [StringLength(100, ErrorMessage = "La raza no puede exceder 100 caracteres")]
        [Display(Name = "Raza")]
        public string Raza { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; } = "Macho";

        [Required(ErrorMessage = "La edad aproximada es requerida")]
        [StringLength(50, ErrorMessage = "La edad no puede exceder 50 caracteres")]
        [Display(Name = "Edad Aproximada")]
        public string EdadAproximada { get; set; }

        [Required(ErrorMessage = "El tamaño es requerido")]
        [Display(Name = "Tamaño")]
        public string Tamanio { get; set; }

        [StringLength(100, ErrorMessage = "El color no puede exceder 100 caracteres")]
        [Display(Name = "Color/Pelaje")]
        public string Color { get; set; }

        [Display(Name = "Categoría")]
        public string Categoria { get; set; } = "Normal";

        [Display(Name = "Tipo Especial")]
        [StringLength(100, ErrorMessage = "El tipo especial no puede exceder 100 caracteres")]
        public string TipoEspecial { get; set; }

        [Display(Name = "Descripción General")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string DescripcionGeneral { get; set; }

        [Display(Name = "Características de Comportamiento")]
        [DataType(DataType.MultilineText)]
        [StringLength(500, ErrorMessage = "Las características no pueden exceder 500 caracteres")]
        public string CaracteristicasComportamiento { get; set; }

        [Display(Name = "Historia de Rescate")]
        [DataType(DataType.MultilineText)]
        [StringLength(1000, ErrorMessage = "La historia no puede exceder 1000 caracteres")]
        public string HistoriaRescate { get; set; }

        [Display(Name = "Imagen Principal")]
        [DataType(DataType.Upload)]
        public HttpPostedFileBase ImagenPrincipalFile { get; set; }

        public byte[] ImagenPrincipal { get; set; }

        public string ImagenBase64 { get; set; }

        [StringLength(50, ErrorMessage = "El número de microchip no puede exceder 50 caracteres")]
        [Display(Name = "Número de Microchip")]
        public string Microchip { get; set; }

        [Required(ErrorMessage = "El estado es requerido")]
        [Display(Name = "Estado Actual")]
        public string Estado { get; set; } = "Rescatada";

        [Required(ErrorMessage = "La fecha de ingreso es requerida")]
        [Display(Name = "Fecha de Ingreso al Refugio")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        [Display(Name = "¿Está Esterilizado/a?")]
        public bool Esterilizado { get; set; } = false;

        // ==========================================
        // ASIGNACIONES DE PERSONAL
        // ==========================================
        [Display(Name = "Veterinario Asignado")]
        public int? VeterinarioAsignado { get; set; }

        [Display(Name = "Rescatista")]
        public int? RescatistaId { get; set; }

        // Nombres para mostrar (no se guardan en BD)
        public string VeterinarioNombre { get; set; }
        public string RescatistaNombre { get; set; }

        // ==========================================
        // 🔥 NUEVAS PROPIEDADES PARA LAS LISTAS
        // ==========================================

        // Listas para dropdowns usando SelectList
        public SelectList VeterinariosList { get; set; }
        public SelectList RescatistasList { get; set; }
        public SelectList EstadosList { get; set; }
        public SelectList TamaniosList { get; set; }
        public SelectList CategoriasList { get; set; }
        public SelectList EspeciesList { get; set; }

        // Listas alternativas usando IEnumerable<SelectListItem> (para mayor flexibilidad)
        public IEnumerable<SelectListItem> Veterinarios { get; set; }
        public IEnumerable<SelectListItem> Rescatistas { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }
        public IEnumerable<SelectListItem> Tamanios { get; set; }
        public IEnumerable<SelectListItem> Categorias { get; set; }
        public IEnumerable<SelectListItem> Especies { get; set; }

        // ==========================================
        // MÉTODOS AUXILIARES
        // ==========================================

        /// <summary>
        /// Devuelve el texto del sexo para mostrar
        /// </summary>
        public string SexoTexto
        {
            get
            {
                return Sexo == "M" ? "Macho" : "Hembra";
            }
        }

        /// <summary>
        /// Verifica si la mascota tiene imagen
        /// </summary>
        public bool TieneImagen
        {
            get
            {
                return ImagenPrincipal != null && ImagenPrincipal.Length > 0;
            }
        }

        /// <summary>
        /// Devuelve la imagen en formato base64 para mostrar en HTML
        /// </summary>
        public string ImagenParaVista
        {
            get
            {
                if (TieneImagen && string.IsNullOrEmpty(ImagenBase64))
                {
                    return Convert.ToBase64String(ImagenPrincipal);
                }
                return ImagenBase64 ?? string.Empty;
            }
        }

        /// <summary>
        /// Constructor por defecto con valores iniciales
        /// </summary>
        public MascotaViewModel()
        {
            // Inicializar listas vacías para evitar null reference
            Veterinarios = new List<SelectListItem>();
            Rescatistas = new List<SelectListItem>();
            Estados = new List<SelectListItem>();
            Tamanios = new List<SelectListItem>();
            Categorias = new List<SelectListItem>();
            Especies = new List<SelectListItem>();

            // Valores por defecto ya asignados en las propiedades
        }

        /// <summary>
        /// Constructor para edición con datos existentes
        /// </summary>
        public static MascotaViewModel DesdeMascota(Models.Mascotas mascota)
        {
            if (mascota == null)
                return new MascotaViewModel();

            return new MascotaViewModel
            {
                MascotaId = mascota.MascotaId,
                Nombre = mascota.Nombre,
                Especie = mascota.Especie,
                Raza = mascota.Raza,
                Sexo = mascota.Sexo == "M" ? "Macho" : "Hembra",
                EdadAproximada = mascota.EdadAproximada,
                Tamanio = mascota.Tamanio,
                Color = mascota.Color,
                Categoria = mascota.Categoria,
                TipoEspecial = mascota.TipoEspecial,
                DescripcionGeneral = mascota.DescripcionGeneral,
                CaracteristicasComportamiento = mascota.CaracteristicasComportamiento,
                HistoriaRescate = mascota.HistoriaRescate,
                ImagenPrincipal = mascota.ImagenPrincipal,
                Microchip = mascota.Microchip,
                Estado = mascota.Estado,
                FechaIngreso = mascota.FechaIngreso ?? DateTime.Now,
                Esterilizado = mascota.Esterilizado ?? false,
                VeterinarioAsignado = mascota.VeterinarioAsignado,
                RescatistaId = mascota.RescatistaId
            };
        }
    }
}