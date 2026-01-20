using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Controllers
{
    public class VoluntariadoController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Voluntariado
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Estadísticas
            ViewBag.TotalVoluntarios = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Voluntario");
            ViewBag.HorasTotales = db.HorasVoluntariado.Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;
            ViewBag.ActividadesRealizadas = db.Actividades.Count(a => a.Estado == "Completada");

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
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Verificar si ya es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId && ur.Roles.NombreRol == "Voluntario");

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

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Verificar si ya es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId && ur.Roles.NombreRol == "Voluntario");

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
                    FechaAsignacion = DateTime.Now
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

            return View(actividades);
        }

        // POST: Voluntariado/InscribirseActividad
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult InscribirseActividad(int actividadId)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            var actividad = db.Actividades.Find(actividadId);

            if (actividad == null)
            {
                return Json(new { success = false, message = "Actividad no encontrada" });
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

            return Json(new { success = true, message = "Inscripción exitosa" });
        }

        // GET: Voluntariado/MisActividades
        [Authorize]
        public ActionResult MisActividades()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Verificar si es voluntario
            var esVoluntario = db.UsuariosRoles
                .Any(ur => ur.UsuarioId == usuario.UsuarioId && ur.Roles.NombreRol == "Voluntario");

            if (!esVoluntario)
            {
                TempData["InfoMessage"] = "Debes inscribirte como voluntario primero";
                return RedirectToAction("Inscribirse");
            }

            // Actividades próximas
            var actividadesProximas = db.InscripcionesActividades
                .Where(i => i.UsuarioId == usuario.UsuarioId &&
                           i.Actividades.FechaActividad > DateTime.Now &&
                           i.Estado == "Confirmada")
                .OrderBy(i => i.Actividades.FechaActividad)
                .ToList();

            ViewBag.ActividadesProximas = actividadesProximas;

            // Historial de actividades
            var historialActividades = db.InscripcionesActividades
                .Where(i => i.UsuarioId == usuario.UsuarioId &&
                           i.Actividades.FechaActividad <= DateTime.Now)
                .OrderByDescending(i => i.Actividades.FechaActividad)
                .Take(10)
                .ToList();

            ViewBag.HistorialActividades = historialActividades;

            // Horas totales
            var horasTotales = db.HorasVoluntariado
                .Where(h => h.VoluntarioId == usuario.UsuarioId)
                .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0;

            ViewBag.HorasTotales = horasTotales;

            return View();
        }

        // GET: Voluntariado/MisHoras
        [Authorize]
        public ActionResult MisHoras()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var horasRegistradas = db.HorasVoluntariado
                .Where(h => h.VoluntarioId == usuario.UsuarioId)
                .OrderByDescending(h => h.Fecha)
                .ToList();

            var horasTotales = horasRegistradas.Sum(h => h.HorasTrabajadas);
            ViewBag.HorasTotales = horasTotales;

            return View(horasRegistradas);
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
