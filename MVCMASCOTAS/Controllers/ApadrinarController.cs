// Controllers/ApadrinarController.cs
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Globalization;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class ApadrinarController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Apadrinar
        public ActionResult Index()
        {
            try
            {
                // Obtener mascotas disponibles para apadrinar
                var mascotas = db.Mascotas
                    .Where(m => m.Activo == true &&
                           m.Estado != "Adoptada" &&
                           m.Estado != "Fallecida")
                    .OrderBy(m => m.Nombre)
                    .Select(m => new MascotaResumenViewModel
                    {
                        MascotaId = m.MascotaId,
                        Nombre = m.Nombre,
                        Especie = m.Especie,
                        Sexo = m.Sexo,
                        EdadAproximada = m.EdadAproximada,
                        Estado = m.Estado,
                        DescripcionGeneral = m.DescripcionGeneral,
                        ImagenPrincipal = m.ImagenPrincipal,
                        Raza = m.Raza,
                        Color = m.Color,
                        Tamanio = m.Tamanio,
                        Esterilizado = m.Esterilizado ?? false
                    })
                    .ToList();

                // Obtener usuario actual
                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                if (usuario != null)
                {
                    // Obtener mascotas que el usuario ya apadrina
                    var apadrinamientos = db.Apadrinamientos
                        .Where(a => a.UsuarioId == usuario.UsuarioId &&
                               (a.Estado == "Activo" || a.Estado == "Pausado"))
                        .Select(a => a.MascotaId)
                        .ToList();

                    ViewBag.ApadrinamientosUsuario = apadrinamientos;
                    ViewBag.UsuarioId = usuario.UsuarioId;
                }

                return View(mascotas);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar las mascotas: " + ex.Message;
                return View(new List<MascotaResumenViewModel>());
            }
        }

        // GET: Apadrinar/Confirmar/{id}
        [HttpGet]
        public ActionResult Confirmar(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Debes iniciar sesión para apadrinar una mascota";
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Confirmar", new { id = id }) });
                }

                var mascota = db.Mascotas.Find(id);
                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("Index");
                }

                // Verificar si ya está apadrinada por el usuario
                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index");
                }

                var apadrinamientoExistente = db.Apadrinamientos
                    .Any(a => a.UsuarioId == usuario.UsuarioId &&
                           a.MascotaId == id &&
                           (a.Estado == "Activo" || a.Estado == "Pausado"));

                if (apadrinamientoExistente)
                {
                    TempData["WarningMessage"] = "Ya apadrinas a esta mascota";
                    return RedirectToAction("Index");
                }

                // Crear ViewModel
                var viewModel = new ConfirmarApadrinamientoViewModel
                {
                    MascotaId = mascota.MascotaId,
                    MascotaNombre = mascota.Nombre,
                    MascotaEspecie = mascota.Especie,
                    MontoMensual = 10.00m,
                    MontoMensualTexto = "10.00",
                    MetodosPago = GetMetodosPago(),
                    UsuarioNombre = usuario.NombreCompleto,
                    UsuarioEmail = usuario.Email,
                    UsuarioTelefono = usuario.Telefono
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar la confirmación: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Apadrinar/Confirmar - VERSIÓN CORREGIDA DEFINITIVA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Confirmar(ConfirmarApadrinamientoViewModel model)
        {
            try
            {
                // Log inicial
                System.Diagnostics.Debug.WriteLine("=== INICIANDO CONFIRMAR POST ASÍNCRONO ===");
                System.Diagnostics.Debug.WriteLine($"MascotaId: {model.MascotaId}, MontoTexto: '{model.MontoMensualTexto}', MetodoPago: {model.MetodoPagoSeleccionado}");

                // 1. Parsear el monto desde el texto antes de validar
                model.ParsearMonto();
                System.Diagnostics.Debug.WriteLine($"Monto parseado: {model.MontoMensual}");

                // 2. Limpiar los errores de ModelState para MontoMensual (porque validamos manualmente)
                ModelState.Remove("MontoMensual");
                ModelState.Remove("MontoMensualTexto");

                // 3. Validación manual del monto
                if (model.MontoMensual < 10)
                {
                    ModelState.AddModelError("MontoMensualTexto", "El monto mínimo es $10.00");
                }

                if (model.MontoMensual > 10000)
                {
                    ModelState.AddModelError("MontoMensualTexto", "El monto máximo es $10,000.00");
                }

                // 4. Validar formato del monto texto
                if (string.IsNullOrEmpty(model.MontoMensualTexto))
                {
                    ModelState.AddModelError("MontoMensualTexto", "El monto mensual es requerido");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(model.MontoMensualTexto, @"^\d+([\.,]\d{1,2})?$"))
                {
                    ModelState.AddModelError("MontoMensualTexto", "Formato inválido. Use 10.00 o 10,00");
                }

                // 5. Validar otros campos requeridos
                if (string.IsNullOrEmpty(model.MetodoPagoSeleccionado))
                {
                    ModelState.AddModelError("MetodoPagoSeleccionado", "El método de pago es requerido");
                }

                if (!model.AceptaTerminos)
                {
                    ModelState.AddModelError("AceptaTerminos", "Debes aceptar los términos y condiciones");
                }

                // 6. Si hay errores, retornar la vista
                if (!ModelState.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("ModelState no válido");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        System.Diagnostics.Debug.WriteLine($"Error: {error.ErrorMessage}");
                    }
                    model.MetodosPago = GetMetodosPago();

                    // Recargar información del usuario
                    var usuarioEmail = User.Identity.Name;
                    var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioEmail);
                    if (usuario != null)
                    {
                        model.UsuarioNombre = usuario.NombreCompleto;
                        model.UsuarioEmail = usuario.Email;
                        model.UsuarioTelefono = usuario.Telefono;
                    }

                    return View(model);
                }

                // Resto del código...
                // Verificar autenticación
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Debes iniciar sesión para continuar";
                    return RedirectToAction("Login", "Account");
                }

                // Obtener usuario
                var usuarioEmail2 = User.Identity.Name;
                System.Diagnostics.Debug.WriteLine($"Usuario Email: {usuarioEmail2}");

                var usuario2 = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioEmail2);

                if (usuario2 == null)
                {
                    System.Diagnostics.Debug.WriteLine("Usuario no encontrado");
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine($"Usuario encontrado: ID {usuario2.UsuarioId}, Nombre: {usuario2.NombreCompleto}");

                // Verificar si ya apadrina esta mascota
                var apadrinamientoExistente = await db.Apadrinamientos
                    .FirstOrDefaultAsync(a => a.UsuarioId == usuario2.UsuarioId
                              && a.MascotaId == model.MascotaId
                              && (a.Estado == "Activo" || a.Estado == "Pausado"));

                if (apadrinamientoExistente != null)
                {
                    System.Diagnostics.Debug.WriteLine("Ya existe apadrinamiento para esta mascota");
                    TempData["ErrorMessage"] = "Ya apadrinas esta mascota";
                    return RedirectToAction("Index");
                }

                // Verificar que la mascota existe
                var mascota = await db.Mascotas.FindAsync(model.MascotaId);
                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "La mascota no existe";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine("Iniciando creación del apadrinamiento...");

                // Usar transacción para garantizar integridad
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Crear el apadrinamiento
                        var nuevoApadrinamiento = new Apadrinamientos
                        {
                            MascotaId = model.MascotaId,
                            UsuarioId = usuario2.UsuarioId,
                            MontoMensual = model.MontoMensual,
                            FechaInicio = DateTime.Now,
                            Estado = "Activo",
                            MetodoPagoPreferido = model.MetodoPagoSeleccionado,
                            DiaCobroMensual = DateTime.Now.Day,
                            Observaciones = $"Apadrinamiento iniciado el {DateTime.Now:dd/MM/yyyy}. Método de pago: {model.MetodoPagoSeleccionado}. Monto: ${model.MontoMensual.ToString("F2", CultureInfo.InvariantCulture)} mensuales"
                        };

                        System.Diagnostics.Debug.WriteLine("Agregando apadrinamiento a la base de datos...");
                        db.Apadrinamientos.Add(nuevoApadrinamiento);
                        await db.SaveChangesAsync();

                        System.Diagnostics.Debug.WriteLine($"Apadrinamiento creado exitosamente. ID: {nuevoApadrinamiento.ApadrinamientoId}");

                        // 2. Crear primer pago registrado (inicial)
                        var primerPago = new PagosApadrinamiento
                        {
                            ApadrinamientoId = nuevoApadrinamiento.ApadrinamientoId,
                            FechaPago = DateTime.Now,
                            Monto = model.MontoMensual,
                            MesPagado = DateTime.Now.ToString("MMMM yyyy"),
                            MetodoPago = model.MetodoPagoSeleccionado,
                            NumeroTransaccion = "APD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                            Estado = "Completado",
                            ComprobanteElectronico = null
                        };

                        System.Diagnostics.Debug.WriteLine("Creando pago inicial...");
                        db.PagosApadrinamiento.Add(primerPago);
                        await db.SaveChangesAsync();

                        System.Diagnostics.Debug.WriteLine("Pago creado exitosamente");

                        // 3. Registrar en auditoría
                        var auditoria = new AuditoriaAcciones
                        {
                            UsuarioId = usuario2.UsuarioId,
                            Accion = "Nuevo Apadrinamiento",
                            Modulo = "Apadrinamiento",
                            Descripcion = $"Usuario {usuario2.NombreCompleto} apadrinó a {model.MascotaNombre}",
                            FechaAccion = DateTime.Now,
                            DireccionIP = Request.UserHostAddress ?? "IP no disponible",
                            Detalles = $"Mascota ID: {model.MascotaId}, Monto mensual: ${model.MontoMensual.ToString("F2", CultureInfo.InvariantCulture)}, Método de pago: {model.MetodoPagoSeleccionado}"
                        };

                        System.Diagnostics.Debug.WriteLine("Creando registro de auditoría...");
                        db.AuditoriaAcciones.Add(auditoria);
                        await db.SaveChangesAsync();

                        // 4. Confirmar la transacción
                        transaction.Commit();
                        System.Diagnostics.Debug.WriteLine("Transacción completada exitosamente");

                        TempData["SuccessMessage"] = $"¡Felicidades! Has apadrinado exitosamente a {model.MascotaNombre}";
                        return RedirectToAction("MisApadrinamientos");
                    }
                    catch (DbEntityValidationException dbEx)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"ERROR DE VALIDACIÓN DE ENTIDAD:");
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                System.Diagnostics.Debug.WriteLine($"- Propiedad: {validationError.PropertyName}");
                                System.Diagnostics.Debug.WriteLine($"  Error: {validationError.ErrorMessage}");
                                ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                            }
                        }
                        model.MetodosPago = GetMetodosPago();

                        // Recargar información del usuario
                        var usuarioEmail = User.Identity.Name;
                        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioEmail);
                        if (usuario != null)
                        {
                            model.UsuarioNombre = usuario.NombreCompleto;
                            model.UsuarioEmail = usuario.Email;
                            model.UsuarioTelefono = usuario.Telefono;
                        }

                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR en transacción: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                        }

                        // Revertir la transacción en caso de error
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR general en Confirmar: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Mostrar mensaje de error más específico
                string mensajeError = "Error al procesar el apadrinamiento";
                if (ex.InnerException != null && ex.InnerException.Message.Contains("CHECK"))
                {
                    mensajeError = "Error en los datos del apadrinamiento. Verifica que los estados sean válidos: 'Activo', 'Pausado', 'Completado' o 'Cancelado' para apadrinamientos, y 'Pendiente', 'Completado' o 'Fallido' para pagos.";
                }
                else if (ex.Message.Contains("FOREIGN KEY"))
                {
                    mensajeError = "Error en las referencias de la base de datos. Verifica que la mascota y el usuario existan";
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("Timeout"))
                {
                    mensajeError = "Timeout en la conexión a la base de datos. Inténtalo nuevamente.";
                }

                TempData["ErrorMessage"] = $"{mensajeError}: {ex.Message}";
                model.MetodosPago = GetMetodosPago();

                // Recargar información del usuario
                var usuarioEmail = User.Identity.Name;
                var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioEmail);
                if (usuario != null)
                {
                    model.UsuarioNombre = usuario.NombreCompleto;
                    model.UsuarioEmail = usuario.Email;
                    model.UsuarioTelefono = usuario.Telefono;
                }

                return View(model);
            }
        }

        // GET: Apadrinar/MisApadrinamientos
        public ActionResult MisApadrinamientos()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Debes iniciar sesión para ver tus apadrinamientos";
                    return RedirectToAction("Login", "Account");
                }

                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Index", "Home");
                }

                // Obtener apadrinamientos activos
                var apadrinamientosActivos = db.Apadrinamientos
                    .Include(a => a.Mascotas)
                    .Where(a => a.UsuarioId == usuario.UsuarioId &&
                           (a.Estado == "Activo" || a.Estado == "Pausado"))
                    .OrderByDescending(a => a.FechaInicio)
                    .ToList()
                    .Select(a => new ApadrinamientoResumenViewModel
                    {
                        ApadrinamientoId = a.ApadrinamientoId,
                        MascotaId = a.MascotaId,
                        MascotaNombre = a.Mascotas?.Nombre ?? "Desconocido",
                        MascotaEspecie = a.Mascotas?.Especie ?? "Desconocido",
                        MascotaImagen = a.Mascotas?.ImagenPrincipal,
                        MontoMensual = a.MontoMensual,
                        FechaInicio = a.FechaInicio ?? DateTime.Now,
                        Estado = a.Estado,
                        TotalContribuido = a.PagosApadrinamiento
                            .Where(p => p.Estado == "Completado")
                            .Sum(p => p.Monto),
                        MesesActivo = a.FechaInicio.HasValue ?
                            (int)((DateTime.Now - a.FechaInicio.Value).TotalDays / 30) : 0,
                        ProximoPago = a.FechaInicio.HasValue ?
                            a.FechaInicio.Value.AddMonths((int)(DateTime.Now.Subtract(a.FechaInicio.Value).TotalDays / 30) + 1) :
                            (DateTime?)null
                    })
                    .ToList();

                // Obtener historial de apadrinamientos finalizados
                var apadrinamientosHistorial = db.Apadrinamientos
                    .Include(a => a.Mascotas)
                    .Where(a => a.UsuarioId == usuario.UsuarioId &&
                           (a.Estado == "Completado" || a.Estado == "Cancelado"))
                    .OrderByDescending(a => a.FechaFin ?? a.FechaInicio)
                    .ToList()
                    .Select(a => new ApadrinamientoResumenViewModel
                    {
                        ApadrinamientoId = a.ApadrinamientoId,
                        MascotaId = a.MascotaId,
                        MascotaNombre = a.Mascotas?.Nombre ?? "Desconocido",
                        MascotaEspecie = a.Mascotas?.Especie ?? "Desconocido",
                        MascotaImagen = a.Mascotas?.ImagenPrincipal,
                        MontoMensual = a.MontoMensual,
                        FechaInicio = a.FechaInicio ?? DateTime.Now,
                        Estado = a.Estado,
                        TotalContribuido = a.PagosApadrinamiento
                            .Where(p => p.Estado == "Completado")
                            .Sum(p => p.Monto),
                        MesesActivo = a.FechaInicio.HasValue && a.FechaFin.HasValue ?
                            (int)((a.FechaFin.Value - a.FechaInicio.Value).TotalDays / 30) : 0
                    })
                    .ToList();

                // Calcular resumen financiero
                var todosApadrinamientos = db.Apadrinamientos
                    .Where(a => a.UsuarioId == usuario.UsuarioId)
                    .ToList();

                var todosPagos = db.PagosApadrinamiento
                    .Where(p => p.Apadrinamientos.UsuarioId == usuario.UsuarioId &&
                           p.Estado == "Completado")
                    .ToList();

                var resumen = new ResumenFinancieroViewModel
                {
                    TotalMensual = apadrinamientosActivos.Sum(a => a.MontoMensual),
                    TotalAnual = apadrinamientosActivos.Sum(a => a.MontoMensual * 12),
                    TotalHistorico = todosPagos.Sum(p => p.Monto),
                    MascotasApadrinadas = apadrinamientosActivos.Count,
                    PagosRealizados = todosPagos.Count,
                    ProximaFechaPago = apadrinamientosActivos
                        .Where(a => a.ProximoPago.HasValue)
                        .OrderBy(a => a.ProximoPago)
                        .Select(a => a.ProximoPago)
                        .FirstOrDefault()
                };

                var viewModel = new MisApadrinamientosViewModel
                {
                    ApadrinamientosActivos = apadrinamientosActivos,
                    ApadrinamientosHistorial = apadrinamientosHistorial,
                    Resumen = resumen
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar tus apadrinamientos: " + ex.Message;
                return View(new MisApadrinamientosViewModel
                {
                    ApadrinamientosActivos = new List<ApadrinamientoResumenViewModel>(),
                    ApadrinamientosHistorial = new List<ApadrinamientoResumenViewModel>(),
                    Resumen = new ResumenFinancieroViewModel()
                });
            }
        }

        // GET: Apadrinar/Detalle/{id}
        public ActionResult Detalle(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Account");
                }

                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                var apadrinamiento = db.Apadrinamientos
                    .Include(a => a.Mascotas)
                    .Include(a => a.PagosApadrinamiento)
                    .FirstOrDefault(a => a.ApadrinamientoId == id &&
                                    a.UsuarioId == usuario.UsuarioId);

                if (apadrinamiento == null)
                {
                    TempData["ErrorMessage"] = "Apadrinamiento no encontrado";
                    return RedirectToAction("MisApadrinamientos");
                }

                var viewModel = new ApadrinamientoDetalleViewModel
                {
                    ApadrinamientoId = apadrinamiento.ApadrinamientoId,
                    MascotaId = apadrinamiento.MascotaId,
                    MascotaNombre = apadrinamiento.Mascotas?.Nombre ?? "Desconocido",
                    MascotaEspecie = apadrinamiento.Mascotas?.Especie ?? "Desconocido",
                    MascotaImagen = apadrinamiento.Mascotas?.ImagenPrincipal,
                    MontoMensual = apadrinamiento.MontoMensual,
                    FechaInicio = apadrinamiento.FechaInicio ?? DateTime.Now,
                    FechaFin = apadrinamiento.FechaFin,
                    Estado = apadrinamiento.Estado,
                    MetodoPagoPreferido = apadrinamiento.MetodoPagoPreferido,
                    DiaCobroMensual = apadrinamiento.DiaCobroMensual ?? 1,
                    Observaciones = apadrinamiento.Observaciones,
                    Pagos = apadrinamiento.PagosApadrinamiento
                        .OrderByDescending(p => p.FechaPago)
                        .Select(p => new PagoViewModel
                        {
                            PagoId = p.PagoId,
                            FechaPago = p.FechaPago ?? DateTime.Now,
                            Monto = p.Monto,
                            MesPagado = p.MesPagado,
                            MetodoPago = p.MetodoPago,
                            NumeroTransaccion = p.NumeroTransaccion,
                            Estado = p.Estado
                        })
                        .ToList(),
                    TotalContribuido = apadrinamiento.PagosApadrinamiento
                        .Where(p => p.Estado == "Completado")
                        .Sum(p => p.Monto)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar el detalle: " + ex.Message;
                return RedirectToAction("MisApadrinamientos");
            }
        }

        // POST: Apadrinar/Pausar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pausar(int id)
        {
            try
            {
                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                var apadrinamiento = db.Apadrinamientos
                    .FirstOrDefault(a => a.ApadrinamientoId == id &&
                                    a.UsuarioId == usuario.UsuarioId);

                if (apadrinamiento == null)
                {
                    TempData["ErrorMessage"] = "Apadrinamiento no encontrado";
                    return RedirectToAction("MisApadrinamientos");
                }

                apadrinamiento.Estado = "Pausado";
                apadrinamiento.Observaciones += $"\nPausado el {DateTime.Now:dd/MM/yyyy}";

                // Registrar auditoría
                var auditoria = new AuditoriaAcciones
                {
                    UsuarioId = usuario.UsuarioId,
                    Accion = "Pausar Apadrinamiento",
                    Modulo = "Apadrinamiento",
                    Descripcion = $"Usuario pausó apadrinamiento ID {id}",
                    FechaAccion = DateTime.Now,
                    DireccionIP = Request.UserHostAddress,
                    Detalles = $"Mascota: {apadrinamiento.MascotaId}"
                };

                db.AuditoriaAcciones.Add(auditoria);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Apadrinamiento pausado exitosamente";
                return RedirectToAction("MisApadrinamientos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al pausar el apadrinamiento: " + ex.Message;
                return RedirectToAction("MisApadrinamientos");
            }
        }

        // POST: Apadrinar/Reactivar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reactivar(int id)
        {
            try
            {
                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                var apadrinamiento = db.Apadrinamientos
                    .FirstOrDefault(a => a.ApadrinamientoId == id &&
                                    a.UsuarioId == usuario.UsuarioId);

                if (apadrinamiento == null)
                {
                    TempData["ErrorMessage"] = "Apadrinamiento no encontrado";
                    return RedirectToAction("MisApadrinamientos");
                }

                apadrinamiento.Estado = "Activo";
                apadrinamiento.Observaciones += $"\nReactivado el {DateTime.Now:dd/MM/yyyy}";

                // Registrar auditoría
                var auditoria = new AuditoriaAcciones
                {
                    UsuarioId = usuario.UsuarioId,
                    Accion = "Reactivar Apadrinamiento",
                    Modulo = "Apadrinamiento",
                    Descripcion = $"Usuario reactivó apadrinamiento ID {id}",
                    FechaAccion = DateTime.Now,
                    DireccionIP = Request.UserHostAddress,
                    Detalles = $"Mascota: {apadrinamiento.MascotaId}"
                };

                db.AuditoriaAcciones.Add(auditoria);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Apadrinamiento reactivado exitosamente";
                return RedirectToAction("MisApadrinamientos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al reactivar el apadrinamiento: " + ex.Message;
                return RedirectToAction("MisApadrinamientos");
            }
        }

        // POST: Apadrinar/Cancelar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id)
        {
            try
            {
                var usuarioEmail = User.Identity.Name;
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == usuarioEmail);

                var apadrinamiento = db.Apadrinamientos
                    .FirstOrDefault(a => a.ApadrinamientoId == id &&
                                    a.UsuarioId == usuario.UsuarioId);

                if (apadrinamiento == null)
                {
                    TempData["ErrorMessage"] = "Apadrinamiento no encontrado";
                    return RedirectToAction("MisApadrinamientos");
                }

                apadrinamiento.Estado = "Cancelado";
                apadrinamiento.FechaFin = DateTime.Now;
                apadrinamiento.Observaciones += $"\nCancelado el {DateTime.Now:dd/MM/yyyy}";

                // Registrar auditoría
                var auditoria = new AuditoriaAcciones
                {
                    UsuarioId = usuario.UsuarioId,
                    Accion = "Cancelar Apadrinamiento",
                    Modulo = "Apadrinamiento",
                    Descripcion = $"Usuario canceló apadrinamiento ID {id}",
                    FechaAccion = DateTime.Now,
                    DireccionIP = Request.UserHostAddress,
                    Detalles = $"Mascota: {apadrinamiento.MascotaId}, Duración: {(apadrinamiento.FechaInicio.HasValue ? (DateTime.Now - apadrinamiento.FechaInicio.Value).Days.ToString() + " días" : "N/A")}"
                };

                db.AuditoriaAcciones.Add(auditoria);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Apadrinamiento cancelado exitosamente";
                return RedirectToAction("MisApadrinamientos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cancelar el apadrinamiento: " + ex.Message;
                return RedirectToAction("MisApadrinamientos");
            }
        }

        // Métodos auxiliares privados
        private List<MetodoPagoViewModel> GetMetodosPago()
        {
            return new List<MetodoPagoViewModel>
            {
                new MetodoPagoViewModel { Id = "tarjeta_credito", Nombre = "Tarjeta de Crédito",
                    Descripcion = "Pago seguro con tarjeta Visa, Mastercard o American Express" },
                new MetodoPagoViewModel { Id = "tarjeta_debito", Nombre = "Tarjeta de Débito",
                    Descripcion = "Pago directo desde tu cuenta bancaria" },
                new MetodoPagoViewModel { Id = "paypal", Nombre = "PayPal",
                    Descripcion = "Pago seguro a través de PayPal" },
                new MetodoPagoViewModel { Id = "transferencia", Nombre = "Transferencia Bancaria",
                    Descripcion = "Transferencia directa a nuestra cuenta bancaria" },
                new MetodoPagoViewModel { Id = "efectivo", Nombre = "Depósito en Efectivo",
                    Descripcion = "Depósito en nuestras oficinas o bancos asociados" }
            };
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