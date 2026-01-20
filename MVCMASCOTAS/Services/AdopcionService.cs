using System;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class AdopcionService
    {
        private readonly RefugioMascotasEntities db;
        private readonly EvaluacionService evaluacionService;

        public AdopcionService()
        {
            db = new RefugioMascotasEntities();
            evaluacionService = new EvaluacionService();
        }

        // Crear solicitud de adopción
        public SolicitudAdopcion CrearSolicitud(int mascotaId, int usuarioId)
        {
            // Verificar si ya existe una solicitud pendiente
            var solicitudExistente = db.SolicitudAdopcion
                .FirstOrDefault(s => s.MascotaId == mascotaId &&
                                    s.UsuarioId == usuarioId &&
                                    (s.Estado == "Pendiente" || s.Estado == "En Revisión"));

            if (solicitudExistente != null)
            {
                return solicitudExistente;
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

        // Registrar formulario de adopción
        public void RegistrarFormulario(int solicitudId, FormularioAdopcionDetalle formulario)
        {
            formulario.SolicitudId = solicitudId;
            db.FormularioAdopcionDetalle.Add(formulario);
            db.SaveChanges();

            // Cambiar estado a "En Revisión"
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.Estado = "En Revisión";
                db.SaveChanges();
            }
        }

        // Evaluar solicitud
        public void EvaluarSolicitud(int solicitudId, int evaluadorId)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud == null) return;

            var formulario = db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);

            if (formulario == null) return;

            // Realizar evaluación automática
            var resultado = evaluacionService.EvaluarSolicitudAdopcion(formulario);

            // Actualizar solicitud
            solicitud.PuntajeEvaluacion = resultado.PuntajeTotal;
            solicitud.ResultadoEvaluacion = resultado.Resultado;
            solicitud.FechaEvaluacion = DateTime.Now;
            solicitud.EvaluadoPor = evaluadorId;
            solicitud.Estado = resultado.Aprobado ? "Aprobada" : "Rechazada";

            if (!resultado.Aprobado)
            {
                solicitud.MotivoRechazo = resultado.Recomendacion;
            }

            db.SaveChanges();

            // Registrar evaluación detallada
            var evaluacion = new EvaluacionAdopcion
            {
                SolicitudId = solicitudId,
                EvaluadorId = evaluadorId,
                FechaEvaluacion = DateTime.Now,
                Puntaje = resultado.PuntajeTotal,
                Resultado = resultado.Resultado,
                Observaciones = resultado.Recomendacion
            };

            db.EvaluacionAdopcion.Add(evaluacion);
            db.SaveChanges();
        }

        // Aprobar solicitud manualmente
        public void AprobarSolicitud(int solicitudId, int evaluadorId, string observaciones = null)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.Estado = "Aprobada";
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = evaluadorId;
                solicitud.Observaciones = observaciones;
                db.SaveChanges();

                // Actualizar estado de la mascota
                var mascota = db.Mascotas.Find(solicitud.MascotaId);
                if (mascota != null)
                {
                    mascota.Estado = "Adoptado";
                    mascota.FechaAdopcion = DateTime.Now;
                    db.SaveChanges();
                }
            }
        }

        // Rechazar solicitud
        public void RechazarSolicitud(int solicitudId, int evaluadorId, string motivoRechazo)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.Estado = "Rechazada";
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = evaluadorId;
                solicitud.MotivoRechazo = motivoRechazo;
                db.SaveChanges();
            }
        }

        // Obtener solicitudes por estado
        public List<SolicitudAdopcion> ObtenerSolicitudesPorEstado(string estado)
        {
            return db.SolicitudAdopcion
                .Where(s => s.Estado == estado)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();
        }

        // Obtener solicitudes de un usuario
        public List<SolicitudAdopcion> ObtenerSolicitudesUsuario(int usuarioId)
        {
            return db.SolicitudAdopcion
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();
        }

        // Obtener solicitud por ID
        public SolicitudAdopcion ObtenerSolicitudPorId(int solicitudId)
        {
            return db.SolicitudAdopcion.Find(solicitudId);
        }

        // Obtener formulario de una solicitud
        public FormularioAdopcionDetalle ObtenerFormularioSolicitud(int solicitudId)
        {
            return db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);
        }

        // Verificar si usuario tiene solicitud para mascota
        public bool TieneSolicitudPendiente(int usuarioId, int mascotaId)
        {
            return db.SolicitudAdopcion
                .Any(s => s.UsuarioId == usuarioId &&
                         s.MascotaId == mascotaId &&
                         (s.Estado == "Pendiente" || s.Estado == "En Revisión"));
        }

        // Obtener estadísticas de adopciones
        public int ObtenerAdopcionesDelMes(int mes, int anio)
        {
            return db.SolicitudAdopcion
                .Count(s => s.Estado == "Aprobada" &&
                           s.FechaRespuesta.HasValue &&
                           s.FechaRespuesta.Value.Month == mes &&
                           s.FechaRespuesta.Value.Year == anio);
        }

        public int ObtenerSolicitudesPendientes()
        {
            return db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente" || s.Estado == "En Revisión");
        }

        // Registrar seguimiento post-adopción
        public void RegistrarSeguimiento(int solicitudId, int responsableId, string descripcion, string resultado)
        {
            var seguimiento = new SeguimientoAdopcion
            {
                SolicitudId = solicitudId,
                FechaSeguimiento = DateTime.Now,
                ResponsableId = responsableId,
                Descripcion = descripcion,
                Resultado = resultado
            };

            db.SeguimientoAdopcion.Add(seguimiento);
            db.SaveChanges();
        }

        // Obtener seguimientos de una adopción
        public List<SeguimientoAdopcion> ObtenerSeguimientos(int solicitudId)
        {
            return db.SeguimientoAdopcion
                .Where(s => s.SolicitudId == solicitudId)
                .OrderByDescending(s => s.FechaSeguimiento)
                .ToList();
        }

        public void Dispose()
        {
            db?.Dispose();
            evaluacionService?.Dispose();
        }
    }
}