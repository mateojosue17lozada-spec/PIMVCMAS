using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class VoluntariadoController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Voluntariado
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Estadísticas
            ViewBag.TotalVoluntarios = db.UsuariosRoles.Count(ur =>
                ur.Roles.NombreRol == "Voluntario" && ur.Usuarios.Activo == true);

            ViewBag.HorasTotales = db.HorasVoluntariado
                .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;

            ViewBag.ActividadesRealizadas = db.Actividades
                .Count(a => a.Estado == "Completada");

            // Próximas actividades
            var proximasActividades = db.Actividades
                .Where(a => a.Estado == "Programada" && a.FechaActividad > DateTime.Now)
                .OrderBy(a => a.FechaActividad)
                .Take(5)
                .ToList();

            ViewBag.ProximasActividades = proximasActividades;

            return View();
        }

        // GET: Voluntariado/Inscribirse
        [Authorize]
        public ActionResult Inscribirse()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Verificar si ya es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId &&
                         ur.Roles.NombreRol == "Voluntario");

            ViewBag.EsVoluntario = esVoluntario;

            return View();
        }

        // POST: Voluntariado/ConfirmarInscripcion
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarInscripcion(string motivacion, string disponibilidad, string habilidades)
        {
            if (string.IsNullOrEmpty(motivacion))
            {
                TempData["ErrorMessage"] = "Debe indicar su motivación para ser voluntario";
                return RedirectToAction("Inscribirse");
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Verificar si ya es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId &&
                         ur.Roles.NombreRol == "Voluntario");

            if (esVoluntario)
            {
                TempData["ErrorMessage"] = "Ya eres voluntario";
                return RedirectToAction("MisActividades");
            }

            // Asignar rol de voluntario
            var rolVoluntario = db.Roles.FirstOrDefault(r => r.NombreRol == "Voluntario");
            if (rolVoluntario != null)
            {
                db.UsuariosRoles.Add(new UsuariosRoles
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = rolVoluntario.RolId,
                    FechaAsignacion = DateTime.Now,
                    AsignadoPor = null // O el ID del administrador si lo hay
                });

                db.SaveChanges();
            }

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Inscripción Voluntario", "Voluntariado",
                $"Motivación: {motivacion}", usuario.UsuarioId);

            // Enviar email de confirmación
            string subject = "¡Bienvenido al equipo de voluntarios!";
            string body = $@"
                <h2>Hola {usuario.NombreCompleto}</h2>
                <p>¡Gracias por unirte a nuestro equipo de voluntarios!</p>
                <p>Tu ayuda es invaluable para el bienestar de nuestros animales.</p>
                <p>Podrás ver y apuntarte a actividades desde tu panel de voluntario.</p>
                <br/>
                <p>Saludos,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendEmailAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = "¡Bienvenido al equipo de voluntarios!";
            return RedirectToAction("MisActividades");
        }

        // GET: Voluntariado/Actividades
        [AllowAnonymous]
        public ActionResult Actividades(string tipo, int page = 1)
        {
            int pageSize = 12;
            var query = db.Actividades.Where(a => a.FechaActividad > DateTime.Now);

            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
            {
                query = query.Where(a => a.TipoActividad == tipo);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var actividades = query
                .OrderBy(a => a.FechaActividad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TipoSeleccionado = tipo;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // Tipos de actividades para el dropdown
            ViewBag.TiposActividades = db.Actividades
                .Select(a => a.TipoActividad)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return View(actividades);
        }

        // GET: Voluntariado/DetalleActividad/5
        [AllowAnonymous]
        public ActionResult DetalleActividad(int id)
        {
            var actividad = db.Actividades.Find(id);

            if (actividad == null)
            {
                return HttpNotFound();
            }

            // Verificar si el usuario está inscrito (si está autenticado)
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name && u.Activo == true);

                if (usuario != null)
                {
                    ViewBag.EstaInscrito = db.InscripcionesActividades
                        .Any(i => i.ActividadId == id &&
                                 i.UsuarioId == usuario.UsuarioId);
                }
            }

            // Contar inscritos
            ViewBag.Inscritos = db.InscripcionesActividades
                .Count(i => i.ActividadId == id && i.Estado == "Confirmada");

            ViewBag.CuposDisponibles = (actividad.CupoMaximo ?? 0) - ViewBag.Inscritos;

            return View(actividad);
        }

        // POST: Voluntariado/InscribirseActividad
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult InscribirseActividad(int actividadId)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            var actividad = db.Actividades.Find(actividadId);

            if (actividad == null)
            {
                return Json(new { success = false, message = "Actividad no encontrada" });
            }

            // Verificar cupos
            var inscritos = db.InscripcionesActividades
                .Count(i => i.ActividadId == actividadId && i.Estado == "Confirmada");

            if (actividad.CupoMaximo.HasValue && inscritos >= actividad.CupoMaximo.Value)
            {
                return Json(new { success = false, message = "No hay cupos disponibles" });
            }

            // Verificar si ya está inscrito
            var yaInscrito = db.InscripcionesActividades
                .Any(i => i.ActividadId == actividadId && i.UsuarioId == usuario.UsuarioId);

            if (yaInscrito)
            {
                return Json(new { success = false, message = "Ya estás inscrito en esta actividad" });
            }

            // Crear inscripción
            var inscripcion = new InscripcionesActividades
            {
                ActividadId = actividadId,
                UsuarioId = usuario.UsuarioId,
                FechaInscripcion = DateTime.Now,
                Estado = "Confirmada"
            };

            db.InscripcionesActividades.Add(inscripcion);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Inscripción Actividad", "Voluntariado",
                $"Actividad: {actividad.NombreActividad}", usuario.UsuarioId);

            // Enviar email de confirmación
            string subject = "Confirmación de inscripción a actividad";
            string body = $@"
                <h2>Hola {usuario.NombreCompleto}</h2>
                <p>Tu inscripción a la actividad <strong>{actividad.NombreActividad}</strong> ha sido confirmada.</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Fecha: {actividad.FechaActividad:dd/MM/yyyy}</li>
                    <li>Hora: {actividad.HoraInicio:HH:mm} - {actividad.HoraFin:HH:mm}</li>
                    <li>Lugar: {actividad.LugarActividad}</li>
                    <li>Requisitos: {actividad.Requisitos ?? "Ninguno"}</li>
                </ul>
                <p>¡Te esperamos!</p>
                <br/>
                <p>Saludos,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendEmailAsync(usuario.Email, subject, body);

            return Json(new { success = true, message = "Inscripción exitosa" });
        }

        // POST: Voluntariado/CancelarInscripcion
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarInscripcion(int actividadId)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            var inscripcion = db.InscripcionesActividades
                .FirstOrDefault(i => i.ActividadId == actividadId &&
                                   i.UsuarioId == usuario.UsuarioId);

            if (inscripcion == null)
            {
                return Json(new { success = false, message = "No estás inscrito en esta actividad" });
            }

            inscripcion.Estado = "Cancelada";
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cancelar Inscripción Actividad", "Voluntariado",
                $"Actividad ID: {actividadId}", usuario.UsuarioId);

            return Json(new { success = true, message = "Inscripción cancelada" });
        }

        // GET: Voluntariado/MisActividades
        [Authorize]
        public ActionResult MisActividades()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Verificar si es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId &&
                         ur.Roles.NombreRol == "Voluntario");

            if (!esVoluntario)
            {
                TempData["InfoMessage"] = "Debes inscribirte como voluntario primero";
                return RedirectToAction("Inscribirse");
            }

            // Actividades próximas
            var actividadesProximas = db.InscripcionesActividades
                .Include(i => i.Actividades)
                .Where(i => i.UsuarioId == usuario.UsuarioId &&
                           i.Actividades.FechaActividad > DateTime.Now &&
                           i.Estado == "Confirmada")
                .OrderBy(i => i.Actividades.FechaActividad)
                .ToList();

            ViewBag.ActividadesProximas = actividadesProximas;

            // Historial de actividades
            var historialActividades = db.InscripcionesActividades
                .Include(i => i.Actividades)
                .Where(i => i.UsuarioId == usuario.UsuarioId &&
                           i.Actividades.FechaActividad <= DateTime.Now)
                .OrderByDescending(i => i.Actividades.FechaActividad)
                .Take(10)
                .ToList();

            ViewBag.HistorialActividades = historialActividades;

            // Horas totales - CORREGIDO: UsuarioId, no VoluntarioId
            var horasTotales = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId)
                .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;

            ViewBag.HorasTotales = horasTotales;

            return View();
        }

        // GET: Voluntariado/MisHoras
        [Authorize]
        public ActionResult MisHoras()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var horasRegistradas = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(h => h.FechaActividad)
                .ToList();

            var horasTotales = horasRegistradas.Sum(h => h.HorasTrabajadas);
            ViewBag.HorasTotales = horasTotales;

            // Horas validadas vs pendientes
            ViewBag.HorasValidadas = horasRegistradas
                .Where(h => h.ValidadoPor != null)
                .Sum(h => h.HorasTrabajadas);

            ViewBag.HorasPendientes = horasRegistradas
                .Where(h => h.ValidadoPor == null)
                .Sum(h => h.HorasTrabajadas);

            return View(horasRegistradas);
        }

        // GET: Voluntariado/RegistrarHoras
        [Authorize]
        public ActionResult RegistrarHoras()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Verificar si es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId &&
                         ur.Roles.NombreRol == "Voluntario");

            if (!esVoluntario)
            {
                TempData["ErrorMessage"] = "Solo los voluntarios pueden registrar horas";
                return RedirectToAction("Inscribirse");
            }

            // Actividades en las que participó
            var actividades = db.InscripcionesActividades
                .Include(i => i.Actividades)
                .Where(i => i.UsuarioId == usuario.UsuarioId &&
                           i.Actividades.FechaActividad <= DateTime.Now &&
                           i.Estado == "Confirmada")
                .Select(i => new {
                    i.ActividadId,
                    i.Actividades.NombreActividad,
                    i.Actividades.FechaActividad
                })
                .ToList();

            ViewBag.Actividades = new SelectList(actividades, "ActividadId", "NombreActividad");

            return View();
        }

        // POST: Voluntariado/RegistrarHoras
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarHoras(int actividadId, decimal horasTrabajadas,
            string descripcion, DateTime fechaActividad) // CORREGIDO: DateTime, no DateTime?
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            if (horasTrabajadas <= 0 || horasTrabajadas > 24)
            {
                TempData["ErrorMessage"] = "Las horas trabajadas deben estar entre 0 y 24";
                return RedirectToAction("RegistrarHoras");
            }

            // Verificar que la actividad existe y el usuario participó
            var participoActividad = db.InscripcionesActividades
                .Any(i => i.ActividadId == actividadId &&
                         i.UsuarioId == usuario.UsuarioId &&
                         i.Estado == "Confirmada");

            if (!participoActividad && actividadId != 0)
            {
                TempData["ErrorMessage"] = "No participaste en esta actividad";
                return RedirectToAction("RegistrarHoras");
            }

            var horas = new HorasVoluntariado
            {
                UsuarioId = usuario.UsuarioId,
                ActividadId = actividadId != 0 ? actividadId : (int?)null,
                FechaActividad = fechaActividad, // CORREGIDO: no es nullable
                HorasTrabajadas = horasTrabajadas,
                TipoActividad = actividadId != 0 ? null : "Otra actividad",
                Descripcion = descripcion,
                ValidadoPor = null, // Pendiente de validación
                FechaValidacion = null
            };

            db.HorasVoluntariado.Add(horas);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Registrar Horas", "Voluntariado",
                $"Horas: {horasTrabajadas}, Descripción: {descripcion}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Horas registradas exitosamente. Pendiente de validación.";
            return RedirectToAction("MisHoras");
        }

        // GET: Voluntariado/ReporteVoluntariado
        [Authorize]
        public ActionResult ReporteVoluntariado()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Horas por mes - CORREGIDO: FechaActividad es DateTime (no nullable)
            var horasPorMes = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId)
                .GroupBy(h => new { h.FechaActividad.Year, h.FechaActividad.Month })
                .Select(g => new {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Horas = g.Sum(h => h.HorasTrabajadas),
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Anio)
                .ThenByDescending(x => x.Mes)
                .Take(12)
                .ToList();

            // Actividades más frecuentes - CORREGIDO
            var actividadesFrecuentes = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId && h.ActividadId != null)
                .GroupBy(h => h.Actividades.NombreActividad)
                .Select(g => new {
                    Actividad = g.Key,
                    Horas = g.Sum(h => h.HorasTrabajadas),
                    Veces = g.Count()
                })
                .OrderByDescending(x => x.Horas)
                .Take(10)
                .ToList();

            ViewBag.HorasPorMes = horasPorMes;
            ViewBag.ActividadesFrecuentes = actividadesFrecuentes;
            ViewBag.TotalHoras = horasPorMes.Sum(h => h.Horas);
            ViewBag.TotalMeses = horasPorMes.Count;
            ViewBag.PromedioMensual = horasPorMes.Any() ?
                horasPorMes.Average(h => h.Horas) : 0;

            // Horas validadas vs pendientes
            ViewBag.HorasValidadas = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId && h.ValidadoPor != null)
                .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;

            ViewBag.HorasPendientes = db.HorasVoluntariado
                .Where(h => h.UsuarioId == usuario.UsuarioId && h.ValidadoPor == null)
                .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;

            return View();
        }

        // GET: Voluntariado/GestionarHoras (solo administradores)
        [AuthorizeRoles("Administrador")]
        public ActionResult GestionarHoras(int? usuarioId, string estado, int page = 1)
        {
            int pageSize = 20;
            var query = db.HorasVoluntariado.AsQueryable();

            if (usuarioId.HasValue)
            {
                query = query.Where(h => h.UsuarioId == usuarioId.Value);
            }

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                if (estado == "Validadas")
                    query = query.Where(h => h.ValidadoPor != null);
                else if (estado == "Pendientes")
                    query = query.Where(h => h.ValidadoPor == null);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var horas = query
                .Include(h => h.Usuarios)
                .Include(h => h.Actividades)
                .Include(h => h.Usuarios1) // ValidadoPor
                .OrderByDescending(h => h.FechaActividad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Usuarios = db.UsuariosRoles
                .Where(ur => ur.Roles.NombreRol == "Voluntario" && ur.Usuarios.Activo == true)
                .Select(ur => ur.Usuarios)
                .OrderBy(u => u.NombreCompleto)
                .ToList();

            ViewBag.UsuarioId = usuarioId;
            ViewBag.Estado = estado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(horas);
        }

        // POST: Voluntariado/ValidarHoras
        [HttpPost]
        [AuthorizeRoles("Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult ValidarHoras(int horasId)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var horas = db.HorasVoluntariado.Find(horasId);

            if (horas == null)
            {
                return Json(new { success = false, message = "Registro de horas no encontrado" });
            }

            horas.ValidadoPor = usuario.UsuarioId;
            horas.FechaValidacion = DateTime.Now;
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Validar Horas", "Voluntariado",
                $"Horas ID: {horasId}", usuario.UsuarioId);

            return Json(new { success = true, message = "Horas validadas exitosamente" });
        }

        // POST: Voluntariado/EliminarHoras
        [HttpPost]
        [AuthorizeRoles("Administrador")]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarHoras(int horasId)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var horas = db.HorasVoluntariado.Find(horasId);

            if (horas == null)
            {
                return Json(new { success = false, message = "Registro de horas no encontrado" });
            }

            db.HorasVoluntariado.Remove(horas);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Eliminar Horas", "Voluntariado",
                $"Horas ID: {horasId}", usuario.UsuarioId);

            return Json(new { success = true, message = "Horas eliminadas exitosamente" });
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