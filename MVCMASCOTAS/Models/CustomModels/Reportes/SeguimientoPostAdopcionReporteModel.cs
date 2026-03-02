using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels.Reportes
{
    public class SeguimientoPostAdopcionReporteModel
    {
        // Información del seguimiento
        public int SeguimientoId { get; set; }
        public DateTime? FechaSeguimiento { get; set; }
        public string TipoSeguimiento { get; set; }
        public string EstadoMascota { get; set; }
        public string CondicionesVivienda { get; set; }
        public string RelacionConAdoptante { get; set; }
        public string Observaciones { get; set; }
        public string Recomendaciones { get; set; }
        public bool RequiereIntervencion { get; set; }
        public DateTime? ProximoSeguimiento { get; set; }

        // Información del contrato
        public int ContratoId { get; set; }
        public string NumeroContrato { get; set; }
        public DateTime? FechaContrato { get; set; }

        // Información de la mascota
        public int MascotaId { get; set; }
        public string NombreMascota { get; set; }
        public string EspecieMascota { get; set; }
        public string RazaMascota { get; set; }

        // Información del adoptante
        public string NombreAdoptante { get; set; }
        public string TelefonoAdoptante { get; set; }
        public string EmailAdoptante { get; set; }
        public string DireccionAdoptante { get; set; }

        // Responsable del seguimiento
        public string ResponsableNombre { get; set; }

        // Metadatos
        public DateTime FechaGeneracion { get; set; }
        public string EstadoSeguimiento // Calculado: Realizado, Pendiente, Vencido
        {
            get
            {
                if (FechaSeguimiento.HasValue)
                    return "Realizado";
                else if (ProximoSeguimiento.HasValue && ProximoSeguimiento.Value < DateTime.Now)
                    return "Vencido";
                else
                    return "Pendiente";
            }
        }
        public int DiasRetraso
        {
            get
            {
                if (!FechaSeguimiento.HasValue && ProximoSeguimiento.HasValue && ProximoSeguimiento.Value < DateTime.Now)
                    return (DateTime.Now - ProximoSeguimiento.Value).Days;
                return 0;
            }
        }
    }
}