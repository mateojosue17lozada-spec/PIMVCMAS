using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class RescateController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Rescate
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Estadísticas
            ViewBag.TotalRescates = db.ReportesRescate.Count();
            ViewBag.RescatesPendientes = db.ReportesRescate.Count(r => r.Estado == "Pendiente");
            ViewBag.RescatesEnProceso = db.ReportesRescate.Count(r => r.Estado == "En proceso");
            ViewBag.RescatesCompletados = db.ReportesRescate.Count(r => r.Estado == "Completado");

            return View();
        }

        // GET: Rescate/Reportar
        [AllowAnonymous]
        public ActionResult Reportar()
        {
            return View();
        }

        // POST: Rescate/Reportar
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Reportar(string nombreReportante, string telefonoReportante, string emailReportante,
            string ubicacion, string descripcionSituacion, string especieAnimal, string estadoAnimal,
            bool requiereUrgencia, HttpPostedFileBase imagenReporte)
        {
            if (string.IsNullOrEmpty(nombreReportante) || string.IsNullOrEmpty(telefonoReportante) ||
                string.IsNullOrEmpty(ubicacion) || string.IsNullOrEmpty(descripcionSituacion))
            {
                TempData["ErrorMessage"] = "Todos los campos obligatorios deben ser completados";
                return View();
            }

            // Validar imagen si se proporciona
            byte[] imagenBytes = null;
            if (imagenReporte != null)
            {
                if (!ImageHelper.IsValidImage(imagenReporte))
                {
                    TempData["ErrorMessage"] = "Archivo de imagen inválido";
                    return View();
                }

                if (!ImageHelper.IsFileSizeValid(imagenReporte, 5))
                {
                    TempData["ErrorMessage"] = "La imagen no debe exceder 5 MB";
                    return View();
                }

                imagenBytes = ImageHelper.ConvertImageToByteArray(imagenReporte);
                imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 800);
            }

            // Obtener usuario si está autenticado
            int? usuarioId = null;
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                usuarioId = usuario?.UsuarioId;
            }

            // Crear reporte
            var reporte = new ReportesRescate
            {
                UsuarioReportante = usuarioId,
                NombreReportante = nombreReportante,
                TelefonoReportante = telefonoReportante,
                EmailReportante = emailReportante,
                UbicacionReporte = ubicacion,
                DescripcionSituacion = descripcionSituacion,
                TipoAnimal = especieAnimal,
                Estado = estadoAnimal,
                RequiereUrgencia = requiereUrgencia,
                ImagenReporte = imagenBytes,
                FechaReporte = DateTime.Now,
                Estado = requiereUrgencia ? "Urgente" : "Pendiente"
            };

            db.ReportesRescate.Add(reporte);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Reportar Rescate", "Rescate",
                $"Ubicación: {ubicacion}, Urgencia: {requiereUrgencia}", usuarioId);

            // Enviar email de confirmación al reportante
            if (!string.IsNullOrEmpty(emailReportante))
            {
                string subject = "Reporte de rescate recibido";
                string body = $@"
                    <h2>Hola {nombreReportante}</h2>
                    <p>Hemos recibido tu reporte de rescate de animal.</p>
                    <p><strong>Detalles del reporte:</strong></p>
                    <ul>
                        <li>Ubicación: {ubicacion}</li>
                        <li>Especie: {especieAnimal ?? "No especificada"}</li>
                        <li>Estado: {estadoAnimal ?? "No especificado"}</li>
                        <li>Urgencia: {(requiereUrgencia ? "Sí" : "No")}</li>
                        <li>Número de reporte: #{reporte.ReporteId}</li>
                    </ul>
                    <p>Nuestro equipo {(requiereUrgencia ? "atenderá tu reporte con prioridad" : "revisará tu reporte pronto")}.</p>
                    <p>Te contactaremos al número {telefonoReportante} para coordinar el rescate.</p>
                    <br/>
                    <p>Gracias por ayudar a los animales,<br/>Equipo de Rescate</p>
                ";

                _ = EmailHelper.SendEmailAsync(emailReportante, subject, body);
            }

            // Enviar notificación al refugio si es urgente
            if (requiereUrgencia)
            {
                string subjectRefugio = $"¡REPORTE URGENTE DE RESCATE! #{reporte.ReporteId}";
                string bodyRefugio = $@"
                    <h2 style='color: red;'>REPORTE URGENTE</h2>
                    <p><strong>Reportante:</strong> {nombreReportante}</p>
                    <p><strong>Teléfono:</strong> {telefonoReportante}</p>
                    <p><strong>Ubicación:</strong> {ubicacion}</p>
                    <p><strong>Descripción:</strong> {descripcionSituacion}</p>
                    <p><strong>Especie:</strong> {especieAnimal ?? "No especificada"}</p>
                    <p><strong>Estado del animal:</strong> {estadoAnimal ?? "No especificado"}</p>
                    <p>Por favor, atender con prioridad.</p>
                ";

                _ = EmailHelper.SendEmailAsync(
                    System.Configuration.ConfigurationManager.AppSettings["EmailRefugio"],
                    subjectRefugio,
                    bodyRefugio
                );
            }

            TempData["SuccessMessage"] = requiereUrgencia
                ? "Reporte de urgencia enviado. Nuestro equipo se pondrá en contacto pronto."
                : "Reporte enviado exitosamente. Te contactaremos pronto.";

            return RedirectToAction("Index");
        }

        // GET: Rescate/MisReportes
        [Authorize]
        public ActionResult MisReportes()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var reportes = db.ReportesRescate
                .Where(r => r.UsuarioReportante == usuario.UsuarioId)
                .OrderByDescending(r => r.FechaReporte)
                .ToList();

            return View(reportes);
        }

        // GET: Rescate/GestionarReportes
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult GestionarReportes(string estado, int page = 1)
        {
            int pageSize = 20;
            var query = db.ReportesRescate.AsQueryable();

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(r => r.Estado == estado);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reportes = query
                .OrderByDescending(r => r.RequiereUrgencia)
                .ThenByDescending(r => r.FechaReporte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(reportes);
        }

        // GET: Rescate/DetalleReporte/5
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult DetalleReporte(int id)
        {
            var reporte = db.ReportesRescate.Find(id);

            if (reporte == null)
            {
                return HttpNotFound();
            }

            ViewBag.ImagenBase64 = reporte.ImagenReporte != null
                ? ImageHelper.GetImageDataUri(reporte.ImagenReporte)
                : null;

            return View(reporte);
        }

        // POST: Rescate/ActualizarEstadoReporte
        [HttpPost]
        [AuthorizeRoles("Administrador", "Rescatista")]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarEstadoReporte(int reporteId, string nuevoEstado, string observaciones)
        {
            var reporte = db.ReportesRescate.Find(reporteId);

            if (reporte == null)
            {
                return HttpNotFound();
            }

            reporte.Estado = nuevoEstado;
            reporte.ObservacionesRescatista = observaciones;

            if (nuevoEstado == "Completado")
            {
                reporte.FechaRescate = DateTime.Now;
            }

            db.SaveChanges();

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Actualizar Estado Reporte", "Rescate",
                $"Reporte ID: {reporteId}, Nuevo estado: {nuevoEstado}", usuario?.UsuarioId);

            // Notificar al reportante
            if (!string.IsNullOrEmpty(reporte.EmailReportante))
            {
                string subject = $"Actualización de tu reporte de rescate #{reporteId}";
                string body = $@"
                    <h2>Hola {reporte.NombreReportante}</h2>
                    <p>Tu reporte de rescate ha sido actualizado.</p>
                    <p><strong>Nuevo estado:</strong> {nuevoEstado}</p>
                    {(!string.IsNullOrEmpty(observaciones) ? $"<p><strong>Observaciones:</strong> {observaciones}</p>" : "")}
                    {(nuevoEstado == "Completado" ? "<p>¡El rescate ha sido completado exitosamente! Gracias por tu reporte.</p>" : "")}
                    <br/>
                    <p>Saludos,<br/>Equipo de Rescate</p>
                ";

                _ = EmailHelper.SendEmailAsync(reporte.EmailReportante, subject, body);
            }

            TempData["SuccessMessage"] = "Estado actualizado exitosamente";
            return RedirectToAction("DetalleReporte", new { id = reporteId });
        }

        // GET: Rescate/MascotasPerdidas
        [AllowAnonymous]
        public ActionResult MascotasPerdidas(string especie, string ciudad, int page = 1)
        {
            int pageSize = 12;
            var query = db.MascotasPerdidas.Where(m => m.Estado == "Perdida");

            if (!string.IsNullOrEmpty(especie) && especie != "Todos")
            {
                query = query.Where(m => m.Especie == especie);
            }

            if (!string.IsNullOrEmpty(ciudad) && ciudad != "Todos")
            {
                query = query.Where(m => m.CiudadPerdida.Contains(ciudad));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var mascotas = query
                .OrderByDescending(m => m.FechaPerdida)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EspecieSeleccionada = especie;
            ViewBag.CiudadSeleccionada = ciudad;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(mascotas);
        }

        // GET: Rescate/ReportarMascotaPerdida
        [Authorize]
        public ActionResult ReportarMascotaPerdida()
        {
            return View();
        }

        // POST: Rescate/ReportarMascotaPerdida
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ReportarMascotaPerdida(string nombreMascota, string especie, string raza,
            string color, string sexo, DateTime fechaPerdida, string ubicacionPerdida, string ciudadPerdida,
            string descripcion, string caracteristicasDistintivas, decimal? recompensa,
            HttpPostedFileBase imagenMascota)
        {
            if (string.IsNullOrEmpty(nombreMascota) || string.IsNullOrEmpty(ubicacionPerdida))
            {
                TempData["ErrorMessage"] = "Los campos obligatorios deben ser completados";
                return View();
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Validar imagen
            byte[] imagenBytes = null;
            if (imagenMascota != null)
            {
                if (!ImageHelper.IsValidImage(imagenMascota))
                {
                    TempData["ErrorMessage"] = "Archivo de imagen inválido";
                    return View();
                }

                imagenBytes = ImageHelper.ConvertImageToByteArray(imagenMascota);
                imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 800);
            }

            var mascotaPerdida = new MascotasPerdidas
            {
                PropietarioId = usuario.UsuarioId,
                NombreMascota = nombreMascota,
                Especie = especie,
                Raza = raza,
                Color = color,
                Sexo = sexo,
                FechaPerdida = fechaPerdida,
                UbicacionPerdida = ubicacionPerdida,
                CiudadPerdida = ciudadPerdida ?? "Quito",
                Descripcion = descripcion,
                CaracteristicasDistintivas = caracteristicasDistintivas,
                Recompensa = recompensa,
                ImagenMascota = imagenBytes,
                FechaReporte = DateTime.Now,
                Estado = "Perdida"
            };

            db.MascotasPerdidas.Add(mascotaPerdida);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Reportar Mascota Perdida", "Rescate",
                $"Mascota: {nombreMascota}, Ubicación: {ubicacionPerdida}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Reporte de mascota perdida creado. Esperamos que la encuentres pronto.";
            return RedirectToAction("MascotasPerdidas");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
