using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Administrador")]
    public class AdminController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            var viewModel = new DashboardViewModel
            {
                // Estadísticas de Mascotas (NOMBRES CORRECTOS)
                TotalMascotas = db.Mascotas.Count(m => m.Activo),
                MascotasDisponibles = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.Activo),
                MascotasEnTratamiento = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo),
                MascotasAdoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada"), // ✅ CORRECTO

                // Adopciones
                SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente"),
                SolicitudesAprobadas = db.SolicitudAdopcion.Count(s => s.Estado == "Aprobada"),
                SolicitudesRechazadas = db.SolicitudAdopcion.Count(s => s.Estado == "Rechazada"),
                AdopcionesEsteMes = db.Mascotas.Count(m =>
                    m.Estado == "Adoptada" &&
                    m.FechaAdopcion.HasValue &&
                    m.FechaAdopcion.Value.Month == DateTime.Now.Month &&
                    m.FechaAdopcion.Value.Year == DateTime.Now.Year),

                // Donaciones (CORREGIR NOMBRES Y PROPIEDADES DE BD)
                NumeroDonantesMes = db.Donaciones.Count(d => // ✅ CORRECTO
                    d.FechaDonacion != null &&
                    d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                    d.FechaDonacion.Value.Year == DateTime.Now.Year),
                TotalDonacionesMes = db.Donaciones // ✅ CORRECTO
                    .Where(d => d.FechaDonacion != null &&
                               d.FechaDonacion.Value.Month == DateTime.Now.Month &&
                               d.FechaDonacion.Value.Year == DateTime.Now.Year)
                    .Sum(d => (decimal?)d.Monto) ?? 0, // ✅ 'Monto' no 'MontoEfectivo'

                // Voluntariado
                VoluntariosActivos = db.UsuariosRoles.Count(ur => ur.Roles.NombreRol == "Voluntario"), // ✅ CORRECTO

                // Apadrinamientos
                ApadrinamientosActivos = db.Apadrinamientos.Count(a => a.Estado == "Activo"),

                // Tienda
                ProductosDisponibles = db.Productos.Count(p => p.Stock > 0 && p.Activo), // ✅ CORRECTO
                ProductosBajoStock = db.Productos.Count(p => p.Stock > 0 && p.Stock <= p.StockMinimo),
                PedidosPendientes = db.Pedidos.Count(p => p.Estado == "Confirmado"),
                VentasMes = db.Pedidos
                    .Where(p => p.Estado == "Entregado" &&
                               p.FechaPedido != null &&
                               p.FechaPedido.Value.Month == DateTime.Now.Month &&
                               p.FechaPedido.Value.Year == DateTime.Now.Year)
                    .Sum(p => (decimal?)p.Total) ?? 0, // En BD es 'Total', no 'MontoTotal'

                // Rescate
                ReportesAbiertos = db.ReportesRescate.Count(r => r.Estado == "Pendiente" || r.Estado == "En proceso"),

                // Contabilidad (si quieres calcular)
                IngresosMes = CalcularIngresosMes(), // Método que debes crear
                EgresosMes = CalcularEgresosMes(),   // Método que debes crear
                BalanceMes = 0 // Calcular automáticamente
            };

            // Calcular Balance
            viewModel.BalanceMes = viewModel.IngresosMes - viewModel.EgresosMes;

            // Datos para ViewBag
            ViewBag.ActividadesRecientes = db.AuditoriaAcciones
                .OrderByDescending(a => a.FechaAccion)
                .Take(10)
                .ToList();

            ViewBag.SolicitudesPendientes = db.SolicitudAdopcion
                .Where(s => s.Estado == "Pendiente" || s.Estado == "En evaluación")
                .OrderBy(s => s.FechaSolicitud)
                .Take(5)
                .ToList();

            // Llenar listas del ViewModel
            viewModel.UltimasSolicitudes = db.SolicitudAdopcion
                .Where(s => s.Estado == "Pendiente")
                .OrderByDescending(s => s.FechaSolicitud)
                .Take(5)
                .Select(s => new SolicitudAdopcionViewModel
                {
                    // Mapear propiedades
                })
                .ToList();

            return View(viewModel);
        }

        // Métodos auxiliares
        private decimal CalcularIngresosMes()
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso" &&
                           m.FechaMovimiento != null &&
                           m.FechaMovimiento.Value.Month == DateTime.Now.Month &&
                           m.FechaMovimiento.Value.Year == DateTime.Now.Year)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        private decimal CalcularEgresosMes()
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso" &&
                           m.FechaMovimiento != null &&
                           m.FechaMovimiento.Value.Month == DateTime.Now.Month &&
                           m.FechaMovimiento.Value.Year == DateTime.Now.Year)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        // GET: Admin/Usuarios
        public ActionResult Usuarios(string rol, string buscar, int page = 1)
        {
            int pageSize = 20;
            var query = db.Usuarios.Where(u => u.Activo);

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(u => u.NombreCompleto.Contains(buscar) ||
                                        u.Email.Contains(buscar) ||
                                        u.Cedula.Contains(buscar));
            }

            // Filtro por rol
            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
            {
                query = query.Where(u => u.UsuariosRoles.Any(ur => ur.Roles.NombreRol == rol));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var usuarios = query
                .OrderBy(u => u.NombreCompleto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Roles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            ViewBag.RolSeleccionado = rol;
            ViewBag.Buscar = buscar;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(usuarios);
        }

        // GET: Admin/EditarUsuario/5
        public ActionResult EditarUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            ViewBag.TodosLosRoles = db.Roles.OrderBy(r => r.NombreRol).ToList();
            ViewBag.RolesDelUsuario = db.UsuariosRoles
                .Where(ur => ur.UsuarioId == id)
                .Select(ur => ur.RolId)
                .ToList();

            ViewBag.ImagenBase64 = usuario.ImagenPerfil != null
                ? ImageHelper.GetImageDataUri(usuario.ImagenPerfil)
                : null;

            return View(usuario);
        }

        // POST: Admin/ActualizarRolesUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarRolesUsuario(int usuarioId, int[] rolesSeleccionados)
        {
            var usuario = db.Usuarios.Find(usuarioId);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            // Eliminar roles existentes
            var rolesActuales = db.UsuariosRoles.Where(ur => ur.UsuarioId == usuarioId).ToList();
            db.UsuariosRoles.RemoveRange(rolesActuales);

            // Agregar nuevos roles
            if (rolesSeleccionados != null)
            {
                foreach (var rolId in rolesSeleccionados)
                {
                    db.UsuariosRoles.Add(new UsuariosRoles
                    {
                        UsuarioId = usuarioId,
                        RolId = rolId,
                        FechaAsignacion = DateTime.Now
                    });
                }
            }

            db.SaveChanges();

            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Actualizar Roles", "Admin",
                $"Usuario ID: {usuarioId}, Roles: {rolesSeleccionados?.Length ?? 0}", usuarioActual?.UsuarioId);

            TempData["SuccessMessage"] = "Roles actualizados exitosamente";
            return RedirectToAction("EditarUsuario", new { id = usuarioId });
        }

        // POST: Admin/DesactivarUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DesactivarUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);

            if (usuario == null)
            {
                return HttpNotFound();
            }

            usuario.Activo = false;
            db.SaveChanges();

            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Desactivar Usuario", "Admin",
                $"Usuario: {usuario.Email}", usuarioActual?.UsuarioId);

            TempData["SuccessMessage"] = "Usuario desactivado exitosamente";
            return RedirectToAction("Usuarios");
        }

        // GET: Admin/SolicitudesAdopcion
        public ActionResult SolicitudesAdopcion(string estado, int page = 1)
        {
            int pageSize = 20;
            var query = db.SolicitudAdopcion.AsQueryable();

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(s => s.Estado == estado);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var solicitudes = query
                .OrderByDescending(s => s.FechaSolicitud)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(solicitudes);
        }

        // GET: Admin/DetallesSolicitud/5
        public ActionResult DetallesSolicitud(int id)
        {
            var solicitud = db.SolicitudAdopcion.Find(id);

            if (solicitud == null)
            {
                return HttpNotFound();
            }

            ViewBag.Formulario = db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == id);

            ViewBag.MascotaImagen = solicitud.Mascotas.ImagenPrincipal != null
                ? ImageHelper.GetImageDataUri(solicitud.Mascotas.ImagenPrincipal)
                : null;

            ViewBag.UsuarioImagen = solicitud.Usuarios.ImagenPerfil != null
                ? ImageHelper.GetImageDataUri(solicitud.Usuarios.ImagenPerfil)
                : null;

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
            solicitud.ObservacionesEvaluador = observaciones;
            solicitud.FechaAprobacion = DateTime.Now;

            db.SaveChanges();

            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Aprobar Solicitud", "Admin",
                $"Solicitud ID: {solicitudId}", usuarioActual?.UsuarioId);

            // Enviar email
            _ = EmailHelper.SendAdoptionApprovedAsync(
                solicitud.Usuarios.Email,
                solicitud.Usuarios.NombreCompleto,
                solicitud.Mascotas.Nombre
            );

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
            solicitud.ObservacionesEvaluador = motivoRechazo;
            solicitud.FechaRespuesta = DateTime.Now;

            db.SaveChanges();

            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Rechazar Solicitud", "Admin",
                $"Solicitud ID: {solicitudId}", usuarioActual?.UsuarioId);

            TempData["SuccessMessage"] = "Solicitud rechazada";
            return RedirectToAction("DetallesSolicitud", new { id = solicitudId });
        }

        // GET: Admin/Configuracion
        public ActionResult Configuracion()
        {
            var config = db.ConfiguracionSistema.FirstOrDefault();

            if (config == null)
            {
                config = new ConfiguracionSistema
                {
                    NombreRefugio = "Refugio de Animales Quito",
                    EmailContacto = "contacto@refugioquito.org",
                    TelefonoContacto = "0999999999",
                    DireccionRefugio = "Quito, Ecuador",
                    CapacidadMaximaMascotas = 100,
                    MontoMinimoApadrinamiento = 10,
                    FechaUltimaActualizacion = DateTime.Now
                };
            }

            return View(config);
        }

        // POST: Admin/ActualizarConfiguracion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActualizarConfiguracion(ConfiguracionSistema model)
        {
            var config = db.ConfiguracionSistema.FirstOrDefault();

            if (config == null)
            {
                model.FechaUltimaActualizacion = DateTime.Now;
                db.ConfiguracionSistema.Add(model);
            }
            else
            {
                config.NombreRefugio = model.NombreRefugio;
                config.EmailContacto = model.EmailContacto;
                config.TelefonoContacto = model.TelefonoContacto;
                config.DireccionRefugio = model.DireccionRefugio;
                config.CapacidadMaximaMascotas = model.CapacidadMaximaMascotas;
                config.MontoMinimoApadrinamiento = model.MontoMinimoApadrinamiento;
                config.MensajeBienvenida = model.MensajeBienvenida;
                config.FechaUltimaActualizacion = DateTime.Now;
            }

            db.SaveChanges();

            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Actualizar Configuración", "Admin",
                "Configuración del sistema actualizada", usuarioActual?.UsuarioId);

            TempData["SuccessMessage"] = "Configuración actualizada exitosamente";
            return RedirectToAction("Configuracion");
        }

        // GET: Admin/Auditoria
        public ActionResult Auditoria(string accion, DateTime? fechaDesde, DateTime? fechaHasta, int page = 1)
        {
            int pageSize = 50;
            var query = db.AuditoriaAcciones.AsQueryable();

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

            return View(auditoria);
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
