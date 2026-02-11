using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Contabilidad", "Administrador")]
    public class ContabilidadController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Contabilidad/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                // Deshabilitar dynamic proxies para evitar el error
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.LazyLoadingEnabled = false;

                // Estadísticas del mes actual
                DateTime inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);

                ViewBag.IngresosMes = db.MovimientosContables
                    .Where(m => m.TipoMovimiento == "Ingreso" &&
                               m.FechaMovimiento >= inicioMes &&
                               m.FechaMovimiento <= finMes)
                    .Sum(m => (decimal?)m.Monto) ?? 0;

                ViewBag.EgresosMes = db.MovimientosContables
                    .Where(m => m.TipoMovimiento == "Egreso" &&
                               m.FechaMovimiento >= inicioMes &&
                               m.FechaMovimiento <= finMes)
                    .Sum(m => (decimal?)m.Monto) ?? 0;

                ViewBag.BalanceMes = ViewBag.IngresosMes - ViewBag.EgresosMes;

                // Estadísticas del año
                DateTime inicioAnio = new DateTime(DateTime.Now.Year, 1, 1);

                ViewBag.IngresosAnio = db.MovimientosContables
                    .Where(m => m.TipoMovimiento == "Ingreso" && m.FechaMovimiento >= inicioAnio)
                    .Sum(m => (decimal?)m.Monto) ?? 0;

                ViewBag.EgresosAnio = db.MovimientosContables
                    .Where(m => m.TipoMovimiento == "Egreso" && m.FechaMovimiento >= inicioAnio)
                    .Sum(m => (decimal?)m.Monto) ?? 0;

                ViewBag.BalanceAnio = ViewBag.IngresosAnio - ViewBag.EgresosAnio;

                // Últimos movimientos - Usar AsNoTracking() para mejor performance
                var ultimosMovimientos = db.MovimientosContables
                    .AsNoTracking()  // Esto evita problemas con dynamic proxies
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Take(10)
                    .ToList();

                ViewBag.UltimosMovimientos = ultimosMovimientos;

                return View();
            }
            catch (Exception ex)
            {
                // Log del error
                System.Diagnostics.Debug.WriteLine($"ERROR Dashboard Contabilidad: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Manejo de error
                TempData["ErrorMessage"] = "Error al cargar el dashboard contable. " + ex.Message;
                return View();
            }
            finally
            {
                // Restaurar configuración
                db.Configuration.ProxyCreationEnabled = true;
                db.Configuration.LazyLoadingEnabled = true;
            }
        }

        // GET: Contabilidad/Movimientos
        public ActionResult Movimientos(string tipo, string categoria, DateTime? fechaDesde,
            DateTime? fechaHasta, int page = 1)
        {
            try
            {
                int pageSize = 50;
                var query = db.MovimientosContables.AsQueryable();

                // Filtros
                if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                {
                    query = query.Where(m => m.TipoMovimiento == tipo);
                }

                if (!string.IsNullOrEmpty(categoria) && categoria != "Todos")
                {
                    query = query.Where(m => m.Categoria == categoria);
                }

                if (fechaDesde.HasValue)
                {
                    query = query.Where(m => m.FechaMovimiento >= fechaDesde.Value);
                }

                if (fechaHasta.HasValue)
                {
                    query = query.Where(m => m.FechaMovimiento <= fechaHasta.Value);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var movimientos = query
                    .Include(m => m.Usuarios)  // Para mostrar el nombre del responsable
                    .AsNoTracking()  // Evitar problemas con dynamic proxies
                    .OrderByDescending(m => m.FechaMovimiento)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Calcular totales
                ViewBag.TotalIngresos = query.Where(m => m.TipoMovimiento == "Ingreso").Sum(m => (decimal?)m.Monto) ?? 0;
                ViewBag.TotalEgresos = query.Where(m => m.TipoMovimiento == "Egreso").Sum(m => (decimal?)m.Monto) ?? 0;
                ViewBag.Balance = ViewBag.TotalIngresos - ViewBag.TotalEgresos;

                ViewBag.TipoSeleccionado = tipo;
                ViewBag.CategoriaSeleccionada = categoria;
                ViewBag.FechaDesde = fechaDesde;
                ViewBag.FechaHasta = fechaHasta;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                // Lista de categorías únicas para el dropdown
                ViewBag.Categorias = db.MovimientosContables
                    .Select(m => m.Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return View(movimientos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar movimientos: " + ex.Message;
                return View(Enumerable.Empty<MovimientosContables>().ToList());
            }
        }

        // GET: Contabilidad/RegistrarMovimiento
        public ActionResult RegistrarMovimiento()
        {
            try
            {
                ViewBag.TiposMovimiento = new SelectList(new[] { "Ingreso", "Egreso" });

                // Categorías según tu base de datos
                ViewBag.CategoriasIngreso = new SelectList(new[] {
                    "Donaciones", "Apadrinamientos", "Ventas Tienda", "Eventos", "Otros Ingresos"
                });
                ViewBag.CategoriasEgreso = new SelectList(new[] {
                    "Alimentos", "Medicamentos", "Servicios Veterinarios", "Mantenimiento",
                    "Servicios Básicos", "Personal", "Otros Egresos"
                });

                ViewBag.MetodosPago = new SelectList(new[] {
                    "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                    "Tarjeta de Débito", "Cheque", "Depósito"
                });

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar formulario: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // POST: Contabilidad/RegistrarMovimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(string tipoMovimiento, string categoria, decimal monto,
            string concepto, string metodoPago, DateTime? fechaMovimiento, string numeroComprobante,
            string observaciones)
        {
            try
            {
                // Validaciones
                if (monto <= 0)
                {
                    TempData["ErrorMessage"] = "El monto debe ser mayor a cero";
                    return RedirectToAction("RegistrarMovimiento");
                }

                if (string.IsNullOrEmpty(concepto))
                {
                    TempData["ErrorMessage"] = "El concepto es requerido";
                    return RedirectToAction("RegistrarMovimiento");
                }

                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction("Logout", "Account");
                }

                var movimiento = new MovimientosContables
                {
                    TipoMovimiento = tipoMovimiento,
                    Categoria = categoria,
                    Monto = monto,
                    Concepto = concepto,
                    MetodoPago = metodoPago,
                    FechaMovimiento = fechaMovimiento ?? DateTime.Now,
                    NumeroComprobante = numeroComprobante,
                    ResponsableRegistro = usuario.UsuarioId,
                    Observaciones = observaciones
                };

                db.MovimientosContables.Add(movimiento);
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Registrar Movimiento", "Contabilidad",
                    $"{tipoMovimiento} - {categoria}: ${monto}", usuario.UsuarioId);

                TempData["SuccessMessage"] = "Movimiento registrado exitosamente";
                return RedirectToAction("Movimientos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al registrar movimiento: " + ex.Message;
                return RedirectToAction("RegistrarMovimiento");
            }
        }

        // GET: Contabilidad/DetallesMovimiento/5
        public ActionResult DetallesMovimiento(int id)
        {
            try
            {
                var movimiento = db.MovimientosContables
                    .AsNoTracking()  // Evitar dynamic proxies
                    .Include(m => m.Usuarios)
                    .FirstOrDefault(m => m.MovimientoId == id);

                if (movimiento == null)
                {
                    TempData["ErrorMessage"] = "Movimiento no encontrado";
                    return RedirectToAction("Movimientos");
                }

                return View(movimiento);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar detalles: " + ex.Message;
                return RedirectToAction("Movimientos");
            }
        }

        // GET: Contabilidad/EditarMovimiento/5
        public ActionResult EditarMovimiento(int id)
        {
            try
            {
                var movimiento = db.MovimientosContables.Find(id);

                if (movimiento == null)
                {
                    TempData["ErrorMessage"] = "Movimiento no encontrado";
                    return RedirectToAction("Movimientos");
                }

                ViewBag.TiposMovimiento = new SelectList(new[] { "Ingreso", "Egreso" }, movimiento.TipoMovimiento);
                ViewBag.MetodosPago = new SelectList(new[] {
                    "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                    "Tarjeta de Débito", "Cheque", "Depósito"
                }, movimiento.MetodoPago);

                return View(movimiento);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar edición: " + ex.Message;
                return RedirectToAction("Movimientos");
            }
        }

        // POST: Contabilidad/EditarMovimiento/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarMovimiento(int id, MovimientosContables model)
        {
            try
            {
                var movimiento = db.MovimientosContables.Find(id);

                if (movimiento == null)
                {
                    TempData["ErrorMessage"] = "Movimiento no encontrado";
                    return RedirectToAction("Movimientos");
                }

                movimiento.TipoMovimiento = model.TipoMovimiento;
                movimiento.Categoria = model.Categoria;
                movimiento.Monto = model.Monto;
                movimiento.Concepto = model.Concepto;
                movimiento.MetodoPago = model.MetodoPago;
                movimiento.FechaMovimiento = model.FechaMovimiento;
                movimiento.NumeroComprobante = model.NumeroComprobante;
                movimiento.Observaciones = model.Observaciones;

                db.SaveChanges();

                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                if (usuario != null)
                {
                    AuditoriaHelper.RegistrarAccion("Editar Movimiento", "Contabilidad",
                        $"Movimiento ID: {id} actualizado", usuario.UsuarioId);
                }

                TempData["SuccessMessage"] = "Movimiento actualizado exitosamente";
                return RedirectToAction("Movimientos");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al actualizar movimiento: " + ex.Message;
                return RedirectToAction("EditarMovimiento", new { id = id });
            }
        }

        // GET: Contabilidad/Reportes
        public ActionResult Reportes()
        {
            return View();
        }

        // POST: Contabilidad/GenerarReporteGeneral
        [HttpPost]
        public ActionResult GenerarReporteGeneral(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var movimientos = db.MovimientosContables
                    .AsNoTracking()
                    .Where(m => m.FechaMovimiento >= fechaInicio && m.FechaMovimiento <= fechaFin)
                    .OrderBy(m => m.FechaMovimiento)
                    .ToList();

                // Agrupar por categoría
                var ingresosPorCategoria = movimientos
                    .Where(m => m.TipoMovimiento == "Ingreso")
                    .GroupBy(m => m.Categoria)
                    .Select(g => new { Categoria = g.Key, Total = g.Sum(m => m.Monto) })
                    .ToList();

                var egresosPorCategoria = movimientos
                    .Where(m => m.TipoMovimiento == "Egreso")
                    .GroupBy(m => m.Categoria)
                    .Select(g => new { Categoria = g.Key, Total = g.Sum(m => m.Monto) })
                    .ToList();

                ViewBag.FechaInicio = fechaInicio;
                ViewBag.FechaFin = fechaFin;
                ViewBag.Movimientos = movimientos;
                ViewBag.IngresosPorCategoria = ingresosPorCategoria;
                ViewBag.EgresosPorCategoria = egresosPorCategoria;
                ViewBag.TotalIngresos = ingresosPorCategoria.Sum(i => i.Total);
                ViewBag.TotalEgresos = egresosPorCategoria.Sum(e => e.Total);
                ViewBag.Balance = ViewBag.TotalIngresos - ViewBag.TotalEgresos;

                return View("ReporteGeneral");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al generar reporte: " + ex.Message;
                return RedirectToAction("Reportes");
            }
        }

        // GET: Contabilidad/ReporteDonaciones
        public ActionResult ReporteDonaciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                if (!fechaInicio.HasValue)
                    fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                if (!fechaFin.HasValue)
                    fechaFin = DateTime.Now;

                var donaciones = db.Donaciones
                    .AsNoTracking()
                    .Where(d => d.FechaDonacion >= fechaInicio.Value && d.FechaDonacion <= fechaFin.Value)
                    .OrderByDescending(d => d.FechaDonacion)
                    .ToList();

                ViewBag.FechaInicio = fechaInicio.Value;
                ViewBag.FechaFin = fechaFin.Value;
                ViewBag.TotalMonetarias = donaciones
                    .Where(d => d.TipoDonacion == "Única" || d.TipoDonacion == "Recurrente")
                    .Sum(d => (decimal?)d.Monto) ?? 0;
                ViewBag.CantidadDonaciones = donaciones.Count;
                ViewBag.DonacionesEspecie = donaciones.Count(d => d.TipoDonacion != "Única" && d.TipoDonacion != "Recurrente");

                return View(donaciones);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar reporte de donaciones: " + ex.Message;
                return View(Enumerable.Empty<Donaciones>().ToList());
            }
        }

        // GET: Contabilidad/GestionarApadrinamientos
        public ActionResult GestionarApadrinamientos(string estado, int page = 1)
        {
            try
            {
                int pageSize = 20;
                var query = db.Apadrinamientos.AsQueryable();

                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                {
                    query = query.Where(a => a.Estado == estado);
                }

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var apadrinamientos = query
                    .Include(a => a.Mascotas)
                    .Include(a => a.Usuarios)
                    .Include(a => a.PagosApadrinamiento)
                    .AsNoTracking()
                    .OrderBy(a => a.FechaInicio)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.EstadoSeleccionado = estado;
                ViewBag.TotalApadrinamientos = totalItems;
                ViewBag.IngresoMensualEstimado = query.Sum(a => (decimal?)a.MontoMensual) ?? 0;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                return View(apadrinamientos);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cargar apadrinamientos: " + ex.Message;
                return View(Enumerable.Empty<Apadrinamientos>().ToList());
            }
        }

        // Resto de los métodos se mantienen igual, solo agregar AsNoTracking() donde sea necesario...

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