using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels.Reportes
{
    public class AdopcionRealizadaReporteModel
    {
        // Información de la adopción
        public int SolicitudId { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaAdopcion { get; set; }
        public string NumeroContrato { get; set; }

        // Información de la mascota
        public int MascotaId { get; set; }
        public string NombreMascota { get; set; }
        public string EspecieMascota { get; set; }
        public string RazaMascota { get; set; }
        public string SexoMascota { get; set; }
        public string EdadMascota { get; set; }
        public string TamanioMascota { get; set; }

        // Información del adoptante
        public int AdoptanteId { get; set; }
        public string NombreAdoptante { get; set; }
        public string CedulaAdoptante { get; set; }
        public string TelefonoAdoptante { get; set; }
        public string EmailAdoptante { get; set; }
        public string DireccionAdoptante { get; set; }
        public string CiudadAdoptante { get; set; }

        // Información del proceso
        public string EstadoSolicitud { get; set; }
        public int? PuntajeEvaluacion { get; set; }
        public string ResultadoEvaluacion { get; set; }

        // Información del responsable
        public string EvaluadoPor { get; set; }
        public string VeterinarioAsignado { get; set; }

        // Metadatos
        public DateTime FechaGeneracion { get; set; }
    }
}