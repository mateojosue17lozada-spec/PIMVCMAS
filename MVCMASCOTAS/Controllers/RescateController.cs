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
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Rescate
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Estadísticas
            ViewBag.TotalRescates = db.ReportesRescate.Count();
            ViewBag.RescatesPendientes = db.ReportesRescate.Count(r => r.Estado == "Pendiente");
            ViewBag.RescatesEnProceso = db.ReportesRescate.Count(r => r.Estado == "En proceso");
            ViewBag.RescatesCompletados = db.ReportesRescate.Count(r => r.Estado == "Rescatado");

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
            string ubicacion, string descripcionSituacion, string especieAnimal, string condicionAnimal,
            string urgencia, HttpPostedFileBase imagenReporte)
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
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                usuarioId = usuario?.UsuarioId;
            }

            // Crear reporte - CORREGIDO según estructura real
            var reporte = new ReportesRescate
            {
                UsuarioReportante = usuarioId,
                NombreReportante = nombreReportante,
                TelefonoReportante = telefonoReportante,
                EmailReportante = emailReportante,
                UbicacionReporte = ubicacion,
                DescripcionSituacion = descripcionSituacion,
                TipoAnimal = especieAnimal,
                CondicionAnimal = condicionAnimal, // CORRECTO: es CondicionAnimal, no Estado
                Urgencia = urgencia ?? "Media", // CORRECTO: es Urgencia, no RequiereUrgencia
                FechaReporte = DateTime.Now,
                Estado = "Pendiente" // CORRECTO: solo asignar una vez
            };

            db.ReportesRescate.Add(reporte);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Reportar Rescate", "Rescate",
                $"Ubicación: {ubicacion}, Urgencia: {urgencia}", usuarioId);

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
                        <li>Condición: {condicionAnimal ?? "No especificada"}</li>
                        <li>Urgencia: {urgencia ?? "Media"}</li>
                        <li>Número de reporte: #{reporte.ReporteId}</li>
                    </ul>
                    <p>Nuestro equipo {(urgencia == "Crítica" || urgencia == "Alta" ? "atenderá tu reporte con prioridad" : "revisará tu reporte pronto")}.</p>
                    <p>Te contactaremos al número {telefonoReportante} para coordinar el rescate.</p>
                    <br/>
                    <p>Gracias por ayudar a los animales,<br/>Equipo de Rescate</p>
                ";

                _ = EmailHelper.SendEmailAsync(emailReportante, subject, body);
            }

            // Enviar notificación al refugio si es urgente
            if (urgencia == "Crítica" || urgencia == "Alta")
            {
                string subjectRefugio = $"¡REPORTE URGENTE DE RESCATE! #{reporte.ReporteId}";
                string bodyRefugio = $@"
                    <h2 style='color: red;'>REPORTE URGENTE ({urgencia})</h2>
                    <p><strong>Reportante:</strong> {nombreReportante}</p>
                    <p><strong>Teléfono:</strong> {telefonoReportante}</p>
                    <p><strong>Ubicación:</strong> {ubicacion}</p>
                    <p><strong>Descripción:</strong> {descripcionSituacion}</p>
                    <p><strong>Especie:</strong> {especieAnimal ?? "No especificada"}</p>
                    <p><strong>Condición del animal:</strong> {condicionAnimal ?? "No especificada"}</p>
                    <p>Por favor, atender con prioridad.</p>
                ";

                // Buscar email de administrador o usar uno por defecto
                var adminEmail = db.Usuarios
                    .Where(u => u.UsuariosRoles.Any(ur => ur.Roles.NombreRol == "Administrador"))
                    .Select(u => u.Email)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(adminEmail))
                {
                    _ = EmailHelper.SendEmailAsync(adminEmail, subjectRefugio, bodyRefugio);
                }
            }

            TempData["SuccessMessage"] = (urgencia == "Crítica" || urgencia == "Alta")
                ? "Reporte de urgencia enviado. Nuestro equipo se pondrá en contacto pronto."
                : "Reporte enviado exitosamente. Te contactaremos pronto.";

            return RedirectToAction("Index");
        }

        // GET: Rescate/MisReportes
        [Authorize]
        public ActionResult MisReportes()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var reportes = db.ReportesRescate
                .Where(r => r.UsuarioReportante == usuario.UsuarioId)
                .OrderByDescending(r => r.FechaReporte)
                .ToList();

            return View(reportes);
        }

        // GET: Rescate/GestionarReportes
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult GestionarReportes(string estado, string urgencia, int page = 1)
        {
            int pageSize = 20;
            var query = db.ReportesRescate.AsQueryable();

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(r => r.Estado == estado);
            }

            if (!string.IsNullOrEmpty(urgencia) && urgencia != "Todas")
            {
                query = query.Where(r => r.Urgencia == urgencia);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reportes = query
                .OrderByDescending(r => r.Urgencia == "Crítica")
                .ThenByDescending(r => r.Urgencia == "Alta")
                .ThenByDescending(r => r.FechaReporte)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.UrgenciaSeleccionada = urgencia;
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

            // Obtener imágenes adicionales si existen
            ViewBag.ImagenesAdicionales = db.ImagenesAdicionales
                .Where(i => i.EntidadTipo == "ReporteRescate" && i.EntidadId == id)
                .OrderBy(i => i.Orden)
                .ToList();

            return View(reporte);
        }

        // POST: Rescate/ActualizarEstadoReporte
        [HttpPost]
        [AuthorizeRoles("Administrador", "Rescatista")]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarEstadoReporte(int reporteId, string nuevoEstado, string observacionesSeguimiento)
        {
            var reporte = db.ReportesRescate.Find(reporteId);

            if (reporte == null)
            {
                return HttpNotFound();
            }

            reporte.Estado = nuevoEstado;

            // Concatenar observaciones
            if (!string.IsNullOrEmpty(observacionesSeguimiento))
            {
                reporte.ObservacionesSeguimiento = string.IsNullOrEmpty(reporte.ObservacionesSeguimiento)
                    ? observacionesSeguimiento
                    : reporte.ObservacionesSeguimiento + "\n\n---\n" + observacionesSeguimiento;
            }

            if (nuevoEstado == "Rescatado" || nuevoEstado == "Cerrado")
            {
                reporte.FechaRescate = DateTime.Now;
            }

            db.SaveChanges();

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario != null)
            {
                AuditoriaHelper.RegistrarAccion("Actualizar Estado Reporte", "Rescate",
                    $"Reporte ID: {reporteId}, Nuevo estado: {nuevoEstado}", usuario.UsuarioId);
            }

            // Notificar al reportante
            if (!string.IsNullOrEmpty(reporte.EmailReportante))
            {
                string subject = $"Actualización de tu reporte de rescate #{reporteId}";
                string body = $@"
                    <h2>Hola {reporte.NombreReportante}</h2>
                    <p>Tu reporte de rescate ha sido actualizado.</p>
                    <p><strong>Nuevo estado:</strong> {nuevoEstado}</p>
                    {(!string.IsNullOrEmpty(observacionesSeguimiento) ? $"<p><strong>Observaciones:</strong> {observacionesSeguimiento}</p>" : "")}
                    {(nuevoEstado == "Rescatado" ? "<p>¡El rescate ha sido completado exitosamente! Gracias por tu reporte.</p>" : "")}
                    <br/>
                    <p>Saludos,<br/>Equipo de Rescate</p>
                ";

                _ = EmailHelper.SendEmailAsync(reporte.EmailReportante, subject, body);
            }

            TempData["SuccessMessage"] = "Estado actualizado exitosamente";
            return RedirectToAction("DetalleReporte", new { id = reporteId });
        }

        // POST: Rescate/AsignarRescatista
        [HttpPost]
        [AuthorizeRoles("Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarRescatista(int reporteId, int rescatistaId)
        {
            var reporte = db.ReportesRescate.Find(reporteId);

            if (reporte == null)
            {
                return HttpNotFound();
            }

            var rescatista = db.Usuarios.Find(rescatistaId);
            if (rescatista == null)
            {
                TempData["ErrorMessage"] = "Rescatista no encontrado";
                return RedirectToAction("DetalleReporte", new { id = reporteId });
            }

            reporte.RescatistaAsignado = rescatistaId;
            reporte.Estado = "En proceso";
            db.SaveChanges();

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario != null)
            {
                AuditoriaHelper.RegistrarAccion("Asignar Rescatista", "Rescate",
                    $"Reporte ID: {reporteId} asignado a {rescatista.NombreCompleto}", usuario.UsuarioId);
            }

            // Notificar al rescatista
            string subjectRescatista = $"Has sido asignado a un reporte de rescate #{reporteId}";
            string bodyRescatista = $@"
                <h2>Hola {rescatista.NombreCompleto}</h2>
                <p>Has sido asignado al siguiente reporte de rescate:</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Ubicación: {reporte.UbicacionReporte}</li>
                    <li>Especie: {reporte.TipoAnimal ?? "No especificada"}</li>
                    <li>Condición: {reporte.CondicionAnimal ?? "No especificada"}</li>
                    <li>Urgencia: {reporte.Urgencia ?? "Media"}</li>
                    <li>Reportante: {reporte.NombreReportante} ({reporte.TelefonoReportante})</li>
                </ul>
                <p>Por favor, contacta al reportante para coordinar el rescate.</p>
            ";

            _ = EmailHelper.SendEmailAsync(rescatista.Email, subjectRescatista, bodyRescatista);

            TempData["SuccessMessage"] = "Rescatista asignado exitosamente";
            return RedirectToAction("DetalleReporte", new { id = reporteId });
        }

        // GET: Rescate/MascotasPerdidas
        [AllowAnonymous]
        public ActionResult MascotasPerdidas(string especie, int page = 1)
        {
            int pageSize = 12;
            var query = db.MascotasPerdidas.Where(m => m.Estado == "Perdida");

            if (!string.IsNullOrEmpty(especie) && especie != "Todos")
            {
                query = query.Where(m => m.Especie == especie);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var mascotas = query
                .OrderByDescending(m => m.FechaPerdida)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EspecieSeleccionada = especie;
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
            string color, string sexo, DateTime fechaPerdida, string ubicacionPerdida,
            string caracteristicasDistintivas, decimal? recompensa,
            HttpPostedFileBase imagenMascota)
        {
            if (string.IsNullOrEmpty(nombreMascota) || string.IsNullOrEmpty(ubicacionPerdida))
            {
                TempData["ErrorMessage"] = "Los campos obligatorios deben ser completados";
                return View();
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

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
                UsuarioPropietario = usuario.UsuarioId, // CORRECTO: es UsuarioPropietario, no PropietarioId
                NombreMascota = nombreMascota,
                Especie = especie,
                Raza = raza,
                Color = color,
                Sexo = sexo,
                FechaPerdida = fechaPerdida,
                UbicacionPerdida = ubicacionPerdida,
                CaracteristicasDistintivas = caracteristicasDistintivas, // CORRECTO: es este campo, no Descripcion
                Recompensa = recompensa,
                ImagenMascota = imagenBytes,
                ContactoNombre = usuario.NombreCompleto,
                ContactoTelefono = usuario.Telefono,
                ContactoEmail = usuario.Email,
                FechaPublicacion = DateTime.Now, // CORRECTO: es FechaPublicacion, no FechaReporte
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

        // GET: Rescate/DetalleMascotaPerdida/5
        [AllowAnonymous]
        public ActionResult DetalleMascotaPerdida(int id)
        {
            var mascota = db.MascotasPerdidas.Find(id);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            return View(mascota);
        }

        // POST: Rescate/ReportarAvistamiento
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ReportarAvistamiento(int mascotaPerdidaId, string nombreReportante,
            string telefonoReportante, string ubicacionAvistamiento, DateTime? fechaAvistamiento,
            string detallesAvistamiento)
        {
            var mascota = db.MascotasPerdidas.Find(mascotaPerdidaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            // Enviar email al propietario
            var propietario = db.Usuarios.Find(mascota.UsuarioPropietario);
            if (propietario != null && !string.IsNullOrEmpty(propietario.Email))
            {
                string subject = $"Posible avistamiento de {mascota.NombreMascota}";
                string body = $@"
                    <h2>Hola {propietario.NombreCompleto}</h2>
                    <p>¡Buenas noticias! Alguien ha reportado un posible avistamiento de <strong>{mascota.NombreMascota}</strong>.</p>
                    <p><strong>Detalles del avistamiento:</strong></p>
                    <ul>
                        <li>Reportante: {nombreReportante}</li>
                        <li>Teléfono: {telefonoReportante}</li>
                        <li>Ubicación: {ubicacionAvistamiento}</li>
                        <li>Fecha: {fechaAvistamiento?.ToString("dd/MM/yyyy") ?? "No especificada"}</li>
                        <li>Detalles: {detallesAvistamiento}</li>
                    </ul>
                    <p>Te recomendamos contactar al reportante lo antes posible.</p>
                    <br/>
                    <p>¡Esperamos que encuentres a {mascota.NombreMascota} pronto!</p>
                ";

                _ = EmailHelper.SendEmailAsync(propietario.Email, subject, body);
            }

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Reportar Avistamiento", "Rescate",
                $"Mascota ID: {mascotaPerdidaId}, Reportante: {nombreReportante}");

            TempData["SuccessMessage"] = "Gracias por reportar el avistamiento. El propietario ha sido notificado.";
            return RedirectToAction("DetalleMascotaPerdida", new { id = mascotaPerdidaId });
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