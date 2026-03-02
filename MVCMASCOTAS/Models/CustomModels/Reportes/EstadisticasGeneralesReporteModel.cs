using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels.Reportes
{
    public class EstadisticasGeneralesReporteModel
    {
        // Totales
        public int TotalMascotas { get; set; }
        public int TotalAdopciones { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int SeguimientosPendientes { get; set; }

        // Distribución por especie
        public int TotalPerros { get; set; }
        public int TotalGatos { get; set; }
        public int TotalOtros { get; set; }

        // Distribución por estado
        public int Disponibles { get; set; }
        public int EnTratamiento { get; set; }
        public int Adoptadas { get; set; }
        public int Rescatadas { get; set; }

        // Métricas de tiempo
        public double PromedioDiasAdopcion { get; set; }
        public int MascotasSinAdoptarLargoPlazo { get; set; } // más de 6 meses

        // Este mes
        public int AdopcionesEsteMes { get; set; }
        public int SolicitudesEsteMes { get; set; }
        public int IngresosEsteMes { get; set; }

        // Fecha de generación
        public DateTime FechaGeneracion { get; set; }
    }
}