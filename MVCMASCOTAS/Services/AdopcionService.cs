using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio de adopciones
    /// </summary>
    public class AdopcionService
    {
        private RefugioMascotasEntities db;

        public AdopcionService()
        {
            db = new RefugioMascotasEntities();
        }

        public AdopcionService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Crea una nueva solicitud de adopción
        /// </summary>
        public SolicitudAdopcion CrearSolicitud(int mascotaId, int usuarioId)
        {
            // Verificar si ya existe solicitud activa
            var solicitudExistente = db.SolicitudAdopcion
                .Any(s => s.MascotaId == mascotaId && s.UsuarioId == usuarioId &&
                         (s.Estado == "Pendiente" || s.Estado == "En evaluación" || s.Estado == "Aprobada"));

            if (solicitudExistente)
            {
                return null;
            }

            var solicitud = new SolicitudAdopcion
            {
                MascotaId = mascotaId,
                UsuarioId = usuarioId,
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente"
            };

            db.SolicitudAdopcion.Add(solicitud);
            db.SaveChanges();

            return solicitud;
        }

        /// <summary>
        /// Evalúa automáticamente una solicitud basándose en el formulario
        /// </summary>
        public ResultadoEvaluacionModel EvaluarSolicitud(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Vivienda (20 puntos)
            if (formulario.ViviendaPropia == true) puntaje += 10;
            if (formulario.TieneJardin == true) puntaje += 5;
            if (formulario.PermisoMascotas == true) puntaje += 5;

            // Experiencia (20 puntos)
            if (formulario.ExperienciaPreviaConMascotas == true) puntaje += 15;
            if (formulario.TieneMascotasActualmente == true && formulario.MascotasEsterilizadas == true)
                puntaje += 5;

            // Disponibilidad (20 puntos)
            if (formulario.TiempoDisponibleDiario == "4+ horas") puntaje += 20;
            else if (formulario.TiempoDisponibleDiario == "2-4 horas") puntaje += 15;
            else if (formulario.TiempoDisponibleDiario == "1-2 horas") puntaje += 10;

            // Compromisos legales (20 puntos)
            if (formulario.AceptaEsterilizacion == true) puntaje += 5;
            if (formulario.AceptaVisitasSeguimiento == true) puntaje += 5;
            if (formulario.AceptaCondicionesLOBA == true) puntaje += 5;
            if (formulario.AceptaDevolucionSiNoPuedeAtender == true) puntaje += 5;

            // Compromiso (20 puntos)
            if (!string.IsNullOrEmpty(formulario.MotivoAdopcion) && formulario.MotivoAdopcion.Length > 50)
                puntaje += 10;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiCambiaResidencia)) puntaje += 5;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiProblemasComportamiento)) puntaje += 5;

            string resultado;
            if (puntaje >= 80)
                resultado = "Apto";
            else if (puntaje >= 60)
                resultado = "Revisión Manual";
            else
                resultado = "No Apto";

            return new ResultadoEvaluacionModel
            {
                Puntaje = puntaje,
                Resultado = resultado,
                Recomendacion = ObtenerRecomendacion(puntaje, formulario)
            };
        }

        /// <summary>
        /// Obtiene recomendaciones basadas en la evaluación
        /// </summary>
        private string ObtenerRecomendacion(int puntaje, FormularioAdopcionDetalle formulario)
        {
            if (puntaje >= 80)
            {
                return "El solicitante cumple con todos los requisitos necesarios para adoptar.";
            }
            else if (puntaje >= 60)
            {
                var recomendaciones = new List<string>();

                if (formulario.ViviendaPropia != true)
                    recomendaciones.Add("Verificar estabilidad de vivienda");

                if (formulario.ExperienciaPreviaConMascotas != true)
                    recomendaciones.Add("Requiere asesoramiento sobre cuidado de mascotas");

                if (formulario.TiempoDisponibleDiario != "4+ horas" && formulario.TiempoDisponibleDiario != "2-4 horas")
                    recomendaciones.Add("Tiempo disponible limitado");

                return "Requiere evaluación manual. " + string.Join(". ", recomendaciones) + ".";
            }
            else
            {
                return "El solicitante no cumple con los requisitos mínimos en este momento.";
            }
        }

        /// <summary>
        /// Aprueba una solicitud de adopción
        /// </summary>
        public bool AprobarSolicitud(int solicitudId, string observaciones)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null || solicitud.Estado == "Aprobada")
            {
                return false;
            }

            solicitud.Estado = "Aprobada";
            solicitud.ObservacionesEvaluador = observaciones;
            solicitud.FechaAprobacion = DateTime.Now;

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Rechaza una solicitud de adopción
        /// </summary>
        public bool RechazarSolicitud(int solicitudId, string motivo)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null)
            {
                return false;
            }

            solicitud.Estado = "Rechazada";
            solicitud.ObservacionesEvaluador = motivo;
            solicitud.FechaRespuesta = DateTime.Now;

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Crea un contrato de adopción
        /// </summary>
        public ContratoAdopcion CrearContrato(int solicitudId)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null || solicitud.Estado != "Aprobada")
            {
                return null;
            }

            var contrato = new ContratoAdopcion
            {
                SolicitudId = solicitudId,
                MascotaId = solicitud.MascotaId,
                AdoptanteId = solicitud.UsuarioId,
                FechaAdopcion = DateTime.Now,
                Estado = "Activo",
                CondicionesEspeciales = "Cumplir con todas las cláusulas estándar del refugio"
            };

            db.ContratoAdopcion.Add(contrato);

            // Actualizar estado de mascota
            var mascota = db.Mascotas.Find(solicitud.MascotaId);
            mascota.Estado = "Adoptada";
            mascota.FechaAdopcion = DateTime.Now;

            db.SaveChanges();

            return contrato;
        }

        /// <summary>
        /// Obtiene solicitudes por estado
        /// </summary>
        public List<SolicitudAdopcion> ObtenerSolicitudesPorEstado(string estado = null)
        {
            var query = db.SolicitudAdopcion.AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(s => s.Estado == estado);
            }

            return query.OrderByDescending(s => s.FechaSolicitud).ToList();
        }

        /// <summary>
        /// Obtiene solicitudes de un usuario
        /// </summary>
        public List<SolicitudAdopcion> ObtenerSolicitudesUsuario(int usuarioId)
        {
            return db.SolicitudAdopcion
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();
        }

        /// <summary>
        /// Registra un seguimiento post-adopción
        /// </summary>
        public SeguimientoAdopcion RegistrarSeguimiento(int contratoId, string observaciones,
            string estadoMascota, string estadoAdoptante)
        {
            var seguimiento = new SeguimientoAdopcion
            {
                ContratoId = contratoId,
                FechaSeguimiento = DateTime.Now,
                Observaciones = observaciones,
                EstadoMascota = estadoMascota,
                EstadoAdoptante = estadoAdoptante
            };

            db.SeguimientoAdopcion.Add(seguimiento);
            db.SaveChanges();

            return seguimiento;
        }

        /// <summary>
        /// Obtiene estadísticas de adopción
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalAdopciones"] = db.ContratoAdopcion.Count(c => c.Estado == "Activo"),
                ["SolicitudesPendientes"] = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente"),
                ["SolicitudesEnEvaluacion"] = db.SolicitudAdopcion.Count(s => s.Estado == "En evaluación"),
                ["AdopcionesEsteMes"] = db.ContratoAdopcion.Count(c =>
                    c.FechaAdopcion.Month == DateTime.Now.Month &&
                    c.FechaAdopcion.Year == DateTime.Now.Year),
                ["TasaAprobacion"] = CalcularTasaAprobacion()
            };

            return stats;
        }

        /// <summary>
        /// Calcula la tasa de aprobación de solicitudes
        /// </summary>
        private decimal CalcularTasaAprobacion()
        {
            var totalSolicitudes = db.SolicitudAdopcion
                .Count(s => s.Estado == "Aprobada" || s.Estado == "Rechazada");

            if (totalSolicitudes == 0)
                return 0;

            var aprobadas = db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada");

            return Math.Round((decimal)aprobadas / totalSolicitudes * 100, 2);
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
