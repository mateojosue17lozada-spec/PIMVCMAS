using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class RescateController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Rescate/Index - Página principal de rescates
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                // Estadísticas para la vista
                ViewBag.TotalRescates = db.ReportesRescate.Count();
                ViewBag.RescatesPendientes = db.ReportesRescate.Count(r => r.Estado == "Pendiente");
                ViewBag.RescatesEnProceso = db.ReportesRescate.Count(r => r.Estado == "En proceso");
                ViewBag.RescatesCompletados = db.ReportesRescate.Count(r => r.Estado == "Rescatado");

                return View();
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Index Rescate", "Rescate",
                    $"Error: {ex.Message}", null);
                ViewBag.Error = "Error al cargar estadísticas";
                return View();
            }
        }

        // =============================================
        // RESCATES DE ANIMALES
        // =============================================

        // GET: Rescate/Reportar - Formulario para reportar rescate
        [AllowAnonymous]
        public ActionResult ReportarRescate()
        {
            return View();
        }

        // POST: Rescate/Reportar - Procesar reporte de rescate
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ReportarRescate(string nombreReportante, string telefonoReportante, string emailReportante,
            string ubicacion, string descripcionSituacion, string especieAnimal, string condicionAnimal,
            string urgencia, HttpPostedFileBase imagenReporte)
        {
            if (string.IsNullOrEmpty(nombreReportante) || string.IsNullOrEmpty(telefonoReportante) ||
                string.IsNullOrEmpty(ubicacion) || string.IsNullOrEmpty(descripcionSituacion))
            {
                TempData["ErrorMessage"] = "Todos los campos obligatorios deben ser completados";
                return View();
            }

            try
            {
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
                    CondicionAnimal = condicionAnimal,
                    Urgencia = urgencia ?? "Media",
                    FechaReporte = DateTime.Now,
                    Estado = "Pendiente"
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

                    _ = EmailHelper.SendNotificationAsync(emailReportante, subject, body);
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
                        _ = EmailHelper.SendNotificationAsync(adminEmail, subjectRefugio, bodyRefugio);
                    }
                }

                TempData["SuccessMessage"] = (urgencia == "Crítica" || urgencia == "Alta")
                    ? "Reporte de urgencia enviado. Nuestro equipo se pondrá en contacto pronto."
                    : "Reporte enviado exitosamente. Te contactaremos pronto.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Reportar Rescate", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al procesar el reporte. Intenta nuevamente.";
                return View();
            }
        }

        // GET: Rescate/DetalleReporte/5
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult DetalleReporte(int id)
        {
            try
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
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Detalle Reporte", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar el reporte";
                return RedirectToAction("Index");
            }
        }

        // POST: Rescate/ActualizarEstadoReporte
        [HttpPost]
        [AuthorizeRoles("Administrador", "Rescatista")]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarEstadoReporte(int reporteId, string nuevoEstado, string observacionesSeguimiento)
        {
            try
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

                    _ = EmailHelper.SendNotificationAsync(reporte.EmailReportante, subject, body);
                }

                TempData["SuccessMessage"] = "Estado actualizado exitosamente";
                return RedirectToAction("DetalleReporte", new { id = reporteId });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Actualizar Estado", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al actualizar el estado";
                return RedirectToAction("DetalleReporte", new { id = reporteId });
            }
        }

        // POST: Rescate/AsignarRescatista
        [HttpPost]
        [AuthorizeRoles("Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarRescatista(int reporteId, int rescatistaId)
        {
            try
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

                _ = EmailHelper.SendNotificationAsync(rescatista.Email, subjectRescatista, bodyRescatista);

                TempData["SuccessMessage"] = "Rescatista asignado exitosamente";
                return RedirectToAction("DetalleReporte", new { id = reporteId });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Asignar Rescatista", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al asignar rescatista";
                return RedirectToAction("DetalleReporte", new { id = reporteId });
            }
        }

        // =============================================
        // MASCOTAS PERDIDAS
        // =============================================

        // GET: Rescate/MascotasPerdidas - Listado de mascotas perdidas
        [AllowAnonymous]
        public ActionResult MascotasPerdidas(string especie = null, string estado = "Perdida", int page = 1)
        {
            try
            {
                int pageSize = 12;
                var query = db.MascotasPerdidas.AsQueryable();

                // Filtrar por especie
                if (!string.IsNullOrEmpty(especie) && especie != "Todos")
                {
                    query = query.Where(m => m.Especie == especie);
                }

                // Filtrar por estado
                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                {
                    query = query.Where(m => m.Estado == estado);
                }

                // Solo mostrar reportes activos (no cerrados hace más de 30 días)
                var fechaLimite = DateTime.Now.AddDays(-30);
                query = query.Where(m => m.Estado != "Cerrado" || m.FechaPublicacion >= fechaLimite);

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var mascotas = query
                    .OrderByDescending(m => m.FechaPerdida)
                    .ThenByDescending(m => m.FechaPublicacion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.EspecieSeleccionada = especie;
                ViewBag.EstadoSeleccionado = estado;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Especies = db.MascotasPerdidas
                    .Select(m => m.Especie)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToList();

                return View(mascotas);
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Mascotas Perdidas", "Rescate",
                    $"Error: {ex.Message}", null);
                ViewBag.Error = "Error al cargar mascotas perdidas";
                return View(new List<MascotasPerdidas>());
            }
        }

        // GET: Rescate/ReportarMascotaPerdida - Formulario para reportar mascota perdida
        [AllowAnonymous]
        public ActionResult ReportarMascotaPerdida()
        {
            // Crear y pasar una instancia del ViewModel
            var model = new ReporteMascotaPerdidaViewModel();

            // El modelo ya tiene valores por defecto definidos en su clase:
            // - Sexo = "M" (por defecto en el modelo)
            // - FechaPerdida = DateTime.Now (por defecto en el modelo)
            // - OfrecerRecompensa = false (valor por defecto de bool)

            return View(model); // ← ¡PASAR EL MODELO A LA VISTA!
        }

        // POST: Rescate/ReportarMascotaPerdida - Procesar reporte de mascota perdida
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ReportarMascotaPerdida(ReporteMascotaPerdidaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validar que acepte términos
            if (!model.AceptaTerminos)
            {
                ModelState.AddModelError("AceptaTerminos", "Debes aceptar los términos y condiciones.");
                return View(model);
            }

            try
            {
                // Verificar si el usuario está autenticado
                int? usuarioId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var usuario = db.Usuarios.FirstOrDefault(u =>
                        u.Email == User.Identity.Name &&
                        (u.Activo == null || u.Activo == true));
                    usuarioId = usuario?.UsuarioId;
                }

                // Procesar imagen
                byte[] imagenBytes = null;
                if (model.ImagenMascota != null && model.ImagenMascota.ContentLength > 0)
                {
                    if (!ImageHelper.IsValidImage(model.ImagenMascota))
                    {
                        ModelState.AddModelError("ImagenMascota",
                            "Archivo de imagen inválido. Formatos permitidos: JPG, PNG, GIF.");
                        return View(model);
                    }

                    if (!ImageHelper.IsFileSizeValid(model.ImagenMascota, 10))
                    {
                        ModelState.AddModelError("ImagenMascota", "La imagen no debe exceder 10 MB.");
                        return View(model);
                    }

                    imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenMascota);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 600);
                }

                // Crear registro de mascota perdida
                var mascotaPerdida = new MascotasPerdidas
                {
                    UsuarioPropietario = usuarioId ?? 0,
                    NombreMascota = model.NombreMascota,
                    Especie = model.Especie,
                    Raza = model.Raza,
                    Sexo = model.Sexo?.ToString() ?? "M", // Conversión de char? a string
                    Edad = model.Edad,
                    Color = model.Color,
                    CaracteristicasDistintivas = model.CaracteristicasDistintivas,
                    FechaPerdida = model.FechaPerdida,
                    UbicacionPerdida = model.UbicacionPerdida,
                    ImagenMascota = imagenBytes,
                    ContactoNombre = model.ContactoNombre,
                    ContactoTelefono = model.ContactoTelefono,
                    ContactoEmail = model.ContactoEmail,
                    Recompensa = model.OfrecerRecompensa ? model.Recompensa : null,
                    Estado = "Perdida",
                    FechaPublicacion = DateTime.Now,
                    Observaciones = model.Observaciones
                };

                db.MascotasPerdidas.Add(mascotaPerdida);
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Reportar Mascota Perdida", "Rescate",
                    $"Mascota: {model.NombreMascota} - Ubicación: {model.UbicacionPerdida}",
                    usuarioId);

                // Enviar email de confirmación si hay email
                if (!string.IsNullOrEmpty(model.ContactoEmail))
                {
                    string subject = $"Reporte de mascota perdida registrado";
                    string body = $@"
                        <h2>Hola {model.ContactoNombre}</h2>
                        <p>Hemos registrado tu reporte de mascota perdida.</p>
                        <p><strong>Detalles del reporte:</strong></p>
                        <ul>
                            <li>Mascota: {model.NombreMascota}</li>
                            <li>Especie: {model.Especie}</li>
                            <li>Ubicación pérdida: {model.UbicacionPerdida}</li>
                            <li>Fecha pérdida: {model.FechaPerdida:dd/MM/yyyy HH:mm}</li>
                            <li>Número de reporte: #{mascotaPerdida.MascotaPerdidaId}</li>
                        </ul>
                        <p>Puedes ver tu reporte aquí: {Url.Action("DetalleMascotaPerdida", "Rescate", new { id = mascotaPerdida.MascotaPerdidaId }, Request.Url.Scheme)}</p>
                        <p>Te notificaremos si alguien reporta un avistamiento.</p>
                        <br/>
                        <p>Esperamos que encuentres a {model.NombreMascota} pronto,<br/>Equipo de Rescate</p>
                    ";

                    _ = EmailHelper.SendNotificationAsync(model.ContactoEmail, subject, body);
                }

                TempData["SuccessMessage"] = "¡Reporte enviado exitosamente! Hemos registrado tu mascota perdida.";
                return RedirectToAction("DetalleMascotaPerdida", new { id = mascotaPerdida.MascotaPerdidaId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al registrar el reporte. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarAccion("Error Reportar Mascota", "Rescate",
                    $"Error: {ex.Message}", null);
                return View(model);
            }
        }

        // GET: Rescate/DetalleMascotaPerdida/5 - Detalle de mascota perdida
        [AllowAnonymous]
        public ActionResult DetalleMascotaPerdida(int id)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                // Obtener información del dueño si existe
                string propietarioNombre = null;
                if (mascota.UsuarioPropietario > 0)
                {
                    var propietario = db.Usuarios.Find(mascota.UsuarioPropietario);
                    propietarioNombre = propietario?.NombreCompleto;
                }

                // Verificar permisos del usuario actual
                bool esPropietario = false;
                bool esAdmin = false;
                bool esRescatista = false;

                if (User.Identity.IsAuthenticated)
                {
                    var usuario = db.Usuarios.FirstOrDefault(u =>
                        u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

                    if (usuario != null)
                    {
                        esPropietario = mascota.UsuarioPropietario > 0 &&
                                        mascota.UsuarioPropietario == usuario.UsuarioId;

                        // Verificar roles
                        var roles = db.UsuariosRoles
                            .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                            .Select(ur => ur.Roles.NombreRol)
                            .ToList();

                        esAdmin = roles.Contains("Administrador");
                        esRescatista = roles.Contains("Rescatista");
                    }
                }

                // Crear ViewModel
                var viewModel = new DetalleMascotaPerdidaViewModel
                {
                    MascotaPerdidaId = mascota.MascotaPerdidaId,
                    NombreMascota = mascota.NombreMascota,
                    Especie = mascota.Especie,
                    Raza = mascota.Raza,
                    Sexo = mascota.Sexo ?? "No especificado",
                    Edad = mascota.Edad,
                    Color = mascota.Color,
                    CaracteristicasDistintivas = mascota.CaracteristicasDistintivas,
                    FechaPerdida = mascota.FechaPerdida,
                    UbicacionPerdida = mascota.UbicacionPerdida,
                    Observaciones = mascota.Observaciones,
                    UsuarioPropietarioId = mascota.UsuarioPropietario > 0 ? mascota.UsuarioPropietario : (int?)null,
                    PropietarioNombre = propietarioNombre,
                    ContactoTelefono = mascota.ContactoTelefono,
                    ContactoEmail = mascota.ContactoEmail,
                    ContactoNombre = mascota.ContactoNombre,
                    Recompensa = mascota.Recompensa,
                    TieneRecompensa = mascota.Recompensa.HasValue && mascota.Recompensa.Value > 0,
                    Estado = mascota.Estado,
                    FechaPublicacion = mascota.FechaPublicacion ?? DateTime.Now,
                    FechaEncontrada = mascota.FechaEncontrada,
                    ImagenMascota = mascota.ImagenMascota,
                    ImagenBase64 = mascota.ImagenMascota != null && mascota.ImagenMascota.Length > 0 ?
                        Convert.ToBase64String(mascota.ImagenMascota) : null,

                    // Permisos
                    EsPropietario = esPropietario,
                    EsAdmin = esAdmin,
                    EsRescatista = esRescatista,
                    PuedeEditar = esPropietario || esAdmin,
                    PuedeMarcarEncontrada = esPropietario || esAdmin || esRescatista,
                    PuedeCerrar = esPropietario || esAdmin
                };

                // Registrar vista
                AuditoriaHelper.RegistrarAccion("Ver Detalle Mascota Perdida", "Rescate",
                    $"Vista de {mascota.NombreMascota}",
                    User.Identity.IsAuthenticated ? GetUsuarioId() : null);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Detalle Mascota", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar los detalles de la mascota";
                return RedirectToAction("MascotasPerdidas");
            }
        }

        // GET: Rescate/MisReportes - Reportes del usuario actual
        [Authorize]
        public ActionResult MisReportes(int page = 1)
        {
            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Login", "Account");
                }

                int pageSize = 12;
                var query = db.MascotasPerdidas
                    .Where(m => m.UsuarioPropietario == usuario.UsuarioId)
                    .OrderByDescending(m => m.FechaPublicacion);

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var reportes = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(reportes);
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Mis Reportes", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar tus reportes";
                return View(new List<MascotasPerdidas>());
            }
        }

        // GET: Rescate/MisReportesRescate - Reportes de rescate del usuario
        [Authorize]
        public ActionResult MisReportesRescate(int page = 1)
        {
            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Login", "Account");
                }

                int pageSize = 12;
                var query = db.ReportesRescate
                    .Where(r => r.UsuarioReportante == usuario.UsuarioId)
                    .OrderByDescending(r => r.FechaReporte);

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var reportes = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(reportes);
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Mis Reportes Rescate", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar tus reportes de rescate";
                return View(new List<ReportesRescate>());
            }
        }

        // GET: Rescate/MarcarEncontrada/5
        [Authorize]
        public ActionResult MarcarEncontrada(int id)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                // Verificar que el usuario sea el dueño o admin
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Login", "Account");
                }

                // Verificar permisos
                bool esPropietario = mascota.UsuarioPropietario > 0 &&
                                    mascota.UsuarioPropietario == usuario.UsuarioId;
                bool esAdmin = db.UsuariosRoles.Any(ur =>
                    ur.UsuarioId == usuario.UsuarioId &&
                    ur.Roles.NombreRol == "Administrador");
                bool esRescatista = db.UsuariosRoles.Any(ur =>
                    ur.UsuarioId == usuario.UsuarioId &&
                    ur.Roles.NombreRol == "Rescatista");

                if (!esPropietario && !esAdmin && !esRescatista)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para modificar este reporte.";
                    return RedirectToAction("MisReportes");
                }

                mascota.Estado = "Encontrada";
                mascota.FechaEncontrada = DateTime.Now;
                db.SaveChanges();

                AuditoriaHelper.RegistrarAccion("Marcar Mascota Encontrada", "Rescate",
                    $"Mascota: {mascota.NombreMascota} marcada como encontrada", usuario.UsuarioId);

                // Enviar email de confirmación si hay email
                if (!string.IsNullOrEmpty(mascota.ContactoEmail))
                {
                    string subject = $"¡{mascota.NombreMascota} ha sido encontrada!";
                    string body = $@"
                        <h2>Hola {mascota.ContactoNombre}</h2>
                        <p>¡Excelentes noticias! <strong>{mascota.NombreMascota}</strong> ha sido marcada como encontrada.</p>
                        <p><strong>Detalles:</strong></p>
                        <ul>
                            <li>Mascota: {mascota.NombreMascota}</li>
                            <li>Fecha de encuentro: {DateTime.Now:dd/MM/yyyy HH:mm}</li>
                            <li>Estado: Encontrada</li>
                        </ul>
                        <p>Gracias por usar nuestro sistema. ¡Esperamos que tengas un feliz reencuentro!</p>
                        <br/>
                        <p>Saludos,<br/>Equipo de Rescate</p>
                    ";

                    _ = EmailHelper.SendNotificationAsync(mascota.ContactoEmail, subject, body);
                }

                TempData["SuccessMessage"] = "¡Excelente noticia! Has marcado a tu mascota como encontrada.";
                return RedirectToAction("DetalleMascotaPerdida", new { id });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Marcar Encontrada", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al marcar como encontrada";
                return RedirectToAction("DetalleMascotaPerdida", new { id });
            }
        }

        // GET: Rescate/CerrarReporte/5
        [Authorize]
        public ActionResult CerrarReporte(int id)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Login", "Account");
                }

                // Verificar permisos (solo dueño o admin)
                bool esPropietario = mascota.UsuarioPropietario > 0 &&
                                    mascota.UsuarioPropietario == usuario.UsuarioId;
                bool esAdmin = db.UsuariosRoles.Any(ur =>
                    ur.UsuarioId == usuario.UsuarioId &&
                    ur.Roles.NombreRol == "Administrador");

                if (!esPropietario && !esAdmin)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para cerrar este reporte.";
                    return RedirectToAction("MisReportes");
                }

                mascota.Estado = "Cerrado";
                db.SaveChanges();

                AuditoriaHelper.RegistrarAccion("Cerrar Reporte Mascota", "Rescate",
                    $"Reporte cerrado para: {mascota.NombreMascota}", usuario.UsuarioId);

                TempData["InfoMessage"] = "Reporte cerrado exitosamente.";
                return RedirectToAction("DetalleMascotaPerdida", new { id });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Cerrar Reporte", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cerrar el reporte";
                return RedirectToAction("DetalleMascotaPerdida", new { id });
            }
        }

        // GET: Rescate/ReportarAvistamiento/5
        [AllowAnonymous]
        public ActionResult ReportarAvistamiento(int id)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null || mascota.Estado != "Perdida")
                {
                    TempData["ErrorMessage"] = "Esta mascota ya fue encontrada o el reporte no existe.";
                    return RedirectToAction("MascotasPerdidas");
                }

                ViewBag.MascotaNombre = mascota.NombreMascota;
                ViewBag.MascotaId = id;

                return View();
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Reportar Avistamiento GET", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar formulario de avistamiento";
                return RedirectToAction("MascotasPerdidas");
            }
        }

        // POST: Rescate/ReportarAvistamiento
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ReportarAvistamiento(int id, FormCollection form)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null || mascota.Estado != "Perdida")
                {
                    TempData["ErrorMessage"] = "Esta mascota ya fue encontrada o el reporte no existe.";
                    return RedirectToAction("MascotasPerdidas");
                }

                var nombreReportante = form["NombreReportante"];
                var telefonoReportante = form["TelefonoReportante"];
                var ubicacionAvistamiento = form["UbicacionAvistamiento"];
                var fechaAvistamiento = form["FechaAvistamiento"];
                var observaciones = form["Observaciones"];

                if (string.IsNullOrEmpty(nombreReportante) || string.IsNullOrEmpty(telefonoReportante) ||
                    string.IsNullOrEmpty(ubicacionAvistamiento))
                {
                    TempData["ErrorMessage"] = "Por favor completa todos los campos requeridos.";
                    return RedirectToAction("ReportarAvistamiento", new { id });
                }

                AuditoriaHelper.RegistrarAccion("Reportar Avistamiento", "Rescate",
                    $"Avistamiento reportado para {mascota.NombreMascota} en {ubicacionAvistamiento}", null);

                // Enviar email al propietario si existe
                if (mascota.UsuarioPropietario > 0 && !string.IsNullOrEmpty(mascota.ContactoEmail))
                {
                    var propietario = db.Usuarios.Find(mascota.UsuarioPropietario);
                    if (propietario != null)
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
                                <li>Fecha: {(DateTime.TryParse(fechaAvistamiento, out DateTime fecha) ? fecha.ToString("dd/MM/yyyy HH:mm") : "No especificada")}</li>
                                <li>Observaciones: {observaciones}</li>
                            </ul>
                            <p>Te recomendamos contactar al reportante lo antes posible.</p>
                            <br/>
                            <p>¡Esperamos que encuentres a {mascota.NombreMascota} pronto!</p>
                        ";

                        _ = EmailHelper.SendNotificationAsync(propietario.Email, subject, body);
                    }
                }

                TempData["SuccessMessage"] = "¡Gracias por reportar el avistamiento! Hemos notificado al dueño.";
                return RedirectToAction("DetalleMascotaPerdida", new { id });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Reportar Avistamiento POST", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al registrar el avistamiento. Intenta nuevamente.";
                return RedirectToAction("ReportarAvistamiento", new { id });
            }
        }

        // GET: Rescate/GestionarReportes - Para administradores/rescatistas
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult GestionarReportes(string estado, string urgencia, int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Gestionar Reportes", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar reportes para gestión";
                return View(new List<ReportesRescate>());
            }
        }

        // GET: Rescate/GestionarMascotasPerdidas - Para administradores/rescatistas
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult GestionarMascotasPerdidas(string estado = null, string especie = null, int page = 1)
        {
            try
            {
                int pageSize = 20;
                var query = db.MascotasPerdidas.AsQueryable();

                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                {
                    query = query.Where(m => m.Estado == estado);
                }

                if (!string.IsNullOrEmpty(especie) && especie != "Todos")
                {
                    query = query.Where(m => m.Especie == especie);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var mascotas = query
                    .OrderByDescending(m => m.FechaPublicacion)
                    .ThenByDescending(m => m.FechaPerdida)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.EstadoSeleccionado = estado;
                ViewBag.EspecieSeleccionada = especie;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(mascotas);
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Gestionar Mascotas Perdidas", "Rescate",
                    $"Error: {ex.Message}", null);
                TempData["ErrorMessage"] = "Error al cargar mascotas perdidas para gestión";
                return View(new List<MascotasPerdidas>());
            }
        }

        // =============================================
        // MÉTODOS AUXILIARES
        // =============================================

        // Método auxiliar para obtener ID de usuario
        private int? GetUsuarioId()
        {
            if (!User.Identity.IsAuthenticated) return null;

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && (u.Activo == null || u.Activo == true));

            return usuario?.UsuarioId;
        }

        // Método para obtener estadísticas (puede ser usado por AJAX)
        [AllowAnonymous]
        [HttpPost]
        public JsonResult ObtenerEstadisticasRescate()
        {
            try
            {
                var estadisticas = new
                {
                    TotalRescates = db.ReportesRescate.Count(),
                    RescatesPendientes = db.ReportesRescate.Count(r => r.Estado == "Pendiente"),
                    RescatesEnProceso = db.ReportesRescate.Count(r => r.Estado == "En proceso"),
                    RescatesCompletados = db.ReportesRescate.Count(r => r.Estado == "Rescatado"),
                    MascotasPerdidasActivas = db.MascotasPerdidas.Count(m => m.Estado == "Perdida"),
                    FechaActualizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };

                return Json(new { success = true, data = estadisticas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
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