using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class HomeController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                // Obtener mascotas disponibles para mostrar en el inicio
                var mascotasDisponibles = db.Mascotas
                    .Where(m => m.Estado == "Disponible para adopción" && m.Activo == true)
                    .OrderByDescending(m => m.FechaIngreso)
                    .Take(6)
                    .ToList();

                ViewBag.MascotasDisponibles = mascotasDisponibles;
                ViewBag.TotalMascotas = db.Mascotas.Count(m => m.Activo == true);
                ViewBag.MascotasAdoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada");
                ViewBag.VoluntariosActivos = db.Usuarios.Count(u => u.Activo == true);
                ViewBag.TotalDonaciones = db.Donaciones.Count(d => d.Estado == "Completada");

                // CORRECCIÓN: Obtener últimas donaciones de manera segura
                var ultimasDonaciones = db.Donaciones
                    .Where(d => d.Estado == "Completada")
                    .OrderByDescending(d => d.FechaDonacion)
                    .Take(5)
                    .ToList()
                    .Select(d =>
                    {
                        string nombreDonante = "Anónimo";

                        if (d.Anonima != true && d.UsuarioId.HasValue)
                        {
                            var usuario = db.Usuarios.Find(d.UsuarioId.Value);
                            if (usuario != null)
                            {
                                nombreDonante = usuario.NombreCompleto;
                            }
                        }

                        return new
                        {
                            Nombre = nombreDonante,
                            Tipo = d.TipoDonacion,
                            Fecha = d.FechaDonacion,
                            Monto = d.Monto
                        };
                    })
                    .ToList();

                ViewBag.UltimasDonaciones = ultimasDonaciones;

                return View();
            }
            catch (Exception ex)
            {
                // Si hay error con la base de datos, mostrar página básica
                ViewBag.Error = "Error cargando datos: " + ex.Message;
                return View();
            }
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Services()
        {
            return View();
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