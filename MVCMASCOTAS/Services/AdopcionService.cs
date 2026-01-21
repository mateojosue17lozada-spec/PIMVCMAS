using System;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
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
            // Verificar si ya existe una solicitud pendiente
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

            return solicitud;
        }

        // Registrar formulario de adopción
        public void RegistrarFormulario(int solicitudId, FormularioAdopcionDetalle formulario)
        {
            formulario.SolicitudId = solicitudId;
            formulario.FechaLlenado = DateTime.Now;
            db.FormularioAdopcionDetalle.Add(formulario);
            db.SaveChanges();

            // Cambiar estado a "En evaluación"
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

            // Registrar evaluación detallada - CORREGIDO según estructura real
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
                PuntajeTotal = resultado.PuntajeTotal, // CORRECTO: es PuntajeTotal, no Puntaje
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

                // Actualizar estado de la mascota - CORREGIDO: "Adoptada", no "Adoptado"
                var mascota = db.Mascotas.Find(solicitud.MascotaId);
                if (mascota != null)
                {
                    mascota.Estado = "Adoptada";
                    mascota.FechaAdopcion = DateTime.Now;
                    db.SaveChanges();
                }

                // Crear evaluación manual
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

                // Crear evaluación de rechazo
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
                .Include("Mascotas")
                .Where(s => s.UsuarioId == usuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();
        }

        // Obtener solicitud por ID
        public SolicitudAdopcion ObtenerSolicitudPorId(int solicitudId)
        {
            return db.SolicitudAdopcion
                .Include("Mascotas")
                .Include("Usuarios")
                .FirstOrDefault(s => s.SolicitudId == solicitudId);
        }

        // Obtener formulario de una solicitud
        public FormularioAdopcionDetalle ObtenerFormularioSolicitud(int solicitudId)
        {
            return db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);
        }

        // Obtener evaluación de una solicitud
        public EvaluacionAdopcion ObtenerEvaluacionSolicitud(int solicitudId)
        {
            return db.EvaluacionAdopcion
                .Include("Usuarios") // Evaluador
                .FirstOrDefault(e => e.SolicitudId == solicitudId);
        }

        // Verificar si usuario tiene solicitud para mascota
        public bool TieneSolicitudPendiente(int usuarioId, int mascotaId)
        {
            return db.SolicitudAdopcion
                .Any(s => s.UsuarioId == usuarioId &&
                         s.MascotaId == mascotaId &&
                         (s.Estado == "Pendiente" || s.Estado == "En evaluación"));
        }

        // Obtener estadísticas de adopciones
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

        // Registrar seguimiento post-adopción - CORREGIDO según estructura real
        public void RegistrarSeguimiento(int contratoId, int responsableId, string estadoMascota,
            string condicionesVivienda, string relacionAdoptante, string observaciones)
        {
            var seguimiento = new SeguimientoAdopcion
            {
                ContratoId = contratoId, // CORRECTO: ContratoId, no SolicitudId
                ResponsableSeguimiento = responsableId, // CORRECTO: ResponsableSeguimiento, no ResponsableId
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

        // Obtener seguimientos de una adopción - CORREGIDO
        public List<SeguimientoAdopcion> ObtenerSeguimientos(int contratoId)
        {
            return db.SeguimientoAdopcion
                .Where(s => s.ContratoId == contratoId)
                .OrderByDescending(s => s.FechaSeguimiento)
                .ToList();
        }

        // Obtener contrato de adopción por solicitud
        public ContratoAdopcion ObtenerContratoPorSolicitud(int solicitudId)
        {
            return db.ContratoAdopcion
                .FirstOrDefault(c => c.SolicitudId == solicitudId);
        }

        // Crear contrato de adopción
        public ContratoAdopcion CrearContrato(int solicitudId, string numeroContrato,
            string terminosCondiciones, string representanteRefugioNombre,
            string representanteRefugioCedula)
        {
            var solicitud = db.SolicitudAdopcion
                .Include("Usuarios")
                .Include("Mascotas")
                .FirstOrDefault(s => s.SolicitudId == solicitudId);

            if (solicitud == null) return null;

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

            return contrato;
        }

        // Generar número de contrato único
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

        // Obtener estadísticas completas de adopciones
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

        // Marcar seguimiento como completado
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

    // Clase para estadísticas de adopción
    public class EstadisticasAdopcion
    {
        public int TotalAdopciones { get; set; }
        public int AdopcionesEsteMes { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int SolicitudesEnEvaluacion { get; set; }
        public decimal TasaAprobacion { get; set; }
    }
}