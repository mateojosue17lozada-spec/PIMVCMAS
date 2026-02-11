using System;
using System.Collections.Generic;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class DashboardViewModel
    {
        // 1. ESTADÍSTICAS DE MASCOTAS
        public int TotalMascotas { get; set; }
        public int MascotasDisponibles { get; set; }
        public int MascotasAdoptadas { get; set; }
        public int MascotasEnTratamiento { get; set; }
        public int MascotasCriticas { get; set; }
        public int MascotasArchivadas { get; set; }
        public int MascotasRescatadas { get; set; }

        // 2. ADOPCIONES
        public int SolicitudesPendientes { get; set; }
        public int SolicitudesAprobadas { get; set; }
        public int SolicitudesRechazadas { get; set; }
        public int AdopcionesEsteMes { get; set; }
        public int AdopcionesEsteAnio { get; set; }

        // 3. DONACIONES
        public decimal TotalDonacionesMes { get; set; }
        public decimal TotalDonacionesAnio { get; set; }
        public int NumeroDonantesMes { get; set; }
        public int ApadrinamientosActivos { get; set; }

        // 4. VOLUNTARIADO
        public int VoluntariosActivos { get; set; }
        public int ActividadesProgramadas { get; set; }
        public decimal HorasVoluntariadoMes { get; set; }

        // 5. VETERINARIA
        public int ConsultasPendientes { get; set; }
        public int TratamientosActivos { get; set; }
        public int VacunacionesPendientes { get; set; }

        // 6. TIENDA
        public int ProductosDisponibles { get; set; }
        public int ProductosBajoStock { get; set; }
        public int PedidosPendientes { get; set; }
        public decimal VentasMes { get; set; }

        // 7. RESCATE
        public int ReportesAbiertos { get; set; }
        public int MascotasPerdidasReportadas { get; set; }

        // 8. CONTABILIDAD
        public decimal IngresosMes { get; set; }
        public decimal EgresosMes { get; set; }
        public decimal BalanceMes { get; set; }

        // 9. NUEVAS PROPIEDADES PARA CONFIGURACIÓN Y AUDITORÍA
        public int RegistrosAuditoriaDia { get; set; }
        public int AjustesSistema { get; set; }
        public int UsuariosConectados { get; set; }
        public int ConfiguracionesCriticas { get; set; }

        // 10. NUEVAS PROPIEDADES PARA MASCOTAS PERDIDAS
        public int MascotasPerdidasActivas { get; set; }
        public int MascotasEncontradas { get; set; }
        public int MascotasPerdidasMes { get; set; }
        public int ReportesAvistamiento { get; set; }

        // 11. NUEVAS PROPIEDADES PARA CAMPAÑAS DE ADOPCIÓN
        public int CampaniasActivas { get; set; }
        public int CampaniasProximasVencer { get; set; }
        public int TotalCampanias { get; set; }
        public int ParticipantesCampanias { get; set; }
        public decimal DonacionesCampaniasMes { get; set; }

        // 12. NUEVAS PROPIEDADES PARA REFUGIOS
        public int TotalRefugios { get; set; }
        public int RefugiosActivos { get; set; }
        public int CapacidadTotal { get; set; }
        public int OcupacionActual { get; set; }
        public int MascotasEnRefugios { get; set; }

        // 13. NUEVAS PROPIEDADES PARA GESTIÓN MASCOTAS (MASCOTASCONTROLLER)
        public int? SeguimientosPendientes { get; set; }
        public int? SeguimientosActivos { get; set; }
        public int? ContratosActivos { get; set; }
        public int? HistorialEstados { get; set; }
        public int? CambiosEstadoMes { get; set; }
        public int? SeguimientosCompletados { get; set; }
        public int? SeguimientosManuales { get; set; }
        public int? ContratosMes { get; set; }
        public int? MascotasExportadasMes { get; set; }
        public int? MascotasCreadasMes { get; set; }
        public int? MascotasEditadasMes { get; set; }
        public int? MascotasArchivadasMes { get; set; }

        // 14. PROPIEDADES DE ESTADO ESPECÍFICAS
        public string EstadoGestionMascotas { get; set; }
        public string EstadoSeguimiento { get; set; }
        public string EstadoContratos { get; set; }

        // 15. LISTAS PARA DETALLES (USANDO ENTIDADES DIRECTAS)
        public List<SolicitudAdopcion> UltimasSolicitudes { get; set; }
        public List<Donaciones> UltimasDonaciones { get; set; }
        public List<Mascotas> MascotasRecientes { get; set; }
        public List<MovimientosContables> UltimosMovimientos { get; set; }
        public List<MascotasPerdidas> UltimasMascotasPerdidas { get; set; }

        // 16. NUEVAS LISTAS PARA CAMPAÑAS Y REFUGIOS
        public List<CampaniaAdopcion> UltimasCampanias { get; set; }
        public List<RefugioAdopcion> RefugiosRecientes { get; set; }

        // 17. NUEVAS LISTAS PARA MASCOTASCONTROLLER
        public List<ContratoAdopcion> UltimosContratos { get; set; }
        public List<SeguimientoAdopcion> UltimosSeguimientos { get; set; }
        public List<HistorialEstadosMascota> UltimosCambiosEstado { get; set; }

        // 18. LISTAS PARA AUDITORÍA Y CONFIGURACIÓN (USANDO VIEWMODELS)
        public List<DashboardConfiguracionResumenViewModel> ConfiguracionesImportantes { get; set; }
        public List<DashboardAuditoriaViewModel> UltimosRegistrosAuditoria { get; set; }

        // 19. ALERTAS Y NOTIFICACIONES
        public List<string> Alertas { get; set; }
        public List<string> AlertasMascotasPerdidas { get; set; }
        public List<string> AlertasCampanias { get; set; }
        public List<string> AlertasRefugios { get; set; }
        public List<string> AlertasGestionMascotas { get; set; }

        // 20. DATOS PARA GRÁFICOS
        public Dictionary<string, int> MascotasPorEspecie { get; set; }
        public Dictionary<string, decimal> IngresosPorTipo { get; set; }
        public Dictionary<string, int> SolicitudesPorEstado { get; set; }

        // 21. NUEVOS DATOS PARA GRÁFICOS DE MASCOTAS PERDIDAS
        public Dictionary<string, int> MascotasPerdidasPorEspecie { get; set; }
        public Dictionary<string, int> MascotasPerdidasPorEstado { get; set; }

        // 22. NUEVOS DATOS PARA GRÁFICOS DE CAMPAÑAS
        public Dictionary<string, int> CampaniasPorEstado { get; set; }
        public Dictionary<string, int> CampaniasPorTipo { get; set; }

        // 23. NUEVOS DATOS PARA GRÁFICOS DE REFUGIOS
        public Dictionary<string, int> RefugiosPorUbicacion { get; set; }
        public Dictionary<string, int> MascotasPorRefugio { get; set; }

        // 24. NUEVOS DICCIONARIOS PARA AUDITORÍA
        public Dictionary<string, int> AuditoriaPorUsuario { get; set; }
        public Dictionary<string, int> AuditoriaPorAccion { get; set; }

        // 25. NUEVOS DICCIONARIOS PARA MASCOTASCONTROLLER
        public Dictionary<string, int> MascotasPorEstado { get; set; }
        public Dictionary<string, int> SeguimientosPorTipo { get; set; }
        public Dictionary<string, int> ContratosPorEstado { get; set; }
        public Dictionary<string, int> CambiosEstadoPorUsuario { get; set; }

        // 26. PROPIEDADES DE SÓLO LECTURA PARA TASAS
        public decimal TasaEncuentros
        {
            get
            {
                if (MascotasPerdidasMes == 0) return 0;
                return (decimal)MascotasEncontradas / MascotasPerdidasMes * 100;
            }
        }

        public decimal TasaAdopcion
        {
            get
            {
                if (TotalMascotas == 0) return 0;
                return (decimal)MascotasAdoptadas / TotalMascotas * 100;
            }
        }

        public decimal TasaOcupacionRefugios
        {
            get
            {
                if (CapacidadTotal == 0) return 0;
                return (decimal)OcupacionActual / CapacidadTotal * 100;
            }
        }

        public decimal EficienciaCampanias
        {
            get
            {
                if (TotalCampanias == 0) return 0;
                return (decimal)CampaniasActivas / TotalCampanias * 100;
            }
        }

        public decimal TasaSeguimientoCompletado
        {
            get
            {
                if (SeguimientosActivos == null || SeguimientosActivos.Value == 0) return 0;
                return (decimal)(SeguimientosCompletados ?? 0) / SeguimientosActivos.Value * 100;
            }
        }

        public decimal TasaCambiosEstado
        {
            get
            {
                if (TotalMascotas == 0) return 0;
                return (decimal)(CambiosEstadoMes ?? 0) / TotalMascotas * 100;
            }
        }

        // 27. PROPIEDADES DE ESTADO
        public bool HayAlertasCriticas
        {
            get
            {
                return MascotasCriticas > 0 || BalanceMes < 0 || ProductosBajoStock > 0 ||
                       ConfiguracionesCriticas > 0 || MascotasPerdidasActivas > 20 ||
                       TasaOcupacionRefugios > 90 || CampaniasProximasVencer > 3 ||
                       (SeguimientosPendientes ?? 0) > 10;
            }
        }

        public bool SistemaSaludable
        {
            get
            {
                return TasaAdopcion > 30 && BalanceMes >= 0 && MascotasCriticas == 0 &&
                       ConfiguracionesCriticas == 0 && MascotasPerdidasActivas <= 10 &&
                       TasaOcupacionRefugios <= 85 && CampaniasActivas > 0 &&
                       (SeguimientosPendientes ?? 0) <= 5 && TasaSeguimientoCompletado >= 70;
            }
        }

        public bool HayAlertasMascotasPerdidas
        {
            get
            {
                return AlertasMascotasPerdidas != null && AlertasMascotasPerdidas.Count > 0;
            }
        }

        public bool HayAlertasCampanias
        {
            get
            {
                return AlertasCampanias != null && AlertasCampanias.Count > 0;
            }
        }

        public bool HayAlertasRefugios
        {
            get
            {
                return AlertasRefugios != null && AlertasRefugios.Count > 0;
            }
        }

        public bool HayAlertasGestionMascotas
        {
            get
            {
                return AlertasGestionMascotas != null && AlertasGestionMascotas.Count > 0;
            }
        }

        public string EstadoAuditoria
        {
            get
            {
                if (RegistrosAuditoriaDia == 0) return "Sin actividad";
                if (RegistrosAuditoriaDia < 50) return "Normal";
                if (RegistrosAuditoriaDia < 200) return "Activo";
                return "Muy activo";
            }
        }

        public string EstadoConfiguracion
        {
            get
            {
                if (ConfiguracionesCriticas > 0) return "Crítico";
                if (AjustesSistema < 10) return "Básico";
                return "Completo";
            }
        }

        public string EstadoMascotasPerdidas
        {
            get
            {
                if (MascotasPerdidasActivas == 0) return "Sin reportes";
                if (MascotasPerdidasActivas <= 5) return "Normal";
                if (MascotasPerdidasActivas <= 15) return "Alerta";
                return "Crítico";
            }
        }

        public string EstadoRefugios
        {
            get
            {
                if (TotalRefugios == 0) return "Sin refugios";
                if (TasaOcupacionRefugios <= 70) return "Óptimo";
                if (TasaOcupacionRefugios <= 85) return "Normal";
                if (TasaOcupacionRefugios <= 95) return "Alerta";
                return "Sobrecargado";
            }
        }

        public string EstadoCampanias
        {
            get
            {
                if (TotalCampanias == 0) return "Sin campañas";
                if (CampaniasActivas >= 3 && EficienciaCampanias >= 70) return "Excelente";
                if (CampaniasActivas >= 1 && EficienciaCampanias >= 40) return "Bueno";
                if (CampaniasActivas >= 1) return "Normal";
                return "Inactivo";
            }
        }

        public string EstadoTasaEncuentros
        {
            get
            {
                if (TasaEncuentros >= 80) return "Excelente";
                if (TasaEncuentros >= 50) return "Buena";
                if (TasaEncuentros >= 30) return "Regular";
                return "Baja";
            }
        }

        public string EstadoGestionMascotasCalculado
        {
            get
            {
                if (!string.IsNullOrEmpty(EstadoGestionMascotas)) return EstadoGestionMascotas;

                if (TotalMascotas == 0) return "Sin mascotas";
                if (MascotasDisponibles == 0 && MascotasArchivadas == 0) return "Sin gestión";
                if (TasaAdopcion >= 50 && TasaSeguimientoCompletado >= 80) return "Excelente";
                if (TasaAdopcion >= 30 && TasaSeguimientoCompletado >= 60) return "Bueno";
                if (TasaAdopcion >= 10 && TasaSeguimientoCompletado >= 40) return "Normal";
                return "Necesita mejorar";
            }
        }

        public string EstadoSeguimientoCalculado
        {
            get
            {
                if (!string.IsNullOrEmpty(EstadoSeguimiento)) return EstadoSeguimiento;

                if (SeguimientosActivos == null || SeguimientosActivos.Value == 0) return "Sin seguimientos";
                if (TasaSeguimientoCompletado >= 90) return "Excelente";
                if (TasaSeguimientoCompletado >= 70) return "Bueno";
                if (TasaSeguimientoCompletado >= 50) return "Regular";
                return "Atrasado";
            }
        }

        public string EstadoContratosCalculado
        {
            get
            {
                if (!string.IsNullOrEmpty(EstadoContratos)) return EstadoContratos;

                if (ContratosActivos == null || ContratosActivos.Value == 0) return "Sin contratos";
                if ((ContratosMes ?? 0) >= 10) return "Muy activo";
                if ((ContratosMes ?? 0) >= 5) return "Activo";
                if ((ContratosMes ?? 0) >= 1) return "Normal";
                return "Inactivo";
            }
        }

        public bool MascotasPerdidasRequiereAtencion
        {
            get
            {
                return MascotasPerdidasActivas > 10 || TasaEncuentros < 30;
            }
        }

        public bool RefugiosRequiereAtencion
        {
            get
            {
                return TasaOcupacionRefugios > 85 || TotalRefugios == 0;
            }
        }

        public bool CampaniasRequiereAtencion
        {
            get
            {
                return CampaniasActivas == 0 || CampaniasProximasVencer > 0;
            }
        }

        public bool GestionMascotasRequiereAtencion
        {
            get
            {
                return MascotasCriticas > 0 || (SeguimientosPendientes ?? 0) > 10 ||
                       TasaAdopcion < 10 || TasaSeguimientoCompletado < 40;
            }
        }

        // 28. MÉTODOS PARA AGREGAR ALERTAS
        public void AgregarAlerta(string mensaje)
        {
            if (Alertas == null)
                Alertas = new List<string>();
            Alertas.Add(mensaje);
        }

        public void AgregarAlertaMascotaPerdida(string mensaje)
        {
            if (AlertasMascotasPerdidas == null)
                AlertasMascotasPerdidas = new List<string>();
            AlertasMascotasPerdidas.Add(mensaje);
        }

        public void AgregarAlertaCampania(string mensaje)
        {
            if (AlertasCampanias == null)
                AlertasCampanias = new List<string>();
            AlertasCampanias.Add(mensaje);
        }

        public void AgregarAlertaRefugio(string mensaje)
        {
            if (AlertasRefugios == null)
                AlertasRefugios = new List<string>();
            AlertasRefugios.Add(mensaje);
        }

        public void AgregarAlertaGestionMascotas(string mensaje)
        {
            if (AlertasGestionMascotas == null)
                AlertasGestionMascotas = new List<string>();
            AlertasGestionMascotas.Add(mensaje);
        }

        // 29. MÉTODOS PARA CONFIGURAR ESTADOS
        public void ConfigurarEstadosGestionMascotas()
        {
            EstadoGestionMascotas = EstadoGestionMascotasCalculado;
            EstadoSeguimiento = EstadoSeguimientoCalculado;
            EstadoContratos = EstadoContratosCalculado;
        }

        // 30. CONSTRUCTOR
        public DashboardViewModel()
        {
            // Inicializar listas
            UltimasSolicitudes = new List<SolicitudAdopcion>();
            UltimasDonaciones = new List<Donaciones>();
            MascotasRecientes = new List<Mascotas>();
            UltimosMovimientos = new List<MovimientosContables>();
            UltimasMascotasPerdidas = new List<MascotasPerdidas>();
            UltimasCampanias = new List<CampaniaAdopcion>();
            RefugiosRecientes = new List<RefugioAdopcion>();
            UltimosContratos = new List<ContratoAdopcion>();
            UltimosSeguimientos = new List<SeguimientoAdopcion>();
            UltimosCambiosEstado = new List<HistorialEstadosMascota>();
            UltimosRegistrosAuditoria = new List<DashboardAuditoriaViewModel>();
            ConfiguracionesImportantes = new List<DashboardConfiguracionResumenViewModel>();
            Alertas = new List<string>();
            AlertasMascotasPerdidas = new List<string>();
            AlertasCampanias = new List<string>();
            AlertasRefugios = new List<string>();
            AlertasGestionMascotas = new List<string>();

            // Inicializar propiedades con valores predeterminados
            SeguimientosPendientes = 0;
            SeguimientosActivos = 0;
            ContratosActivos = 0;
            HistorialEstados = 0;
            CambiosEstadoMes = 0;
            SeguimientosCompletados = 0;
            SeguimientosManuales = 0;
            ContratosMes = 0;
            MascotasExportadasMes = 0;
            MascotasCreadasMes = 0;
            MascotasEditadasMes = 0;
            MascotasArchivadasMes = 0;
            MascotasArchivadas = 0;

            // Inicializar diccionarios
            MascotasPorEspecie = new Dictionary<string, int>();
            IngresosPorTipo = new Dictionary<string, decimal>();
            SolicitudesPorEstado = new Dictionary<string, int>();
            MascotasPerdidasPorEspecie = new Dictionary<string, int>();
            MascotasPerdidasPorEstado = new Dictionary<string, int>();
            CampaniasPorEstado = new Dictionary<string, int>();
            CampaniasPorTipo = new Dictionary<string, int>();
            RefugiosPorUbicacion = new Dictionary<string, int>();
            MascotasPorRefugio = new Dictionary<string, int>();
            AuditoriaPorUsuario = new Dictionary<string, int>();
            AuditoriaPorAccion = new Dictionary<string, int>();
            MascotasPorEstado = new Dictionary<string, int>();
            SeguimientosPorTipo = new Dictionary<string, int>();
            ContratosPorEstado = new Dictionary<string, int>();
            CambiosEstadoPorUsuario = new Dictionary<string, int>();
        }
    }

    // ViewModels específicos para Dashboard (para evitar ambigüedades)
    public class DashboardAuditoriaViewModel
    {
        public string Accion { get; set; }
        public string Usuario { get; set; }
        public string Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public string Detalles { get; set; }
    }

    public class DashboardConfiguracionResumenViewModel
    {
        public int Id { get; set; }
        public string Clave { get; set; }
        public string Valor { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public bool EsCritica { get; set; }
        public DateTime UltimaModificacion { get; set; }
        public string ModificadoPor { get; set; }
    }

    // ViewModel específico para resumen de gestión de mascotas
    public class DashboardResumenGestionMascotasViewModel
    {
        public int TotalMascotas { get; set; }
        public int MascotasDisponibles { get; set; }
        public int MascotasAdoptadas { get; set; }
        public int MascotasEnTratamiento { get; set; }
        public int MascotasArchivadas { get; set; }
        public int SeguimientosPendientes { get; set; }
        public int SeguimientosActivos { get; set; }
        public int ContratosActivos { get; set; }
        public int CambiosEstadoMes { get; set; }
        public decimal TasaAdopcion { get; set; }
        public decimal TasaSeguimientoCompletado { get; set; }
        public string EstadoGestion { get; set; }
        public List<string> Alertas { get; set; }
    }
}