using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class SolicitudAdopcionViewModel
    {
        public int SolicitudId { get; set; }
        public int MascotaId { get; set; }
        public int UsuarioId { get; set; }

        [Display(Name = "Fecha de Solicitud")]
        public DateTime FechaSolicitud { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Puntaje de Evaluación")]
        public int? PuntajeEvaluacion { get; set; }

        [Display(Name = "Resultado de Evaluación")]
        public string ResultadoEvaluacion { get; set; }

        [Display(Name = "Fecha de Evaluación")]
        public DateTime? FechaEvaluacion { get; set; }

        public int? EvaluadoPor { get; set; }

        [Display(Name = "Motivo de Rechazo")]
        [DataType(DataType.MultilineText)]
        public string MotivoRechazo { get; set; }

        [Display(Name = "Fecha de Respuesta")]
        public DateTime? FechaRespuesta { get; set; }

        [Display(Name = "Observaciones")]
        [DataType(DataType.MultilineText)]
        public string Observaciones { get; set; }

        // Información de la mascota
        public string NombreMascota { get; set; }
        public string EspecieMascota { get; set; }
        public string RazaMascota { get; set; }
        public string ImagenMascotaBase64 { get; set; }

        // Información del solicitante
        public string NombreSolicitante { get; set; }
        public string EmailSolicitante { get; set; }
        public string TelefonoSolicitante { get; set; }
        public string DireccionSolicitante { get; set; }
        public string CiudadSolicitante { get; set; }

        // Información del evaluador
        public string NombreEvaluador { get; set; }

        // Datos del formulario de adopción
        public FormularioAdopcionViewModel FormularioDetalle { get; set; }

        // Para mostrar estado visual
        public string EstadoColor { get; set; }
        public string EstadoIcono { get; set; }

        // Validación de estado
        public bool PuedeAprobar => Estado == "Pendiente" || Estado == "En Revisión";
        public bool PuedeRechazar => Estado == "Pendiente" || Estado == "En Revisión";
        public bool EstaAprobada => Estado == "Aprobada";
        public bool EstaRechazada => Estado == "Rechazada";
        public bool EstaPendiente => Estado == "Pendiente";
    }
}