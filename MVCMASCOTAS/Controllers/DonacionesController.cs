using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    public class DonacionesController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Donaciones
        [AllowAnonymous]
        public ActionResult Index()
        {
            // Estadísticas
            ViewBag.TotalDonaciones = db.Donaciones.Count();
            ViewBag.MontoTotalRecaudado = db.Donaciones.Sum(d => d.MontoEfectivo) ?? 0;
            ViewBag.DonacionesEsteMes = db.Donaciones
                .Count(d => d.FechaDonacion.Month == DateTime.Now.Month &&
                           d.FechaDonacion.Year == DateTime.Now.Year);

            // Últimas donaciones (anónimas)
            var ultimasDonaciones = db.Donaciones
                .Where(d => d.PublicarEnWeb)
                .OrderByDescending(d => d.FechaDonacion)
                .Take(10)
                .Select(d => new
                {
                    Donante = d.AnonimatoDonante ? "Anónimo" : d.NombreDonante,
                    Tipo = d.TipoDonacion,
                    Fecha = d.FechaDonacion
                })
                .ToList();

            ViewBag.UltimasDonaciones = ultimasDonaciones;

            return View();
        }

        // GET: Donaciones/Donar
        [AllowAnonymous]
        public ActionResult Donar()
        {
            var model = new DonacionViewModel();

            // Si está autenticado, prellenar datos
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuario != null)
                {
                    model.NombreDonante = usuario.NombreCompleto;
                    model.EmailDonante = usuario.Email;
                    model.TelefonoDonante = usuario.Telefono;
                }
            }

            return View(model);
        }

        // POST: Donaciones/Donar
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Donar(DonacionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validaciones
            if (model.TipoDonacion == "Monetaria" && (!model.MontoEfectivo.HasValue || model.MontoEfectivo <= 0))
            {
                ModelState.AddModelError("MontoEfectivo", "Debe ingresar un monto válido");
                return View(model);
            }

            if (model.TipoDonacion == "Especie" && string.IsNullOrEmpty(model.DescripcionDonacion))
            {
                ModelState.AddModelError("DescripcionDonacion", "Debe describir la donación en especie");
                return View(model);
            }

            // Obtener usuario si está autenticado
            int? usuarioId = null;
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                usuarioId = usuario?.UsuarioId;
            }

            // Crear donación
            var donacion = new Donaciones
            {
                UsuarioId = usuarioId,
                TipoDonacion = model.TipoDonacion,
                MontoEfectivo = model.MontoEfectivo,
                DescripcionDonacion = model.DescripcionDonacion,
                MetodoPago = model.MetodoPago,
                NombreDonante = model.NombreDonante,
                EmailDonante = model.EmailDonante,
                TelefonoDonante = model.TelefonoDonante,
                AnonimatoDonante = model.AnonimatoDonante,
                PublicarEnWeb = model.PublicarEnWeb,
                FechaDonacion = DateTime.Now,
                EstadoDonacion = "Pendiente"
            };

            db.Donaciones.Add(donacion);
            db.SaveChanges();

            // Si es monetaria, registrar en contabilidad
            if (model.TipoDonacion == "Monetaria" && model.MontoEfectivo.HasValue)
            {
                var movimiento = new MovimientosContables
                {
                    TipoMovimiento = "Ingreso",
                    Categoria = "Donaciones",
                    Monto = model.MontoEfectivo.Value,
                    Descripcion = $"Donación de {model.NombreDonante ?? "Anónimo"}",
                    FechaMovimiento = DateTime.Now,
                    MetodoPago = model.MetodoPago
                };
                db.MovimientosContables.Add(movimiento);
                db.SaveChanges();
            }

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Donar", "Donaciones",
                $"Nueva donación: {model.TipoDonacion}, Monto: ${model.MontoEfectivo ?? 0}", usuarioId);

            // Enviar email de agradecimiento
            if (!string.IsNullOrEmpty(model.EmailDonante))
            {
                string subject = "¡Gracias por tu donación!";
                string body = $@"
                    <h2>Estimado/a {model.NombreDonante}</h2>
                    <p>Queremos agradecerte sinceramente por tu generosa donación al Refugio de Animales Quito.</p>
                    <p><strong>Detalles de tu donación:</strong></p>
                    <ul>
                        <li>Tipo: {model.TipoDonacion}</li>
                        {(model.MontoEfectivo.HasValue ? $"<li>Monto: ${model.MontoEfectivo.Value:N2}</li>" : "")}
                        <li>Fecha: {DateTime.Now:dd/MM/yyyy}</li>
                    </ul>
                    <p>Tu apoyo nos permite continuar rescatando y cuidando animales necesitados.</p>
                    <br/>
                    <p>Con gratitud,<br/>Equipo del Refugio de Animales Quito</p>
                ";

                _ = EmailHelper.SendEmailAsync(model.EmailDonante, subject, body);
            }

            TempData["SuccessMessage"] = "¡Gracias por tu donación! Tu apoyo es muy importante para nosotros.";
            return RedirectToAction("Index");
        }

        // GET: Donaciones/MisDonaciones
        [Authorize]
        public ActionResult MisDonaciones()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var donaciones = db.Donaciones
                .Where(d => d.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();

            return View(donaciones);
        }

        // GET: Donaciones/Apadrinar
        [Authorize]
        public ActionResult Apadrinar()
        {
            // Mascotas disponibles para apadrinamiento
            var mascotas = db.Mascotas
                .Where(m => m.Activo && (m.Estado == "Rescatada" || m.Estado == "En tratamiento" || m.Estado == "Disponible para adopción"))
                .OrderBy(m => m.Nombre)
                .ToList();

            return View(mascotas);
        }

        // POST: Donaciones/ConfirmarApadrinamiento
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarApadrinamiento(int mascotaId, decimal montoMensual)
        {
            if (montoMensual < 10)
            {
                TempData["ErrorMessage"] = "El monto mínimo de apadrinamiento es $10/mes";
                return RedirectToAction("Apadrinar");
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            // Verificar si ya apadrina esta mascota
            var apadrinamientoExistente = db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId && a.PadrinoId == usuario.UsuarioId && a.Estado == "Activo");

            if (apadrinamientoExistente)
            {
                TempData["ErrorMessage"] = "Ya estás apadrinando a esta mascota";
                return RedirectToAction("MisApadrinamientos");
            }

            // Crear apadrinamiento
            var apadrinamiento = new Apadrinamientos
            {
                MascotaId = mascotaId,
                PadrinoId = usuario.UsuarioId,
                MontoMensual = montoMensual,
                FechaInicio = DateTime.Now,
                Estado = "Activo"
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Apadrinar", "Donaciones",
                $"Mascota: {mascota.Nombre}, Monto mensual: ${montoMensual}", usuario.UsuarioId);

            TempData["SuccessMessage"] = $"¡Gracias por apadrinar a {mascota.Nombre}! Recibirás actuaciones periódicas.";
            return RedirectToAction("MisApadrinamientos");
        }

        // GET: Donaciones/MisApadrinamientos
        [Authorize]
        public ActionResult MisApadrinamientos()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var apadrinamientos = db.Apadrinamientos
                .Where(a => a.PadrinoId == usuario.UsuarioId)
                .OrderByDescending(a => a.FechaInicio)
                .ToList();

            return View(apadrinamientos);
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
