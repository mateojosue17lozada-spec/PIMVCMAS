using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Contabilidad", "Administrador")]
    public class ContabilidadController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

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

            return View(movimientos);
        }

        // GET: Contabilidad/RegistrarMovimiento
        public ActionResult RegistrarMovimiento()
        {
            ViewBag.TiposMovimiento = new SelectList(new[] { "Ingreso", "Egreso" });
            ViewBag.CategoriasIngreso = new SelectList(new[] {
                "Donaciones", "Apadrinamientos", "Ventas Tienda", "Eventos", "Otros Ingresos"
            });
            ViewBag.CategoriasEgreso = new SelectList(new[] {
                "Alimentos", "Medicamentos", "Servicios Veterinarios", "Mantenimiento",
                "Servicios Básicos", "Personal", "Otros Egresos"
            });

            return View();
        }

        // POST: Contabilidad/RegistrarMovimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(string tipoMovimiento, string categoria, decimal monto,
            string descripcion, string metodoPago, DateTime? fechaMovimiento, string numeroComprobante)
        {
            if (monto <= 0)
            {
                TempData["ErrorMessage"] = "El monto debe ser mayor a cero";
                return RedirectToAction("RegistrarMovimiento");
            }

            if (string.IsNullOrEmpty(descripcion))
            {
                TempData["ErrorMessage"] = "La descripción es requerida";
                return RedirectToAction("RegistrarMovimiento");
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var movimiento = new MovimientosContables
            {
                TipoMovimiento = tipoMovimiento,
                Categoria = categoria,
                Monto = monto,
                Descripcion = descripcion,
                MetodoPago = metodoPago,
                FechaMovimiento = fechaMovimiento ?? DateTime.Now,
                NumeroComprobante = numeroComprobante,
                ResponsableRegistroId = usuario.UsuarioId
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Registrar Movimiento", "Contabilidad",
                $"{tipoMovimiento} - {categoria}: ${monto}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Movimiento registrado exitosamente";
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

            return View();
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
            ViewBag.TotalMonetarias = donaciones.Where(d => d.TipoDonacion == "Monetaria").Sum(d => d.MontoEfectivo) ?? 0;
            ViewBag.CantidadDonaciones = donaciones.Count;
            ViewBag.DonacionesEspecie = donaciones.Count(d => d.TipoDonacion == "Especie");

            return View(donaciones);
        }

        // POST: Contabilidad/ExportarReportePDF
        [HttpPost]
        public ActionResult ExportarReporteDonacionesPDF(DateTime fechaInicio, DateTime fechaFin)
        {
            var donaciones = db.Donaciones
                .Where(d => d.FechaDonacion >= fechaInicio && d.FechaDonacion <= fechaFin)
                .OrderBy(d => d.FechaDonacion)
                .ToList();

            byte[] pdfBytes = PdfHelper.GenerarReporteDonaciones(donaciones, fechaInicio, fechaFin);

            return File(pdfBytes, "application/pdf", $"Reporte_Donaciones_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf");
        }

        // GET: Contabilidad/GestionarApadrinamientos
        public ActionResult GestionarApadrinamientos()
        {
            var apadrinamientos = db.Apadrinamientos
                .Where(a => a.Estado == "Activo")
                .OrderBy(a => a.FechaInicio)
                .ToList();

            ViewBag.TotalApadrinamientos = apadrinamientos.Count;
            ViewBag.IngresoMensualEstimado = apadrinamientos.Sum(a => a.MontoMensual);

            return View(apadrinamientos);
        }

        // POST: Contabilidad/RegistrarPagoApadrinamiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarPagoApadrinamiento(int apadrinamientoId, decimal monto,
            string metodoPago, DateTime? fechaPago)
        {
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);

            if (apadrinamiento == null)
            {
                return HttpNotFound();
            }

            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamientoId,
                MontoPagado = monto,
                FechaPago = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago
            };

            db.PagosApadrinamiento.Add(pago);

            // Registrar en contabilidad
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Apadrinamientos",
                Monto = monto,
                Descripcion = $"Pago apadrinamiento - Mascota: {apadrinamiento.Mascotas.Nombre}, Padrino: {apadrinamiento.Usuarios.NombreCompleto}",
                FechaMovimiento = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            AuditoriaHelper.RegistrarAccion("Registrar Pago Apadrinamiento", "Contabilidad",
                $"Apadrinamiento ID: {apadrinamientoId}, Monto: ${monto}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Pago registrado exitosamente";
            return RedirectToAction("GestionarApadrinamientos");
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
