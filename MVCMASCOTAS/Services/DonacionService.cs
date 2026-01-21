using System;
using MVCMASCOTAS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace MVCMASCOTAS.Services
{
    public class DonacionService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;
        private readonly ContabilidadService contabilidadService;

        public DonacionService()
        {
            db = new RefugioMascotasDBEntities();
            contabilidadService = new ContabilidadService();
        }

        // Registrar donación
        public Donaciones RegistrarDonacion(int? usuarioId, string tipoDonacion, decimal monto,
            string frecuencia, string metodoPago, string numeroTransaccion, bool anonima, string mensaje = null)
        {
            var donacion = new Donaciones
            {
                UsuarioId = usuarioId,
                TipoDonacion = tipoDonacion,
                Monto = monto,
                Frecuencia = frecuencia,
                FechaDonacion = DateTime.Now,
                MetodoPago = metodoPago,
                NumeroTransaccion = numeroTransaccion,
                Estado = "Completada",
                Anonima = anonima,
                Mensaje = mensaje,
                ComprobanteElectronico = GenerarComprobanteElectronico()
            };

            db.Donaciones.Add(donacion);
            db.SaveChanges();

            // Registrar en contabilidad si es donación monetaria
            if (tipoDonacion == "Monetaria" && usuarioId.HasValue)
            {
                string descripcion = anonima ? "Donación anónima" :
                    $"Donación de usuario ID: {usuarioId.Value}";

                contabilidadService.RegistrarMovimiento(
                    tipo: "Ingreso",
                    monto: monto,
                    categoria: "Donaciones",
                    concepto: $"{descripcion} - {frecuencia}",
                    usuarioId: usuarioId.Value,
                    tipoReferencia: "Donacion",
                    referenciaId: donacion.DonacionId,
                    metodoPago: metodoPago,
                    observaciones: mensaje
                );
            }

            return donacion;
        }

        // Registrar apadrinamiento (versión mejorada)
        public Apadrinamientos RegistrarApadrinamiento(int usuarioId, int mascotaId, decimal montoMensual,
            DateTime fechaInicio, int duracionMeses, string metodoPagoPreferido, int? diaCobroMensual = 15)
        {
            var fechaFin = fechaInicio.AddMonths(duracionMeses);

            var apadrinamiento = new Apadrinamientos
            {
                UsuarioId = usuarioId,
                MascotaId = mascotaId,
                MontoMensual = montoMensual,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Activo",
                MetodoPagoPreferido = metodoPagoPreferido, // Nombre correcto
                DiaCobroMensual = diaCobroMensual,
                Observaciones = $"Apadrinamiento iniciado el {DateTime.Now:dd/MM/yyyy}"
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            // Registrar primer pago
            RegistrarPagoApadrinamiento(apadrinamiento.ApadrinamientoId, montoMensual,
                DateTime.Now, "Completado", metodoPagoPreferido);

            return apadrinamiento;
        }

        // Registrar pago de apadrinamiento (versión mejorada)
        public PagosApadrinamiento RegistrarPagoApadrinamiento(int apadrinamientoId, decimal monto,
            DateTime fechaPago, string estado, string metodoPago = null)
        {
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);
            if (apadrinamiento == null)
                throw new ArgumentException("Apadrinamiento no encontrado");

            // Determinar el mes que se está pagando
            string mesPagado = fechaPago.ToString("MMMM yyyy");

            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamientoId,
                Monto = monto,
                FechaPago = fechaPago,
                Estado = estado,
                MetodoPago = metodoPago ?? apadrinamiento.MetodoPagoPreferido,
                MesPagado = mesPagado,
                ComprobanteElectronico = GenerarNumeroComprobantePago()
            };

            db.PagosApadrinamiento.Add(pago);
            db.SaveChanges();

            // Registrar en contabilidad si el pago está completado
            if (estado == "Completado")
            {
                // Obtener información de la mascota
                var mascota = db.Mascotas.Find(apadrinamiento.MascotaId);
                string nombreMascota = mascota?.Nombre ?? $"ID: {apadrinamiento.MascotaId}";

                contabilidadService.RegistrarMovimiento(
                    tipo: "Ingreso",
                    monto: monto,
                    categoria: "Apadrinamientos",
                    concepto: $"Pago apadrinamiento: {nombreMascota}",
                    usuarioId: apadrinamiento.UsuarioId,
                    tipoReferencia: "Apadrinamiento",
                    referenciaId: apadrinamientoId,
                    metodoPago: pago.MetodoPago,
                    observaciones: $"Mes: {mesPagado}"
                );
            }

            return pago;
        }

        // Obtener donaciones de un usuario
        public List<Donaciones> ObtenerDonacionesUsuario(int usuarioId)
        {
            return db.Donaciones
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();
        }

        // Obtener apadrinamientos activos con información de mascotas
        public List<Apadrinamientos> ObtenerApadrinamientosActivos()
        {
            return db.Apadrinamientos
                .Include(a => a.Mascotas) // Incluir información de la mascota
                .Include(a => a.Usuarios) // Incluir información del usuario
                .Where(a => a.Estado == "Activo")
                .OrderBy(a => a.FechaInicio)
                .ToList();
        }

        // Obtener apadrinamientos de un usuario con información de mascotas
        public List<Apadrinamientos> ObtenerApadrinamientosUsuario(int usuarioId)
        {
            return db.Apadrinamientos
                .Include(a => a.Mascotas) // Incluir información de la mascota
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.FechaInicio)
                .ToList();
        }

        // Obtener apadrinamientos de una mascota con información del usuario
        public List<Apadrinamientos> ObtenerApadrinamientosMascota(int mascotaId)
        {
            return db.Apadrinamientos
                .Include(a => a.Usuarios) // Incluir información del usuario
                .Where(a => a.MascotaId == mascotaId)
                .OrderByDescending(a => a.FechaInicio)
                .ToList();
        }

        // Cancelar apadrinamiento
        public void CancelarApadrinamiento(int apadrinamientoId, string motivo)
        {
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);
            if (apadrinamiento != null)
            {
                apadrinamiento.Estado = "Cancelado";
                // Usar el campo Observaciones para almacenar el motivo
                apadrinamiento.Observaciones = $"CANCELADO - {DateTime.Now:dd/MM/yyyy}: {motivo}. " +
                    (apadrinamiento.Observaciones ?? "");
                db.SaveChanges();
            }
        }

        // Suspender apadrinamiento temporalmente
        public void SuspenderApadrinamiento(int apadrinamientoId, string motivo, DateTime? fechaReanudacion = null)
        {
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);
            if (apadrinamiento != null)
            {
                apadrinamiento.Estado = "Pausado";
                string observacion = $"SUSPENDIDO - {DateTime.Now:dd/MM/yyyy}: {motivo}";
                if (fechaReanudacion.HasValue)
                    observacion += $". Reanudación programada: {fechaReanudacion.Value:dd/MM/yyyy}";
                apadrinamiento.Observaciones = observacion + ". " + (apadrinamiento.Observaciones ?? "");
                db.SaveChanges();
            }
        }

        // Obtener total de donaciones del mes
        public decimal ObtenerTotalDonacionesMes(int mes, int anio)
        {
            return db.Donaciones
                .Where(d => d.TipoDonacion == "Monetaria" &&
                           d.FechaDonacion.HasValue &&
                           d.FechaDonacion.Value.Month == mes &&
                           d.FechaDonacion.Value.Year == anio &&
                           d.Estado == "Completada")
                .Sum(d => d.Monto); // Monto ya es decimal, no nullable
        }

        // Obtener total de pagos de apadrinamiento del mes
        public decimal ObtenerTotalApadrinamientosMes(int mes, int anio)
        {
            return db.PagosApadrinamiento
                .Where(p => p.FechaPago.HasValue &&
                           p.FechaPago.Value.Month == mes &&
                           p.FechaPago.Value.Year == anio &&
                           p.Estado == "Completado")
                .Sum(p => p.Monto);
        }

        // Obtener número de donantes del mes
        public int ObtenerNumeroDonantesMes(int mes, int anio)
        {
            return db.Donaciones
                .Where(d => d.FechaDonacion.HasValue &&
                           d.FechaDonacion.Value.Month == mes &&
                           d.FechaDonacion.Value.Year == anio &&
                           d.UsuarioId.HasValue &&
                           d.TipoDonacion == "Monetaria")
                .Select(d => d.UsuarioId.Value)
                .Distinct()
                .Count();
        }

        // Obtener número de padrinos activos
        public int ObtenerNumeroPadrinosActivos()
        {
            return db.Apadrinamientos
                .Where(a => a.Estado == "Activo")
                .Select(a => a.UsuarioId)
                .Distinct()
                .Count();
        }

        // Obtener últimas donaciones
        public List<Donaciones> ObtenerUltimasDonaciones(int cantidad = 10)
        {
            return db.Donaciones
                .Include(d => d.Usuarios) // Incluir información del usuario
                .Where(d => d.TipoDonacion == "Monetaria")
                .OrderByDescending(d => d.FechaDonacion)
                .Take(cantidad)
                .ToList();
        }

        // Obtener donaciones por tipo
        public Dictionary<string, decimal> ObtenerResumenDonacionesPorTipo(int mes, int anio)
        {
            return db.Donaciones
                .Where(d => d.FechaDonacion.HasValue &&
                           d.FechaDonacion.Value.Month == mes &&
                           d.FechaDonacion.Value.Year == anio &&
                           d.Estado == "Completada")
                .GroupBy(d => d.TipoDonacion)
                .Select(g => new { Tipo = g.Key, Total = g.Sum(d => d.Monto) })
                .ToDictionary(x => x.Tipo ?? "Sin tipo", x => x.Total);
        }

        // Generar comprobante electrónico para donaciones
        private string GenerarComprobanteElectronico()
        {
            var fecha = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"DON-{fecha}-{random}";
        }

        // Generar número de comprobante para pagos de apadrinamiento
        private string GenerarNumeroComprobantePago()
        {
            var fecha = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"PAG-APD-{fecha}-{random}";
        }

        // Verificar si mascota tiene apadrinamiento activo
        public bool TieneApadrinamientoActivo(int mascotaId)
        {
            return db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId && a.Estado == "Activo");
        }

        // Obtener pagos vencidos de apadrinamientos
        public List<Apadrinamientos> ObtenerApadrinamientosConPagosVencidos()
        {
            // Buscar apadrinamientos activos cuyo último pago sea de hace más de 35 días
            var apadrinamientosActivos = db.Apadrinamientos
                .Where(a => a.Estado == "Activo")
                .ToList();

            var vencidos = new List<Apadrinamientos>();

            foreach (var apadrinamiento in apadrinamientosActivos)
            {
                var ultimoPago = db.PagosApadrinamiento
                    .Where(p => p.ApadrinamientoId == apadrinamiento.ApadrinamientoId &&
                               p.Estado == "Completado")
                    .OrderByDescending(p => p.FechaPago)
                    .FirstOrDefault();

                if (ultimoPago == null ||
                    (DateTime.Now - ultimoPago.FechaPago.GetValueOrDefault()).TotalDays > 35)
                {
                    vencidos.Add(apadrinamiento);
                }
            }

            return vencidos;
        }

        // Enviar recordatorio de pago
        public void EnviarRecordatorioPago(int apadrinamientoId)
        {
            var apadrinamiento = db.Apadrinamientos
                .Include(a => a.Usuarios)
                .Include(a => a.Mascotas)
                .FirstOrDefault(a => a.ApadrinamientoId == apadrinamientoId);

            if (apadrinamiento != null && apadrinamiento.Usuarios != null)
            {
                // Aquí implementarías el envío de email o notificación
                // Por ahora solo actualizamos las observaciones
                apadrinamiento.Observaciones = $"Recordatorio enviado {DateTime.Now:dd/MM/yyyy HH:mm}. " +
                    (apadrinamiento.Observaciones ?? "");
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            contabilidadService?.Dispose();
            db?.Dispose();
        }
    }
}