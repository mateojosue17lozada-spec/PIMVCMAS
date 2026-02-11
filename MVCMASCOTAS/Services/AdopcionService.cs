using MVCMASCOTAS.Controllers;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCMASCOTAS.Services
{
    public class AdopcionService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;
        private readonly EvaluacionService evaluacionService;

        public AdopcionService()
        {
            db = new RefugioMascotasDBEntities();
            evaluacionService = new EvaluacionService();
        }

        // Crear solicitud de adopción
        public SolicitudAdopcion CrearSolicitud(int mascotaId, int usuarioId)
        {
            var solicitudExistente = db.SolicitudAdopcion
                .FirstOrDefault(s => s.MascotaId == mascotaId &&
                                    s.UsuarioId == usuarioId &&
                                    (s.Estado == "Pendiente" || s.Estado == "En evaluación"));

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

            // ✅ Usar tu método específico de auditoría
            var mascota = db.Mascotas.Find(mascotaId);
            AuditoriaHelper.RegistrarSolicitudAdopcion(
                solicitudId: solicitud.SolicitudId,
                usuarioId: usuarioId,
                mascotaId: mascotaId,
                nombreMascota: mascota?.Nombre ?? "Desconocida"
            );

            return solicitud;
        }

        // Registrar formulario de adopción
        public void RegistrarFormulario(int solicitudId, FormularioAdopcionDetalle formulario)
        {
            formulario.SolicitudId = solicitudId;
            formulario.FechaLlenado = DateTime.Now;
            db.FormularioAdopcionDetalle.Add(formulario);
            db.SaveChanges();

            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.Estado = "En evaluación";
                db.SaveChanges();
            }
        }

        // Evaluar solicitud automáticamente
        public void EvaluarSolicitudAutomatica(int solicitudId, int evaluadorId)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud == null) return;

            var formulario = db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);

            if (formulario == null) return;

            var resultado = evaluacionService.EvaluarSolicitud(formulario);

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

            var evaluacion = new EvaluacionAdopcion
            {
                SolicitudId = solicitudId,
                EvaluadorId = evaluadorId,
                FechaEvaluacion = DateTime.Now,
                PuntajeVivienda = resultado.PuntajeVivienda,
                PuntajeExperiencia = resultado.PuntajeExperiencia,
                PuntajeDisponibilidad = resultado.PuntajeDisponibilidad,
                PuntajeReferencias = resultado.PuntajeReferencias,
                PuntajeCompromiso = resultado.PuntajeCompromiso,
                PuntajeTotal = resultado.PuntajeTotal,
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
                solicitud.ResultadoEvaluacion = "Aprobada";
                solicitud.FechaEvaluacion = DateTime.Now;
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = evaluadorId;
                solicitud.Observaciones = observaciones;
                db.SaveChanges();

                // ✅ NO cambiar estado a "Adoptada" aquí - se hace al firmar contrato
                // var mascota = db.Mascotas.Find(solicitud.MascotaId);
                // if (mascota != null)
                // {
                //     mascota.Estado = "Adoptada";  // ❌ ELIMINAR
                //     mascota.FechaAdopcion = DateTime.Now;
                //     db.SaveChanges();
                // }

                var evaluacion = new EvaluacionAdopcion
                {
                    SolicitudId = solicitudId,
                    EvaluadorId = evaluadorId,
                    FechaEvaluacion = DateTime.Now,
                    Resultado = "Aprobada",
                    Observaciones = observaciones
                };
                db.EvaluacionAdopcion.Add(evaluacion);
                db.SaveChanges();

                // ✅ Auditoría
                var usuario = db.Usuarios.Find(solicitud.UsuarioId);
                var mascota = db.Mascotas.Find(solicitud.MascotaId);
                AuditoriaHelper.RegistrarAprobacionAdopcion(
                    solicitudId: solicitudId,
                    aprobadorId: evaluadorId,
                    nombreAdoptante: usuario?.NombreCompleto ?? "Desconocido",
                    nombreMascota: mascota?.Nombre ?? "Desconocida"
                );
            }
        }

        // Rechazar solicitud
        public void RechazarSolicitud(int solicitudId, int evaluadorId, string motivoRechazo)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.Estado = "Rechazada";
                solicitud.ResultadoEvaluacion = "Rechazada";
                solicitud.FechaEvaluacion = DateTime.Now;
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = evaluadorId;
                solicitud.MotivoRechazo = motivoRechazo;
                db.SaveChanges();

                var evaluacion = new EvaluacionAdopcion
                {
                    SolicitudId = solicitudId,
                    EvaluadorId = evaluadorId,
                    FechaEvaluacion = DateTime.Now,
                    Resultado = "Rechazada",
                    Observaciones = motivoRechazo
                };
                db.EvaluacionAdopcion.Add(evaluacion);
                db.SaveChanges();

                // ✅ Auditoría
                AuditoriaHelper.RegistrarRechazoAdopcion(
                    solicitudId: solicitudId,
                    rechazadorId: evaluadorId,
                    motivo: motivoRechazo
                );
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

        public List<SolicitudAdopcion> ObtenerSolicitudesUsuario(int usuarioId)
        {
            return db.SolicitudAdopcion
                .Include("Mascotas")
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();
        }

        public SolicitudAdopcion ObtenerSolicitudPorId(int solicitudId)
        {
            return db.SolicitudAdopcion
                .Include("Mascotas")
                .Include("Usuarios")
                .FirstOrDefault(s => s.SolicitudId == solicitudId);
        }

        public FormularioAdopcionDetalle ObtenerFormularioSolicitud(int solicitudId)
        {
            return db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);
        }

        public EvaluacionAdopcion ObtenerEvaluacionSolicitud(int solicitudId)
        {
            return db.EvaluacionAdopcion
                .Include("Usuarios")
                .FirstOrDefault(e => e.SolicitudId == solicitudId);
        }

        public bool TieneSolicitudPendiente(int usuarioId, int mascotaId)
        {
            return db.SolicitudAdopcion
                .Any(s => s.UsuarioId == usuarioId &&
                         s.MascotaId == mascotaId &&
                         (s.Estado == "Pendiente" || s.Estado == "En evaluación"));
        }

        public int ObtenerAdopcionesDelMes(int mes, int anio)
        {
            return db.Mascotas
                .Count(m => m.Estado == "Adoptada" &&
                           m.FechaAdopcion.HasValue &&
                           m.FechaAdopcion.Value.Month == mes &&
                           m.FechaAdopcion.Value.Year == anio);
        }

        public int ObtenerSolicitudesPendientes()
        {
            return db.SolicitudAdopcion
                .Count(s => s.Estado == "Pendiente" || s.Estado == "En evaluación");
        }

        // Registrar seguimiento post-adopción
        public void RegistrarSeguimiento(int contratoId, int responsableId, string estadoMascota,
            string condicionesVivienda, string relacionAdoptante, string observaciones)
        {
            var seguimiento = new SeguimientoAdopcion
            {
                ContratoId = contratoId,
                ResponsableSeguimiento = responsableId,
                FechaSeguimiento = DateTime.Now,
                TipoSeguimiento = "Rutinario",
                EstadoMascota = estadoMascota,
                CondicionesVivienda = condicionesVivienda,
                RelacionConAdoptante = relacionAdoptante,
                Observaciones = observaciones,
                ProximoSeguimiento = DateTime.Now.AddMonths(3)
            };

            db.SeguimientoAdopcion.Add(seguimiento);
            db.SaveChanges();
        }

        public List<SeguimientoAdopcion> ObtenerSeguimientos(int contratoId)
        {
            return db.SeguimientoAdopcion
                .Where(s => s.ContratoId == contratoId)
                .OrderByDescending(s => s.FechaSeguimiento)
                .ToList();
        }

        public ContratoAdopcion ObtenerContratoPorSolicitud(int solicitudId)
        {
            return db.ContratoAdopcion
                .FirstOrDefault(c => c.SolicitudId == solicitudId);
        }

        // ✅ NUEVO: Crear contrato Y cambiar estado a "Adoptada"
        public ContratoAdopcion CrearContrato(int solicitudId, string numeroContrato,
            string terminosCondiciones, string representanteRefugioNombre,
            string representanteRefugioCedula, int usuarioCreador)
        {
            var solicitud = db.SolicitudAdopcion
                .Include("Usuarios")
                .Include("Mascotas")
                .FirstOrDefault(s => s.SolicitudId == solicitudId);

            if (solicitud == null) return null;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var contrato = new ContratoAdopcion
                    {
                        SolicitudId = solicitudId,
                        NumeroContrato = numeroContrato,
                        FechaContrato = DateTime.Now,
                        AdoptanteNombre = solicitud.Usuarios.NombreCompleto,
                        AdoptanteCedula = solicitud.Usuarios.Cedula,
                        AdoptanteDireccion = solicitud.Usuarios.Direccion ?? "",
                        AdoptanteTelefono = solicitud.Usuarios.Telefono ?? "",
                        RepresentanteRefugioNombre = representanteRefugioNombre,
                        RepresentanteRefugioCedula = representanteRefugioCedula,
                        MascotaNombre = solicitud.Mascotas.Nombre,
                        MascotaEspecie = solicitud.Mascotas.Especie,
                        MascotaMicrochip = solicitud.Mascotas.Microchip,
                        TerminosCondiciones = terminosCondiciones,
                        Estado = "Activo"
                    };

                    db.ContratoAdopcion.Add(contrato);
                    db.SaveChanges();

                    // ✅ AHORA SÍ cambiar mascota a "Adoptada"
                    var mascota = db.Mascotas.Find(solicitud.MascotaId);
                    if (mascota != null)
                    {
                        mascota.Estado = "Adoptada";
                        mascota.FechaAdopcion = DateTime.Now;
                        db.SaveChanges();
                    }

                    // ✅ Cambiar solicitud a "Completada"
                    solicitud.Estado = "Completada";
                    db.SaveChanges();

                    // ✅ Auditoría
                    AuditoriaHelper.RegistrarAccion(
                        accion: "Creación de Contrato",
                        controlador: "Adopcion",
                        detalles: $"Contrato {numeroContrato} creado para {solicitud.Usuarios.NombreCompleto} - Mascota: {solicitud.Mascotas.Nombre}",
                        usuarioId: usuarioCreador
                    );

                    transaction.Commit();
                    return contrato;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public string GenerarNumeroContrato()
        {
            string prefijo = "CONTRATO-";
            string fecha = DateTime.Now.ToString("yyyyMMdd");
            int secuencia = 1;

            var ultimoContrato = db.ContratoAdopcion
                .Where(c => c.NumeroContrato.StartsWith($"{prefijo}{fecha}"))
                .OrderByDescending(c => c.NumeroContrato)
                .FirstOrDefault();

            if (ultimoContrato != null && ultimoContrato.NumeroContrato.Length >= prefijo.Length + fecha.Length + 4)
            {
                string secuenciaStr = ultimoContrato.NumeroContrato.Substring(ultimoContrato.NumeroContrato.Length - 4);
                int.TryParse(secuenciaStr, out secuencia);
                secuencia++;
            }

            return $"{prefijo}{fecha}-{secuencia:D4}";
        }

        public EstadisticasAdopcion ObtenerEstadisticasAdopcion()
        {
            var hoy = DateTime.Now;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            return new EstadisticasAdopcion
            {
                TotalAdopciones = db.Mascotas.Count(m => m.Estado == "Adoptada"),
                AdopcionesEsteMes = db.Mascotas.Count(m => m.Estado == "Adoptada" &&
                                                          m.FechaAdopcion.HasValue &&
                                                          m.FechaAdopcion >= inicioMes &&
                                                          m.FechaAdopcion <= finMes),
                SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente"),
                SolicitudesEnEvaluacion = db.SolicitudAdopcion.Count(s => s.Estado == "En evaluación"),
                TasaAprobacion = db.SolicitudAdopcion.Any() ?
                    (decimal)db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada") /
                    db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada" || s.Estado == "Rechazada") * 100 : 0
            };
        }

        public void CompletarSeguimiento(int seguimientoId, bool requiereIntervencion,
            string recomendaciones, DateTime? proximoSeguimiento)
        {
            var seguimiento = db.SeguimientoAdopcion.Find(seguimientoId);
            if (seguimiento != null)
            {
                seguimiento.RequiereIntervencion = requiereIntervencion;
                seguimiento.Recomendaciones = recomendaciones;
                seguimiento.ProximoSeguimiento = proximoSeguimiento;
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            db?.Dispose();
            evaluacionService?.Dispose();
        }
    }

    public class EstadisticasAdopcion
    {
        public int TotalAdopciones { get; set; }
        public int AdopcionesEsteMes { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int SolicitudesEnEvaluacion { get; set; }
        public decimal TasaAprobacion { get; set; }
    }
}
