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

            // Últimos movimientos
            var ultimosMovimientos = db.MovimientosContables
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(10)
                .ToList();

            ViewBag.UltimosMovimientos = ultimosMovimientos;

            return View();
        }

        // GET: Contabilidad/Movimientos
        public ActionResult Movimientos(string tipo, string categoria, DateTime? fechaDesde,
            DateTime? fechaHasta, int page = 1)
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

        // GET: Contabilidad/RegistrarMovimiento
        public ActionResult RegistrarMovimiento()
        {
            ViewBag.TiposMovimiento = new SelectList(new[] { "Ingreso", "Egreso" });

            // Categorías según tu base de datos (puedes ajustar estas)
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

        // POST: Contabilidad/RegistrarMovimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(string tipoMovimiento, string categoria, decimal monto,
            string concepto, string metodoPago, DateTime? fechaMovimiento, string numeroComprobante,
            string observaciones)
        {
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
                return RedirectToAction("Logout", "Account");
            }

            var movimiento = new MovimientosContables
            {
                TipoMovimiento = tipoMovimiento,
                Categoria = categoria,
                Monto = monto,
                Concepto = concepto,  // CORRECTO: es 'Concepto' no 'Descripcion'
                MetodoPago = metodoPago,
                FechaMovimiento = fechaMovimiento ?? DateTime.Now,
                NumeroComprobante = numeroComprobante,
                ResponsableRegistro = usuario.UsuarioId,  // CORRECTO: es 'ResponsableRegistro' no 'ResponsableRegistroId'
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

        // GET: Contabilidad/DetallesMovimiento/5
        public ActionResult DetallesMovimiento(int id)
        {
            var movimiento = db.MovimientosContables
                .Include(m => m.Usuarios)  // Para mostrar el responsable
                .FirstOrDefault(m => m.MovimientoId == id);

            if (movimiento == null)
            {
                return HttpNotFound();
            }

            return View(movimiento);
        }

        // GET: Contabilidad/EditarMovimiento/5
        public ActionResult EditarMovimiento(int id)
        {
            var movimiento = db.MovimientosContables.Find(id);

            if (movimiento == null)
            {
                return HttpNotFound();
            }

            ViewBag.TiposMovimiento = new SelectList(new[] { "Ingreso", "Egreso" }, movimiento.TipoMovimiento);
            ViewBag.MetodosPago = new SelectList(new[] {
                "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                "Tarjeta de Débito", "Cheque", "Depósito"
            }, movimiento.MetodoPago);

            return View(movimiento);
        }

        // POST: Contabilidad/EditarMovimiento/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarMovimiento(int id, MovimientosContables model)
        {
            var movimiento = db.MovimientosContables.Find(id);

            if (movimiento == null)
            {
                return HttpNotFound();
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

        // GET: Contabilidad/Reportes
        public ActionResult Reportes()
        {
            return View();
        }

        // POST: Contabilidad/GenerarReporteGeneral
        [HttpPost]
        public ActionResult GenerarReporteGeneral(DateTime fechaInicio, DateTime fechaFin)
        {
            var movimientos = db.MovimientosContables
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

        // GET: Contabilidad/ReporteDonaciones
        public ActionResult ReporteDonaciones(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (!fechaInicio.HasValue)
                fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!fechaFin.HasValue)
                fechaFin = DateTime.Now;

            var donaciones = db.Donaciones
                .Where(d => d.FechaDonacion >= fechaInicio.Value && d.FechaDonacion <= fechaFin.Value)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();

            ViewBag.FechaInicio = fechaInicio.Value;
            ViewBag.FechaFin = fechaFin.Value;

            // CORRECTO: usar Monto, no MontoEfectivo
            ViewBag.TotalMonetarias = donaciones
                .Where(d => d.TipoDonacion == "Única" || d.TipoDonacion == "Recurrente")
                .Sum(d => (decimal?)d.Monto) ?? 0;

            ViewBag.CantidadDonaciones = donaciones.Count;
            ViewBag.DonacionesEspecie = donaciones.Count(d => d.TipoDonacion != "Única" && d.TipoDonacion != "Recurrente");

            return View(donaciones);
        }

        // POST: Contabilidad/ExportarReporteDonacionesPDF
        [HttpPost]
        public ActionResult ExportarReporteDonacionesPDF(DateTime fechaInicio, DateTime fechaFin)
        {
            var donaciones = db.Donaciones
                .Include(d => d.Usuarios)
                .Where(d => d.FechaDonacion >= fechaInicio && d.FechaDonacion <= fechaFin)
                .OrderBy(d => d.FechaDonacion)
                .ToList();

            // Verificar si el PDFHelper existe, si no, crear un archivo simple
            try
            {
                byte[] pdfBytes = PdfHelper.GenerarReporteDonaciones(donaciones, fechaInicio, fechaFin);
                return File(pdfBytes, "application/pdf", $"Reporte_Donaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf");
            }
            catch
            {
                // Si no hay PDFHelper, redirigir a vista HTML
                TempData["ErrorMessage"] = "El generador de PDF no está disponible. Descargue el reporte en formato HTML.";
                return RedirectToAction("ReporteDonaciones", new { fechaInicio, fechaFin });
            }
        }

        // GET: Contabilidad/GestionarApadrinamientos
        public ActionResult GestionarApadrinamientos(string estado, int page = 1)
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

        // GET: Contabilidad/RegistrarPagoApadrinamiento/5
        public ActionResult RegistrarPagoApadrinamiento(int id)
        {
            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .Include(a => a.Usuarios)
                .FirstOrDefault(a => a.ApadrinamientoId == id);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            ViewBag.Apadrinamiento = apadrinamiento;
            ViewBag.MetodosPago = new SelectList(new[] {
                "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                "Tarjeta de Débito", "Cheque", "Depósito"
            });

            return View();
        }

        // POST: Contabilidad/RegistrarPagoApadrinamiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarPagoApadrinamiento(int apadrinamientoId, decimal monto,
            string metodoPago, DateTime? fechaPago, string mesPagado, string numeroTransaccion)
        {
            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .Include(a => a.Usuarios)
                .FirstOrDefault(a => a.ApadrinamientoId == apadrinamientoId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            if (monto <= 0)
            {
                TempData["ErrorMessage"] = "El monto debe ser mayor a cero";
                return RedirectToAction("RegistrarPagoApadrinamiento", new { id = apadrinamientoId });
            }

            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamientoId,
                Monto = monto,  // CORRECTO: es 'Monto' no 'MontoPagado'
                FechaPago = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago,
                MesPagado = mesPagado,
                NumeroTransaccion = numeroTransaccion,
                Estado = "Completado"
            };

            db.PagosApadrinamiento.Add(pago);

            // Registrar en contabilidad
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Apadrinamientos",
                Monto = monto,
                Concepto = $"Pago apadrinamiento - Mascota: {apadrinamiento.Mascotas?.Nombre}, Padrino: {apadrinamiento.Usuarios?.NombreCompleto}",
                FechaMovimiento = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago,
                NumeroComprobante = numeroTransaccion,
                ResponsableRegistro = usuario?.UsuarioId ?? 0,
                Observaciones = $"Pago correspondiente a {mesPagado}"
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            if (usuario != null)
            {
                AuditoriaHelper.RegistrarAccion("Registrar Pago Apadrinamiento", "Contabilidad",
                    $"Apadrinamiento ID: {apadrinamientoId}, Monto: ${monto}", usuario.UsuarioId);
            }

            TempData["SuccessMessage"] = "Pago registrado exitosamente";
            return RedirectToAction("GestionarApadrinamientos");
        }

        // GET: Contabilidad/DetallesApadrinamiento/5
        public ActionResult DetallesApadrinamiento(int id)
        {
            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Mascotas)
                .Include(a => a.Usuarios)
                .Include(a => a.PagosApadrinamiento)
                .FirstOrDefault(a => a.ApadrinamientoId == id);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            ViewBag.TotalPagado = apadrinamiento.PagosApadrinamiento.Sum(p => (decimal?)p.Monto) ?? 0;
            ViewBag.PagosRealizados = apadrinamiento.PagosApadrinamiento.Count;

            return View(apadrinamiento);
        }

        // GET: Contabilidad/ReporteMensual
        public ActionResult ReporteMensual(int? anio, int? mes)
        {
            if (!anio.HasValue)
                anio = DateTime.Now.Year;

            if (!mes.HasValue)
                mes = DateTime.Now.Month;

            DateTime inicioMes = new DateTime(anio.Value, mes.Value, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);

            var movimientos = db.MovimientosContables
                .Where(m => m.FechaMovimiento >= inicioMes && m.FechaMovimiento <= finMes)
                .OrderBy(m => m.FechaMovimiento)
                .ToList();

            var donaciones = db.Donaciones
                .Where(d => d.FechaDonacion >= inicioMes && d.FechaDonacion <= finMes)
                .ToList();

            var pagosApadrinamientos = db.PagosApadrinamiento
                .Where(p => p.FechaPago >= inicioMes && p.FechaPago <= finMes)
                .ToList();

            ViewBag.Anio = anio.Value;
            ViewBag.Mes = mes.Value;
            ViewBag.NombreMes = inicioMes.ToString("MMMM");
            ViewBag.Movimientos = movimientos;
            ViewBag.Donaciones = donaciones;
            ViewBag.PagosApadrinamientos = pagosApadrinamientos;

            ViewBag.TotalIngresos = movimientos.Where(m => m.TipoMovimiento == "Ingreso").Sum(m => m.Monto);
            ViewBag.TotalEgresos = movimientos.Where(m => m.TipoMovimiento == "Egreso").Sum(m => m.Monto);
            ViewBag.TotalDonaciones = donaciones.Sum(d => d.Monto);
            ViewBag.TotalApadrinamientos = pagosApadrinamientos.Sum(p => p.Monto);
            ViewBag.Balance = ViewBag.TotalIngresos - ViewBag.TotalEgresos;

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