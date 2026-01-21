using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace MVCMASCOTAS.Controllers
{
    public class DonacionesController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Donaciones
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                // Estadísticas - CORREGIDO
                ViewBag.TotalDonaciones = db.Donaciones.Count();
                ViewBag.MontoTotalRecaudado = db.Donaciones.Sum(d => (decimal?)d.Monto) ?? 0;

                // CORREGIDO: Verificar que FechaDonacion no sea null
                ViewBag.DonacionesEsteMes = db.Donaciones
                    .Count(d => d.FechaDonacion.HasValue &&
                               d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                               d.FechaDonacion.Value.Year == DateTime.Now.Year);

                // Últimas donaciones (las públicas)
                var ultimasDonaciones = db.Donaciones
                    .Where(d => !d.Anonima.HasValue || d.Anonima == false) // No anónimas
                    .OrderByDescending(d => d.FechaDonacion)
                    .Take(10)
                    .Select(d => new
                    {
                        Donante = d.Anonima == true ? "Anónimo" : d.Usuarios.NombreCompleto ?? "Donante",
                        Tipo = d.TipoDonacion,
                        Monto = d.Monto,
                        Fecha = d.FechaDonacion
                    })
                    .ToList();

                ViewBag.UltimasDonaciones = ultimasDonaciones;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar estadísticas: " + ex.Message;
                return View();
            }
        }

        // GET: Donaciones/Donar
        [AllowAnonymous]
        public ActionResult Donar()
        {
            var model = new DonacionViewModel();

            // Si está autenticado, prellenar datos
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                if (usuario != null)
                {
                    model.NombreCompleto = usuario.NombreCompleto;
                    model.Email = usuario.Email;
                    model.Telefono = usuario.Telefono;
                }
            }

            ViewBag.TiposDonacion = new SelectList(new[] { "Única", "Recurrente" });
            ViewBag.MetodosPago = new SelectList(new[] {
                "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                "Tarjeta de Débito", "Cheque", "Depósito"
            });

            return View(model);
        }

        // POST: Donaciones/Donar
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Donar(DonacionViewModel model)
        {
            ViewBag.TiposDonacion = new SelectList(new[] { "Única", "Recurrente" });
            ViewBag.MetodosPago = new SelectList(new[] {
                "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                "Tarjeta de Débito", "Cheque", "Depósito"
            });

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validaciones
            if (model.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "Debe ingresar un monto válido");
                return View(model);
            }

            // Obtener usuario si está autenticado
            int? usuarioId = null;
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                usuarioId = usuario?.UsuarioId;
            }

            // Crear donación - CORREGIDO según estructura real de la BD
            var donacion = new Donaciones
            {
                UsuarioId = usuarioId,
                TipoDonacion = model.TipoDonacion,
                Monto = model.Monto,  // CORRECTO: es Monto, no MontoEfectivo
                Frecuencia = model.TipoDonacion == "Recurrente" ? "Mensual" : null,
                FechaDonacion = DateTime.Now,
                MetodoPago = model.MetodoPago,
                Estado = "Completada",  // CORRECTO: es Estado, no EstadoDonacion
                Anonima = model.Anonima,  // CORRECTO: es Anonima, no AnonimatoDonante
                Mensaje = !string.IsNullOrEmpty(model.Mensaje) ? model.Mensaje : null,
                NumeroTransaccion = !string.IsNullOrEmpty(model.NumeroTransaccion) ? model.NumeroTransaccion : null
            };

            db.Donaciones.Add(donacion);
            db.SaveChanges();

            // Registrar en contabilidad
            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            int responsableId = usuarioActual?.UsuarioId ?? 0;

            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Donaciones",
                Monto = model.Monto,
                Concepto = $"Donación {(model.Anonima ? "Anónima" : $"de {model.NombreCompleto}")}", // CORRECTO: es Concepto, no Descripcion
                FechaMovimiento = DateTime.Now,
                MetodoPago = model.MetodoPago,
                NumeroComprobante = model.NumeroTransaccion,
                ResponsableRegistro = responsableId,
                Observaciones = model.Mensaje
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Donar", "Donaciones",
                $"Nueva donación: {model.TipoDonacion}, Monto: ${model.Monto}", usuarioId);

            // Enviar email de agradecimiento
            if (!string.IsNullOrEmpty(model.Email))
            {
                string subject = "¡Gracias por tu donación!";
                string body = $@"
                    <h2>Estimado/a {model.NombreCompleto}</h2>
                    <p>Queremos agradecerte sinceramente por tu generosa donación al Refugio de Animales Quito.</p>
                    <p><strong>Detalles de tu donación:</strong></p>
                    <ul>
                        <li>Tipo: {model.TipoDonacion}</li>
                        <li>Monto: ${model.Monto:N2}</li>
                        <li>Fecha: {DateTime.Now:dd/MM/yyyy}</li>
                        {(model.TipoDonacion == "Recurrente" ? "<li>Frecuencia: Mensual</li>" : "")}
                    </ul>
                    <p>Tu apoyo nos permite continuar rescatando y cuidando animales necesitados.</p>
                    <br/>
                    <p>Con gratitud,<br/>Equipo del Refugio de Animales Quito</p>
                ";

                _ = EmailHelper.SendEmailAsync(model.Email, subject, body);
            }

            TempData["SuccessMessage"] = "¡Gracias por tu donación! Tu apoyo es muy importante para nosotros.";
            return RedirectToAction("Index");
        }

        // GET: Donaciones/MisDonaciones
        [Authorize]
        public ActionResult MisDonaciones()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var donaciones = db.Donaciones
                .Where(d => d.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();

            ViewBag.TotalDonado = donaciones.Sum(d => d.Monto);
            ViewBag.DonacionesRecurrentes = donaciones.Count(d => d.TipoDonacion == "Recurrente");

            return View(donaciones);
        }

        // GET: Donaciones/Apadrinar
        [Authorize]
        public ActionResult Apadrinar()
        {
            // Mascotas disponibles para apadrinamiento - CORREGIDO: Activo puede ser null
            var mascotas = db.Mascotas
                .Where(m => m.Activo == true &&
                           (m.Estado == "Rescatada" ||
                            m.Estado == "En tratamiento" ||
                            m.Estado == "Disponible para adopción"))
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

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            // Verificar si ya apadrina esta mascota - CORREGIDO: UsuarioId, no PadrinoId
            var apadrinamientoExistente = db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId &&
                         a.UsuarioId == usuario.UsuarioId &&
                         a.Estado == "Activo");

            if (apadrinamientoExistente)
            {
                TempData["ErrorMessage"] = "Ya estás apadrinando a esta mascota";
                return RedirectToAction("MisApadrinamientos");
            }

            // Crear apadrinamiento - CORREGIDO según estructura real
            var apadrinamiento = new Apadrinamientos
            {
                MascotaId = mascotaId,
                UsuarioId = usuario.UsuarioId,  // CORRECTO: es UsuarioId, no PadrinoId
                MontoMensual = montoMensual,
                FechaInicio = DateTime.Now,
                Estado = "Activo",
                DiaCobroMensual = DateTime.Now.Day,
                MetodoPagoPreferido = "Transferencia Bancaria"
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            // Registrar primer pago
            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamiento.ApadrinamientoId,
                Monto = montoMensual,
                FechaPago = DateTime.Now,
                MesPagado = DateTime.Now.ToString("MM/yyyy"),
                MetodoPago = "Transferencia Bancaria",
                Estado = "Completado"
            };
            db.PagosApadrinamiento.Add(pago);
            db.SaveChanges();

            // Registrar en contabilidad
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Apadrinamientos",
                Monto = montoMensual,
                Concepto = $"Apadrinamiento inicial - Mascota: {mascota.Nombre}, Padrino: {usuario.NombreCompleto}",
                FechaMovimiento = DateTime.Now,
                MetodoPago = "Transferencia Bancaria",
                ResponsableRegistro = usuario.UsuarioId,
                Observaciones = "Apadrinamiento inicial"
            };
            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Apadrinar", "Donaciones",
                $"Mascota: {mascota.Nombre}, Monto mensual: ${montoMensual}", usuario.UsuarioId);

            // Enviar email de confirmación
            string subject = "¡Confirmación de Apadrinamiento!";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>¡Gracias por apadrinar a <strong>{mascota.Nombre}</strong>!</p>
                <p><strong>Detalles de tu apadrinamiento:</strong></p>
                <ul>
                    <li>Mascota: {mascota.Nombre} ({mascota.Especie})</li>
                    <li>Monto mensual: ${montoMensual:N2}</li>
                    <li>Fecha de inicio: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>Recibirás actualizaciones periódicas sobre {mascota.Nombre} y su bienestar.</p>
                <br/>
                <p>Con gratitud,<br/>Equipo del Refugio de Animales Quito</p>
            ";

            _ = EmailHelper.SendEmailAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"¡Gracias por apadrinar a {mascota.Nombre}! Recibirás actualizaciones periódicas.";
            return RedirectToAction("MisApadrinamientos");
        }

        // GET: Donaciones/MisApadrinamientos
        [Authorize]
        public ActionResult MisApadrinamientos()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var apadrinamientos = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .Include(a => a.PagosApadrinamiento)
                .Where(a => a.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(a => a.FechaInicio)
                .ToList();

            ViewBag.TotalMensual = apadrinamientos.Sum(a => a.MontoMensual);
            ViewBag.TotalPagado = apadrinamientos.SelectMany(a => a.PagosApadrinamiento).Sum(p => p.Monto);

            return View(apadrinamientos);
        }

        // GET: Donaciones/DetallesApadrinamiento/5
        [Authorize]
        public ActionResult DetallesApadrinamiento(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .Include(a => a.PagosApadrinamiento)
                .FirstOrDefault(a => a.ApadrinamientoId == id && a.UsuarioId == usuario.UsuarioId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            ViewBag.TotalPagado = apadrinamiento.PagosApadrinamiento.Sum(p => p.Monto);
            ViewBag.MesesApadrinados = apadrinamiento.PagosApadrinamiento.Count;

            return View(apadrinamiento);
        }

        // POST: Donaciones/CancelarApadrinamiento/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarApadrinamiento(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .FirstOrDefault(a => a.ApadrinamientoId == id && a.UsuarioId == usuario.UsuarioId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            if (apadrinamiento.Estado == "Cancelado")
            {
                TempData["ErrorMessage"] = "Este apadrinamiento ya está cancelado";
                return RedirectToAction("MisApadrinamientos");
            }

            apadrinamiento.Estado = "Cancelado";
            apadrinamiento.FechaFin = DateTime.Now;
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cancelar Apadrinamiento", "Donaciones",
                $"Mascota: {apadrinamiento.Mascotas?.Nombre}", usuario.UsuarioId);

            // Enviar email de cancelación
            string subject = "Confirmación de Cancelación de Apadrinamiento";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>Confirmamos la cancelación de tu apadrinamiento de <strong>{apadrinamiento.Mascotas?.Nombre}</strong>.</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Mascota: {apadrinamiento.Mascotas?.Nombre}</li>
                    <li>Fecha de inicio: {apadrinamiento.FechaInicio:dd/MM/yyyy}</li>
                    <li>Fecha de finalización: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>Agradecemos tu apoyo durante este tiempo. Esperamos contar contigo nuevamente en el futuro.</p>
                <br/>
                <p>Atentamente,<br/>Equipo del Refugio de Animales Quito</p>
            ";

            _ = EmailHelper.SendEmailAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"Apadrinamiento de {apadrinamiento.Mascotas?.Nombre} cancelado exitosamente.";
            return RedirectToAction("MisApadrinamientos");
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

    // ViewModel para Donaciones
    public class DonacionViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El tipo de donación es requerido")]
        [Display(Name = "Tipo de Donación")]
        public string TipoDonacion { get; set; }

        [Required(ErrorMessage = "El monto es requerido")]
        [Range(1, 10000, ErrorMessage = "El monto debe estar entre $1 y $10,000")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        [Display(Name = "Método de Pago")]
        public string MetodoPago { get; set; }

        [Display(Name = "Número de Transacción/Comprobante")]
        public string NumeroTransaccion { get; set; }

        [Display(Name = "Donación Anónima")]
        public bool Anonima { get; set; }

        [StringLength(500, ErrorMessage = "El mensaje no puede exceder 500 caracteres")]
        [Display(Name = "Mensaje (opcional)")]
        public string Mensaje { get; set; }
    }
}