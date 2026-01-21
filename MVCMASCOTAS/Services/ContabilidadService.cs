using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Services
{
    public class ContabilidadService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;

        public ContabilidadService()
        {
            db = new RefugioMascotasDBEntities();
        }

        // Registrar movimiento contable
        public MovimientosContables RegistrarMovimiento(string tipo, decimal monto, string categoria, string concepto, int usuarioId,
                                                       string tipoReferencia = null, int? referenciaId = null,
                                                       string metodoPago = null, string observaciones = null)
        {
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = tipo, // "Ingreso" o "Egreso" (nombre correcto según DB)
                Categoria = categoria,
                Monto = monto,
                Concepto = concepto, // Nombre correcto según DB
                FechaMovimiento = DateTime.Now,
                ResponsableRegistro = usuarioId, // Nombre correcto según DB
                TipoReferencia = tipoReferencia,
                ReferenciaId = referenciaId,
                MetodoPago = metodoPago,
                Observaciones = observaciones,
                NumeroComprobante = GenerarNumeroComprobante(tipo) // Nombre correcto según DB
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            return movimiento;
        }

        // Obtener balance del mes
        public decimal ObtenerBalanceMes(int mes, int anio)
        {
            var ingresos = ObtenerIngresosMes(mes, anio);
            var egresos = ObtenerEgresosMes(mes, anio);
            return ingresos - egresos;
        }

        // Obtener ingresos del mes
        public decimal ObtenerIngresosMes(int mes, int anio)
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso" && // Nombre correcto
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == mes &&
                           m.FechaMovimiento.Value.Year == anio)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        // Obtener egresos del mes
        public decimal ObtenerEgresosMes(int mes, int anio)
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso" && // Nombre correcto
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == mes &&
                           m.FechaMovimiento.Value.Year == anio)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        // Obtener movimientos por período
        public List<MovimientosContables> ObtenerMovimientosPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
        {
            return db.MovimientosContables
                .Where(m => m.FechaMovimiento >= fechaInicio && m.FechaMovimiento <= fechaFin)
                .OrderByDescending(m => m.FechaMovimiento)
                .ToList();
        }

        // Obtener movimientos por categoría
        public List<MovimientosContables> ObtenerMovimientosPorCategoria(string categoria, int mes, int anio)
        {
            return db.MovimientosContables
                .Where(m => m.Categoria == categoria &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == mes &&
                           m.FechaMovimiento.Value.Year == anio)
                .OrderByDescending(m => m.FechaMovimiento)
                .ToList();
        }

        // Obtener resumen por categorías
        public Dictionary<string, decimal> ObtenerResumenPorCategorias(int mes, int anio, string tipo)
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == tipo && // Nombre correcto
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == mes &&
                           m.FechaMovimiento.Value.Year == anio)
                .GroupBy(m => m.Categoria)
                .Select(g => new { Categoria = g.Key, Total = g.Sum(m => m.Monto) })
                .ToDictionary(x => x.Categoria, x => x.Total);
        }

        // Obtener últimos movimientos
        public List<MovimientosContables> ObtenerUltimosMovimientos(int cantidad = 10)
        {
            return db.MovimientosContables
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(cantidad)
                .ToList();
        }

        // Generar número de comprobante
        private string GenerarNumeroComprobante(string tipoMovimiento)
        {
            var prefijo = tipoMovimiento == "Ingreso" ? "ING" : "EGR";
            var fecha = DateTime.Now.ToString("yyyyMMdd");

            var conteoHoy = db.MovimientosContables
                .Count(m => m.TipoMovimiento == tipoMovimiento && // Nombre correcto
                           DbFunctions.TruncateTime(m.FechaMovimiento) == DbFunctions.TruncateTime(DateTime.Now));

            var consecutivo = conteoHoy + 1;
            return $"{prefijo}-{fecha}-{consecutivo:D4}";
        }

        // Obtener estadísticas anuales
        public Dictionary<int, decimal> ObtenerIngresosPorMes(int anio)
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso" && // Nombre correcto
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Year == anio)
                .GroupBy(m => m.FechaMovimiento.Value.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(m => m.Monto) })
                .ToDictionary(x => x.Mes, x => x.Total);
        }

        public Dictionary<int, decimal> ObtenerEgresosPorMes(int anio)
        {
            return db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso" && // Nombre correcto
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Year == anio)
                .GroupBy(m => m.FechaMovimiento.Value.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(m => m.Monto) })
                .ToDictionary(x => x.Mes, x => x.Total);
        }

        // Métodos adicionales útiles
        public List<MovimientosContables> BuscarMovimientos(string criterio)
        {
            return db.MovimientosContables
                .Where(m => m.Concepto.Contains(criterio) ||
                           m.Categoria.Contains(criterio) ||
                           m.NumeroComprobante.Contains(criterio))
                .OrderByDescending(m => m.FechaMovimiento)
                .Take(50)
                .ToList();
        }

        public decimal ObtenerTotalIngresos(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso");

            if (fechaInicio.HasValue)
                query = query.Where(m => m.FechaMovimiento >= fechaInicio);

            if (fechaFin.HasValue)
                query = query.Where(m => m.FechaMovimiento <= fechaFin);

            return query.Sum(m => (decimal?)m.Monto) ?? 0;
        }

        public decimal ObtenerTotalEgresos(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso");

            if (fechaInicio.HasValue)
                query = query.Where(m => m.FechaMovimiento >= fechaInicio);

            if (fechaFin.HasValue)
                query = query.Where(m => m.FechaMovimiento <= fechaFin);

            return query.Sum(m => (decimal?)m.Monto) ?? 0;
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}