// Controllers/ReportesController.cs
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels.Reportes;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        public ActionResult Index()
        {
            ViewBag.TotalAdopciones = db.ContratoAdopcion.Count();
            ViewBag.TotalMascotas = db.Mascotas.Count(m => m.Activo == true);
            ViewBag.SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente");
            ViewBag.SeguimientosPendientes = db.SeguimientoAdopcion.Count(s => s.FechaSeguimiento == null);
            return View();
        }

        private byte[] RenderizarReportePDF(string rutaRdlc, string nombreDataSet, object datos, ReportParameter[] parametros)
        {
            LocalReport localReport = new LocalReport();
            localReport.ReportPath = Server.MapPath(rutaRdlc);

            ReportDataSource dataSource = new ReportDataSource(nombreDataSet, datos);
            localReport.DataSources.Clear();
            localReport.DataSources.Add(dataSource);
            localReport.SetParameters(parametros);

            string mimeType, encoding, fileNameExtension;
            string[] streams;
            Warning[] warnings;

            byte[] bytes = localReport.Render("PDF", null, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);
            return bytes;
        }

        // ============================================
        // REPORTE 1: ADOPCIONES REALIZADAS
        // ============================================
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ReporteAdopciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var datos = ObtenerDatosAdopciones(fechaInicio, fechaFin);

                if (!datos.Any())
                {
                    TempData["InfoMessage"] = "No hay datos para el período seleccionado.";
                    return RedirectToAction("Index", "Reportes");
                }

                ReportParameter[] parametros = new ReportParameter[]
                {
                    new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    new ReportParameter("Usuario", User.Identity.Name ?? "Sistema"),
                    new ReportParameter("FechaInicio", fechaInicio?.ToString("dd/MM/yyyy") ?? "Todos"),
                    new ReportParameter("FechaFin", fechaFin?.ToString("dd/MM/yyyy") ?? "Todos"),
                    new ReportParameter("TotalRegistros", datos.Count.ToString()),
                    new ReportParameter("TotalAdopciones", datos.Count.ToString())
                };

                byte[] bytes = RenderizarReportePDF("~/Reports/rptAdopcionesRealizadas.rdlc", "DataSetAdopciones", datos, parametros);
                return File(bytes, "application/pdf", $"Adopciones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error Reporte Adopciones: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                return RedirectToAction("Index", "Reportes");
            }
        }

        private List<AdopcionRealizadaReporteModel> ObtenerDatosAdopciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = from c in db.ContratoAdopcion
                        join s in db.SolicitudAdopcion on c.SolicitudId equals s.SolicitudId
                        join m in db.Mascotas on s.MascotaId equals m.MascotaId
                        join u in db.Usuarios on s.UsuarioId equals u.UsuarioId
                        join ev in db.Usuarios on s.EvaluadoPor equals ev.UsuarioId into evaluadorJoin
                        from evaluador in evaluadorJoin.DefaultIfEmpty()
                        where c.Estado == "Activo" || c.Estado == "Completado"
                        select new
                        {
                            s.SolicitudId,
                            FechaSolicitud = s.FechaSolicitud ?? DateTime.Now,
                            FechaAdopcion = c.FechaContrato ?? DateTime.Now,
                            c.NumeroContrato,
                            MascotaId = m.MascotaId,
                            NombreMascota = m.Nombre,
                            EspecieMascota = m.Especie,
                            RazaMascota = m.Raza ?? "Mestizo",
                            SexoMascota = m.Sexo,
                            EdadMascota = m.EdadAproximada ?? "No especificada",
                            TamanioMascota = m.Tamanio ?? "No especificado",
                            AdoptanteId = u.UsuarioId,
                            NombreAdoptante = u.NombreCompleto,
                            CedulaAdoptante = u.Cedula ?? "No registrada",
                            TelefonoAdoptante = u.Telefono ?? "No registrado",
                            EmailAdoptante = u.Email,
                            DireccionAdoptante = u.Direccion ?? "No registrada",
                            CiudadAdoptante = u.Ciudad ?? "No registrada",
                            EstadoSolicitud = s.Estado,
                            s.PuntajeEvaluacion,
                            s.ResultadoEvaluacion,
                            EvaluadoPor = evaluador != null ? evaluador.NombreCompleto : "No evaluado",
                            VeterinarioAsignado = m.Usuarios1 != null ? m.Usuarios1.NombreCompleto : "No asignado"
                        };

            // ✅ Variables locales fuera del lambda para evitar NotSupportedException
            if (fechaInicio.HasValue)
            {
                DateTime inicio = fechaInicio.Value.Date;
                query = query.Where(a => a.FechaAdopcion >= inicio);
            }

            if (fechaFin.HasValue)
            {
                DateTime fin = fechaFin.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.FechaAdopcion <= fin);
            }

            // ✅ ToList() antes de mapear al modelo final
            return query.ToList().Select(a => new AdopcionRealizadaReporteModel
            {
                SolicitudId = a.SolicitudId,
                FechaSolicitud = a.FechaSolicitud,
                FechaAdopcion = a.FechaAdopcion,
                NumeroContrato = a.NumeroContrato,
                MascotaId = a.MascotaId,
                NombreMascota = a.NombreMascota,
                EspecieMascota = a.EspecieMascota,
                RazaMascota = a.RazaMascota,
                SexoMascota = a.SexoMascota == "M" ? "Macho" : "Hembra",
                EdadMascota = a.EdadMascota,
                TamanioMascota = a.TamanioMascota,
                AdoptanteId = a.AdoptanteId,
                NombreAdoptante = a.NombreAdoptante,
                CedulaAdoptante = a.CedulaAdoptante,
                TelefonoAdoptante = a.TelefonoAdoptante,
                EmailAdoptante = a.EmailAdoptante,
                DireccionAdoptante = a.DireccionAdoptante,
                CiudadAdoptante = a.CiudadAdoptante,
                EstadoSolicitud = a.EstadoSolicitud,
                PuntajeEvaluacion = a.PuntajeEvaluacion,
                ResultadoEvaluacion = a.ResultadoEvaluacion,
                EvaluadoPor = a.EvaluadoPor,
                VeterinarioAsignado = a.VeterinarioAsignado,
                FechaGeneracion = DateTime.Now
            }).OrderByDescending(a => a.FechaAdopcion).ToList();
        }

        // ============================================
        // REPORTE 2: MASCOTAS REGISTRADAS
        // ============================================
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ReporteMascotas(string especie = "", string estado = "", string sexo = "", bool soloActivas = true)
        {
            try
            {
                var datos = ObtenerDatosMascotas(especie, estado, sexo, soloActivas);

                if (!datos.Any())
                {
                    TempData["InfoMessage"] = "No hay datos para los filtros seleccionados.";
                    return RedirectToAction("Index", "Reportes");
                }

                ReportParameter[] parametros = new ReportParameter[]
                {
                    new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    new ReportParameter("Usuario", User.Identity.Name ?? "Sistema"),
                    new ReportParameter("FiltroEspecie", string.IsNullOrEmpty(especie) ? "Todas" : especie),
                    new ReportParameter("FiltroEstado", string.IsNullOrEmpty(estado) ? "Todos" : estado),
                    new ReportParameter("FiltroSexo", string.IsNullOrEmpty(sexo) ? "Todos" : sexo),
                    new ReportParameter("TotalRegistros", datos.Count.ToString()),
                    new ReportParameter("TotalPerros", datos.Count(m => m.Especie == "Perro").ToString()),
                    new ReportParameter("TotalGatos", datos.Count(m => m.Especie == "Gato").ToString()),
                    new ReportParameter("TotalDisponibles", datos.Count(m => m.Estado == "Disponible para adopción").ToString())
                };

                byte[] bytes = RenderizarReportePDF("~/Reports/rptMascotasRegistradas.rdlc", "DataSetMascotas", datos, parametros);
                return File(bytes, "application/pdf", $"Mascotas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error Reporte Mascotas: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                return RedirectToAction("Index", "Reportes");
            }
        }

        private List<MascotaRegistradaReporteModel> ObtenerDatosMascotas(string especie, string estado, string sexo, bool soloActivas)
        {
            var query = from m in db.Mascotas
                        select new
                        {
                            m.MascotaId,
                            m.Nombre,
                            m.Especie,
                            m.Raza,
                            m.Sexo,
                            m.EdadAproximada,
                            m.Tamanio,
                            m.Color,
                            m.Categoria,
                            m.TipoEspecial,
                            m.FechaIngreso,
                            m.FechaDisponible,
                            m.FechaAdopcion,
                            m.Estado,
                            m.Esterilizado,
                            m.Microchip,
                            m.Activo,
                            VeterinarioNombre = m.Usuarios1 != null ? m.Usuarios1.NombreCompleto : "No asignado",
                            RescatistaNombre = m.Usuarios != null ? m.Usuarios.NombreCompleto : "No registrado",
                            TotalCambiosEstado = m.HistorialEstadosMascota.Count,
                            TotalTratamientos = m.Tratamientos.Count,
                            TotalVacunas = m.MascotaVacunas.Count
                        };

            if (!string.IsNullOrEmpty(especie))
                query = query.Where(m => m.Especie == especie);
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(m => m.Estado == estado);
            if (!string.IsNullOrEmpty(sexo))
                query = query.Where(m => m.Sexo == sexo);
            if (soloActivas)
                query = query.Where(m => m.Activo == true);

            // ✅ ToList() primero, luego calcular DiasEnRefugio en memoria
            return query.ToList().Select(m => new MascotaRegistradaReporteModel
            {
                MascotaId = m.MascotaId,
                Nombre = m.Nombre,
                Especie = m.Especie,
                Raza = m.Raza ?? "Mestizo",
                Sexo = m.Sexo == "M" ? "Macho" : "Hembra",
                EdadAproximada = m.EdadAproximada ?? "No especificada",
                Tamanio = m.Tamanio ?? "No especificado",
                Color = m.Color ?? "No especificado",
                Categoria = m.Categoria ?? "Normal",
                TipoEspecial = m.TipoEspecial,
                FechaIngreso = m.FechaIngreso,
                FechaDisponible = m.FechaDisponible,
                FechaAdopcion = m.FechaAdopcion,
                Estado = m.Estado,
                Esterilizado = m.Esterilizado,
                Microchip = m.Microchip ?? "No registrado",
                Activo = m.Activo,
                VeterinarioAsignado = m.VeterinarioNombre,
                RescatistaNombre = m.RescatistaNombre,
                TotalCambiosEstado = m.TotalCambiosEstado,
                TotalTratamientos = m.TotalTratamientos,
                TotalVacunas = m.TotalVacunas,
                DiasEnRefugio = m.FechaIngreso.HasValue ? (DateTime.Now - m.FechaIngreso.Value).Days : 0,
                FechaGeneracion = DateTime.Now
            }).OrderByDescending(m => m.FechaIngreso).ToList();
        }

        // ============================================
        // REPORTE 3: SOLICITUDES DE ADOPCIÓN
        // ============================================
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ReporteSolicitudes(string estado = "", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            try
            {
                var datos = ObtenerDatosSolicitudes(estado, fechaInicio, fechaFin);

                if (!datos.Any())
                {
                    TempData["InfoMessage"] = "No hay solicitudes para los filtros seleccionados.";
                    return RedirectToAction("Index", "Reportes");
                }

                ReportParameter[] parametros = new ReportParameter[]
                {
                    new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    new ReportParameter("Usuario", User.Identity.Name ?? "Sistema"),
                    new ReportParameter("FiltroEstado", string.IsNullOrEmpty(estado) ? "Todos" : estado),
                    new ReportParameter("FechaInicio", fechaInicio?.ToString("dd/MM/yyyy") ?? "Sin filtro"),
                    new ReportParameter("FechaFin", fechaFin?.ToString("dd/MM/yyyy") ?? "Sin filtro"),
                    new ReportParameter("TotalRegistros", datos.Count.ToString()),
                    new ReportParameter("Pendientes", datos.Count(s => s.Estado == "Pendiente").ToString()),
                    new ReportParameter("Aprobadas", datos.Count(s => s.Estado == "Aprobada").ToString()),
                    new ReportParameter("Rechazadas", datos.Count(s => s.Estado == "Rechazada").ToString())
                };

                byte[] bytes = RenderizarReportePDF("~/Reports/rptSolicitudesAdopcion.rdlc", "DataSetSolicitudes", datos, parametros);
                return File(bytes, "application/pdf", $"Solicitudes_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error Reporte Solicitudes: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                return RedirectToAction("Index", "Reportes");
            }
        }

        private List<SolicitudAdopcionReporteModel> ObtenerDatosSolicitudes(string estado, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = from s in db.SolicitudAdopcion
                        join m in db.Mascotas on s.MascotaId equals m.MascotaId
                        join u in db.Usuarios on s.UsuarioId equals u.UsuarioId
                        join ev in db.Usuarios on s.EvaluadoPor equals ev.UsuarioId into evaluadorJoin
                        from evaluador in evaluadorJoin.DefaultIfEmpty()
                        join f in db.FormularioAdopcionDetalle on s.SolicitudId equals f.SolicitudId into formularioJoin
                        from formulario in formularioJoin.DefaultIfEmpty()
                        select new
                        {
                            s.SolicitudId,
                            FechaSolicitud = s.FechaSolicitud ?? DateTime.Now,
                            s.Estado,
                            s.EstadoAdopcion,
                            SolicitanteId = u.UsuarioId,
                            u.NombreCompleto,
                            Cedula = u.Cedula ?? "No registrada",
                            Telefono = u.Telefono ?? "No registrado",
                            u.Email,
                            Ciudad = u.Ciudad ?? "No especificada",
                            MascotaId = m.MascotaId,
                            NombreMascota = m.Nombre,
                            EspecieMascota = m.Especie,
                            RazaMascota = m.Raza ?? "Mestizo",
                            s.FechaEvaluacion,
                            s.PuntajeEvaluacion,
                            s.ResultadoEvaluacion,
                            EvaluadorNombre = evaluador != null ? evaluador.NombreCompleto : "No evaluado",
                            TipoVivienda = formulario != null ? formulario.TipoVivienda : "No disponible",
                            ViviendaPropia = formulario != null && formulario.ViviendaPropia.HasValue ? (formulario.ViviendaPropia.Value ? "Sí" : "No") : "No disponible",
                            TieneJardin = formulario != null && formulario.TieneJardin.HasValue ? (formulario.TieneJardin.Value ? "Sí" : "No") : "No disponible",
                            PersonasEnCasa = formulario != null ? formulario.PersonasEnCasa : null,
                            HayNinios = formulario != null && formulario.HayNinios.HasValue ? (formulario.HayNinios.Value ? "Sí" : "No") : "No disponible",
                            ExperienciaPrevia = formulario != null && formulario.ExperienciaPreviaConMascotas.HasValue ? (formulario.ExperienciaPreviaConMascotas.Value ? "Sí" : "No") : "No disponible",
                            TieneMascotas = formulario != null && formulario.TieneMascotasActualmente.HasValue ? (formulario.TieneMascotasActualmente.Value ? "Sí" : "No") : "No disponible",
                            TiempoDisponible = formulario != null ? formulario.TiempoDisponibleDiario : "No disponible",
                            AceptaEsterilizacion = formulario != null ? (formulario.AceptaEsterilizacion ? "Sí" : "No") : "No disponible",
                            AceptaSeguimiento = formulario != null ? (formulario.AceptaVisitasSeguimiento ? "Sí" : "No") : "No disponible",
                            s.FechaRespuesta,
                            s.MotivoRechazo,
                            s.Observaciones
                        };

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(s => s.Estado == estado);
            if (fechaInicio.HasValue)
                query = query.Where(s => s.FechaSolicitud >= fechaInicio.Value.Date);
            if (fechaFin.HasValue)
                query = query.Where(s => s.FechaSolicitud <= fechaFin.Value.Date.AddDays(1).AddSeconds(-1));

            // ✅ ToList() primero, luego calcular DiasEnProceso en memoria
            return query.ToList().Select(s => new SolicitudAdopcionReporteModel
            {
                SolicitudId = s.SolicitudId,
                FechaSolicitud = s.FechaSolicitud,
                Estado = s.Estado,
                EstadoAdopcion = s.EstadoAdopcion,
                SolicitanteId = s.SolicitanteId,
                NombreSolicitante = s.NombreCompleto,
                CedulaSolicitante = s.Cedula,
                TelefonoSolicitante = s.Telefono,
                EmailSolicitante = s.Email,
                CiudadSolicitante = s.Ciudad,
                MascotaId = s.MascotaId,
                NombreMascota = s.NombreMascota,
                EspecieMascota = s.EspecieMascota,
                RazaMascota = s.RazaMascota,
                FechaEvaluacion = s.FechaEvaluacion,
                PuntajeEvaluacion = s.PuntajeEvaluacion,
                ResultadoEvaluacion = s.ResultadoEvaluacion,
                EvaluadorNombre = s.EvaluadorNombre,
                TipoVivienda = s.TipoVivienda,
                ViviendaPropia = s.ViviendaPropia,
                TieneJardin = s.TieneJardin,
                PersonasEnCasa = s.PersonasEnCasa,
                HayNinios = s.HayNinios,
                ExperienciaPrevia = s.ExperienciaPrevia,
                TieneMascotas = s.TieneMascotas,
                TiempoDisponible = s.TiempoDisponible,
                AceptaEsterilizacion = s.AceptaEsterilizacion,
                AceptaSeguimiento = s.AceptaSeguimiento,
                FechaRespuesta = s.FechaRespuesta,
                MotivoRechazo = s.MotivoRechazo,
                Observaciones = s.Observaciones,
                // ✅ Calculado en memoria, no en SQL
                DiasEnProceso = s.FechaRespuesta.HasValue
                    ? (s.FechaRespuesta.Value - s.FechaSolicitud).Days
                    : (DateTime.Now - s.FechaSolicitud).Days,
                FechaGeneracion = DateTime.Now
            }).OrderByDescending(s => s.FechaSolicitud).ToList();
        }

        // ============================================
        // REPORTE 4: POST-SEGUIMIENTO
        // ============================================
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ReporteSeguimientos(string tipo = "todos", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            try
            {
                var datos = ObtenerDatosSeguimientos(tipo, fechaDesde, fechaHasta);

                if (!datos.Any())
                {
                    TempData["InfoMessage"] = "No hay datos de seguimiento para los filtros seleccionados.";
                    return RedirectToAction("Index", "Reportes");
                }

                int realizados = datos.Count(s => s.FechaSeguimiento.HasValue);
                int pendientes = datos.Count(s => !s.FechaSeguimiento.HasValue && (!s.ProximoSeguimiento.HasValue || s.ProximoSeguimiento >= DateTime.Now));
                int vencidos = datos.Count(s => !s.FechaSeguimiento.HasValue && s.ProximoSeguimiento.HasValue && s.ProximoSeguimiento < DateTime.Now);
                int requierenIntervencion = datos.Count(s => s.RequiereIntervencion);

                ReportParameter[] parametros = new ReportParameter[]
                {
                    new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    new ReportParameter("Usuario", User.Identity.Name ?? "Sistema"),
                    new ReportParameter("FiltroTipo", tipo),
                    new ReportParameter("FechaDesde", fechaDesde?.ToString("dd/MM/yyyy") ?? "Todas"),
                    new ReportParameter("FechaHasta", fechaHasta?.ToString("dd/MM/yyyy") ?? "Todas"),
                    new ReportParameter("TotalRegistros", datos.Count.ToString()),
                    new ReportParameter("Realizados", realizados.ToString()),
                    new ReportParameter("Pendientes", pendientes.ToString()),
                    new ReportParameter("Vencidos", vencidos.ToString()),
                    new ReportParameter("RequierenIntervencion", requierenIntervencion.ToString())
                };

                byte[] bytes = RenderizarReportePDF("~/Reports/rptSeguimientoPostAdopcion.rdlc", "DataSetSeguimientos", datos, parametros);
                return File(bytes, "application/pdf", $"Seguimientos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error Reporte Seguimientos: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                return RedirectToAction("Index", "Reportes");
            }
        }

        private List<SeguimientoPostAdopcionReporteModel> ObtenerDatosSeguimientos(string tipo, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = from seg in db.SeguimientoAdopcion
                        join c in db.ContratoAdopcion on seg.ContratoId equals c.ContratoId
                        join s in db.SolicitudAdopcion on c.SolicitudId equals s.SolicitudId
                        join m in db.Mascotas on s.MascotaId equals m.MascotaId
                        join uAdoptante in db.Usuarios on s.UsuarioId equals uAdoptante.UsuarioId
                        join uResponsable in db.Usuarios on seg.ResponsableSeguimiento equals uResponsable.UsuarioId into responsableJoin
                        from responsable in responsableJoin.DefaultIfEmpty()
                        select new SeguimientoPostAdopcionReporteModel
                        {
                            SeguimientoId = seg.SeguimientoId,
                            FechaSeguimiento = seg.FechaSeguimiento,
                            TipoSeguimiento = seg.TipoSeguimiento,
                            EstadoMascota = seg.EstadoMascota,
                            CondicionesVivienda = seg.CondicionesVivienda,
                            RelacionConAdoptante = seg.RelacionConAdoptante,
                            Observaciones = seg.Observaciones,
                            Recomendaciones = seg.Recomendaciones,
                            RequiereIntervencion = seg.RequiereIntervencion,
                            ProximoSeguimiento = seg.ProximoSeguimiento,
                            ContratoId = c.ContratoId,
                            NumeroContrato = c.NumeroContrato,
                            FechaContrato = c.FechaContrato,
                            MascotaId = m.MascotaId,
                            NombreMascota = m.Nombre,
                            EspecieMascota = m.Especie,
                            RazaMascota = m.Raza ?? "Mestizo",
                            NombreAdoptante = uAdoptante.NombreCompleto,
                            TelefonoAdoptante = uAdoptante.Telefono ?? "No registrado",
                            EmailAdoptante = uAdoptante.Email,
                            DireccionAdoptante = uAdoptante.Direccion ?? "No registrada",
                            ResponsableNombre = responsable != null ? responsable.NombreCompleto : "No asignado",
                            FechaGeneracion = DateTime.Now
                        };

            if (tipo == "realizados") query = query.Where(s => s.FechaSeguimiento.HasValue);
            else if (tipo == "pendientes") query = query.Where(s => !s.FechaSeguimiento.HasValue);
            else if (tipo == "vencidos") query = query.Where(s => !s.FechaSeguimiento.HasValue && s.ProximoSeguimiento.HasValue && s.ProximoSeguimiento < DateTime.Now);
            else if (tipo == "intervencion") query = query.Where(s => s.RequiereIntervencion);

            if (fechaDesde.HasValue)
                query = query.Where(s => s.FechaSeguimiento >= fechaDesde.Value.Date || s.ProximoSeguimiento >= fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                query = query.Where(s => s.FechaSeguimiento <= fechaHasta.Value.Date.AddDays(1) || s.ProximoSeguimiento <= fechaHasta.Value.Date.AddDays(1));

            return query.OrderByDescending(s => s.ProximoSeguimiento ?? s.FechaSeguimiento).ToList();
        }

        // ============================================
        // REPORTE 5: ESTADÍSTICAS GENERALES
        // ============================================
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ReporteEstadisticas()
        {
            try
            {
                var stats = ObtenerEstadisticasGenerales();
                var lista = new List<EstadisticasGeneralesReporteModel> { stats };

                ReportParameter[] parametros = new ReportParameter[]
                {
                    new ReportParameter("FechaGeneracion", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                    new ReportParameter("Usuario", User.Identity.Name ?? "Sistema")
                };

                byte[] bytes = RenderizarReportePDF("~/Reports/rptEstadisticasGenerales.rdlc", "DataSetEstadisticas", lista, parametros);
                return File(bytes, "application/pdf", $"Estadisticas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error Reporte Estadísticas: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                return RedirectToAction("Index", "Reportes");
            }
        }

        private EstadisticasGeneralesReporteModel ObtenerEstadisticasGenerales()
        {
            var hoy = DateTime.Now;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);
            var model = new EstadisticasGeneralesReporteModel();

            model.TotalMascotas = db.Mascotas.Count(m => m.Activo == true);
            model.TotalAdopciones = db.ContratoAdopcion.Count();
            model.SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente" || s.Estado == "En evaluación");
            model.SeguimientosPendientes = db.SeguimientoAdopcion.Count(s => s.FechaSeguimiento == null);
            model.TotalPerros = db.Mascotas.Count(m => m.Especie == "Perro" && m.Activo == true);
            model.TotalGatos = db.Mascotas.Count(m => m.Especie == "Gato" && m.Activo == true);
            model.TotalOtros = db.Mascotas.Count(m => m.Especie != "Perro" && m.Especie != "Gato" && m.Activo == true);
            model.Disponibles = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.Activo == true);
            model.EnTratamiento = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo == true);
            model.Adoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada");
            model.Rescatadas = db.Mascotas.Count(m => m.Estado == "Rescatada" && m.Activo == true);

            var adopcionesConFechas = db.Mascotas
    .Where(m => m.FechaAdopcion.HasValue && m.FechaIngreso.HasValue)
    .Select(m => new { Ingreso = m.FechaIngreso.Value, Adopcion = m.FechaAdopcion.Value })
    .ToList(); // ✅ Traer a memoria primero

            if (adopcionesConFechas.Any())
                model.PromedioDiasAdopcion = adopcionesConFechas.Average(a => (a.Adopcion - a.Ingreso).TotalDays);

            var fechaLimite = hoy.AddMonths(-6);
            model.MascotasSinAdoptarLargoPlazo = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.FechaIngreso.HasValue && m.FechaIngreso < fechaLimite);
            model.AdopcionesEsteMes = db.Mascotas.Count(m => m.FechaAdopcion.HasValue && m.FechaAdopcion >= inicioMes && m.FechaAdopcion <= finMes);
            model.SolicitudesEsteMes = db.SolicitudAdopcion.Count(s => s.FechaSolicitud >= inicioMes && s.FechaSolicitud <= finMes);
            model.IngresosEsteMes = db.Mascotas.Count(m => m.FechaIngreso >= inicioMes && m.FechaIngreso <= finMes);
            model.FechaGeneracion = hoy;
            return model;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db?.Dispose();
            base.Dispose(disposing);
        }


        // AGREGA estas acciones al ReportesController existente

        // ============================================
        // VISTAS HTML CON GRÁFICAS
        // ============================================

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult VerAdopciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var datos = ObtenerDatosAdopciones(fechaInicio, fechaFin);
            ViewBag.FechaInicio = fechaInicio?.ToString("dd/MM/yyyy") ?? "Todos";
            ViewBag.FechaFin = fechaFin?.ToString("dd/MM/yyyy") ?? "Todos";
            ViewBag.FechaInicioParm = fechaInicio?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.FechaFinParm = fechaFin?.ToString("yyyy-MM-dd") ?? "";
            return View(datos);
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult VerMascotas(string especie = "", string estado = "", string sexo = "", bool soloActivas = true)
        {
            var datos = ObtenerDatosMascotas(especie, estado, sexo, soloActivas);
            ViewBag.Especie = especie;
            ViewBag.Estado = estado;
            ViewBag.Sexo = sexo;
            ViewBag.SoloActivas = soloActivas;
            return View(datos);
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult VerSolicitudes(string estado = "", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var datos = ObtenerDatosSolicitudes(estado, fechaInicio, fechaFin);
            ViewBag.Estado = estado;
            ViewBag.FechaInicioParm = fechaInicio?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.FechaFinParm = fechaFin?.ToString("yyyy-MM-dd") ?? "";
            return View(datos);
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult VerSeguimientos(string tipo = "todos", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var datos = ObtenerDatosSeguimientos(tipo, fechaDesde, fechaHasta);
            ViewBag.Tipo = tipo;
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd") ?? "";
            return View(datos);
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult VerEstadisticas()
        {
            var stats = ObtenerEstadisticasGenerales();
            return View(stats);
        }
    }
}