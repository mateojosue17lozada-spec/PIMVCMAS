using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class FormularioAdopcionViewModel
    {
        // Datos de Vivienda
        [Required(ErrorMessage = "El tipo de vivienda es requerido")]
        [Display(Name = "Tipo de Vivienda")]
        public string TipoVivienda { get; set; }

        [Display(Name = "¿Es vivienda propia?")]
        public bool ViviendaPropia { get; set; }

        [Display(Name = "¿Tiene jardín?")]
        public bool TieneJardin { get; set; }

        [Display(Name = "Tamaño del jardín")]
        public string TamanioJardin { get; set; }

        [Display(Name = "¿Tiene permiso para tener mascotas?")]
        public bool PermisoMascotas { get; set; }

        // Experiencia
        [Display(Name = "¿Ha tenido mascotas antes?")]
        public bool ExperienciaPreviaConMascotas { get; set; }

        [Display(Name = "Detalle su experiencia")]
        [DataType(DataType.MultilineText)]
        public string DetalleExperiencia { get; set; }

        [Display(Name = "¿Tiene mascotas actualmente?")]
        public bool TieneMascotasActualmente { get; set; }

        [Display(Name = "Cantidad de perros")]
        public int CantidadPerros { get; set; }

        [Display(Name = "Cantidad de gatos")]
        public int CantidadGatos { get; set; }

        [Display(Name = "¿Están esterilizadas?")]
        public bool MascotasEsterilizadas { get; set; }

        // Disponibilidad
        [Required(ErrorMessage = "El tiempo disponible es requerido")]
        [Display(Name = "Tiempo disponible diario")]
        public string TiempoDisponibleDiario { get; set; }

        [Display(Name = "Personas en casa")]
        public int PersonasEnCasa { get; set; }

        [Display(Name = "¿Hay niños en casa?")]
        public bool HayNinios { get; set; }

        [Display(Name = "Edades de los niños")]
        public string EdadesNinios { get; set; }

        // Compromisos Legales
        [Required(ErrorMessage = "Debe aceptar la esterilización")]
        [Display(Name = "Acepto esterilizar a la mascota")]
        public bool AceptaEsterilizacion { get; set; }

        [Required(ErrorMessage = "Debe aceptar las visitas de seguimiento")]
        [Display(Name = "Acepto visitas de seguimiento")]
        public bool AceptaVisitasSeguimiento { get; set; }

        [Required(ErrorMessage = "Debe aceptar las condiciones de LOBA")]
        [Display(Name = "Acepto las condiciones de la Ley Orgánica de Bienestar Animal")]
        public bool AceptaCondicionesLOBA { get; set; }

        [Required(ErrorMessage = "Debe aceptar la devolución responsable")]
        [Display(Name = "Acepto devolver la mascota al refugio si no puedo atenderla")]
        public bool AceptaDevolucionSiNoPuedeAtender { get; set; }

        // Motivación
        [Required(ErrorMessage = "El motivo de adopción es requerido")]
        [Display(Name = "¿Por qué desea adoptar?")]
        [DataType(DataType.MultilineText)]
        public string MotivoAdopcion { get; set; }

        [Display(Name = "¿Qué haría si cambia de residencia?")]
        [DataType(DataType.MultilineText)]
        public string QuePasaSiCambiaResidencia { get; set; }

        [Display(Name = "¿Qué haría si la mascota tiene problemas de comportamiento?")]
        [DataType(DataType.MultilineText)]
        public string QuePasaSiProblemasComportamiento { get; set; }
    }
}