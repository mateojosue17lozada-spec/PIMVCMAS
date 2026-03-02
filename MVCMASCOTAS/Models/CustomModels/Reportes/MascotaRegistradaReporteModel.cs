using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels.Reportes
{
    public class MascotaRegistradaReporteModel
    {
        // Información básica
        public int MascotaId { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public string Sexo { get; set; }
        public string EdadAproximada { get; set; }
        public string Tamanio { get; set; }
        public string Color { get; set; }

        // Clasificación
        public string Categoria { get; set; }
        public string TipoEspecial { get; set; }

        // Fechas importantes
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaDisponible { get; set; }
        public DateTime? FechaAdopcion { get; set; }

        // Estado actual
        public string Estado { get; set; }
        public bool? Esterilizado { get; set; }
        public string Microchip { get; set; }
        public bool? Activo { get; set; }

        // Responsables
        public string VeterinarioAsignado { get; set; }
        public string RescatistaNombre { get; set; }

        // Estadísticas
        public int TotalCambiosEstado { get; set; }
        public int TotalTratamientos { get; set; }
        public int TotalVacunas { get; set; }

        // Tiempo en el refugio
        public int DiasEnRefugio { get; set; }

        // Metadatos
        public DateTime FechaGeneracion { get; set; }
    }
}