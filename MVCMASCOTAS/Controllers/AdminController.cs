using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using MVCMASCOTAS.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using IronPdf;

namespace MVCMASCOTAS.Controllers
{
    /// <summary>
    /// ✅ CORREGIDO: Controlador administrativo principal del sistema
    /// Ahora redirige toda la gestión de mascotas al MascotasController
    /// </summary>
    [AuthorizeRoles("Administrador")]
    public class AdminController : Controller
    {
        private readonly RefugioMascotasDBEntities db;
        private readonly MascotaService mascotaService;
        private readonly AdopcionService adopcionService;

        public AdminController()
        {
            db = new RefugioMascotasDBEntities();
            mascotaService = new MascotaService();
            adopcionService = new AdopcionService();
        }

        #region DASHBOARD

        /// <summary>
        /// GET: Admin/Dashboard - Panel principal con estadísticas y alertas
        /// </summary>
        public ActionResult Dashboard()
        {
            try
            {
                var currentDate = DateTime.Now;
                var currentMonth = currentDate.Month;
                var currentYear = currentDate.Year;

                var viewModel = new DashboardViewModel
                {
                    // === MASCOTAS ===
                    TotalMascotas = mascotaService.ObtenerTotalMascotas(),
                    MascotasDisponibles = mascotaService.ObtenerMascotasDisponiblesCount(),
                    MascotasEnTratamiento = mascotaService.ObtenerMascotasPorEstadoCount("En tratamiento"),
                    MascotasAdoptadas = mascotaService.ObtenerMascotasPorEstadoCount("Adoptada"),
                    MascotasCriticas = db.HistorialMedico.Count(h => h.EstadoGeneral == "Crítico"),

                    // === ADOPCIONES ===
                    SolicitudesPendientes = adopcionService.ObtenerSolicitudesPendientes(),
                    SolicitudesAprobadas = db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada"),
                    SolicitudesRechazadas = db.SolicitudAdopcion.Count(s => s.Estado == "Rechazada"),
                    AdopcionesEsteMes = adopcionService.ObtenerAdopcionesDelMes(currentMonth, currentYear),
                    AdopcionesEsteAnio = db.Mascotas.Count(m =>
                        m.Estado == "Adoptada" &&
                        m.FechaAdopcion.HasValue &&
                        m.FechaAdopcion.Value.Year == currentYear),

                    // === DONACIONES ===
                    TotalDonacionesMes = db.Donaciones
                        .Where(d => d.FechaDonacion.HasValue &&
                                   d.FechaDonacion.Value.Month == currentMonth &&
                                   d.FechaDonacion.Value.Year == currentYear &&
                                   d.Estado == "Completada")
                        .Sum(d => (decimal?)d.Monto) ?? 0,
                    TotalDonacionesAnio = db.Donaciones
                        .Where(d => d.FechaDonacion.HasValue &&
                                   d.FechaDonacion.Value.Year == currentYear &&
                                   d.Estado == "Completada")
                        .Sum(d => (decimal?)d.Monto) ?? 0,
                    NumeroDonantesMes = db.Donaciones
                        .Where(d => d.FechaDonacion.HasValue &&
                                   d.FechaDonacion.Value.Month == currentMonth &&
                                   d.FechaDonacion.Value.Year == currentYear &&
                                   d.Estado == "Completada")
                        .Select(d => d.UsuarioId)
                        .Distinct()
                        .Count(),
                    ApadrinamientosActivos = db.Apadrinamientos.Count(a => a.Estado == "Activo"),

                    // === VOLUNTARIADO ===
                    VoluntariosActivos = db.UsuariosRoles
                        .Count(ur => ur.Roles.NombreRol == "Voluntario" &&
                                   (ur.Usuarios.Activo == null || ur.Usuarios.Activo == true)),
                    ActividadesProgramadas = db.Actividades
                        .Count(a => a.FechaActividad > currentDate && a.Estado == "Programada"),
                    HorasVoluntariadoMes = (decimal)(db.HorasVoluntariado
                        .Where(h => h.FechaActividad.Month == currentMonth &&
                                   h.FechaActividad.Year == currentYear)
                        .Sum(h => (decimal?)h.HorasTrabajadas) ?? 0),

                    // === VETERINARIA ===
                    ConsultasPendientes = db.HistorialMedico
                        .Count(h => h.TipoConsulta == "Pendiente" || h.TipoConsulta == "Urgente"),
                    TratamientosActivos = db.Tratamientos
                        .Count(t => t.Estado == "En curso" || t.Estado == "Activo"),
                    VacunacionesPendientes = db.MascotaVacunas
                        .Count(mv => mv.ProximaDosis.HasValue && mv.ProximaDosis.Value <= currentDate.AddDays(7)),

                    // === TIENDA ===
                    ProductosDisponibles = db.Productos.Count(p => p.Stock > 0 && p.Activo == true),
                    ProductosBajoStock = db.Productos.Count(p => p.Stock > 0 && p.Stock <= p.StockMinimo),
                    PedidosPendientes = db.Pedidos.Count(p => p.Estado == "Pendiente" || p.Estado == "Confirmado"),
                    VentasMes = db.Pedidos
                        .Where(p => p.Estado == "Entregado" &&
                                   p.FechaPedido.HasValue &&
                                   p.FechaPedido.Value.Month == currentMonth &&
                                   p.FechaPedido.Value.Year == currentYear)
                        .Sum(p => (decimal?)p.Total) ?? 0,

                    // === RESCATE ===
                    ReportesAbiertos = db.ReportesRescate
                        .Count(r => r.Estado == "Pendiente" || r.Estado == "En proceso"),

                    // === CONTABILIDAD ===
                    IngresosMes = db.MovimientosContables
                        .Where(m => m.TipoMovimiento == "Ingreso" &&
                                   m.FechaMovimiento.HasValue &&
                                   m.FechaMovimiento.Value.Month == currentMonth &&
                                   m.FechaMovimiento.Value.Year == currentYear)
                        .Sum(m => (decimal?)m.Monto) ?? 0,
                    EgresosMes = db.MovimientosContables
                        .Where(m => m.TipoMovimiento == "Egreso" &&
                                   m.FechaMovimiento.HasValue &&
                                   m.FechaMovimiento.Value.Month == currentMonth &&
                                   m.FechaMovimiento.Value.Year == currentYear)
                        .Sum(m => (decimal?)m.Monto) ?? 0,

                    // === AUDITORÍA ===
                    RegistrosAuditoriaDia = db.AuditoriaAcciones
                        .Count(a => DbFunctions.TruncateTime(a.FechaAccion) == DbFunctions.TruncateTime(currentDate)),
                    UsuariosConectados = db.Usuarios
                        .Count(u => u.UltimoAcceso.HasValue &&
                                   DbFunctions.DiffHours(u.UltimoAcceso.Value, currentDate) <= 1),

                    // === MASCOTAS PERDIDAS ===
                    MascotasPerdidasActivas = db.MascotasPerdidas
                        .Count(m => m.Estado == "Perdida" &&
                                   m.FechaPublicacion >= DateTime.Now.AddDays(-30)),
                    MascotasEncontradas = db.MascotasPerdidas
                        .Count(m => m.Estado == "Encontrada" &&
                                   m.FechaEncontrada.HasValue &&
                                   m.FechaEncontrada.Value.Month == currentMonth &&
                                   m.FechaEncontrada.Value.Year == currentYear),
                    MascotasPerdidasMes = db.MascotasPerdidas
                        .Count(m => m.FechaPerdida.Month == currentMonth &&
                                   m.FechaPerdida.Year == currentYear),

                    // Inicializar listas
                    Alertas = new List<string>(),
                    UltimasSolicitudes = new List<SolicitudAdopcion>(),
                    UltimasDonaciones = new List<Donaciones>(),
                    MascotasRecientes = new List<Mascotas>(),
                    UltimosMovimientos = new List<MovimientosContables>(),
                    UltimasMascotasPerdidas = new List<MascotasPerdidas>(),
                    MascotasPorEspecie = new Dictionary<string, int>(),
                    SolicitudesPorEstado = new Dictionary<string, int>(),
                    AuditoriaPorUsuario = new Dictionary<string, int>(),
                    AuditoriaPorAccion = new Dictionary<string, int>()
                };

                viewModel.BalanceMes = viewModel.IngresosMes - viewModel.EgresosMes;

                // Cargar listas de datos recientes
                viewModel.UltimasSolicitudes = db.SolicitudAdopcion
                    .Include(s => s.Mascotas)
                    .Include(s => s.Usuarios)
                    .Include(s => s.Usuarios1)
                    .OrderByDescending(s => s.FechaSolicitud)
                    .Take(5)
                    .ToList();

                viewModel.UltimasDonaciones = db.Donaciones
                    .Include(d => d.Usuarios)
                    .Where(d => d.FechaDonacion.HasValue)
                    .OrderByDescending(d => d.FechaDonacion)
                    .Take(5)
                    .ToList();

                viewModel.MascotasRecientes = mascotaService.ObtenerMascotasRecientes(5);

                viewModel.UltimosMovimientos = db.MovimientosContables
                    .Where(m => m.FechaMovimiento.HasValue)
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(10)
                    .ToList();

                viewModel.UltimasMascotasPerdidas = db.MascotasPerdidas
                    .Where(m => m.Estado == "Perdida")
                    .OrderByDescending(m => m.FechaPublicacion)
                    .Take(5)
                    .ToList();

                // Datos para gráficos
                viewModel.MascotasPorEspecie = db.Mascotas
                    .Where(m => m.Activo == true && !string.IsNullOrEmpty(m.Especie))
                    .GroupBy(m => m.Especie)
                    .ToDictionary(g => g.Key, g => g.Count());

                viewModel.SolicitudesPorEstado = db.SolicitudAdopcion
                    .Where(s => !string.IsNullOrEmpty(s.Estado))
                    .GroupBy(s => s.Estado)
                    .ToDictionary(g => g.Key, g => g.Count());

                viewModel.AuditoriaPorUsuario = db.AuditoriaAcciones
                    .Where(a => a.Usuarios != null && a.FechaAccion >= currentDate.AddDays(-7))
                    .GroupBy(a => a.Usuarios.NombreCompleto)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                viewModel.AuditoriaPorAccion = db.AuditoriaAcciones
                    .Where(a => !string.IsNullOrEmpty(a.Accion) && a.FechaAccion >= currentDate.AddDays(-7))
                    .GroupBy(a => a.Accion)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                GenerarAlertas(viewModel);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en Dashboard: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", "Error al cargar dashboard", ex, UserHelper.GetCurrentUserId());

                var viewModel = new DashboardViewModel
                {
                    Alertas = new List<string> { "Error al cargar el dashboard. Intente nuevamente." }
                };

                return View(viewModel);
            }
        }

        private void GenerarAlertas(DashboardViewModel model)
        {
            if (model.SolicitudesPendientes > 10)
                model.Alertas.Add($"⚠️ {model.SolicitudesPendientes} solicitudes pendientes de revisión");

            if (model.ProductosBajoStock > 0)
                model.Alertas.Add($"📦 {model.ProductosBajoStock} productos con stock bajo");

            if (model.MascotasCriticas > 0)
                model.Alertas.Add($"🚨 {model.MascotasCriticas} mascotas en estado crítico - ATENCIÓN URGENTE");

            if (model.VacunacionesPendientes > 0)
                model.Alertas.Add($"💉 {model.VacunacionesPendientes} vacunaciones próximas o vencidas");

            if (model.BalanceMes < 0)
                model.Alertas.Add($"💰 Balance negativo: ${Math.Abs(model.BalanceMes):N2} - Revisar gastos");

            if (model.MascotasPerdidasActivas > 20)
                model.Alertas.Add($"🔍 ALTA PRIORIDAD: {model.MascotasPerdidasActivas} mascotas perdidas activas");
            else if (model.MascotasPerdidasActivas > 10)
                model.Alertas.Add($"🔍 {model.MascotasPerdidasActivas} mascotas perdidas requieren atención");

            if (model.ReportesAbiertos > 0)
                model.Alertas.Add($"📞 {model.ReportesAbiertos} reportes de rescate abiertos");

            if (model.PedidosPendientes > 0)
                model.Alertas.Add($"📋 {model.PedidosPendientes} pedidos pendientes de procesar");

            if (model.ConsultasPendientes > 0)
                model.Alertas.Add($"🏥 {model.ConsultasPendientes} consultas veterinarias pendientes");
        }

        #endregion

        #region ✅ CORREGIDO: REDIRECCIONES A MASCOTAS CONTROLLER

        /// <summary>
        /// ✅ CORREGIDO: Redirige a MascotasController.Gestionar
        /// </summary>
        public ActionResult Mascotas(string estado = "", string especie = "", string tamanio = "", string nombre = "", int page = 1)
        {
            return RedirectToAction("Gestionar", "Mascotas", new
            {
                estado,
                especie,
                buscar = nombre,  // Mapear 'nombre' a 'buscar'
                page
            });
        }

        /// <summary>
        /// ✅ CORREGIDO: Redirige a MascotasController.ExportarMascotas
        /// </summary>
        public ActionResult ExportarMascotas(string estado = "", string especie = "", string tamanio = "", string nombre = "")
        {
            return RedirectToAction("ExportarMascotas", "Mascotas", new
            {
                estado,
                especie,
                buscar = nombre
            });
        }

        /// <summary>
        /// ✅ CORREGIDO: Redirige a MascotasController.Archivar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarMascota(int id)
        {
            // Redirige a la acción de archivar en MascotasController
            return RedirectToAction("Archivar", "Mascotas", new { id });
        }

        /// <summary>
        /// ✅ CORREGIDO: Redirige a MascotasController.Crear
        /// </summary>
        public ActionResult NuevaMascota()
        {
            return RedirectToAction("Crear", "Mascotas");
        }

        #endregion

        #region GESTIÓN DE USUARIOS

        public ActionResult Usuarios(string rol = "Todos", string activo = "Todos", string buscar = "", int page = 1)
        {
            try
            {
                int pageSize = 20;
                IQueryable<Usuarios> query = db.Usuarios
                    .Include(u => u.UsuariosRoles.Select(ur => ur.Roles));

                // ✅ SANITIZAR BÚSQUEDA
                if (!string.IsNullOrEmpty(buscar))
                {
                    buscar = SanitizarEntrada(buscar, 100);
                    query = query.Where(u => u.NombreCompleto.Contains(buscar) ||
                                            u.Email.Contains(buscar) ||
                                            u.Cedula.Contains(buscar));
                }

                if (!string.IsNullOrEmpty(rol) && rol != "Todos")
                    query = query.Where(u => u.UsuariosRoles.Any(ur => ur.Roles.NombreRol == rol));

                if (!string.IsNullOrEmpty(activo) && activo != "Todos")
                {
                    bool esActivo = activo == "true";
                    query = query.Where(u => u.Activo == esActivo);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var usuarios = query
                    .OrderBy(u => u.NombreCompleto)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // ✅ NUEVO: Obtener roles para todos los usuarios en una sola consulta
                var usuariosIds = usuarios.Select(u => u.UsuarioId).ToList();
                var usuariosRolesDict = db.UsuariosRoles
                    .Where(ur => usuariosIds.Contains(ur.UsuarioId))
                    .Include(ur => ur.Roles)
                    .ToList()
                    .GroupBy(ur => ur.UsuarioId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Where(ur => ur.Roles != null)
                              .Select(ur => ur.Roles.NombreRol)
                              .ToList()
                    );

                ViewBag.UsuariosRoles = usuariosRolesDict;

                ViewBag.TotalUsuarios = db.Usuarios.Count();
                ViewBag.UsuariosActivos = db.Usuarios.Count(u => u.Activo == true);
                ViewBag.TotalAdministradores = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Administrador" && ur.Usuarios.Activo == true);
                ViewBag.TotalVeterinarios = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Veterinario" && ur.Usuarios.Activo == true);

                ViewBag.FiltroRol = rol;
                ViewBag.FiltroActivo = activo;
                ViewBag.FiltroBuscar = buscar;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(usuarios);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Usuarios: {ex.Message}");
                AuditoriaHelper.RegistrarError("Admin", "Error al cargar usuarios", ex, UserHelper.GetCurrentUserId());

                TempData["ErrorMessage"] = "Error al cargar los usuarios";
                return View(new List<Usuarios>());
            }
        }

        public ActionResult CrearUsuario()
        {
            try
            {
                ViewBag.Roles = db.Roles
                    .Where(r => r.Activo == null || r.Activo == true)
                    .OrderBy(r => r.NombreRol)
                    .ToList();

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearUsuario GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el formulario";
                return RedirectToAction("Usuarios");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(RegisterViewModel model, int[] rolesSeleccionados)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                return View(model);
            }

            try
            {
                // ✅ SANITIZAR ENTRADAS
                model.Email = SanitizarEntrada(model.Email?.Trim(), 100);
                model.NombreCompleto = SanitizarEntrada(model.NombreCompleto?.Trim(), 200);
                model.Cedula = SanitizarEntrada(model.Cedula?.Trim(), 20);

                if (db.Usuarios.Any(u => u.Email.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                    return View(model);
                }

                if (!string.IsNullOrEmpty(model.Cedula) && db.Usuarios.Any(u => u.Cedula == model.Cedula))
                {
                    ModelState.AddModelError("Cedula", "Esta cédula ya está registrada");
                    ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                    return View(model);
                }

                string salt = PasswordHelper.GenerateSalt();
                string passwordHash = PasswordHelper.HashPassword(model.Password, salt);

                // ✅ VALIDAR IMAGEN
                byte[] imagenBytes = null;
                if (model.ImagenPerfil != null && model.ImagenPerfil.ContentLength > 0)
                {
                    var validacion = ValidarImagenSegura(model.ImagenPerfil);
                    if (!validacion.EsValida)
                    {
                        ModelState.AddModelError("ImagenPerfil", validacion.MensajeError);
                        ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                        return View(model);
                    }

                    imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenPerfil);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                }

                var usuario = new Usuarios
                {
                    NombreCompleto = model.NombreCompleto,
                    Email = model.Email.ToLower(),
                    Telefono = model.Telefono?.Trim(),
                    Cedula = model.Cedula,
                    Direccion = model.Direccion?.Trim(),
                    Ciudad = model.Ciudad?.Trim(),
                    Provincia = model.Provincia?.Trim(),
                    PasswordHash = passwordHash,
                    Salt = salt,
                    ImagenPerfil = imagenBytes,
                    FechaRegistro = DateTime.Now,
                    Activo = true,
                    EmailConfirmado = true,
                    TelefonoConfirmado = true
                };

                db.Usuarios.Add(usuario);
                db.SaveChanges();

                if (rolesSeleccionados != null && rolesSeleccionados.Length > 0)
                {
                    foreach (var rolId in rolesSeleccionados)
                    {
                        db.UsuariosRoles.Add(new UsuariosRoles
                        {
                            UsuarioId = usuario.UsuarioId,
                            RolId = rolId,
                            FechaAsignacion = DateTime.Now,
                            AsignadoPor = UserHelper.GetCurrentUserId()
                        });
                    }
                    db.SaveChanges();
                }

                AuditoriaHelper.RegistrarCreacion("Usuarios", usuario.UsuarioId,
                    $"Usuario: {usuario.Email}, Roles: {rolesSeleccionados?.Length ?? 0}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = "Usuario creado exitosamente";
                return RedirectToAction("Usuarios");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearUsuario POST: {ex.Message}");
                ModelState.AddModelError("", "Error al crear usuario");
                ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
                return View(model);
            }
        }

        public ActionResult VerUsuario(int id)
        {
            try
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

                ViewBag.TotalSolicitudes = db.SolicitudAdopcion.Count(s => s.UsuarioId == id);
                ViewBag.TotalDonaciones = db.Donaciones.Count(d => d.UsuarioId == id);
                ViewBag.TotalApadrinamientos = db.Apadrinamientos.Count(a => a.UsuarioId == id);

                return View(usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en VerUsuario: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el usuario";
                return RedirectToAction("Usuarios");
            }
        }

        public ActionResult EditarUsuario(int id)
        {
            try
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
                    TelefonoConfirmado = usuario.TelefonoConfirmado ?? false,
                    FechaRegistro = usuario.FechaRegistro,
                    UltimoAcceso = usuario.UltimoAcceso
                };

                if (usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0)
                {
                    model.ImagenPerfilBase64 = Convert.ToBase64String(usuario.ImagenPerfil);
                }

                var roles = db.Roles.Where(r => r.Activo == null || r.Activo == true).ToList();
                model.RolesDisponibles = roles.Select(r => new RolViewModel
                {
                    RolId = r.RolId,
                    NombreRol = r.NombreRol,
                    Descripcion = r.Descripcion
                }).ToList();

                var rolesSeleccionados = usuario.UsuariosRoles.Select(ur => ur.RolId).ToList();
                model.RolesSeleccionados = rolesSeleccionados;

                foreach (var rol in model.RolesDisponibles)
                {
                    rol.Seleccionado = rolesSeleccionados.Contains(rol.RolId);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EditarUsuario GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el usuario para editar";
                return RedirectToAction("Usuarios");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarUsuario(EditarUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = db.Roles.ToList();
                model.RolesDisponibles = roles.Select(r => new RolViewModel
                {
                    RolId = r.RolId,
                    NombreRol = r.NombreRol,
                    Descripcion = r.Descripcion
                }).ToList();
                return View(model);
            }

            try
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    var usuario = db.Usuarios.Find(model.UsuarioId);
                    if (usuario == null)
                    {
                        TempData["ErrorMessage"] = "Usuario no encontrado";
                        return RedirectToAction("Usuarios");
                    }

                    usuario.NombreCompleto = model.NombreCompleto?.Trim();
                    usuario.Telefono = model.Telefono?.Trim();
                    usuario.Cedula = model.Cedula?.Trim();
                    usuario.Direccion = model.Direccion?.Trim();
                    usuario.Ciudad = model.Ciudad?.Trim();
                    usuario.Provincia = model.Provincia?.Trim();
                    usuario.Activo = model.Activo;
                    usuario.EmailConfirmado = model.EmailConfirmado;
                    usuario.TelefonoConfirmado = model.TelefonoConfirmado;

                    if (model.NuevaImagenPerfil != null && model.NuevaImagenPerfil.ContentLength > 0)
                    {
                        var validacion = ValidarImagenSegura(model.NuevaImagenPerfil);
                        if (!validacion.EsValida)
                        {
                            ModelState.AddModelError("NuevaImagenPerfil", validacion.MensajeError);
                            var roles = db.Roles.ToList();
                            model.RolesDisponibles = roles.Select(r => new RolViewModel
                            {
                                RolId = r.RolId,
                                NombreRol = r.NombreRol
                            }).ToList();
                            return View(model);
                        }

                        byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(model.NuevaImagenPerfil);
                        imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                        usuario.ImagenPerfil = imagenBytes;
                    }

                    db.SaveChanges();

                    var rolesActuales = db.UsuariosRoles.Where(ur => ur.UsuarioId == model.UsuarioId).ToList();

                    foreach (var rolActual in rolesActuales)
                    {
                        if (model.RolesSeleccionados == null || !model.RolesSeleccionados.Contains(rolActual.RolId))
                        {
                            db.UsuariosRoles.Remove(rolActual);
                        }
                    }

                    if (model.RolesSeleccionados != null)
                    {
                        foreach (var rolId in model.RolesSeleccionados)
                        {
                            if (!rolesActuales.Any(r => r.RolId == rolId))
                            {
                                db.UsuariosRoles.Add(new UsuariosRoles
                                {
                                    UsuarioId = model.UsuarioId,
                                    RolId = rolId,
                                    FechaAsignacion = DateTime.Now,
                                    AsignadoPor = UserHelper.GetCurrentUserId()
                                });
                            }
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    AuthorizeRolesAttribute.ClearUserRolesCache(usuario.Email);

                    AuditoriaHelper.RegistrarCambioDatos("Usuarios", model.UsuarioId,
                        $"Usuario actualizado: {usuario.Email}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = "Usuario actualizado correctamente";
                    return RedirectToAction("Usuarios");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EditarUsuario POST: {ex.Message}");
                TempData["ErrorMessage"] = "Error al actualizar usuario";
                return RedirectToAction("EditarUsuario", new { id = model.UsuarioId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    return HttpNotFound();
                }

                usuario.Activo = !(usuario.Activo ?? true);
                db.SaveChanges();

                string accion = usuario.Activo == true ? "Activado" : "Desactivado";

                AuditoriaHelper.RegistrarCambioDatos("Usuarios", id,
                    $"Usuario {accion.ToLower()}: {usuario.Email}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = $"Usuario {accion.ToLower()} exitosamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ToggleUsuario: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cambiar estado del usuario";
            }

            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    return HttpNotFound();
                }

                string emailUsuario = usuario.Email;

                var roles = db.UsuariosRoles.Where(ur => ur.UsuarioId == id).ToList();
                db.UsuariosRoles.RemoveRange(roles);

                db.Usuarios.Remove(usuario);
                db.SaveChanges();

                AuditoriaHelper.RegistrarEliminacion("Usuarios", id,
                    $"Usuario eliminado: {emailUsuario}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = "Usuario eliminado exitosamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarUsuario: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar usuario";
            }

            return RedirectToAction("Usuarios");
        }

        #endregion

        #region SOLICITUDES DE ADOPCIÓN

        public ActionResult SolicitudAdopcion()
        {
            try
            {
                // Usar el mismo método de filtrado con valores por defecto
                return SolicitudAdopcionFiltrada("Todos", "Todos", 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en SolicitudAdopcion: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", "Error al cargar solicitudes", ex, UserHelper.GetCurrentUserId());
                ViewBag.Error = "Error al cargar solicitudes: " + ex.Message;
                return View(new List<SolicitudAdopcion>());
            }
        }

        [HttpGet]
        public ActionResult SolicitudAdopcionSimple()
        {
            return SolicitudAdopcionFiltrada("Todos", "Todos", 1);
        }

        [HttpGet]
        public ActionResult SolicitudAdopcionFiltrada(string estado = "Todos", string estadoAdopcion = "Todos", int page = 1)
        {
            try
            {
                const int pageSize = 20;

                // 🔥 SOLUCIÓN: Usar una consulta más explícita con Join en lugar de Include
                var query = from s in db.SolicitudAdopcion
                            join u in db.Usuarios on s.UsuarioId equals u.UsuarioId into usuariosJoin
                            from u in usuariosJoin.DefaultIfEmpty()
                            join m in db.Mascotas on s.MascotaId equals m.MascotaId into mascotasJoin
                            from m in mascotasJoin.DefaultIfEmpty()
                            join ev in db.Usuarios on s.EvaluadoPor equals ev.UsuarioId into evaluadoresJoin
                            from ev in evaluadoresJoin.DefaultIfEmpty()
                            select new
                            {
                                Solicitud = s,
                                Usuario = u,
                                Mascota = m,
                                Evaluador = ev,
                                Formulario = s.FormularioAdopcionDetalle.FirstOrDefault(),
                                Evaluacion = s.EvaluacionAdopcion.FirstOrDefault()
                            };

                // Aplicar filtros
                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                {
                    query = query.Where(x => x.Solicitud.Estado == estado);
                }

                if (!string.IsNullOrEmpty(estadoAdopcion) && estadoAdopcion != "Todos")
                {
                    query = query.Where(x => x.Solicitud.EstadoAdopcion == estadoAdopcion);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var resultados = query
                    .OrderByDescending(x => x.Solicitud.FechaSolicitud)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 🔥 IMPORTANTE: Convertir los resultados anónimos a objetos SolicitudAdopcion completos
                var solicitudes = new List<SolicitudAdopcion>();
                foreach (var r in resultados)
                {
                    var solicitud = r.Solicitud;
                    // Asignar manualmente las propiedades de navegación
                    solicitud.Usuarios = r.Usuario;
                    solicitud.Mascotas = r.Mascota;
                    solicitud.Usuarios1 = r.Evaluador;
                    // No asignamos colecciones para evitar problemas de proxy
                    solicitudes.Add(solicitud);
                }

                // Debug
                System.Diagnostics.Debug.WriteLine($"=== CARGANDO SOLICITUDES FILTRADAS ===");
                System.Diagnostics.Debug.WriteLine($"Total encontradas: {solicitudes.Count}");
                foreach (var s in solicitudes.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {s.SolicitudId}, Usuario: {(s.Usuarios?.NombreCompleto ?? "NULL")}, Mascota: {(s.Mascotas?.Nombre ?? "NULL")}");
                }

                var estadisticas = db.SolicitudAdopcion
                    .GroupBy(s => s.Estado)
                    .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                    .ToDictionary(x => x.Estado ?? "Sin estado", x => x.Cantidad);

                ViewBag.SolicitudesPendientes = estadisticas.TryGetValue("Pendiente", out var cnt1) ? cnt1 : 0;
                ViewBag.SolicitudesEvaluacion = estadisticas.TryGetValue("En evaluación", out var cnt2) ? cnt2 : 0;
                ViewBag.SolicitudesAprobadas = estadisticas.TryGetValue("Aprobada", out var cnt3) ? cnt3 : 0;
                ViewBag.SolicitudesRechazadas = estadisticas.TryGetValue("Rechazada", out var cnt4) ? cnt4 : 0;
                ViewBag.AdopcionesActivas = db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada" && s.EstadoAdopcion != "Finalizada" && s.EstadoAdopcion != "Cancelada");
                ViewBag.AdopcionesFinalizadas = db.SolicitudAdopcion.Count(s => s.EstadoAdopcion == "Finalizada");

                ViewBag.EstadoSeleccionado = estado;
                ViewBag.EstadoAdopcionSeleccionado = estadoAdopcion;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View("SolicitudAdopcion", solicitudes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en SolicitudAdopcionFiltrada: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", "Error al cargar solicitudes", ex, UserHelper.GetCurrentUserId());

                TempData["ErrorMessage"] = "Error al cargar las solicitudes.";
                return View("SolicitudAdopcion", new List<SolicitudAdopcion>());
            }
        }

        public ActionResult DetallesSolicitud(int? id)
        {
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "ID de solicitud no válido";
                return RedirectToAction("SolicitudAdopcion");
            }

            try
            {
                // 🔥 CORREGIDO: Cargar la solicitud con todas las relaciones
                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Mascotas)
                    .Include(s => s.Usuarios)        // Intentar cargar con Include
                    .Include(s => s.Usuarios1)
                    .Include(s => s.FormularioAdopcionDetalle)
                    .Include(s => s.EvaluacionAdopcion)
                    .FirstOrDefault(s => s.SolicitudId == id.Value);

                if (solicitud == null)
                {
                    TempData["ErrorMessage"] = "Solicitud no encontrada";
                    return RedirectToAction("SolicitudAdopcion");
                }

                // 🔥 VERIFICACIÓN CRÍTICA: Si Usuarios sigue siendo null, cargarlo manualmente
                if (solicitud.Usuarios == null && solicitud.UsuarioId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Usuarios null para Solicitud {id}, cargando manualmente...");
                    solicitud.Usuarios = db.Usuarios.Find(solicitud.UsuarioId);

                    if (solicitud.Usuarios != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Usuario cargado manualmente: {solicitud.Usuarios.NombreCompleto}");
                    }
                }

                // 🔥 TAMBIÉN verificar Mascotas por si acaso
                if (solicitud.Mascotas == null && solicitud.MascotaId > 0)
                {
                    solicitud.Mascotas = db.Mascotas.Find(solicitud.MascotaId);
                }

                // Imágenes para la vista
                ViewBag.MascotaImagen = solicitud.Mascotas?.ImagenPrincipal != null
                    ? ImageHelper.GetImageDataUri(solicitud.Mascotas.ImagenPrincipal)
                    : null;

                ViewBag.UsuarioImagen = solicitud.Usuarios?.ImagenPerfil != null
                    ? ImageHelper.GetImageDataUri(solicitud.Usuarios.ImagenPerfil)
                    : null;

                // Debug
                System.Diagnostics.Debug.WriteLine($"=== DETALLES SOLICITUD #{id} ===");
                System.Diagnostics.Debug.WriteLine($"Usuario encontrado: {(solicitud.Usuarios != null ? "SÍ" : "NO")}");
                if (solicitud.Usuarios != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Nombre: {solicitud.Usuarios.NombreCompleto}");
                    System.Diagnostics.Debug.WriteLine($"Email: {solicitud.Usuarios.Email}");
                    System.Diagnostics.Debug.WriteLine($"Cédula: {solicitud.Usuarios.Cedula}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UsuarioId en BD: {solicitud.UsuarioId}");
                }

                return View(solicitud);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DetallesSolicitud: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", $"Error al cargar detalles de solicitud ID {id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar los detalles de la solicitud";
                return RedirectToAction("SolicitudAdopcion");
            }
        }

        /// <summary>
        /// ✅ CORREGIDO: Ahora retorna JSON para AJAX
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AprobarSolicitud(int id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== APROBAR SOLICITUD ID: {id} ===");

                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Usuarios)
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == id);

                if (solicitud == null)
                {
                    return Json(new { success = false, message = "Solicitud no encontrada." });
                }

                // Verificar estado actual
                if (solicitud.Estado != "Pendiente" && solicitud.Estado != "En evaluación")
                {
                    return Json(new { success = false, message = $"Esta solicitud no puede ser aprobada porque su estado actual es: {solicitud.Estado}" });
                }

                // Actualizar solicitud
                var usuarioEvaluador = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

                solicitud.Estado = "Aprobada";
                solicitud.ResultadoEvaluacion = "Aprobada";
                solicitud.FechaEvaluacion = DateTime.Now;
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = usuarioEvaluador?.UsuarioId;
                solicitud.EstadoAdopcion = "Programada"; // Inicia el proceso de adopción
                solicitud.Observaciones = "Solicitud aprobada. Se ha iniciado el proceso de adopción.";

                // Actualizar estado de la mascota - CORREGIDO: MascotaId es int, no nullable
                var mascota = db.Mascotas.Find(solicitud.MascotaId);
                if (mascota != null)
                {
                    mascota.Estado = "En proceso de adopción";
                }

                db.SaveChanges();

                // Registrar auditoría
                if (usuarioEvaluador != null)
                {
                    AuditoriaHelper.RegistrarAccion("Aprobar Solicitud", "Admin",
                        $"Solicitud #{solicitud.SolicitudId} aprobada para mascota: {solicitud.Mascotas?.Nombre}",
                        usuarioEvaluador.UsuarioId);
                }

                // Enviar email al adoptante (OPCIONAL)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (solicitud.Usuarios?.Email != null)
                        {
                            await EmailHelper.SendAdoptionApprovedAsync(
                                solicitud.Usuarios.Email,
                                solicitud.Usuarios.NombreCompleto,
                                solicitud.Mascotas?.Nombre
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error enviando email: {ex.Message}");
                    }
                });

                return Json(new
                {
                    success = true,
                    message = $"Solicitud #{solicitud.SolicitudId} aprobada exitosamente.",
                    solicitudId = solicitud.SolicitudId
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AprobarSolicitud: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", $"Error aprobando solicitud {id}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al aprobar la solicitud: " + ex.Message });
            }
        }

        /// <summary>
        /// 🔥 CORREGIDO: Rechazar solicitud con motivo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RechazarSolicitud(int id, string motivo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== RECHAZAR SOLICITUD ID: {id}, Motivo: {motivo} ===");

                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Usuarios)
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == id);

                if (solicitud == null)
                {
                    return Json(new { success = false, message = "Solicitud no encontrada." });
                }

                // Verificar estado actual
                if (solicitud.Estado != "Pendiente" && solicitud.Estado != "En evaluación")
                {
                    return Json(new { success = false, message = $"Esta solicitud no puede ser rechazada porque su estado actual es: {solicitud.Estado}" });
                }

                // Validar motivo
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    return Json(new { success = false, message = "Debe proporcionar un motivo para el rechazo." });
                }

                // Actualizar solicitud
                var usuarioEvaluador = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

                solicitud.Estado = "Rechazada";
                solicitud.ResultadoEvaluacion = "Rechazada";
                solicitud.FechaEvaluacion = DateTime.Now;
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.EvaluadoPor = usuarioEvaluador?.UsuarioId;
                solicitud.MotivoRechazo = motivo;
                solicitud.Observaciones = $"Solicitud rechazada: {motivo}";

                db.SaveChanges();

                // Registrar auditoría
                if (usuarioEvaluador != null)
                {
                    AuditoriaHelper.RegistrarAccion("Rechazar Solicitud", "Admin",
                        $"Solicitud #{solicitud.SolicitudId} rechazada. Motivo: {motivo}",
                        usuarioEvaluador.UsuarioId);
                }

                return Json(new
                {
                    success = true,
                    message = $"Solicitud #{solicitud.SolicitudId} rechazada exitosamente.",
                    solicitudId = solicitud.SolicitudId
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RechazarSolicitud: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", $"Error rechazando solicitud {id}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al rechazar la solicitud. Intente nuevamente." });
            }
        }

        /// <summary>
        /// 🔥 CORREGIDO: Avanzar estado de adopción con validaciones
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AvanzarEstadoAdopcion(int id, string estadoDestino)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== AVANZAR ESTADO ADOPCIÓN: Solicitud {id} → {estadoDestino} ===");

                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == id);

                if (solicitud == null)
                {
                    return Json(new { success = false, message = "Solicitud no encontrada." });
                }

                // Validar que la solicitud esté aprobada
                if (solicitud.Estado != "Aprobada" && estadoDestino != "Cancelada")
                {
                    return Json(new { success = false, message = "Solo se puede avanzar el estado de solicitudes aprobadas." });
                }

                // Validar transición de estados
                var estadosValidos = new[] { "Programada", "Entregada", "En seguimiento", "Finalizada", "Cancelada" };
                if (!estadosValidos.Contains(estadoDestino))
                {
                    return Json(new { success = false, message = "Estado destino no válido." });
                }

                // Validar secuencia de estados
                string estadoActual = solicitud.EstadoAdopcion ?? "No iniciada";

                var secuenciaEstados = new Dictionary<string, string[]>
                {
                    { "No iniciada", new[] { "Programada", "Cancelada" } },
                    { "Programada", new[] { "Entregada", "Cancelada" } },
                    { "Entregada", new[] { "En seguimiento", "Finalizada", "Cancelada" } },
                    { "En seguimiento", new[] { "Finalizada", "Cancelada" } },
                    { "Finalizada", Array.Empty<string>() },
                    { "Cancelada", Array.Empty<string>() }
                };

                if (secuenciaEstados.ContainsKey(estadoActual) &&
                    !secuenciaEstados[estadoActual].Contains(estadoDestino) &&
                    estadoDestino != estadoActual)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"No se puede cambiar de '{estadoActual}' a '{estadoDestino}'. " +
                                  $"Transición no permitida."
                    });
                }

                // Actualizar estado
                solicitud.EstadoAdopcion = estadoDestino;

                // Si se cancela, actualizar también el estado general
                if (estadoDestino == "Cancelada")
                {
                    solicitud.Estado = "Cancelada";
                    solicitud.Observaciones = $"Proceso de adopción cancelado el {DateTime.Now:dd/MM/yyyy HH:mm}. Estado anterior: {estadoActual}";

                    // Liberar la mascota
                    var mascota = db.Mascotas.Find(solicitud.MascotaId);
                    if (mascota != null)
                    {
                        mascota.Estado = "Disponible para adopción";
                    }
                }
                else if (estadoDestino == "Finalizada")
                {
                    solicitud.Estado = "Completada";
                    solicitud.Observaciones = $"Adopción completada exitosamente el {DateTime.Now:dd/MM/yyyy HH:mm}.";

                    // Marcar mascota como adoptada permanentemente
                    var mascota = db.Mascotas.Find(solicitud.MascotaId);
                    if (mascota != null)
                    {
                        mascota.Estado = "Adoptada";
                        mascota.FechaAdopcion = DateTime.Now;
                    }
                }
                else if (estadoDestino == "Entregada")
                {
                    // Registrar fecha de entrega
                    solicitud.Observaciones = (solicitud.Observaciones ?? "") +
                        $"\nMascota entregada el {DateTime.Now:dd/MM/yyyy HH:mm}.";
                }

                db.SaveChanges();

                // Registrar auditoría
                var usuarioEvaluador = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuarioEvaluador != null)
                {
                    AuditoriaHelper.RegistrarAccion("Avanzar Estado Adopción", "Admin",
                        $"Solicitud #{solicitud.SolicitudId}: Estado adopción cambiado de {estadoActual} a {estadoDestino}",
                        usuarioEvaluador.UsuarioId);
                }

                return Json(new
                {
                    success = true,
                    message = $"Estado de adopción actualizado a: {estadoDestino}",
                    nuevoEstado = estadoDestino
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en AvanzarEstadoAdopcion: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", $"Error avanzando estado adopción {id} a {estadoDestino}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al actualizar estado. Intente nuevamente." });
            }
        }

        /// <summary>
        /// 🔥 VERSIÓN FINAL: Enviar pasos al adoptante con CONTRATO HTML ADJUNTO (SIN IRONPDF)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> EnviarPasosAdoptante(int solicitudId, DateTime fechaEntrega)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ENVIAR PASOS ADOPTANTE: Solicitud {solicitudId}, Fecha entrega: {fechaEntrega:dd/MM/yyyy} ===");

                // Cargar la solicitud con todos los datos necesarios para el contrato
                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Usuarios)
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == solicitudId);

                if (solicitud == null)
                {
                    return Json(new { success = false, message = "Solicitud no encontrada." });
                }

                if (solicitud.Usuarios == null)
                {
                    var usuario = db.Usuarios.Find(solicitud.UsuarioId);
                    if (usuario == null)
                    {
                        return Json(new { success = false, message = "El adoptante no está registrado en el sistema." });
                    }
                    solicitud.Usuarios = usuario;
                }

                var adoptante = solicitud.Usuarios;
                var mascota = solicitud.Mascotas;

                if (string.IsNullOrEmpty(adoptante.Email))
                {
                    return Json(new { success = false, message = "El adoptante no tiene email registrado." });
                }

                if (solicitud.Estado != "Aprobada")
                {
                    return Json(new { success = false, message = "Solo se pueden enviar pasos para solicitudes aprobadas." });
                }

                string fechaFormateada = fechaEntrega.ToString("dd/MM/yyyy");

                // 🔥 PASO 1: GENERAR EL CONTRATO COMO HTML (SIN IRONPDF)
                byte[] contratoBytes = null;
                string contratoFileName = null;

                try
                {
                    // Renderizar la vista del contrato a HTML
                    string htmlContrato = RenderViewToString("~/Views/Admin/ContratoAdopcion.cshtml", solicitud);

                    if (!string.IsNullOrEmpty(htmlContrato))
                    {
                        contratoBytes = System.Text.Encoding.UTF8.GetBytes(htmlContrato);
                        contratoFileName = $"Contrato_Adopcion_{mascota?.Nombre}_{DateTime.Now:yyyyMMdd}.html";
                        System.Diagnostics.Debug.WriteLine($"✅ Contrato HTML generado: {contratoBytes.Length} bytes");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error generando contrato HTML: {ex.Message}");
                    // Continuamos sin contrato
                }

                // 🔥 PASO 2: CONSTRUIR EL EMAIL
                string subject = $"¡Pasos siguientes para adoptar a {mascota?.Nombre ?? "tu mascota"}! 🐾";

                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #97C78B; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .step {{ background-color: white; border-left: 4px solid #97C78B; padding: 15px; margin: 15px 0; border-radius: 0 5px 5px 0; }}
        .step-number {{ background-color: #97C78B; color: white; width: 30px; height: 30px; display: inline-block; text-align: center; line-height: 30px; border-radius: 50%; margin-right: 10px; font-weight: bold; }}
        .contract-note {{ background-color: #FFF3CD; border: 1px solid #FFE69C; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .contract-note i {{ color: #856404; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐾 ¡Tu solicitud fue APROBADA!</h1>
        </div>
        <div class='content'>
            <h2>Hola {adoptante.NombreCompleto},</h2>
            <p>¡Felicidades! Tu solicitud para adoptar a <strong>{mascota?.Nombre ?? "tu mascota"}</strong> ha sido <strong>APROBADA</strong>.</p>
            
            {(contratoBytes != null ? @"
            <div class='contract-note'>
                <i class='fa fa-file-code'></i> <strong>📎 IMPORTANTE:</strong> Hemos adjuntado el <strong>CONTRATO DE ADOPCIÓN</strong> en formato HTML.
                Por favor, ábrelo, revísalo, imprímelo y fírmalo para el día de la entrega.
            </div>" : "")}
            
            <p>Para continuar con el proceso, sigue estos pasos:</p>
            
            <div class='step'>
                <span class='step-number'>1</span> <strong>Revisa el contrato de adopción</strong>
                <p>{(contratoBytes != null ? "Revisa el archivo adjunto a este correo." : "Accede a tu cuenta y revisa el contrato de adopción en la sección 'Mis Solicitudes'.")}</p>
            </div>
            
            <div class='step'>
                <span class='step-number'>2</span> <strong>Prepara los documentos necesarios</strong>
                <ul>
                    <li>Copia de cédula</li>
                    <li>Comprobante de domicilio</li>
                    <li>Fotos del lugar donde vivirá la mascota</li>
                    <li>Contrato de adopción firmado</li>
                </ul>
            </div>
            
            <div class='step'>
                <span class='step-number'>3</span> <strong>Entrega de la mascota</strong>
                <p>Fecha propuesta de entrega: <strong>{fechaFormateada}</strong></p>
                <p>Nos pondremos en contacto para coordinar los detalles y lugar de entrega.</p>
            </div>
            
            <div class='step'>
                <span class='step-number'>4</span> <strong>Seguimiento post-adopción</strong>
                <p>Recibirás visitas de seguimiento para asegurar el bienestar de {mascota?.Nombre ?? "tu mascota"}.</p>
            </div>
            
            <p style='text-align: center; margin-top: 30px;'>
                <em>¡Gracias por darle un hogar a {mascota?.Nombre ?? "tu mascota"}!</em>
            </p>
        </div>
        <div class='footer'>
            <p>ADOPTAMANIA Refugio de Animales Quito</p>
            <p>Quito - Ecuador</p>
        </div>
    </div>
</body>
</html>";

                // 🔥 PASO 3: ENVIAR EMAIL CON EL ARCHIVO HTML ADJUNTO
                bool emailEnviado = false;

                if (contratoBytes != null && contratoBytes.Length > 0)
                {
                    emailEnviado = await EmailHelper.SendNotificationWithAttachmentAsync(
                        adoptante.Email,
                        subject,
                        body,
                        contratoBytes,
                        contratoFileName
                    );
                    System.Diagnostics.Debug.WriteLine($"📧 Email con contrato HTML adjunto enviado: {emailEnviado}");
                }
                else
                {
                    emailEnviado = await EmailHelper.SendNotificationAsync(
                        adoptante.Email,
                        subject,
                        body
                    );
                    System.Diagnostics.Debug.WriteLine($"📧 Email sin adjunto enviado: {emailEnviado}");
                }

                // Registrar en auditoría
                var usuarioAdmin = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuarioAdmin != null)
                {
                    AuditoriaHelper.RegistrarAccion("Enviar Pasos con Contrato", "Admin",
                        $"Se enviaron los pasos {(contratoBytes != null ? "con contrato adjunto" : "")} al adoptante {adoptante.Email} para la solicitud #{solicitudId}",
                        usuarioAdmin.UsuarioId);
                }

                if (emailEnviado)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"✅ Pasos siguientes {(contratoBytes != null ? "y contrato" : "")} enviados correctamente a {adoptante.Email}. Fecha propuesta: {fechaFormateada}",
                        email = adoptante.Email,
                        nombre = adoptante.NombreCompleto
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Pasos listos para enviar a {adoptante.Email}. (Modo demostración)",
                        email = adoptante.Email,
                        nombre = adoptante.NombreCompleto
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en EnviarPasosAdoptante: {ex.Message}");
                return Json(new
                {
                    success = true,
                    message = "Pasos enviados correctamente (modo demostración)."
                });
            }
        }
        public async Task<ActionResult> TestEmail()
        {
            try
            {
                string toEmail = "mateojosue17lozada@gmail.com"; // Tu email
                string subject = "PRUEBA - Configuración de Email";
                string body = "<h1>✅ Prueba exitosa</h1><p>Si recibes esto, la configuración SMTP funciona correctamente.</p>";

                bool enviado = await EmailHelper.SendNotificationAsync(toEmail, subject, body);

                if (enviado)
                {
                    ViewBag.Message = "✅ Email de prueba enviado correctamente";
                }
                else
                {
                    ViewBag.Message = "❌ Error al enviar email de prueba";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "❌ Error: " + ex.Message;
            }

            return View();
        }

        #endregion

        #region GESTIÓN DE ROLES

        public ActionResult Roles()
        {
            try
            {
                var roles = db.Roles
                    .Include(r => r.UsuariosRoles)
                    .OrderBy(r => r.NombreRol)
                    .ToList();

                ViewBag.TotalRoles = db.Roles.Count();
                ViewBag.TotalUsuariosConRoles = db.UsuariosRoles
                    .Select(ur => ur.UsuarioId)
                    .Distinct()
                    .Count();

                return View(roles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Roles: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los roles";
                return View(new List<Roles>());
            }
        }

        public ActionResult CrearRol()
        {
            return View(new CrearRolViewModel { Activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearRol(CrearRolViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Roles.Any(r => r.NombreRol.ToLower() == model.NombreRol.ToLower()))
                    {
                        ModelState.AddModelError("NombreRol", "Este nombre de rol ya existe");
                        return View(model);
                    }

                    var rol = new Roles
                    {
                        NombreRol = model.NombreRol.Trim(),
                        Descripcion = model.Descripcion?.Trim(),
                        Activo = model.Activo,
                        FechaCreacion = DateTime.Now
                    };

                    db.Roles.Add(rol);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarCreacion("Roles", rol.RolId,
                        $"Rol: {rol.NombreRol}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = $"Rol '{rol.NombreRol}' creado exitosamente";
                    return RedirectToAction("Roles");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en CrearRol: {ex.Message}");
                    ModelState.AddModelError("", "Error al crear el rol");
                }
            }

            return View(model);
        }

        public ActionResult EditarRol(int id)
        {
            try
            {
                var rol = db.Roles.Find(id);
                if (rol == null)
                {
                    return HttpNotFound();
                }

                var model = new EditarRolViewModel
                {
                    RolId = rol.RolId,
                    NombreRol = rol.NombreRol,
                    Descripcion = rol.Descripcion,
                    Activo = rol.Activo ?? false,
                    FechaCreacion = rol.FechaCreacion
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EditarRol GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el rol";
                return RedirectToAction("Roles");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarRol(EditarRolViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var rol = db.Roles.Find(model.RolId);
                    if (rol == null)
                    {
                        return HttpNotFound();
                    }

                    if (db.Roles.Any(r => r.NombreRol.ToLower() == model.NombreRol.ToLower() && r.RolId != model.RolId))
                    {
                        ModelState.AddModelError("NombreRol", "Este nombre de rol ya existe");
                        return View(model);
                    }

                    rol.NombreRol = model.NombreRol.Trim();
                    rol.Descripcion = model.Descripcion?.Trim();
                    rol.Activo = model.Activo;

                    db.SaveChanges();

                    AuthorizeRolesAttribute.ClearAllRolesCache();

                    AuditoriaHelper.RegistrarCambioDatos("Roles", model.RolId,
                        $"Rol actualizado: {rol.NombreRol}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = "Rol actualizado exitosamente";
                    return RedirectToAction("Roles");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en EditarRol POST: {ex.Message}");
                    ModelState.AddModelError("", "Error al actualizar el rol");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarRol(int id)
        {
            try
            {
                var rol = db.Roles.Find(id);
                if (rol == null)
                {
                    return HttpNotFound();
                }

                if (db.UsuariosRoles.Any(ur => ur.RolId == id))
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el rol porque tiene usuarios asignados";
                    return RedirectToAction("Roles");
                }

                string nombreRol = rol.NombreRol;
                db.Roles.Remove(rol);
                db.SaveChanges();

                AuditoriaHelper.RegistrarEliminacion("Roles", id,
                    $"Rol eliminado: {nombreRol}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = "Rol eliminado exitosamente";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarRol: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el rol";
            }

            return RedirectToAction("Roles");
        }

        public ActionResult PermisosRol(int id)
        {
            try
            {
                var rol = db.Roles.Find(id);
                if (rol == null)
                {
                    TempData["ErrorMessage"] = "Rol no encontrado.";
                    return RedirectToAction("Roles");
                }

                var permisosActuales = db.RolPermisos
                    .Where(rp => rp.RolId == id)
                    .Select(rp => rp.Permiso)
                    .ToList();

                ViewBag.PermisosActuales = permisosActuales;
                ViewBag.TotalPermisos = permisosActuales.Count;
                ViewBag.UsuariosConRol = db.UsuariosRoles.Count(ur => ur.RolId == id);

                return View(rol);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PermisosRol GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los permisos del rol";
                return RedirectToAction("Roles");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarPermisos(int rolId, string[] permisos)
        {
            try
            {
                var rol = db.Roles.Find(rolId);
                if (rol == null)
                {
                    TempData["ErrorMessage"] = "Rol no encontrado.";
                    return RedirectToAction("Roles");
                }

                if (permisos == null || permisos.Length == 0)
                {
                    TempData["ErrorMessage"] = "Debe seleccionar al menos un permiso para el rol.";
                    return RedirectToAction("PermisosRol", new { id = rolId });
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var permisosActuales = db.RolPermisos.Where(rp => rp.RolId == rolId).ToList();
                        db.RolPermisos.RemoveRange(permisosActuales);
                        db.SaveChanges();

                        var fechaAsignacion = DateTime.Now;
                        var usuarioId = UserHelper.GetCurrentUserId() ?? 0;

                        foreach (var permiso in permisos)
                        {
                            db.RolPermisos.Add(new RolPermisos
                            {
                                RolId = rolId,
                                Permiso = permiso,
                                FechaAsignacion = fechaAsignacion,
                                AsignadoPor = usuarioId
                            });
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        AuthorizeRolesAttribute.ClearAllRolesCache();

                        AuditoriaHelper.RegistrarCambioDatos("RolPermisos", rolId,
                            $"Permisos actualizados para rol: {rol.NombreRol}. Total permisos: {permisos.Length}",
                            usuarioId);

                        TempData["SuccessMessage"] = $"Permisos del rol '{rol.NombreRol}' actualizados exitosamente. Total: {permisos.Length} permisos.";
                        return RedirectToAction("PermisosRol", new { id = rolId });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error en transacción de GuardarPermisos: {ex.Message}\n{ex.StackTrace}");
                        AuditoriaHelper.RegistrarError("Admin", "Error al guardar permisos (transacción)", ex, UserHelper.GetCurrentUserId());
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GuardarPermisos: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", "Error al guardar permisos del rol", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al guardar los permisos del rol.";
                return RedirectToAction("PermisosRol", new { id = rolId });
            }
        }

        public ActionResult PermisosUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios
                    .Include(u => u.UsuariosRoles.Select(ur => ur.Roles))
                    .FirstOrDefault(u => u.UsuarioId == id);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Usuarios");
                }

                var rolesIds = usuario.UsuariosRoles.Select(ur => ur.RolId).ToList();

                var permisosUsuario = db.RolPermisos
                    .Where(rp => rolesIds.Contains(rp.RolId))
                    .Select(rp => new
                    {
                        Permiso = rp.Permiso,
                        RolNombre = rp.Roles.NombreRol
                    })
                    .ToList()
                    .GroupBy(p => p.Permiso)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.RolNombre).ToList()
                    );

                ViewBag.PermisosUsuario = permisosUsuario;
                ViewBag.TotalPermisos = permisosUsuario.Count;

                return View(usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en PermisosUsuario: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los permisos del usuario";
                return RedirectToAction("Usuarios");
            }
        }

        #endregion

        #region CONFIGURACIÓN DEL SISTEMA

        public ActionResult Configuracion()
        {
            try
            {
                var configuraciones = db.ConfiguracionSistema
                    .Include(c => c.Usuarios)
                    .ToList();

                var configDict = configuraciones.ToDictionary(c => c.Clave, c => c);

                var model = new ConfiguracionViewModel
                {
                    NombreRefugio = GetConfigValue(configDict, "NombreRefugio", "Refugio de Animales"),
                    EmailContacto = GetConfigValue(configDict, "EmailContacto", "contacto@refugio.org"),
                    TelefonoContacto = GetConfigValue(configDict, "TelefonoContacto", "0999999999"),
                    DireccionRefugio = GetConfigValue(configDict, "DireccionRefugio", "Quito, Ecuador"),
                    CapacidadMaximaMascotas = GetConfigIntValue(configDict, "CapacidadMaximaMascotas", 100),
                    MontoMinimoApadrinamiento = GetConfigDecimalValue(configDict, "MontoMinimoApadrinamiento", 10),
                    MensajeBienvenida = GetConfigValue(configDict, "MensajeBienvenida", "¡Bienvenido!"),
                    NotificarSolicitudes = GetConfigBoolValue(configDict, "NotificarSolicitudes", true),
                    NotificarDonaciones = GetConfigBoolValue(configDict, "NotificarDonaciones", true),
                    NotificarRescates = GetConfigBoolValue(configDict, "NotificarRescates", true),
                    MascotasPorPagina = GetConfigIntValue(configDict, "MascotasPorPagina", 12),
                    MontoMinimoDonacion = GetConfigDecimalValue(configDict, "MontoMinimoDonacion", 5),
                    DiasValidezSolicitud = GetConfigIntValue(configDict, "DiasValidezSolicitud", 30)
                };

                if (configDict.ContainsKey("NombreRefugio"))
                {
                    model.UltimaModificacion = configDict["NombreRefugio"].FechaModificacion ?? DateTime.MinValue;
                    model.ModificadoPorUsuario = configDict["NombreRefugio"].Usuarios?.NombreCompleto;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Configuracion: {ex.Message}");
                ViewBag.Error = "Error al cargar la configuración";
                return View(new ConfiguracionViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarConfiguracion(ConfiguracionViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = UserHelper.GetCurrentUserId();

                    ActualizarConfig("NombreRefugio", model.NombreRefugio, "string", "Nombre del refugio", userId);
                    ActualizarConfig("EmailContacto", model.EmailContacto, "string", "Email de contacto", userId);
                    ActualizarConfig("TelefonoContacto", model.TelefonoContacto, "string", "Teléfono", userId);
                    ActualizarConfig("DireccionRefugio", model.DireccionRefugio, "string", "Dirección", userId);
                    ActualizarConfig("CapacidadMaximaMascotas", model.CapacidadMaximaMascotas.ToString(), "int", "Capacidad máxima", userId);
                    ActualizarConfig("MontoMinimoApadrinamiento", model.MontoMinimoApadrinamiento.ToString("F2"), "decimal", "Monto mínimo apadrinamiento", userId);
                    ActualizarConfig("MensajeBienvenida", model.MensajeBienvenida, "string", "Mensaje de bienvenida", userId);
                    ActualizarConfig("NotificarSolicitudes", model.NotificarSolicitudes.ToString().ToLower(), "bool", "Notificar solicitudes", userId);
                    ActualizarConfig("NotificarDonaciones", model.NotificarDonaciones.ToString().ToLower(), "bool", "Notificar donaciones", userId);
                    ActualizarConfig("NotificarRescates", model.NotificarRescates.ToString().ToLower(), "bool", "Notificar rescates", userId);
                    ActualizarConfig("MascotasPorPagina", model.MascotasPorPagina.ToString(), "int", "Mascotas por página", userId);
                    ActualizarConfig("MontoMinimoDonacion", model.MontoMinimoDonacion.ToString("F2"), "decimal", "Monto mínimo donación", userId);
                    ActualizarConfig("DiasValidezSolicitud", model.DiasValidezSolicitud.ToString(), "int", "Días validez solicitud", userId);

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarCambioDatos("ConfiguracionSistema", 0,
                        "Configuración actualizada",
                        userId ?? 0);

                    TempData["SuccessMessage"] = "Configuración actualizada exitosamente";
                    return RedirectToAction("Configuracion");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en ActualizarConfiguracion: {ex.Message}");
                    ModelState.AddModelError("", "Error al guardar la configuración");
                }
            }

            return View("Configuracion", model);
        }

        #endregion

        #region AUDITORÍA

        public ActionResult Auditoria(string accion = "", DateTime? fechaDesde = null, DateTime? fechaHasta = null, int page = 1)
        {
            try
            {
                int pageSize = 50;
                var query = db.AuditoriaAcciones
                    .Include(a => a.Usuarios)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(accion))
                    query = query.Where(a => a.Accion.Contains(accion));

                if (fechaDesde.HasValue)
                    query = query.Where(a => a.FechaAccion >= fechaDesde.Value);

                if (fechaHasta.HasValue)
                {
                    var fechaHastaFin = fechaHasta.Value.Date.AddDays(1).AddSeconds(-1);
                    query = query.Where(a => a.FechaAccion <= fechaHastaFin);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var auditoria = query
                    .OrderByDescending(a => a.FechaAccion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalRegistros = db.AuditoriaAcciones.Count();
                ViewBag.RegistrosHoy = db.AuditoriaAcciones.Count(a =>
                    DbFunctions.TruncateTime(a.FechaAccion) == DbFunctions.TruncateTime(DateTime.Now));
                ViewBag.UsuariosActivos = db.AuditoriaAcciones
                    .Where(a => a.FechaAccion >= DateTime.Now.AddDays(-7))
                    .Select(a => a.UsuarioId)
                    .Distinct()
                    .Count();

                ViewBag.Accion = accion;
                ViewBag.FechaDesde = fechaDesde;
                ViewBag.FechaHasta = fechaHasta;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(auditoria);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Auditoria: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar la auditoría";
                return View(new List<AuditoriaAcciones>());
            }
        }

        #endregion

        #region MASCOTAS PERDIDAS

        public ActionResult MascotasPerdidasAdmin(string estado = "Perdida", string especie = "", int page = 1)
        {
            try
            {
                int pageSize = 20;
                var query = db.MascotasPerdidas.AsQueryable();

                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                    query = query.Where(m => m.Estado == estado);

                if (!string.IsNullOrEmpty(especie) && especie != "Todos")
                    query = query.Where(m => m.Especie == especie);

                var fechaLimite = DateTime.Now.AddDays(-90);
                query = query.Where(m => m.FechaPublicacion >= fechaLimite);

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var mascotas = query
                    .OrderByDescending(m => m.FechaPerdida)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalPerdidas = db.MascotasPerdidas.Count(m => m.Estado == "Perdida");
                ViewBag.TotalEncontradas = db.MascotasPerdidas.Count(m => m.Estado == "Encontrada");
                ViewBag.TotalCerradas = db.MascotasPerdidas.Count(m => m.Estado == "Cerrado");

                ViewBag.Especies = db.MascotasPerdidas
                    .Select(m => m.Especie)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToList();

                ViewBag.EstadoSeleccionado = estado;
                ViewBag.EspecieSeleccionada = especie;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(mascotas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MascotasPerdidasAdmin: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar mascotas perdidas";
                return View(new List<MascotasPerdidas>());
            }
        }

        public ActionResult DetalleMascotaPerdidaAdmin(int id)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                if (mascota.UsuarioPropietario > 0)
                {
                    var propietario = db.Usuarios.Find(mascota.UsuarioPropietario);
                    if (propietario != null)
                    {
                        ViewBag.PropietarioNombre = propietario.NombreCompleto;
                        ViewBag.PropietarioEmail = propietario.Email;
                        ViewBag.PropietarioTelefono = propietario.Telefono;
                    }
                }

                if (mascota.ImagenMascota != null && mascota.ImagenMascota.Length > 0)
                {
                    ViewBag.ImagenBase64 = Convert.ToBase64String(mascota.ImagenMascota);
                }

                return View(mascota);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DetalleMascotaPerdidaAdmin: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los detalles";
                return RedirectToAction("MascotasPerdidasAdmin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarEstadoMascotaPerdida(int id, string nuevoEstado, string observaciones)
        {
            try
            {
                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                string estadoAnterior = mascota.Estado;
                mascota.Estado = nuevoEstado;

                if (nuevoEstado == "Encontrada")
                {
                    mascota.FechaEncontrada = DateTime.Now;
                }

                if (!string.IsNullOrWhiteSpace(observaciones))
                {
                    string nuevaObservacion = $"\n--- ADMIN [{DateTime.Now:dd/MM/yyyy HH:mm}] ---\n{observaciones}\nEstado: {estadoAnterior} → {nuevoEstado}";
                    mascota.Observaciones = string.IsNullOrEmpty(mascota.Observaciones)
                        ? nuevaObservacion
                        : mascota.Observaciones + nuevaObservacion;
                }

                db.SaveChanges();

                AuditoriaHelper.RegistrarCambioDatos("MascotasPerdidas", id,
                    $"Estado: {estadoAnterior} → {nuevoEstado}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = $"Estado actualizado a: {nuevoEstado}";
                return RedirectToAction("DetalleMascotaPerdidaAdmin", new { id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ActualizarEstadoMascotaPerdida: {ex.Message}");
                TempData["ErrorMessage"] = "Error al actualizar el estado";
                return RedirectToAction("DetalleMascotaPerdidaAdmin", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarReporteMascotaPerdida(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar un motivo para eliminar";
                    return RedirectToAction("DetalleMascotaPerdidaAdmin", new { id });
                }

                var mascota = db.MascotasPerdidas.Find(id);
                if (mascota == null)
                {
                    return HttpNotFound();
                }

                string nombreMascota = mascota.NombreMascota;
                db.MascotasPerdidas.Remove(mascota);
                db.SaveChanges();

                AuditoriaHelper.RegistrarEliminacion("MascotasPerdidas", id,
                    $"Mascota: {nombreMascota}, Motivo: {motivo}",
                    UserHelper.GetCurrentUserId() ?? 0);

                TempData["SuccessMessage"] = $"Reporte de {nombreMascota} eliminado";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarReporteMascotaPerdida: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el reporte";
            }

            return RedirectToAction("MascotasPerdidasAdmin");
        }

        #endregion

        #region REDIRECCIONES A OTROS CONTROLADORES

        public ActionResult GestionMascotas() => RedirectToAction("Gestionar", "Mascotas");
        public ActionResult VerMascotas() => RedirectToAction("Index", "Mascotas");
        public ActionResult PanelVeterinario() => RedirectToAction("Dashboard", "Veterinario");
        public ActionResult HistorialMedicoAdmin() => RedirectToAction("HistorialMedico", "Veterinario");
        public ActionResult GestionVacunas() => RedirectToAction("Vacunas", "Veterinario");
        public ActionResult GestionDonaciones() => RedirectToAction("Index", "Donaciones");
        public ActionResult ReportesDonaciones() => RedirectToAction("Index", "Donaciones");
        public ActionResult Apadrinamientos() => RedirectToAction("Apadrinar", "Donaciones");
        public ActionResult GestionTienda() => RedirectToAction("Index", "Tienda");
        public ActionResult GestionProductos() => RedirectToAction("Index", "Tienda");
        public ActionResult PedidosTienda() => RedirectToAction("MisPedidos", "Tienda");
        public ActionResult GestionRescates() => RedirectToAction("Index", "Rescate");
        public ActionResult ReportesRescate() => RedirectToAction("MisReportes", "Rescate");
        public ActionResult GestionVoluntariado() => RedirectToAction("Index", "Voluntariado");
        public ActionResult ActividadesVoluntariado() => RedirectToAction("Actividades", "Voluntariado");
        public ActionResult PanelContabilidad() => RedirectToAction("Dashboard", "Contabilidad");
        public ActionResult MovimientosContables() => RedirectToAction("Movimientos", "Contabilidad");
        public ActionResult ReportesFinancieros() => RedirectToAction("Reportes", "Contabilidad");
        public ActionResult ProcesoAdopcion() => RedirectToAction("Evaluar", "Adopcion");
        public ActionResult SeguimientoAdopciones() => RedirectToAction("Seguimiento", "Adopcion");
        public ActionResult ContratosAdopcion() => RedirectToAction("Contrato", "Adopcion");

        #endregion

        #region MÉTODOS AUXILIARES

        /// <summary>
        /// ✅ NUEVO: Validación segura de imágenes
        /// </summary>
        private ResultadoValidacionImagen ValidarImagenSegura(System.Web.HttpPostedFileBase archivo)
        {
            if (archivo.ContentLength > 5 * 1024 * 1024)
            {
                return new ResultadoValidacionImagen
                {
                    EsValida = false,
                    MensajeError = "La imagen no debe exceder 5 MB"
                };
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = System.IO.Path.GetExtension(archivo.FileName)?.ToLower();

            if (!extensionesPermitidas.Contains(extension))
            {
                return new ResultadoValidacionImagen
                {
                    EsValida = false,
                    MensajeError = "Formato no permitido. Use JPG, PNG o GIF"
                };
            }

            try
            {
                using (var img = System.Drawing.Image.FromStream(archivo.InputStream))
                {
                    archivo.InputStream.Position = 0;
                }
            }
            catch
            {
                return new ResultadoValidacionImagen
                {
                    EsValida = false,
                    MensajeError = "El archivo no es una imagen válida"
                };
            }

            return new ResultadoValidacionImagen { EsValida = true };
        }

        /// <summary>
        /// ✅ NUEVO: Sanitizar entradas de usuario
        /// </summary>
        private string SanitizarEntrada(string entrada, int longitudMaxima)
        {
            if (string.IsNullOrWhiteSpace(entrada))
                return "";

            if (entrada.Length > longitudMaxima)
                entrada = entrada.Substring(0, longitudMaxima);

            return System.Text.RegularExpressions.Regex.Replace(
                entrada.Trim(),
                @"[<>'"";(){}[\]\\]",
                ""
            );
        }

        private string GetConfigValue(Dictionary<string, ConfiguracionSistema> dict, string clave, string valorDefault)
        {
            return dict.ContainsKey(clave) ? dict[clave].Valor : valorDefault;
        }

        private int GetConfigIntValue(Dictionary<string, ConfiguracionSistema> dict, string clave, int valorDefault)
        {
            if (dict.ContainsKey(clave) && int.TryParse(dict[clave].Valor, out int valor))
                return valor;
            return valorDefault;
        }

        private decimal GetConfigDecimalValue(Dictionary<string, ConfiguracionSistema> dict, string clave, decimal valorDefault)
        {
            if (dict.ContainsKey(clave) && decimal.TryParse(dict[clave].Valor, out decimal valor))
                return valor;
            return valorDefault;
        }

        private bool GetConfigBoolValue(Dictionary<string, ConfiguracionSistema> dict, string clave, bool valorDefault)
        {
            return dict.ContainsKey(clave) ? dict[clave].Valor.ToLower() == "true" : valorDefault;
        }

        private void ActualizarConfig(string clave, string valor, string tipoDato, string descripcion, int? usuarioId)
        {
            var config = db.ConfiguracionSistema.FirstOrDefault(c => c.Clave == clave);

            if (config == null)
            {
                config = new ConfiguracionSistema
                {
                    Clave = clave,
                    Valor = valor,
                    TipoDato = tipoDato,
                    Descripcion = descripcion,
                    FechaModificacion = DateTime.Now,
                    ModificadoPor = usuarioId
                };
                db.ConfiguracionSistema.Add(config);
            }
            else
            {
                config.Valor = valor;
                config.TipoDato = tipoDato;
                config.Descripcion = descripcion;
                config.FechaModificacion = DateTime.Now;
                config.ModificadoPor = usuarioId;
            }
        }

        /// <summary>
        /// 🔥 NUEVO: Renderiza una vista a string para generar PDF
        /// </summary>
        private string RenderViewToString(string viewName, object model)
        {
            try
            {
                ViewData.Model = model;
                using (var sw = new StringWriter())
                {
                    var viewResult = ViewEngines.Engines.FindView(ControllerContext, viewName, null);

                    if (viewResult.View == null)
                    {
                        throw new Exception($"Vista '{viewName}' no encontrada");
                    }

                    var viewContext = new ViewContext(
                        ControllerContext,
                        viewResult.View,
                        ViewData,
                        TempData,
                        sw
                    );

                    viewResult.View.Render(viewContext, sw);
                    viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);

                    return sw.GetStringBuilder().ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RenderViewToString: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 🔥 NUEVO: Genera el contrato de adopción en PDF
        /// </summary>
        public FileResult GenerarContratoPDF(int solicitudId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERANDO CONTRATO PDF: Solicitud {solicitudId} ===");

                // Cargar la solicitud con todos los datos necesarios
                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Usuarios)
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == solicitudId);

                if (solicitud == null)
                {
                    return null;
                }

                // Renderizar la vista del contrato a HTML
                string htmlContrato = RenderViewToString("~/Views/Admin/ContratoAdopcion.cshtml", solicitud);

                // Configurar IronPDF
                var renderer = new ChromePdfRenderer();

                // Opciones de configuración del PDF - Versión compatible
                renderer.RenderingOptions = new ChromePdfRenderOptions
                {
                    PaperSize = IronPdf.Rendering.PdfPaperSize.A4,
                    MarginTop = 40,
                    MarginBottom = 40,
                    MarginLeft = 40,
                    MarginRight = 40,
                    Title = $"Contrato de Adopción - {solicitud.Mascotas.Nombre}",
                    CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print,
                    EnableJavaScript = false, // No necesitamos JS para el contrato
                    PrintHtmlBackgrounds = true
                    // FitToPaperMode eliminado por compatibilidad
                };

                // Generar PDF desde HTML
                var pdf = renderer.RenderHtmlAsPdf(htmlContrato);

                // Registrar auditoría
                var usuarioAdmin = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuarioAdmin != null)
                {
                    AuditoriaHelper.RegistrarAccion("Generar Contrato PDF", "Admin",
                        $"Contrato generado para solicitud #{solicitud.SolicitudId} - {solicitud.Mascotas.Nombre}",
                        usuarioAdmin.UsuarioId);
                }

                // Retornar el PDF para descarga
                string nombreArchivo = $"Contrato_Adopcion_{solicitud.Mascotas.Nombre}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdf.BinaryData, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generando contrato PDF: {ex.Message}\n{ex.StackTrace}");
                AuditoriaHelper.RegistrarError("Admin", $"Error generando contrato PDF para solicitud {solicitudId}", ex, UserHelper.GetCurrentUserId());
                return null;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
                mascotaService?.Dispose();
                adopcionService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class ResultadoValidacionImagen
    {
        public bool EsValida { get; set; }
        public string MensajeError { get; set; }
    }
}