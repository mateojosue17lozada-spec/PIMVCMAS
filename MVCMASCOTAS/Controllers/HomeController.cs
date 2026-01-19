using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Helpers;

namespace MVCMASCOTAS.Controllers
{
    public class HomeController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Home
        public ActionResult Index()
        {
            // Estadísticas para la página principal
            ViewBag.TotalMascotasDisponibles = db.Mascotas
                .Count(m => m.Estado == "Disponible para adopción" && m.Activo);

            ViewBag.TotalAdopciones = db.Mascotas
                .Count(m => m.Estado == "Adoptada");

            ViewBag.TotalVoluntarios = db.UsuariosRoles
                .Count(ur => ur.Roles.NombreRol == "Voluntario");

            // Mascotas destacadas (últimas 6 disponibles)
            var mascotasDestacadas = db.Mascotas
                .Where(m => m.Estado == "Disponible para adopción" && m.Activo)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(6)
                .ToList();

            ViewBag.MascotasDestacadas = mascotasDestacadas;

            // Próximas actividades
            var proximasActividades = db.Actividades
                .Where(a => a.Estado == "Programada" && a.FechaActividad > DateTime.Now)
                .OrderBy(a => a.FechaActividad)
                .Take(3)
                .ToList();

            ViewBag.ProximasActividades = proximasActividades;

            return View();
        }

        // GET: Home/About
        public ActionResult About()
        {
            ViewBag.Message = "Sobre el Refugio de Animales Quito";

            // Estadísticas generales
            ViewBag.TotalMascotasRescatadas = db.Mascotas.Count(m => m.Activo);
            ViewBag.TotalAdopciones = db.Mascotas.Count(m => m.Estado == "Adoptada");
            ViewBag.TotalDonaciones = db.Donaciones.Count();
            ViewBag.TotalApadrinamientos = db.Apadrinamientos.Count(a => a.Estado == "Activo");

            return View();
        }

        // GET: Home/Contact
        public ActionResult Contact()
        {
            ViewBag.Message = "Contáctanos";
            return View();
        }

        // POST: Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(string nombre, string email, string mensaje)
        {
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(mensaje))
            {
                TempData["ErrorMessage"] = "Todos los campos son requeridos.";
                return View();
            }

            // Enviar email al refugio
            string subject = $"Mensaje de contacto de {nombre}";
            string body = $@"
                <h3>Nuevo mensaje de contacto</h3>
                <p><strong>Nombre:</strong> {nombre}</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Mensaje:</strong></p>
                <p>{mensaje}</p>
            ";

            _ = EmailHelper.SendEmailAsync(
                System.Configuration.ConfigurationManager.AppSettings["EmailRefugio"],
                subject,
                body
            );

            TempData["SuccessMessage"] = "Tu mensaje ha sido enviado. Nos pondremos en contacto pronto.";
            return RedirectToAction("Contact");
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