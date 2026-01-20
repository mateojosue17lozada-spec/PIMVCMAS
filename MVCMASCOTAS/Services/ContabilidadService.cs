using System;
using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class ContabilidadService
    {
        private readonly RefugioMascotasEntities db;

        public ContabilidadService()
        {
            db = new RefugioMascotasEntities();
        }

        // Registrar movimiento contable
        public MovimientosContables RegistrarMovimiento(string tipo, decimal monto, string categoria, string descripcion, int usuarioId, string referencia = null)
        {
            var movimiento = new MovimientosContables
            {
                Tipo = tipo, // "Ingreso" o "Egreso"
                Categoria = categoria,
                Monto = monto,
                Descripcion = descripcion,
                FechaMovimiento = DateTime.Now,
                UsuarioRegistro = usuarioId,
                Referencia = referencia,
                Comprobante = GenerarNumeroComprobante(tipo)
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
                .Where(m => m.Tipo == "Ingreso" &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Month == mes &&
                           m.FechaMovimiento.Value.Year == anio)
                .Sum(m => (decimal?)m.Monto) ?? 0;
        }

        // Obtener egresos del mes
        public decimal ObtenerEgresosMes(int mes, int anio)
        {
            return db.MovimientosContables
                .Where(m => m.Tipo == "Egreso" &&
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
                .Where(m => m.Tipo == tipo &&
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
        private string GenerarNumeroComprobante(string tipo)
        {
            var prefijo = tipo == "Ingreso" ? "ING" : "EGR";
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var consecutivo = db.MovimientosContables
                .Where(m => m.Tipo == tipo &&
                           DbFunctions.TruncateTime(m.FechaMovimiento) == DbFunctions.TruncateTime(DateTime.Now))
                .Count() + 1;

            return $"{prefijo}-{fecha}-{consecutivo:D4}";
        }

        // Obtener estadísticas anuales
        public Dictionary<int, decimal> ObtenerIngresosPorMes(int anio)
        {
            return db.MovimientosContables
                .Where(m => m.Tipo == "Ingreso" &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Year == anio)
                .GroupBy(m => m.FechaMovimiento.Value.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(m => m.Monto) })
                .ToDictionary(x => x.Mes, x => x.Total);
        }

        public Dictionary<int, decimal> ObtenerEgresosPorMes(int anio)
        {
            return db.MovimientosContables
                .Where(m => m.Tipo == "Egreso" &&
                           m.FechaMovimiento.HasValue &&
                           m.FechaMovimiento.Value.Year == anio)
                .GroupBy(m => m.FechaMovimiento.Value.Month)
                .Select(g => new { Mes = g.Key, Total = g.Sum(m => m.Monto) })
                .ToDictionary(x => x.Mes, x => x.Total);
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}