using System;
using System.Collections.Generic;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Estadísticas generales
        public int TotalMascotas { get; set; }
        public int MascotasDisponibles { get; set; }
        public int MascotasAdoptadas { get; set; }
        public int MascotasEnTratamiento { get; set; }
        public int MascotasCriticas { get; set; }

        // Adopciones
        public int SolicitudesPendientes { get; set; }
        public int SolicitudesAprobadas { get; set; }
        public int SolicitudesRechazadas { get; set; }
        public int AdopcionesEsteMes { get; set; }
        public int AdopcionesEsteAnio { get; set; }

        // Donaciones
        public decimal TotalDonacionesMes { get; set; }
        public decimal TotalDonacionesAnio { get; set; }
        public int NumeroDonantesMes { get; set; }
        public int ApadrinamientosActivos { get; set; }

        // Voluntariado
        public int VoluntariosActivos { get; set; }
        public int ActividadesProgramadas { get; set; }
        public decimal HorasVoluntariadoMes { get; set; }

        // Veterinaria
        public int ConsultasPendientes { get; set; }
        public int TratamientosActivos { get; set; }
        public int VacunacionesPendientes { get; set; }

        // Tienda
        public int ProductosDisponibles { get; set; }
        public int ProductosBajoStock { get; set; }
        public int PedidosPendientes { get; set; }
        public decimal VentasMes { get; set; }

        // Contabilidad
        public decimal IngresosMes { get; set; }
        public decimal EgresosMes { get; set; }
        public decimal BalanceMes { get; set; }

        // Rescate
        public int ReportesAbiertos { get; set; }
        public int MascotasPerdidasReportadas { get; set; }

        // Listas para gráficos y detalles
        public List<SolicitudAdopcionViewModel> UltimasSolicitudes { get; set; }
        public List<DonacionViewModel> UltimasDonaciones { get; set; }
        public List<MascotaViewModel> MascotasRecientes { get; set; }
        public List<MovimientosContables> UltimosMovimientos { get; set; }

        // Alertas y notificaciones
        public List<string> Alertas { get; set; }

        public DashboardViewModel()
        {
            UltimasSolicitudes = new List<SolicitudAdopcionViewModel>();
            UltimasDonaciones = new List<DonacionViewModel>();
            MascotasRecientes = new List<MascotaViewModel>();
            UltimosMovimientos = new List<MovimientosContables>();
            Alertas = new List<string>();
        }
    }
}