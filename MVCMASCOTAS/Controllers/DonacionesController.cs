using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
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
                ViewBag.TotalDonaciones = db.Donaciones.Count();
                ViewBag.MontoTotalRecaudado = db.Donaciones.Sum(d => (decimal?)d.Monto) ?? 0;

                ViewBag.DonacionesEsteMes = db.Donaciones
                    .Count(d => d.FechaDonacion.HasValue &&
                               d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                               d.FechaDonacion.Value.Year == DateTime.Now.Year);

                // CORRECCIÓN: Cambiar para ser consistente con HomeController
                var ultimasDonaciones = db.Donaciones
                    .Where(d => d.Estado == "Completada")
                    .OrderByDescending(d => d.FechaDonacion)
                    .Take(10)
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

                        // Determinar tipo específico basado en el mensaje
                        string tipoEspecifico = d.TipoDonacion;
                        if (d.Mensaje != null && d.Mensaje.Contains("[Tipo Original:"))
                        {
                            // Extraer el tipo original del mensaje
                            var inicio = d.Mensaje.IndexOf("[Tipo Original:") + 15;
                            var fin = d.Mensaje.IndexOf("]", inicio);
                            if (fin > inicio)
                            {
                                tipoEspecifico = d.Mensaje.Substring(inicio, fin - inicio).Trim();
                            }
                        }
                        else if (d.Mensaje != null && d.Mensaje.Contains("[Apadrinamiento]"))
                        {
                            tipoEspecifico = "Apadrinamiento";
                        }

                        // CORRECCIÓN: Usar las mismas propiedades que el HomeController
                        return new
                        {
                            Donante = nombreDonante,  // Igual que HomeController
                            Tipo = tipoEspecifico,    // Mostrar tipo específico
                            Monto = d.Monto,          // Igual que HomeController
                            Fecha = d.FechaDonacion   // Igual que HomeController
                        };
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

            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                if (usuario != null)
                {
                    model.NombreDonante = usuario.NombreCompleto;
                    model.EmailDonante = usuario.Email;
                    model.TelefonoDonante = usuario.Telefono;
                }
            }

            // Cargar mascotas para apadrinamiento
            var mascotas = db.Mascotas
                .Where(m => m.Activo == true &&
                           (m.Estado == "Disponible para adopción" ||
                            m.Estado == "En tratamiento" ||
                            m.Estado == "Rescatada"))
                .Select(m => new {
                    m.MascotaId,
                    Nombre = m.Nombre + " (" + m.Especie + ")"
                })
                .ToList();

            ViewBag.Mascotas = new SelectList(mascotas, "MascotaId", "Nombre");

            return View(model);
        }

        // POST: Donaciones/Donar
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Donar(DonacionViewModel model)
        {
            // Cargar mascotas para si hay error
            var mascotas = db.Mascotas
                .Where(m => m.Activo == true)
                .Select(m => new {
                    m.MascotaId,
                    Nombre = m.Nombre + " (" + m.Especie + ")"
                })
                .ToList();

            ViewBag.Mascotas = new SelectList(mascotas, "MascotaId", "Nombre");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validaciones específicas
            if (model.TipoDonacion == "Monetaria" && model.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "Debe ingresar un monto válido para donaciones monetarias");
                return View(model);
            }

            if (model.TipoDonacion == "Apadrinamiento" && !model.MascotaId.HasValue)
            {
                ModelState.AddModelError("MascotaId", "Debe seleccionar una mascota para apadrinar");
                return View(model);
            }

            if (model.TipoDonacion == "Apadrinamiento" && model.Monto < 10)
            {
                ModelState.AddModelError("Monto", "El monto mínimo de apadrinamiento es $10/mes");
                return View(model);
            }

            // Obtener usuario si está autenticado
            int? usuarioId = null;
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                usuarioId = usuario?.UsuarioId;
            }

            // Crear donación con mapeo correcto para la BD
            var donacion = new Donaciones
            {
                UsuarioId = usuarioId,
                // CORREGIDO: Mapear los valores de la aplicación a los de la BD
                // "Monetaria" y "Apadrinamiento" son "Recurrente"
                // "Especie" es "Única"
                TipoDonacion = (model.TipoDonacion == "Apadrinamiento" || model.TipoDonacion == "Monetaria")
                                ? "Recurrente" : "Única",
                Monto = model.TipoDonacion == "Especie" ? 0 : model.Monto,
                Frecuencia = model.Frecuencia,
                FechaDonacion = DateTime.Now,
                MetodoPago = model.TipoDonacion == "Monetaria" || model.TipoDonacion == "Apadrinamiento" ? model.MetodoPago : null,
                Estado = "Pendiente",
                Anonima = model.Anonima,
                // Guardar el tipo original en el mensaje para poder identificarlo después
                Mensaje = $"[Tipo Original: {model.TipoDonacion}] " + (model.Mensaje ?? ""),
                NumeroTransaccion = model.NumeroTransaccion
            };

            try
            {
                db.Donaciones.Add(donacion);
                db.SaveChanges();

                // Si es apadrinamiento, crear registro adicional
                if (model.TipoDonacion == "Apadrinamiento" && model.MascotaId.HasValue)
                {
                    var apadrinamiento = new Apadrinamientos
                    {
                        MascotaId = model.MascotaId.Value,
                        UsuarioId = usuarioId ?? 0,
                        MontoMensual = model.Monto,
                        FechaInicio = DateTime.Now,
                        Estado = "Activo",
                        DiaCobroMensual = DateTime.Now.Day,
                        MetodoPagoPreferido = model.MetodoPago,
                        Observaciones = model.DescripcionArticulo ?? model.Mensaje
                    };

                    db.Apadrinamientos.Add(apadrinamiento);
                    db.SaveChanges();

                    // Registrar primer pago
                    var pago = new PagosApadrinamiento
                    {
                        ApadrinamientoId = apadrinamiento.ApadrinamientoId,
                        Monto = model.Monto,
                        FechaPago = DateTime.Now,
                        MesPagado = DateTime.Now.ToString("MM/yyyy"),
                        MetodoPago = model.MetodoPago,
                        Estado = "Completado"
                    };
                    db.PagosApadrinamiento.Add(pago);
                    db.SaveChanges();
                }

                // Registrar en contabilidad solo si es monetaria o apadrinamiento
                if ((model.TipoDonacion == "Monetaria" || model.TipoDonacion == "Apadrinamiento") && model.Monto > 0)
                {
                    var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                    int responsableId = usuarioActual?.UsuarioId ?? 0;

                    var movimiento = new MovimientosContables
                    {
                        TipoMovimiento = "Ingreso",
                        Categoria = model.TipoDonacion == "Apadrinamiento" ? "Apadrinamientos" : "Donaciones",
                        Monto = model.Monto,
                        Concepto = model.TipoDonacion == "Apadrinamiento"
                            ? $"Apadrinamiento - Mascota ID: {model.MascotaId}"
                            : $"Donación {(model.Anonima ? "Anónima" : $"de {model.NombreDonante}")}",
                        FechaMovimiento = DateTime.Now,
                        MetodoPago = model.MetodoPago,
                        NumeroComprobante = model.NumeroTransaccion,
                        ResponsableRegistro = responsableId,
                        Observaciones = model.Mensaje ?? model.DescripcionArticulo
                    };

                    db.MovimientosContables.Add(movimiento);
                    db.SaveChanges();
                }

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Donar", "Donaciones",
                    $"Tipo: {model.TipoDonacion}, Monto: ${model.Monto}", usuarioId);

                // Enviar email de agradecimiento
                if (!string.IsNullOrEmpty(model.EmailDonante))
                {
                    string subject = "¡Gracias por tu donación!";

                    string detalles = "";
                    if (model.TipoDonacion == "Apadrinamiento" && model.MascotaId.HasValue)
                    {
                        var mascota = db.Mascotas.Find(model.MascotaId.Value);
                        detalles = $"<li>Mascota apadrinada: {mascota?.Nombre ?? "No especificada"}</li>";
                    }
                    else if (model.TipoDonacion == "Monetaria")
                    {
                        detalles = $"<li>Monto: ${model.Monto:N2}</li>";
                    }
                    else if (model.TipoDonacion == "Especie")
                    {
                        detalles = $"<li>Artículo: {model.DescripcionArticulo}</li>";
                    }

                    string body = $@"
                        <h2>Estimado/a {model.NombreDonante}</h2>
                        <p>Queremos agradecerte sinceramente por tu generosa donación al Refugio de Animales Quito.</p>
                        <p><strong>Detalles de tu donación:</strong></p>
                        <ul>
                            <li>Tipo: {model.TipoDonacion}</li>
                            {detalles}
                            <li>Fecha: {DateTime.Now:dd/MM/yyyy}</li>
                            {(model.TipoDonacion == "Apadrinamiento" ? "<li>Recibirás actualizaciones mensuales sobre tu mascota apadrinada</li>" : "")}
                        </ul>
                        <p>Tu apoyo nos permite continuar rescatando y cuidando animales necesitados.</p>
                        <br/>
                        <p>Con gratitud,<br/>Equipo del Refugio de Animales Quito</p>
                    ";

                    _ = EmailHelper.SendNotificationAsync(model.EmailDonante, subject, body);
                }

                TempData["SuccessMessage"] = "¡Gracias por tu donación! Tu apoyo es muy importante para nosotros.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Capturar errores específicos de BD
                ModelState.AddModelError("", $"Error al guardar la donación: {ex.Message}");
                return View(model);
            }
        }

        // GET: Donaciones/MisDonaciones
        [Authorize]
        public ActionResult MisDonaciones()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var donaciones = db.Donaciones
                .Where(d => d.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();

            // Agregar información de tipo específico para la vista
            var donacionesConDetalle = donaciones.Select(d =>
            {
                // Determinar tipo específico
                string tipoEspecifico = d.TipoDonacion;
                string descripcion = d.Mensaje ?? "";

                if (d.Mensaje != null && d.Mensaje.Contains("[Tipo Original:"))
                {
                    var inicio = d.Mensaje.IndexOf("[Tipo Original:") + 15;
                    var fin = d.Mensaje.IndexOf("]", inicio);
                    if (fin > inicio)
                    {
                        tipoEspecifico = d.Mensaje.Substring(inicio, fin - inicio).Trim();
                        // Limpiar el mensaje (quitar la parte del tipo)
                        descripcion = d.Mensaje.Substring(fin + 1).Trim();
                    }
                }

                return new
                {
                    Donacion = d,
                    TipoEspecifico = tipoEspecifico,
                    DescripcionLimpia = descripcion
                };
            }).ToList();

            ViewBag.TotalDonado = donaciones.Sum(d => d.Monto);
            ViewBag.DonacionesRecurrentes = donaciones.Count(d => d.TipoDonacion == "Recurrente");
            ViewBag.DonacionesConDetalle = donacionesConDetalle;

            return View(donaciones);
        }

        // GET: Donaciones/Apadrinar
        [Authorize]
        public ActionResult Apadrinar()
        {
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
                return RedirectToAction("Login", "Account");
            }

            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            var apadrinamientoExistente = db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId &&
                         a.UsuarioId == usuario.UsuarioId &&
                         a.Estado == "Activo");

            if (apadrinamientoExistente)
            {
                TempData["ErrorMessage"] = "Ya estás apadrinando a esta mascota";
                return RedirectToAction("MisApadrinamientos");
            }

            var apadrinamiento = new Apadrinamientos
            {
                MascotaId = mascotaId,
                UsuarioId = usuario.UsuarioId,
                MontoMensual = montoMensual,
                FechaInicio = DateTime.Now,
                Estado = "Activo",
                DiaCobroMensual = DateTime.Now.Day,
                MetodoPagoPreferido = "Transferencia Bancaria"
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            // AGREGAR: Crear registro de donación también para apadrinamiento
            var donacion = new Donaciones
            {
                UsuarioId = usuario.UsuarioId,
                TipoDonacion = "Recurrente", // Apadrinamiento es recurrente
                Monto = montoMensual,
                Frecuencia = "Mensual",
                FechaDonacion = DateTime.Now,
                MetodoPago = "Transferencia Bancaria",
                Estado = "Completada",
                Anonima = false,
                Mensaje = $"[Apadrinamiento] {mascota.Nombre} - Monto mensual: ${montoMensual}"
            };

            db.Donaciones.Add(donacion);
            db.SaveChanges();

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

            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Apadrinamientos",
                Monto = montoMensual,
                Concepto = $"Apadrinamiento - {mascota.Nombre} por {usuario.NombreCompleto}",
                FechaMovimiento = DateTime.Now,
                MetodoPago = "Transferencia Bancaria",
                ResponsableRegistro = usuario.UsuarioId,
                Observaciones = "Apadrinamiento inicial"
            };
            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Apadrinar", "Donaciones",
                $"Mascota: {mascota.Nombre}, Monto: ${montoMensual}/mes", usuario.UsuarioId);

            string subject = "¡Confirmación de Apadrinamiento!";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>¡Gracias por apadrinar a <strong>{mascota.Nombre}</strong>!</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Mascota: {mascota.Nombre} ({mascota.Especie})</li>
                    <li>Monto mensual: ${montoMensual:N2}</li>
                    <li>Fecha inicio: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>Recibirás actualizaciones sobre {mascota.Nombre}.</p>
                <br/>
                <p>Con gratitud,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendNotificationAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"¡Gracias por apadrinar a {mascota.Nombre}!";
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
                return RedirectToAction("Login", "Account");
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
                return RedirectToAction("Login", "Account");
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

        // POST: Donaciones/SuspenderApadrinamiento/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SuspenderApadrinamiento(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .FirstOrDefault(a => a.ApadrinamientoId == id && a.UsuarioId == usuario.UsuarioId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            if (apadrinamiento.Estado == "Pausado")
            {
                TempData["ErrorMessage"] = "Este apadrinamiento ya está pausado";
                return RedirectToAction("MisApadrinamientos");
            }

            if (apadrinamiento.Estado == "Cancelado" || apadrinamiento.Estado == "Completado")
            {
                TempData["ErrorMessage"] = "No se puede pausar un apadrinamiento que está cancelado o completado";
                return RedirectToAction("MisApadrinamientos");
            }

            // Cambiar a "Pausado" en lugar de "Cancelado"
            apadrinamiento.Estado = "Pausado";
            apadrinamiento.Observaciones = apadrinamiento.Observaciones + " [Pausado el " + DateTime.Now.ToString("dd/MM/yyyy") + "]";
            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Suspender Apadrinamiento", "Donaciones",
                $"Mascota: {apadrinamiento.Mascotas?.Nombre}", usuario.UsuarioId);

            string subject = "Confirmación de Pausa de Apadrinamiento";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>Confirmamos la pausa del apadrinamiento de <strong>{apadrinamiento.Mascotas?.Nombre}</strong>.</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Mascota: {apadrinamiento.Mascotas?.Nombre}</li>
                    <li>Inicio: {apadrinamiento.FechaInicio:dd/MM/yyyy}</li>
                    <li>Pausado: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>Puedes reactivar este apadrinamiento en cualquier momento desde la sección de Mis Apadrinamientos.</p>
                <br/>
                <p>Atentamente,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendNotificationAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"Apadrinamiento pausado exitosamente.";
            return RedirectToAction("MisApadrinamientos");
        }

        // POST: Donaciones/ReactivarApadrinamiento/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ReactivarApadrinamiento(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .FirstOrDefault(a => a.ApadrinamientoId == id && a.UsuarioId == usuario.UsuarioId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            if (apadrinamiento.Estado == "Activo")
            {
                TempData["ErrorMessage"] = "Este apadrinamiento ya está activo";
                return RedirectToAction("MisApadrinamientos");
            }

            if (apadrinamiento.Estado == "Cancelado" || apadrinamiento.Estado == "Completado")
            {
                TempData["ErrorMessage"] = "No se puede reactivar un apadrinamiento que está cancelado o completado";
                return RedirectToAction("MisApadrinamientos");
            }

            // Cambiar a "Activo"
            apadrinamiento.Estado = "Activo";
            apadrinamiento.Observaciones = apadrinamiento.Observaciones + " [Reactivado el " + DateTime.Now.ToString("dd/MM/yyyy") + "]";
            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Reactivar Apadrinamiento", "Donaciones",
                $"Mascota: {apadrinamiento.Mascotas?.Nombre}", usuario.UsuarioId);

            string subject = "Confirmación de Reactivación de Apadrinamiento";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>¡Buenas noticias! Hemos reactivado el apadrinamiento de <strong>{apadrinamiento.Mascotas?.Nombre}</strong>.</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Mascota: {apadrinamiento.Mascotas?.Nombre}</li>
                    <li>Inicio: {apadrinamiento.FechaInicio:dd/MM/yyyy}</li>
                    <li>Reactivado: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>¡Gracias por continuar con tu apoyo!</p>
                <br/>
                <p>Atentamente,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendNotificationAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"Apadrinamiento reactivado exitosamente.";
            return RedirectToAction("MisApadrinamientos");
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
                return RedirectToAction("Login", "Account");
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

            AuditoriaHelper.RegistrarAccion("Cancelar Apadrinamiento", "Donaciones",
                $"Mascota: {apadrinamiento.Mascotas?.Nombre}", usuario.UsuarioId);

            string subject = "Confirmación de Cancelación";
            string body = $@"
                <h2>Estimado/a {usuario.NombreCompleto}</h2>
                <p>Confirmamos la cancelación del apadrinamiento de <strong>{apadrinamiento.Mascotas?.Nombre}</strong>.</p>
                <p><strong>Detalles:</strong></p>
                <ul>
                    <li>Mascota: {apadrinamiento.Mascotas?.Nombre}</li>
                    <li>Inicio: {apadrinamiento.FechaInicio:dd/MM/yyyy}</li>
                    <li>Fin: {DateTime.Now:dd/MM/yyyy}</li>
                </ul>
                <p>Agradecemos tu apoyo. Esperamos verte nuevamente.</p>
                <br/>
                <p>Atentamente,<br/>Equipo del Refugio</p>
            ";

            _ = EmailHelper.SendNotificationAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"Apadrinamiento cancelado exitosamente.";
            return RedirectToAction("MisApadrinamientos");
        }

        // GET: Donaciones/HistorialPagos/5
        [Authorize]
        public ActionResult HistorialPagos(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .FirstOrDefault(a => a.ApadrinamientoId == id && a.UsuarioId == usuario.UsuarioId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            var pagos = db.PagosApadrinamiento
                .Where(p => p.ApadrinamientoId == id)
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            ViewBag.Apadrinamiento = apadrinamiento;
            ViewBag.TotalPagado = pagos.Sum(p => p.Monto);
            ViewBag.PagosCompletados = pagos.Count(p => p.Estado == "Completado");

            return View(pagos);
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