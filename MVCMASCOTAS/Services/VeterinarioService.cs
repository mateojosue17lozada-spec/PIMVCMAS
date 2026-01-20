using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio veterinaria
    /// </summary>
    public class VeterinarioService
    {
        private RefugioMascotasEntities db;

        public VeterinarioService()
        {
            db = new RefugioMascotasEntities();
        }

        public VeterinarioService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Registra una consulta veterinaria
        /// </summary>
        public HistorialMedico RegistrarConsulta(int mascotaId, int veterinarioId, string diagnostico,
            string tratamiento, decimal? peso, decimal? temperatura, string observaciones)
        {
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = veterinarioId,
                TipoRegistro = "Consulta",
                Diagnostico = diagnostico,
                TratamientoRecetado = tratamiento,
                Peso = peso,
                Temperatura = temperatura,
                Observaciones = observaciones,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            return historial;
        }

        /// <summary>
        /// Registra la aplicación de una vacuna
        /// </summary>
        public MascotaVacunas RegistrarVacuna(int mascotaId, int vacunaId, int veterinarioId,
            DateTime? proximaDosis = null)
        {
            var mascotaVacuna = new MascotaVacunas
            {
                MascotaId = mascotaId,
                VacunaId = vacunaId,
                FechaAplicacion = DateTime.Now,
                VeterinarioId = veterinarioId,
                FechaProximaDosis = proximaDosis
            };

            db.MascotaVacunas.Add(mascotaVacuna);

            // Registrar en historial
            var vacuna = db.Vacunas.Find(vacunaId);
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = veterinarioId,
                TipoRegistro = "Vacunación",
                Diagnostico = $"Vacuna aplicada: {vacuna.NombreVacuna}",
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            return mascotaVacuna;
        }

        /// <summary>
        /// Inicia un tratamiento para una mascota
        /// </summary>
        public Tratamientos IniciarTratamiento(int mascotaId, int veterinarioId, string tipo,
            string descripcion, string medicamentos, int duracionDias, decimal? costo)
        {
            var tratamiento = new Tratamientos
            {
                MascotaId = mascotaId,
                TipoTratamiento = tipo,
                Descripcion = descripcion,
                Medicamentos = medicamentos,
                FechaInicio = DateTime.Now,
                DuracionEstimadaDias = duracionDias,
                CostoEstimado = costo,
                VeterinarioResponsableId = veterinarioId,
                Estado = "En curso"
            };

            db.Tratamientos.Add(tratamiento);

            // Actualizar estado de mascota
            var mascota = db.Mascotas.Find(mascotaId);
            mascota.Estado = "En tratamiento";

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = veterinarioId,
                TipoRegistro = "Inicio Tratamiento",
                Diagnostico = $"{tipo}: {descripcion}",
                TratamientoRecetado = medicamentos,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            return tratamiento;
        }

        /// <summary>
        /// Finaliza un tratamiento
        /// </summary>
        public bool FinalizarTratamiento(int tratamientoId, int veterinarioId, string resultados)
        {
            var tratamiento = db.Tratamientos.Find(tratamientoId);

            if (tratamiento == null || tratamiento.Estado != "En curso")
            {
                return false;
            }

            tratamiento.FechaFin = DateTime.Now;
            tratamiento.Estado = "Completado";
            tratamiento.Resultados = resultados;

            // Verificar otros tratamientos activos
            var otrosTratamientos = db.Tratamientos
                .Any(t => t.MascotaId == tratamiento.MascotaId &&
                         t.Estado == "En curso" &&
                         t.TratamientoId != tratamientoId);

            if (!otrosTratamientos)
            {
                var mascota = db.Mascotas.Find(tratamiento.MascotaId);
                mascota.Estado = "Disponible para adopción";
            }

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = tratamiento.MascotaId,
                VeterinarioId = veterinarioId,
                TipoRegistro = "Fin Tratamiento",
                Diagnostico = $"Tratamiento completado: {tratamiento.TipoTratamiento}",
                Observaciones = resultados,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Obtiene el historial médico de una mascota
        /// </summary>
        public List<HistorialMedico> ObtenerHistorialMascota(int mascotaId)
        {
            return db.HistorialMedico
                .Where(h => h.MascotaId == mascotaId)
                .OrderByDescending(h => h.FechaRegistro)
                .ToList();
        }

        /// <summary>
        /// Obtiene las vacunas de una mascota
        /// </summary>
        public List<MascotaVacunas> ObtenerVacunasMascota(int mascotaId)
        {
            return db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId)
                .OrderByDescending(v => v.FechaAplicacion)
                .ToList();
        }

        /// <summary>
        /// Obtiene tratamientos activos de una mascota
        /// </summary>
        public List<Tratamientos> ObtenerTratamientosActivos(int mascotaId)
        {
            return db.Tratamientos
                .Where(t => t.MascotaId == mascotaId && t.Estado == "En curso")
                .OrderBy(t => t.FechaInicio)
                .ToList();
        }

        /// <summary>
        /// Obtiene todas las vacunas disponibles
        /// </summary>
        public List<Vacunas> ObtenerTodasLasVacunas()
        {
            return db.Vacunas.OrderBy(v => v.NombreVacuna).ToList();
        }

        /// <summary>
        /// Verifica si una mascota tiene vacunas pendientes
        /// </summary>
        public bool TieneVacunasPendientes(int mascotaId)
        {
            var vacunasAplicadas = db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId)
                .Select(v => v.VacunaId)
                .ToList();

            var todasVacunas = db.Vacunas.Select(v => v.VacunaId).ToList();

            return todasVacunas.Except(vacunasAplicadas).Any();
        }

        /// <summary>
        /// Cambia el estado de una mascota
        /// </summary>
        public bool CambiarEstadoMascota(int mascotaId, int veterinarioId, string nuevoEstado,
            string observaciones)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return false;
            }

            string estadoAnterior = mascota.Estado;
            mascota.Estado = nuevoEstado;

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = veterinarioId,
                TipoRegistro = "Cambio Estado",
                Diagnostico = $"Estado cambiado de '{estadoAnterior}' a '{nuevoEstado}'",
                Observaciones = observaciones,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Obtiene estadísticas veterinarias
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            var stats = new Dictionary<string, object>
            {
                ["MascotasEnTratamiento"] = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo),
                ["TratamientosActivos"] = db.Tratamientos.Count(t => t.Estado == "En curso"),
                ["ConsultasEsteMes"] = db.HistorialMedico.Count(h =>
                    h.FechaRegistro.Month == DateTime.Now.Month &&
                    h.FechaRegistro.Year == DateTime.Now.Year),
                ["VacunasAplicadasEsteMes"] = db.MascotaVacunas.Count(v =>
                    v.FechaAplicacion.Month == DateTime.Now.Month &&
                    v.FechaAplicacion.Year == DateTime.Now.Year)
            };

            return stats;
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
