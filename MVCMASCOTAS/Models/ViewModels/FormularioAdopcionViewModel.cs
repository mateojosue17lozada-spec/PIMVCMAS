using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    // Enums para opciones predefinidas
    public enum MotivoAdopcionEnum
    {
        [Display(Name = "Compañía para mí o mi familia")]
        Compania,
        [Display(Name = "Darle un hogar a un animal necesitado")]
        Hogar,
        [Display(Name = "Protección del hogar")]
        Proteccion,
        [Display(Name = "Para mis hijos")]
        ParaHijos,
        [Display(Name = "Rescatar al animal")]
        Rescate,
        [Display(Name = "Otro motivo")]
        Otro
    }

    public enum PlanCambioResidenciaEnum
    {
        [Display(Name = "Me llevaría la mascota conmigo")]
        LlevarConmigo,
        [Display(Name = "Buscaría un lugar que acepte mascotas")]
        BuscarLugar,
        [Display(Name = "La dejaría con familiares/amigos")]
        Familiares,
        [Display(Name = "La devolvería al refugio")]
        Devolver,
        [Display(Name = "No lo he considerado")]
        NoConsiderado
    }

    public enum PlanProblemasComportamientoEnum
    {
        [Display(Name = "Consultaría con un veterinario")]
        Veterinario,
        [Display(Name = "Buscaría un entrenador profesional")]
        Entrenador,
        [Display(Name = "Buscaría información en internet/libros")]
        Autoaprendizaje,
        [Display(Name = "Consultaría con el refugio")]
        Refugio,
        [Display(Name = "No sé qué haría")]
        NoSabe
    }

    public enum QuienCuidaraEnum
    {
        [Display(Name = "Yo trabajo desde casa / soy independiente")]
        TrabajoCasa,
        [Display(Name = "Mi cónyuge/pareja")]
        Conyuge,
        [Display(Name = "Mis hijos/adultos en casa")]
        Hijos,
        [Display(Name = "Un familiar/amigo")]
        Familiar,
        [Display(Name = "La mascota estará sola")]
        Sola,
        [Display(Name = "Otro")]
        Otro
    }

    public enum VeterinarioReferenciaEnum
    {
        [Display(Name = "Sí, tengo veterinario de confianza")]
        Si,
        [Display(Name = "No, pero conozco clínicas cercanas")]
        Conozco,
        [Display(Name = "No, buscaré uno cuando lo necesite")]
        No
    }

    public class FormularioAdopcionViewModel
    {
        // Propiedad para identificar la mascota a adoptar
        [Required(ErrorMessage = "El ID de la mascota es requerido")]
        public int MascotaId { get; set; }

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

        // Composición del Hogar
        [Required(ErrorMessage = "El número de personas es requerido")]
        [Display(Name = "Personas en casa")]
        public int PersonasEnCasa { get; set; }

        [Display(Name = "¿Hay niños en casa?")]
        public bool HayNinios { get; set; }

        [Display(Name = "Edades de los niños")]
        public string EdadesNinios { get; set; }

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

        [Display(Name = "Otras mascotas")]
        public string OtrasMascotas { get; set; }

        [Display(Name = "¿Están esterilizadas?")]
        public bool MascotasEsterilizadas { get; set; }

        // Disponibilidad
        [Required(ErrorMessage = "El tiempo disponible es requerido")]
        [Display(Name = "Tiempo disponible diario")]
        public string TiempoDisponibleDiario { get; set; }

        [Required(ErrorMessage = "Especifique quién cuidará a la mascota")]
        [Display(Name = "¿Quién cuidará de la mascota durante el día?")]
        public QuienCuidaraEnum? QuienCuidaraMascota { get; set; }

        [Display(Name = "Especifique otro cuidador")]
        public string OtroQuienCuidaraMascota { get; set; }

        // Motivación - AHORA CON OPCIONES PREDEFINIDAS
        [Required(ErrorMessage = "El motivo de adopción es requerido")]
        [Display(Name = "¿Por qué deseas adoptar esta mascota?")]
        public MotivoAdopcionEnum? MotivoAdopcion { get; set; }

        [Display(Name = "Especifique otro motivo")]
        public string OtroMotivoAdopcion { get; set; }

        [Required(ErrorMessage = "Debe especificar sus planes si cambia de residencia")]
        [Display(Name = "¿Qué harías si cambias de residencia?")]
        public PlanCambioResidenciaEnum? QuePasaSiCambiaResidencia { get; set; }

        [Required(ErrorMessage = "Debe especificar cómo manejaría problemas de comportamiento")]
        [Display(Name = "¿Qué harías ante problemas de comportamiento?")]
        public PlanProblemasComportamientoEnum? QuePasaSiProblemasComportamiento { get; set; }

        // Referencias - MEJORADAS
        [Display(Name = "¿Tiene veterinario de referencia?")]
        public VeterinarioReferenciaEnum? VeterinarioReferencia { get; set; }

        [Display(Name = "Referencia personal 1")]
        public string ReferenciaPersonal1 { get; set; }

        [Display(Name = "Teléfono referencia 1")]
        public string TelefonoReferencia1 { get; set; }

        [Display(Name = "Referencia personal 2")]
        public string ReferenciaPersonal2 { get; set; }

        [Display(Name = "Teléfono referencia 2")]
        public string TelefonoReferencia2 { get; set; }

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
    }
}