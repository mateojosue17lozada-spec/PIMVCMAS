using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Helpers
{
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
}