using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels.Reportes
{
    public class SolicitudAdopcionReporteModel
    {
        // Información de la solicitud
        public int SolicitudId { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; }
        public string EstadoAdopcion { get; set; }

        // Solicitante
        public int SolicitanteId { get; set; }
        public string NombreSolicitante { get; set; }
        public string CedulaSolicitante { get; set; }
        public string TelefonoSolicitante { get; set; }
        public string EmailSolicitante { get; set; }
        public string CiudadSolicitante { get; set; }

        // Mascota solicitada
        public int MascotaId { get; set; }
        public string NombreMascota { get; set; }
        public string EspecieMascota { get; set; }
        public string RazaMascota { get; set; }

        // Evaluación
        public DateTime? FechaEvaluacion { get; set; }
        public int? PuntajeEvaluacion { get; set; }
        public string ResultadoEvaluacion { get; set; }
        public string EvaluadorNombre { get; set; }

        // Datos del formulario
        public string TipoVivienda { get; set; }
        public string ViviendaPropia { get; set; }  // "Sí" / "No"
        public string TieneJardin { get; set; }
        public int? PersonasEnCasa { get; set; }
        public string HayNinios { get; set; }
        public string ExperienciaPrevia { get; set; }
        public string TieneMascotas { get; set; }
        public string TiempoDisponible { get; set; }

        // Compromisos
        public string AceptaEsterilizacion { get; set; }
        public string AceptaSeguimiento { get; set; }

        // Resolución
        public DateTime? FechaRespuesta { get; set; }
        public string MotivoRechazo { get; set; }
        public string Observaciones { get; set; }

        // Tiempos de procesamiento
        public int DiasEnProceso { get; set; }

        // Metadatos
        public DateTime FechaGeneracion { get; set; }
    }
}