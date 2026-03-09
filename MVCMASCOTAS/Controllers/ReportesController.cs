using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels.Reportes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // ============================================================
        // INDEX
        // ============================================================
        public ActionResult Index()
        {
            ViewBag.TotalAdopciones = db.ContratoAdopcion.Count();
            ViewBag.TotalMascotas = db.Mascotas.Count(m => m.Activo == true);
            ViewBag.SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente");
            ViewBag.SeguimientosPendientes = db.SeguimientoAdopcion.Count(s => s.FechaSeguimiento == null);
            return View();
        }

        // ============================================================
        // DATOS: ADOPCIONES
        // ============================================================
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

        // ============================================================
        // DATOS: MASCOTAS
        // ============================================================
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

            if (!string.IsNullOrEmpty(especie)) query = query.Where(m => m.Especie == especie);
            if (!string.IsNullOrEmpty(estado)) query = query.Where(m => m.Estado == estado);
            if (!string.IsNullOrEmpty(sexo)) query = query.Where(m => m.Sexo == sexo);
            if (soloActivas) query = query.Where(m => m.Activo == true);

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

        // ============================================================
        // DATOS: SOLICITUDES
        // ============================================================
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

            if (!string.IsNullOrEmpty(estado)) query = query.Where(s => s.Estado == estado);
            if (fechaInicio.HasValue) query = query.Where(s => s.FechaSolicitud >= fechaInicio.Value.Date);
            if (fechaFin.HasValue) query = query.Where(s => s.FechaSolicitud <= fechaFin.Value.Date.AddDays(1).AddSeconds(-1));

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
                DiasEnProceso = s.FechaRespuesta.HasValue
                                ? (s.FechaRespuesta.Value - s.FechaSolicitud).Days
                                : (DateTime.Now - s.FechaSolicitud).Days,
                FechaGeneracion = DateTime.Now
            }).OrderByDescending(s => s.FechaSolicitud).ToList();
        }

        // ============================================================
        // DATOS: SEGUIMIENTOS
        // ============================================================
        private List<SeguimientoPostAdopcionReporteModel> ObtenerDatosSeguimientos(string tipo, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = from seg in db.SeguimientoAdopcion
                        join c in db.ContratoAdopcion on seg.ContratoId equals c.ContratoId
                        join s in db.SolicitudAdopcion on c.SolicitudId equals s.SolicitudId
                        join m in db.Mascotas on s.MascotaId equals m.MascotaId
                        join uAdoptante in db.Usuarios on s.UsuarioId equals uAdoptante.UsuarioId
                        join uResponsable in db.Usuarios on seg.ResponsableSeguimiento equals uResponsable.UsuarioId into responsableJoin
                        from responsable in responsableJoin.DefaultIfEmpty()
                        select new
                        {
                            seg.SeguimientoId,
                            seg.FechaSeguimiento,
                            seg.TipoSeguimiento,
                            seg.EstadoMascota,
                            seg.CondicionesVivienda,
                            seg.RelacionConAdoptante,
                            seg.Observaciones,
                            seg.Recomendaciones,
                            seg.RequiereIntervencion,
                            seg.ProximoSeguimiento,
                            c.ContratoId,
                            c.NumeroContrato,
                            c.FechaContrato,
                            m.MascotaId,
                            m.Nombre,
                            m.Especie,
                            Raza = m.Raza ?? "Mestizo",
                            NombreAdoptante = uAdoptante.NombreCompleto,
                            TelefonoAdoptante = uAdoptante.Telefono ?? "No registrado",
                            EmailAdoptante = uAdoptante.Email,
                            DireccionAdoptante = uAdoptante.Direccion ?? "No registrada",
                            ResponsableNombre = responsable != null ? responsable.NombreCompleto : "No asignado"
                        };

            if (tipo == "realizados") query = query.Where(s => s.FechaSeguimiento.HasValue);
            else if (tipo == "pendientes") query = query.Where(s => !s.FechaSeguimiento.HasValue);
            else if (tipo == "vencidos") query = query.Where(s => !s.FechaSeguimiento.HasValue && s.ProximoSeguimiento.HasValue && s.ProximoSeguimiento < DateTime.Now);
            else if (tipo == "intervencion") query = query.Where(s => s.RequiereIntervencion);

            if (fechaDesde.HasValue) query = query.Where(s => s.FechaSeguimiento >= fechaDesde.Value.Date || s.ProximoSeguimiento >= fechaDesde.Value.Date);
            if (fechaHasta.HasValue) query = query.Where(s => s.FechaSeguimiento <= fechaHasta.Value.Date.AddDays(1) || s.ProximoSeguimiento <= fechaHasta.Value.Date.AddDays(1));

            return query.ToList().Select(x => new SeguimientoPostAdopcionReporteModel
            {
                SeguimientoId = x.SeguimientoId,
                FechaSeguimiento = x.FechaSeguimiento,
                TipoSeguimiento = x.TipoSeguimiento,
                EstadoMascota = x.EstadoMascota,
                CondicionesVivienda = x.CondicionesVivienda,
                RelacionConAdoptante = x.RelacionConAdoptante,
                Observaciones = x.Observaciones,
                Recomendaciones = x.Recomendaciones,
                RequiereIntervencion = x.RequiereIntervencion,
                ProximoSeguimiento = x.ProximoSeguimiento,
                ContratoId = x.ContratoId,
                NumeroContrato = x.NumeroContrato,
                FechaContrato = x.FechaContrato,
                MascotaId = x.MascotaId,
                NombreMascota = x.Nombre,
                EspecieMascota = x.Especie,
                RazaMascota = x.Raza,
                NombreAdoptante = x.NombreAdoptante,
                TelefonoAdoptante = x.TelefonoAdoptante,
                EmailAdoptante = x.EmailAdoptante,
                DireccionAdoptante = x.DireccionAdoptante,
                ResponsableNombre = x.ResponsableNombre,
                FechaGeneracion = DateTime.Now
            }).OrderByDescending(s => s.ProximoSeguimiento ?? s.FechaSeguimiento).ToList();
        }

        // ============================================================
        // DATOS: ESTADÍSTICAS
        // ============================================================
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
                .ToList();

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

        // ============================================================
        // VISTAS HTML
        // ============================================================
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
            return View(ObtenerEstadisticasGenerales());
        }

        // ============================================================
        // EXPORTAR EXCEL/CSV
        // ============================================================
        private FileContentResult GenerarCsv(string[] headers, IEnumerable<string[]> rows, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(h => "\"" + h + "\"")));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", row.Select(c => "\"" + (c ?? "").Replace("\"", "\"\"") + "\"")));
            var bytes = Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ExportarExcelAdopciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var d = ObtenerDatosAdopciones(fechaInicio, fechaFin);
            var h = new[] { "N° Contrato", "Fecha Adopción", "Mascota", "Especie", "Raza", "Sexo", "Adoptante", "Cédula", "Teléfono", "Email", "Ciudad", "Estado", "Puntaje", "Resultado", "Evaluador", "Veterinario" };
            var r = d.Select(a => new[] { a.NumeroContrato, a.FechaAdopcion.ToString("dd/MM/yyyy"), a.NombreMascota, a.EspecieMascota, a.RazaMascota, a.SexoMascota, a.NombreAdoptante, a.CedulaAdoptante, a.TelefonoAdoptante, a.EmailAdoptante, a.CiudadAdoptante, a.EstadoSolicitud, a.PuntajeEvaluacion?.ToString() ?? "N/A", a.ResultadoEvaluacion, a.EvaluadoPor, a.VeterinarioAsignado });
            return GenerarCsv(h, r, $"Adopciones_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ExportarExcelMascotas(string especie = "", string estado = "", string sexo = "", bool soloActivas = true)
        {
            var d = ObtenerDatosMascotas(especie, estado, sexo, soloActivas);
            var h = new[] { "Nombre", "Especie", "Raza", "Sexo", "Edad", "Tamaño", "Color", "Categoría", "Estado", "Esterilizado", "Microchip", "Fecha Ingreso", "Días en Refugio", "Veterinario", "Rescatista", "Tratamientos", "Vacunas" };
            var r = d.Select(m => new[] { m.Nombre, m.Especie, m.Raza, m.Sexo, m.EdadAproximada, m.Tamanio, m.Color, m.Categoria, m.Estado, m.Esterilizado == true ? "Sí" : "No", m.Microchip, m.FechaIngreso?.ToString("dd/MM/yyyy") ?? "N/A", m.DiasEnRefugio.ToString(), m.VeterinarioAsignado, m.RescatistaNombre, m.TotalTratamientos.ToString(), m.TotalVacunas.ToString() });
            return GenerarCsv(h, r, $"Mascotas_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ExportarExcelSolicitudes(string estado = "", DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var d = ObtenerDatosSolicitudes(estado, fechaInicio, fechaFin);
            var h = new[] { "ID", "Fecha", "Solicitante", "Cédula", "Teléfono", "Email", "Ciudad", "Mascota", "Especie", "Raza", "Estado", "Puntaje", "Resultado", "Evaluador", "Tipo Vivienda", "Días en Proceso" };
            var r = d.Select(s => new[] { s.SolicitudId.ToString(), s.FechaSolicitud.ToString("dd/MM/yyyy"), s.NombreSolicitante, s.CedulaSolicitante, s.TelefonoSolicitante, s.EmailSolicitante, s.CiudadSolicitante, s.NombreMascota, s.EspecieMascota, s.RazaMascota, s.Estado, s.PuntajeEvaluacion?.ToString() ?? "N/A", s.ResultadoEvaluacion, s.EvaluadorNombre, s.TipoVivienda, s.DiasEnProceso.ToString() });
            return GenerarCsv(h, r, $"Solicitudes_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ExportarExcelSeguimientos(string tipo = "todos", DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var d = ObtenerDatosSeguimientos(tipo, fechaDesde, fechaHasta);
            var h = new[] { "N° Contrato", "Mascota", "Especie", "Adoptante", "Teléfono", "Tipo", "Fecha Seguim.", "Próximo", "Estado Mascota", "Cond. Vivienda", "Relación", "Intervención", "Responsable", "Observaciones" };
            var r = d.Select(s => new[] { s.NumeroContrato, s.NombreMascota, s.EspecieMascota, s.NombreAdoptante, s.TelefonoAdoptante, s.TipoSeguimiento, s.FechaSeguimiento?.ToString("dd/MM/yyyy") ?? "PENDIENTE", s.ProximoSeguimiento?.ToString("dd/MM/yyyy") ?? "No programado", s.EstadoMascota, s.CondicionesVivienda, s.RelacionConAdoptante, s.RequiereIntervencion ? "Sí" : "No", s.ResponsableNombre, s.Observaciones });
            return GenerarCsv(h, r, $"Seguimientos_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult ExportarExcelEstadisticas()
        {
            var s = ObtenerEstadisticasGenerales();
            var h = new[] { "Métrica", "Valor" };
            var r = new[] {
                new[] { "Total mascotas activas",       s.TotalMascotas.ToString() },
                new[] { "Total adopciones",             s.TotalAdopciones.ToString() },
                new[] { "Solicitudes pendientes",       s.SolicitudesPendientes.ToString() },
                new[] { "Seguimientos pendientes",      s.SeguimientosPendientes.ToString() },
                new[] { "Perros",                       s.TotalPerros.ToString() },
                new[] { "Gatos",                        s.TotalGatos.ToString() },
                new[] { "Otros",                        s.TotalOtros.ToString() },
                new[] { "Disponibles para adopción",    s.Disponibles.ToString() },
                new[] { "En tratamiento",               s.EnTratamiento.ToString() },
                new[] { "Adoptadas",                    s.Adoptadas.ToString() },
                new[] { "Rescatadas",                   s.Rescatadas.ToString() },
                new[] { "Promedio días hasta adopción", s.PromedioDiasAdopcion.ToString("0.0") },
                new[] { "Sin adoptar >6 meses",         s.MascotasSinAdoptarLargoPlazo.ToString() },
                new[] { "Adopciones este mes",          s.AdopcionesEsteMes.ToString() },
                new[] { "Solicitudes este mes",         s.SolicitudesEsteMes.ToString() },
                new[] { "Ingresos este mes",            s.IngresosEsteMes.ToString() }
            };
            return GenerarCsv(h, r, $"Estadisticas_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db?.Dispose();
            base.Dispose(disposing);
        }
    }
}