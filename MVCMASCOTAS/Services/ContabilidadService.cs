using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio contable
    /// </summary>
    public class ContabilidadService
    {
        private RefugioMascotasEntities db;

        public ContabilidadService()
        {
            db = new RefugioMascotasEntities();
        }

        public ContabilidadService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Registra un movimiento contable
        /// </summary>
        public MovimientosContables RegistrarMovimiento(string tipo, string categoria, decimal monto,
            string descripcion, string metodoPago, DateTime? fecha, string numeroComprobante, int? responsableId)
        {
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = tipo,
                Categoria = categoria,
                Monto = monto,
                Descripcion = descripcion,
                MetodoPago = metodoPago,
                FechaMovimiento = fecha ?? DateTime.Now,
                NumeroComprobante = numeroComprobante,
                ResponsableRegistroId = responsableId
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            return movimiento;
        }

        /// <summary>
        /// Obtiene movimientos con filtros
        /// </summary>
        public List<MovimientosContables> ObtenerMovimientos(string tipo = null, string categoria = null,
            DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = db.MovimientosContables.AsQueryable();

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

            return query.OrderByDescending(m => m.FechaMovimiento).ToList();
        }

        /// <summary>
        /// Calcula el balance entre ingresos y egresos
        /// </summary>
        public Dictionary<string, decimal> CalcularBalance(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var movimientos = ObtenerMovimientos(null, null, fechaDesde, fechaHasta);

            var ingresos = movimientos
                .Where(m => m.TipoMovimiento == "Ingreso")
                .Sum(m => m.Monto);

            var egresos = movimientos
                .Where(m => m.TipoMovimiento == "Egreso")
                .Sum(m => m.Monto);

            return new Dictionary<string, decimal>
            {
                ["Ingresos"] = ingresos,
                ["Egresos"] = egresos,
                ["Balance"] = ingresos - egresos
            };
        }

        /// <summary>
        /// Obtiene ingresos agrupados por categoría
        /// </summary>
        public Dictionary<string, decimal> ObtenerIngresosPorCategoria(DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            var query = db.MovimientosContables.Where(m => m.TipoMovimiento == "Ingreso");

            if (fechaDesde.HasValue)
                query = query.Where(m => m.FechaMovimiento >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(m => m.FechaMovimiento <= fechaHasta.Value);

            return query
                .GroupBy(m => m.Categoria)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));
        }

        /// <summary>
        /// Obtiene egresos agrupados por categoría
        /// </summary>
        public Dictionary<string, decimal> ObtenerEgresosPorCategoria(DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            var query = db.MovimientosContables.Where(m => m.TipoMovimiento == "Egreso");

            if (fechaDesde.HasValue)
                query = query.Where(m => m.FechaMovimiento >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(m => m.FechaMovimiento <= fechaHasta.Value);

            return query
                .GroupBy(m => m.Categoria)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));
        }

        /// <summary>
        /// Obtiene estadísticas mensuales
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticasMes(int mes, int anio)
        {
            DateTime inicioMes = new DateTime(anio, mes, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);

            var balance = CalcularBalance(inicioMes, finMes);
            var ingresosPorCategoria = ObtenerIngresosPorCategoria(inicioMes, finMes);
            var egresosPorCategoria = ObtenerEgresosPorCategoria(inicioMes, finMes);

            return new Dictionary<string, object>
            {
                ["Mes"] = mes,
                ["Anio"] = anio,
                ["TotalIngresos"] = balance["Ingresos"],
                ["TotalEgresos"] = balance["Egresos"],
                ["Balance"] = balance["Balance"],
                ["IngresosPorCategoria"] = ingresosPorCategoria,
                ["EgresosPorCategoria"] = egresosPorCategoria
            };
        }

        /// <summary>
        /// Obtiene estadísticas anuales
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticasAnio(int anio)
        {
            DateTime inicioAnio = new DateTime(anio, 1, 1);
            DateTime finAnio = new DateTime(anio, 12, 31);

            var balance = CalcularBalance(inicioAnio, finAnio);
            var ingresosPorCategoria = ObtenerIngresosPorCategoria(inicioAnio, finAnio);
            var egresosPorCategoria = ObtenerEgresosPorCategoria(inicioAnio, finAnio);

            // Ingresos y egresos por mes
            var ingresosPorMes = new Dictionary<int, decimal>();
            var egresosPorMes = new Dictionary<int, decimal>();

            for (int mes = 1; mes <= 12; mes++)
            {
                var inicioMes = new DateTime(anio, mes, 1);
                var finMes = inicioMes.AddMonths(1).AddDays(-1);

                var balanceMes = CalcularBalance(inicioMes, finMes);
                ingresosPorMes[mes] = balanceMes["Ingresos"];
                egresosPorMes[mes] = balanceMes["Egresos"];
            }

            return new Dictionary<string, object>
            {
                ["Anio"] = anio,
                ["TotalIngresos"] = balance["Ingresos"],
                ["TotalEgresos"] = balance["Egresos"],
                ["Balance"] = balance["Balance"],
                ["IngresosPorCategoria"] = ingresosPorCategoria,
                ["EgresosPorCategoria"] = egresosPorCategoria,
                ["IngresosPorMes"] = ingresosPorMes,
                ["EgresosPorMes"] = egresosPorMes
            };
        }

        /// <summary>
        /// Obtiene el total de donaciones
        /// </summary>
        public decimal ObtenerTotalDonaciones(DateTime? fechaDesde = null, DateTime? fechaHasta = null)
        {
            var query = db.Donaciones.Where(d => d.TipoDonacion == "Monetaria");

            if (fechaDesde.HasValue)
                query = query.Where(d => d.FechaDonacion >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(d => d.FechaDonacion <= fechaHasta.Value);

            return query.Sum(d => d.MontoEfectivo) ?? 0;
        }

        /// <summary>
        /// Obtiene el total de apadrinamientos activos por mes
        /// </summary>
        public decimal ObtenerIngresoMensualApadrinamientos()
        {
            return db.Apadrinamientos
                .Where(a => a.Estado == "Activo")
                .Sum(a => (decimal?)a.MontoMensual) ?? 0;
        }

        /// <summary>
        /// Genera un resumen financiero
        /// </summary>
        public Dictionary<string, object> GenerarResumenFinanciero(DateTime fechaInicio, DateTime fechaFin)
        {
            var balance = CalcularBalance(fechaInicio, fechaFin);
            var totalDonaciones = ObtenerTotalDonaciones(fechaInicio, fechaFin);

            var resumen = new Dictionary<string, object>
            {
                ["FechaInicio"] = fechaInicio,
                ["FechaFin"] = fechaFin,
                ["TotalIngresos"] = balance["Ingresos"],
                ["TotalEgresos"] = balance["Egresos"],
                ["Balance"] = balance["Balance"],
                ["TotalDonaciones"] = totalDonaciones,
                ["IngresoMensualApadrinamientos"] = ObtenerIngresoMensualApadrinamientos(),
                ["PromedioIngresoDiario"] = CalcularPromedioIngresoDiario(fechaInicio, fechaFin),
                ["PromedioEgresoDiario"] = CalcularPromedioEgresoDiario(fechaInicio, fechaFin)
            };

            return resumen;
        }

        /// <summary>
        /// Calcula el promedio de ingresos diarios
        /// </summary>
        private decimal CalcularPromedioIngresoDiario(DateTime fechaInicio, DateTime fechaFin)
        {
            int dias = (fechaFin - fechaInicio).Days + 1;
            var totalIngresos = db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Ingreso" &&
                           m.FechaMovimiento >= fechaInicio &&
                           m.FechaMovimiento <= fechaFin)
                .Sum(m => (decimal?)m.Monto) ?? 0;

            return dias > 0 ? totalIngresos / dias : 0;
        }

        /// <summary>
        /// Calcula el promedio de egresos diarios
        /// </summary>
        private decimal CalcularPromedioEgresoDiario(DateTime fechaInicio, DateTime fechaFin)
        {
            int dias = (fechaFin - fechaInicio).Days + 1;
            var totalEgresos = db.MovimientosContables
                .Where(m => m.TipoMovimiento == "Egreso" &&
                           m.FechaMovimiento >= fechaInicio &&
                           m.FechaMovimiento <= fechaFin)
                .Sum(m => (decimal?)m.Monto) ?? 0;

            return dias > 0 ? totalEgresos / dias : 0;
        }

        /// <summary>
        /// Verifica si hay fondos suficientes
        /// </summary>
        public bool HayFondosSuficientes(decimal montoRequerido)
        {
            var balance = CalcularBalance();
            return balance["Balance"] >= montoRequerido;
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
