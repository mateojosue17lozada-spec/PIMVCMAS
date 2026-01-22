using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Administrador")]
    public class AdminController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    // 1. ESTADÍSTICAS DE MASCOTAS
                    TotalMascotas = db.Mascotas.Count(m => m.Activo == true),
                    MascotasDisponibles = db.Mascotas.Count(m =>
                        m.Estado == "Disponible para adopción" && m.Activo == true),
                    MascotasEnTratamiento = db.Mascotas.Count(m =>
                        m.Estado == "En tratamiento" && m.Activo == true),
                    MascotasAdoptadas = db.Mascotas.Count(m =>
                        m.Estado == "Adoptada"),

                    // 2. ADOPCIONES
                    SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente"),
                    SolicitudesAprobadas = db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada"),
                    SolicitudesRechazadas = db.SolicitudAdopcion.Count(s => s.Estado == "Rechazada"),

                    AdopcionesEsteMes = db.Mascotas.Count(m =>
                        m.Estado == "Adoptada" &&
                        m.FechaAdopcion.HasValue &&
                        m.FechaAdopcion.Value.Month == DateTime.Now.Month &&
                        m.FechaAdopcion.Value.Year == DateTime.Now.Year),

                    // 3. DONACIONES
                    NumeroDonantesMes = db.Donaciones.Count(d =>
                        d.FechaDonacion.HasValue &&
                        d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                        d.FechaDonacion.Value.Year == DateTime.Now.Year),

                    TotalDonacionesMes = db.Donaciones
                        .Where(d => d.FechaDonacion.HasValue &&
                                   d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                                   d.FechaDonacion.Value.Year == DateTime.Now.Year)
                        .Sum(d => (decimal?)d.Monto) ?? 0,

                    // 4. VOLUNTARIADO
                    VoluntariosActivos = db.UsuariosRoles
                        .Count(ur => ur.Roles.NombreRol == "Voluntario" &&
                                   ur.Usuarios.Activo == true),

                    // 5. APADRINAMIENTOS
                    ApadrinamientosActivos = db.Apadrinamientos.Count(a => a.Estado == "Activo"),

                    // 6. TIENDA
                    ProductosDisponibles = db.Productos.Count(p => p.Stock > 0 && p.Activo == true),
                    ProductosBajoStock = db.Productos.Count(p => p.Stock > 0 && p.Stock <= p.StockMinimo),
                    PedidosPendientes = db.Pedidos.Count(p => p.Estado == "Confirmado"),

                    VentasMes = db.Pedidos
                        .Where(p => p.Estado == "Entregado" &&
                                   p.FechaPedido.HasValue &&
                                   p.FechaPedido.Value.Month == DateTime.Now.Month &&
                                   p.FechaPedido.Value.Year == DateTime.Now.Year)
                        .Sum(p => (decimal?)p.Total) ?? 0,

                    // 7. RESCATE
                    ReportesAbiertos = db.ReportesRescate.Count(r =>
                        r.Estado == "Pendiente" || r.Estado == "En proceso"),

                    // 8. CONTABILIDAD
                    IngresosMes = CalcularIngresosMes(),
                    EgresosMes = CalcularEgresosMes(),
                    BalanceMes = 0
                };

                // Calcular Balance
                viewModel.BalanceMes = viewModel.IngresosMes - viewModel.EgresosMes;

                // 9. ACTIVIDADES RECIENTES
                ViewBag.ActividadesRecientes = db.AuditoriaAcciones
                    .OrderByDescending(a => a.FechaAccion)
                    .Take(10)
                    .ToList();

                // 10. SOLICITUDES PENDIENTES
                ViewBag.SolicitudesPendientes = db.SolicitudAdopcion
                    .Where(s => s.Estado == "Pendiente")
                    .OrderBy(s => s.FechaSolicitud)
                    .Take(5)
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar el dashboard: " + ex.Message;
                return View(new DashboardViewModel());
            }
        }

        private decimal CalcularIngresosMes()
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso" &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == DateTime.Now.Month &&
                           m.FechaMovimiento.Value.Year == DateTime.Now.Year)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        private decimal CalcularEgresosMes()
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso" &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == DateTime.Now.Month &&
                           m.FechaMovimiento.Value.Year == DateTime.Now.Year)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        // GET: Admin/Usuarios
        public ActionResult Usuarios(string rol = "Todos", string activo = "Todos", string buscar = "", int page = 1)
        {
            int pageSize = 10;
            IQueryable<Usuarios> query = db.Usuarios
                .Include(u => u.UsuariosRoles.Select(ur => ur.Roles));

            // Filtro por rol
            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
            {
                query = query.Where(u => u.UsuariosRoles.Any(ur => ur.Roles.NombreRol == rol));
            }

            // Filtro por estado activo
            if (!string.IsNullOrEmpty(activo) && activo != "Todos")
            {
                bool esActivo = activo == "true";
                query = query.Where(u => u.Activo == esActivo);
            }

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(u => u.NombreCompleto.Contains(buscar) ||
                                        u.Email.Contains(buscar) ||
                                        u.Cedula.Contains(buscar));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var usuarios = query
                .OrderBy(u => u.NombreCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Calcular estadísticas
            ViewBag.TotalUsuarios = db.Usuarios.Count();
            ViewBag.UsuariosActivos = db.Usuarios.Count(u => u.Activo == true);
            ViewBag.TotalAdministradores = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Administrador" && ur.Usuarios.Activo == true);
            ViewBag.TotalVeterinarios = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Veterinario" && ur.Usuarios.Activo == true);

            // Pasar filtros a la vista
            ViewBag.FiltroRol = rol;
            ViewBag.FiltroActivo = activo;
            ViewBag.FiltroBuscar = buscar;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(usuarios);
        }

        // GET: Admin/CrearUsuario
        public ActionResult CrearUsuario()
        {
            ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            return View();
        }

        // POST: Admin/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(RegistroUsuarioViewModel model, int[] rolesSeleccionados)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe
                if (db.Usuarios.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado.");
                    ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                    return View(model);
                }

                // Verificar si la cédula ya existe
                if (!string.IsNullOrEmpty(model.Cedula) && db.Usuarios.Any(u => u.Cedula == model.Cedula))
                {
                    ModelState.AddModelError("Cedula", "Esta cédula ya está registrada.");
                    ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                    return View(model);
                }

                // Generar salt y hash de contraseña
                string salt = PasswordHelper.GenerateSalt();
                string passwordHash = PasswordHelper.HashPassword(model.Password, salt);

                byte[] imagenBytes = null;
                if (model.ImagenPerfil != null && model.ImagenPerfil.ContentLength > 0)
                {
                    if (!ImageHelper.IsValidImage(model.ImagenPerfil))
                    {
                        ModelState.AddModelError("ImagenPerfil", "Formato de imagen inválido.");
                        ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                        return View(model);
                    }

                    imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenPerfil);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                }

                // Crear usuario
                var usuario = new Usuarios
                {
                    NombreCompleto = model.NombreCompleto,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    Cedula = model.Cedula,
                    Direccion = model.Direccion,
                    Ciudad = model.Ciudad,
                    Provincia = model.Provincia,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    ImagenPerfil = imagenBytes,
                    FechaRegistro = DateTime.Now,
                    Activo = true,
                    EmailConfirmado = true,
                    TelefonoConfirmado = true
                };

                try
                {
                    db.Usuarios.Add(usuario);
                    db.SaveChanges();

                    // Asignar roles seleccionados
                    if (rolesSeleccionados != null && rolesSeleccionados.Length > 0)
                    {
                        foreach (var rolId in rolesSeleccionados)
                        {
                            db.UsuariosRoles.Add(new UsuariosRoles
                            {
                                UsuarioId = usuario.UsuarioId,
                                RolId = rolId,
                                FechaAsignacion = DateTime.Now,
                                AsignadoPor = ObtenerUsuarioActualId()
                            });
                        }
                        db.SaveChanges();
                    }

                    // Auditoría
                    AuditoriaHelper.RegistrarAccion("Crear Usuario", "Admin",
                        $"Usuario creado: {usuario.Email}", ObtenerUsuarioActualId() ?? 0);

                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction("Usuarios");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al crear usuario: " + ex.Message);
                }
            }

            ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            return View(model);
        }

        // GET: Admin/VerUsuario/5
        public ActionResult VerUsuario(int id)
        {
            var usuario = db.Usuarios
                .Include(u => u.UsuariosRoles.Select(ur => ur.Roles))
                .FirstOrDefault(u => u.UsuarioId == id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            ViewBag.RolesUsuario = usuario.UsuariosRoles.Select(ur => ur.Roles.NombreRol).ToList();
            ViewBag.ImagenBase64 = usuario.ImagenPerfil != null ?
                ImageHelper.GetImageDataUri(usuario.ImagenPerfil) : null;

            return View(usuario);
        }

        // GET: Admin/EditarUsuario/5
        public ActionResult EditarUsuario(int id)
        {
            var usuario = db.Usuarios
                .Include(u => u.UsuariosRoles)
                .FirstOrDefault(u => u.UsuarioId == id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            var model = new EditarUsuarioViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Cedula = usuario.Cedula,
                Direccion = usuario.Direccion,
                Ciudad = usuario.Ciudad,
                Provincia = usuario.Provincia,
                Activo = usuario.Activo ?? true,
                EmailConfirmado = usuario.EmailConfirmado ?? false,
                TelefonoConfirmado = usuario.TelefonoConfirmado ?? false
            };

            if (usuario.ImagenPerfil != null)
            {
                model.ImagenPerfilBase64 = ImageHelper.GetImageDataUri(usuario.ImagenPerfil);
            }

            ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            ViewBag.RolesSeleccionados = usuario.UsuariosRoles.Select(ur => ur.RolId).ToList();

            return View(model);
        }

        // POST: Admin/EditarUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarUsuario(EditarUsuarioViewModel model, int[] rolesSeleccionados)
        {
            if (ModelState.IsValid)
            {
                var usuario = db.Usuarios.Find(model.UsuarioId);
                if (usuario == null)
                {
                    return HttpNotFound();
                }

                // Actualizar datos básicos
                usuario.NombreCompleto = model.NombreCompleto;
                usuario.Telefono = model.Telefono;
                usuario.Direccion = model.Direccion;
                usuario.Ciudad = model.Ciudad;
                usuario.Provincia = model.Provincia;
                usuario.Activo = model.Activo;
                usuario.EmailConfirmado = model.EmailConfirmado;
                usuario.TelefonoConfirmado = model.TelefonoConfirmado;

                // Actualizar imagen si se proporciona
                if (model.NuevaImagenPerfil != null && model.NuevaImagenPerfil.ContentLength > 0)
                {
                    if (!ImageHelper.IsValidImage(model.NuevaImagenPerfil))
                    {
                        ModelState.AddModelError("NuevaImagenPerfil", "Formato de imagen inválido.");
                        ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                        ViewBag.RolesSeleccionados = rolesSeleccionados;
                        return View(model);
                    }

                    byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(model.NuevaImagenPerfil);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                    usuario.ImagenPerfil = imagenBytes;
                }

                // Actualizar roles
                var rolesActuales = db.UsuariosRoles.Where(ur => ur.UsuarioId == model.UsuarioId).ToList();
                db.UsuariosRoles.RemoveRange(rolesActuales);

                if (rolesSeleccionados != null)
                {
                    foreach (var rolId in rolesSeleccionados)
                    {
                        db.UsuariosRoles.Add(new UsuariosRoles
                        {
                            UsuarioId = model.UsuarioId,
                            RolId = rolId,
                            FechaAsignacion = DateTime.Now,
                            AsignadoPor = ObtenerUsuarioActualId()
                        });
                    }
                }

                try
                {
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion("Editar Usuario", "Admin",
                        $"Usuario editado: {usuario.Email}", ObtenerUsuarioActualId() ?? 0);

                    TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                    return RedirectToAction("Usuarios");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar usuario: " + ex.Message);
                }
            }

            ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            ViewBag.RolesSeleccionados = rolesSeleccionados;
            return View(model);
        }

        // POST: Admin/ToggleUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            // Cambiar estado
            usuario.Activo = !(usuario.Activo ?? true);

            // Si está siendo activado, asegurar que EmailConfirmado y TelefonoConfirmado sean true
            if (usuario.Activo == true)
            {
                usuario.EmailConfirmado = true;
                usuario.TelefonoConfirmado = true;
            }

            try
            {
                db.SaveChanges();

                string accion = usuario.Activo == true ? "Activado" : "Desactivado";
                AuditoriaHelper.RegistrarAccion($"{accion} Usuario", "Admin",
                    $"Usuario {accion.ToLower()}: {usuario.Email}", ObtenerUsuarioActualId() ?? 0);

                TempData["SuccessMessage"] = $"Usuario {accion.ToLower()} exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cambiar estado del usuario: {ex.Message}";
            }

            return RedirectToAction("Usuarios");
        }

        // POST: Admin/EliminarUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            try
            {
                // Primero eliminar roles asociados
                var roles = db.UsuariosRoles.Where(ur => ur.UsuarioId == id).ToList();
                db.UsuariosRoles.RemoveRange(roles);

                // Luego eliminar usuario
                db.Usuarios.Remove(usuario);
                db.SaveChanges();

                AuditoriaHelper.RegistrarAccion("Eliminar Usuario", "Admin",
                    $"Usuario eliminado: {usuario.Email}", ObtenerUsuarioActualId() ?? 0);

                TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al eliminar usuario: {ex.Message}";
            }

            return RedirectToAction("Usuarios");
        }

        // GET: Admin/SolicitudesAdopcion
        public ActionResult SolicitudesAdopcion(string estado, int page = 1)
        {
            int pageSize = 10;
            var query = db.SolicitudAdopcion.AsQueryable();

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(s => s.Estado == estado);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var solicitudes = query
                .Include(s => s.Mascotas)
                .Include(s => s.Usuarios)
                .OrderByDescending(s => s.FechaSolicitud)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(solicitudes);
        }

        // GET: Admin/DetallesSolicitud/5
        public ActionResult DetallesSolicitud(int id)
        {
            var solicitud = db.SolicitudAdopcion
                .Include(s => s.Mascotas)
                .Include(s => s.Usuarios)
                .FirstOrDefault(s => s.SolicitudId == id);

            if (solicitud == null)
            {
                return HttpNotFound();
            }

            ViewBag.Formulario = db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == id);

            ViewBag.Evaluacion = db.EvaluacionAdopcion
                .FirstOrDefault(e => e.SolicitudId == id);

            if (solicitud.Mascotas != null && solicitud.Mascotas.ImagenPrincipal != null)
            {
                ViewBag.MascotaImagen = ImageHelper.GetImageDataUri(solicitud.Mascotas.ImagenPrincipal);
            }

            if (solicitud.Usuarios != null && solicitud.Usuarios.ImagenPerfil != null)
            {
                ViewBag.UsuarioImagen = ImageHelper.GetImageDataUri(solicitud.Usuarios.ImagenPerfil);
            }

            return View(solicitud);
        }

        // POST: Admin/AprobarSolicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AprobarSolicitud(int solicitudId, string observaciones)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null)
            {
                return HttpNotFound();
            }

            solicitud.Estado = "Aprobada";
            solicitud.ResultadoEvaluacion = "Aprobada";
            solicitud.FechaEvaluacion = DateTime.Now;
            solicitud.EvaluadoPor = ObtenerUsuarioActualId();
            solicitud.Observaciones = observaciones;

            // Actualizar la mascota
            var mascota = db.Mascotas.Find(solicitud.MascotaId);
            if (mascota != null)
            {
                mascota.Estado = "Adoptada";
                mascota.FechaAdopcion = DateTime.Now;
            }

            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Aprobar Solicitud", "Admin",
                $"Solicitud ID: {solicitudId}", ObtenerUsuarioActualId() ?? 0);

            TempData["SuccessMessage"] = "Solicitud aprobada exitosamente";
            return RedirectToAction("DetallesSolicitud", new { id = solicitudId });
        }

        // POST: Admin/RechazarSolicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RechazarSolicitud(int solicitudId, string motivoRechazo)
        {
            if (string.IsNullOrEmpty(motivoRechazo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo de rechazo";
                return RedirectToAction("DetallesSolicitud", new { id = solicitudId });
            }

            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null)
            {
                return HttpNotFound();
            }

            solicitud.Estado = "Rechazada";
            solicitud.ResultadoEvaluacion = "Rechazada";
            solicitud.MotivoRechazo = motivoRechazo;
            solicitud.FechaEvaluacion = DateTime.Now;
            solicitud.EvaluadoPor = ObtenerUsuarioActualId();

            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Rechazar Solicitud", "Admin",
                $"Solicitud ID: {solicitudId}", ObtenerUsuarioActualId() ?? 0);

            TempData["SuccessMessage"] = "Solicitud rechazada";
            return RedirectToAction("DetallesSolicitud", new { id = solicitudId });
        }

        // GET: Admin/Configuracion
        public ActionResult Configuracion()
        {
            var configuraciones = db.ConfiguracionSistema.ToList();
            var configDict = configuraciones.ToDictionary(c => c.Clave, c => c.Valor);

            var model = new ConfiguracionViewModel
            {
                NombreRefugio = configDict.ContainsKey("NombreRefugio") ? configDict["NombreRefugio"] : "Refugio de Animales Quito",
                EmailContacto = configDict.ContainsKey("EmailContacto") ? configDict["EmailContacto"] : "contacto@refugioquito.org",
                TelefonoContacto = configDict.ContainsKey("TelefonoContacto") ? configDict["TelefonoContacto"] : "0999999999",
                DireccionRefugio = configDict.ContainsKey("DireccionRefugio") ? configDict["DireccionRefugio"] : "Quito, Ecuador",
                CapacidadMaximaMascotas = configDict.ContainsKey("CapacidadMaximaMascotas") ?
                    (int.TryParse(configDict["CapacidadMaximaMascotas"], out int capacidad) ? capacidad : 100) : 100,
                MontoMinimoApadrinamiento = configDict.ContainsKey("MontoMinimoApadrinamiento") ?
                    (decimal.TryParse(configDict["MontoMinimoApadrinamiento"], out decimal monto) ? monto : 10) : 10,
                MensajeBienvenida = configDict.ContainsKey("MensajeBienvenida") ? configDict["MensajeBienvenida"] : "¡Bienvenido a nuestro refugio!"
            };

            return View(model);
        }

        // POST: Admin/ActualizarConfiguracion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarConfiguracion(ConfiguracionViewModel model)
        {
            if (ModelState.IsValid)
            {
                ActualizarConfig("NombreRefugio", model.NombreRefugio);
                ActualizarConfig("EmailContacto", model.EmailContacto);
                ActualizarConfig("TelefonoContacto", model.TelefonoContacto);
                ActualizarConfig("DireccionRefugio", model.DireccionRefugio);
                ActualizarConfig("CapacidadMaximaMascotas", model.CapacidadMaximaMascotas.ToString());
                ActualizarConfig("MontoMinimoApadrinamiento", model.MontoMinimoApadrinamiento.ToString());
                ActualizarConfig("MensajeBienvenida", model.MensajeBienvenida);

                db.SaveChanges();

                AuditoriaHelper.RegistrarAccion("Actualizar Configuración", "Admin",
                    "Configuración del sistema actualizada", ObtenerUsuarioActualId() ?? 0);

                TempData["SuccessMessage"] = "Configuración actualizada exitosamente";
                return RedirectToAction("Configuracion");
            }

            return View(model);
        }

        private void ActualizarConfig(string clave, string valor)
        {
            var config = db.ConfiguracionSistema.FirstOrDefault(c => c.Clave == clave);

            if (config == null)
            {
                config = new ConfiguracionSistema
                {
                    Clave = clave,
                    Valor = valor,
                    TipoDato = "string",
                    FechaModificacion = DateTime.Now,
                    ModificadoPor = ObtenerUsuarioActualId()
                };
                db.ConfiguracionSistema.Add(config);
            }
            else
            {
                config.Valor = valor;
                config.FechaModificacion = DateTime.Now;
                config.ModificadoPor = ObtenerUsuarioActualId();
            }
        }

        // GET: Admin/Auditoria
        public ActionResult Auditoria(string accion, DateTime? fechaDesde, DateTime? fechaHasta, int page = 1)
        {
            int pageSize = 50;
            var query = db.AuditoriaAcciones
                .Include(a => a.Usuarios)
                .AsQueryable();

            if (!string.IsNullOrEmpty(accion))
            {
                query = query.Where(a => a.Accion.Contains(accion));
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(a => a.FechaAccion >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(a => a.FechaAccion <= fechaHasta.Value);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var auditoria = query
                .OrderByDescending(a => a.FechaAccion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Accion = accion;
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(auditoria);
        }

        // MÉTODO AUXILIAR
        private int? ObtenerUsuarioActualId()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            return usuario?.UsuarioId;
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

    // VIEWMODELS NECESARIOS
    public class RegistroUsuarioViewModel
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string Telefono { get; set; }

        [StringLength(13, ErrorMessage = "La cédula no puede exceder 13 caracteres")]
        public string Cedula { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        public string Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        public string Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
        public string Provincia { get; set; }

        public HttpPostedFileBase ImagenPerfil { get; set; }
    }

    public class EditarUsuarioViewModel
    {
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string NombreCompleto { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string Telefono { get; set; }

        [StringLength(13, ErrorMessage = "La cédula no puede exceder 13 caracteres")]
        public string Cedula { get; set; }

        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        public string Direccion { get; set; }

        [StringLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
        public string Ciudad { get; set; }

        [StringLength(100, ErrorMessage = "La provincia no puede exceder 100 caracteres")]
        public string Provincia { get; set; }

        public bool Activo { get; set; }
        public bool EmailConfirmado { get; set; }
        public bool TelefonoConfirmado { get; set; }

        public string ImagenPerfilBase64 { get; set; }
        public HttpPostedFileBase NuevaImagenPerfil { get; set; }
    }

    public class ConfiguracionViewModel
    {
        [Required(ErrorMessage = "El nombre del refugio es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string NombreRefugio { get; set; }

        [Required(ErrorMessage = "El email de contacto es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        public string EmailContacto { get; set; }

        [Required(ErrorMessage = "El teléfono de contacto es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string TelefonoContacto { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        public string DireccionRefugio { get; set; }

        [Required(ErrorMessage = "La capacidad máxima es requerida")]
        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000")]
        public int CapacidadMaximaMascotas { get; set; }

        [Required(ErrorMessage = "El monto mínimo es requerido")]
        [Range(1, 1000, ErrorMessage = "El monto debe estar entre 1 y 1000")]
        public decimal MontoMinimoApadrinamiento { get; set; }

        [StringLength(1000, ErrorMessage = "El mensaje no puede exceder 1000 caracteres")]
        public string MensajeBienvenida { get; set; }
    }
}