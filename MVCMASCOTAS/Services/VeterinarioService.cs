using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class VeterinarioService
    {
        private readonly RefugioMascotasEntities db;

        public VeterinarioService()
        {
            db = new RefugioMascotasEntities();
        }

        // Registrar consulta médica
        public HistorialMedico RegistrarConsulta(int mascotaId, string tipoConsulta, string diagnostico,
            string tratamiento, decimal costo, int veterinarioId, string observaciones = null)
        {
            var consulta = new HistorialMedico
            {
                MascotaId = mascotaId,
                FechaConsulta = DateTime.Now,
                TipoConsulta = tipoConsulta,
                Diagnostico = diagnostico,
                Tratamiento = tratamiento,
                Costo = costo,
                VeterinarioId = veterinarioId,
                Observaciones = observaciones
            };

            db.HistorialMedico.Add(consulta);
            db.SaveChanges();

            return consulta;
        }

        // Registrar vacuna
        public MascotaVacunas RegistrarVacuna(int mascotaId, int vacunaId, DateTime fechaAplicacion,
            DateTime? proximaDosis, int veterinarioId, string lote = null)
        {
            var mascotaVacuna = new MascotaVacunas
            {
                MascotaId = mascotaId,
                VacunaId = vacunaId,
                FechaAplicacion = fechaAplicacion,
                ProximaDosis = proximaDosis,
                VeterinarioId = veterinarioId,
                Lote = lote
            };

            db.MascotaVacunas.Add(mascotaVacuna);
            db.SaveChanges();

            return mascotaVacuna;
        }

        // Registrar tratamiento
        public Tratamientos RegistrarTratamiento(int mascotaId, string nombreTratamiento, string descripcion,
            DateTime fechaInicio, DateTime? fechaFin, decimal costo, int veterinarioId, string estado = "Activo")
        {
            var tratamiento = new Tratamientos
            {
                MascotaId = mascotaId,
                NombreTratamiento = nombreTratamiento,
                Descripcion = descripcion,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Costo = costo,
                Estado = estado,
                VeterinarioId = veterinarioId
            };

            db.Tratamientos.Add(tratamiento);
            db.SaveChanges();

            return tratamiento;
        }

        // Obtener historial médico de una mascota
        public List<HistorialMedico> ObtenerHistorialMedico(int mascotaId)
        {
            return db.HistorialMedico
                .Where(h => h.MascotaId == mascotaId)
                .OrderByDescending(h => h.FechaConsulta)
                .ToList();
        }

        // Obtener vacunas de una mascota
        public List<MascotaVacunas> ObtenerVacunasMascota(int mascotaId)
        {
            return db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId)
                .OrderByDescending(v => v.FechaAplicacion)
                .ToList();
        }

        // Obtener tratamientos de una mascota
        public List<Tratamientos> ObtenerTratamientosMascota(int mascotaId)
        {
            return db.Tratamientos
                .Where(t => t.MascotaId == mascotaId)
                .OrderByDescending(t => t.FechaInicio)
                .ToList();
        }

        // Obtener tratamientos activos
        public List<Tratamientos> ObtenerTratamientosActivos()
        {
            return db.Tratamientos
                .Where(t => t.Estado == "Activo")
                .OrderBy(t => t.FechaInicio)
                .ToList();
        }

        // Obtener vacunas pendientes (próximas dosis)
        public List<MascotaVacunas> ObtenerVacunasPendientes()
        {
            var hoy = DateTime.Now.Date;
            var proximoMes = hoy.AddMonths(1);

            return db.MascotaVacunas
                .Where(v => v.ProximaDosis.HasValue &&
                           v.ProximaDosis.Value >= hoy &&
                           v.ProximaDosis.Value <= proximoMes)
                .OrderBy(v => v.ProximaDosis)
                .ToList();
        }

        // Obtener consultas pendientes
        public int ObtenerConsultasPendientes()
        {
            return db.HistorialMedico
                .Count(h => h.TipoConsulta == "Pendiente");
        }

        // Finalizar tratamiento
        public void FinalizarTratamiento(int tratamientoId, DateTime fechaFin)
        {
            var tratamiento = db.Tratamientos.Find(tratamientoId);
            if (tratamiento != null)
            {
                tratamiento.Estado = "Finalizado";
                tratamiento.FechaFin = fechaFin;
                db.SaveChanges();
            }
        }

        // Obtener mascotas asignadas a un veterinario
        public List<Mascotas> ObtenerMascotasAsignadas(int veterinarioId)
        {
            return db.Mascotas
                .Where(m => m.VeterinarioAsignado == veterinarioId && m.Activo == true)
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        // Obtener todas las vacunas disponibles
        public List<Vacunas> ObtenerVacunasDisponibles()
        {
            return db.Vacunas
                .Where(v => v.Activo == true)
                .OrderBy(v => v.NombreVacuna)
                .ToList();
        }

        // Verificar si una mascota tiene vacunas al día
        public bool TieneVacunasAlDia(int mascotaId)
        {
            var vacunasPendientes = db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId &&
                           v.ProximaDosis.HasValue &&
                           v.ProximaDosis.Value < DateTime.Now)
                .Count();

            return vacunasPendientes == 0;
        }

        // Obtener estadísticas veterinarias
        public int ObtenerTotalConsultasMes(int mes, int anio)
        {
            return db.HistorialMedico
                .Count(h => h.FechaConsulta.HasValue &&
                           h.FechaConsulta.Value.Month == mes &&
                           h.FechaConsulta.Value.Year == anio);
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}