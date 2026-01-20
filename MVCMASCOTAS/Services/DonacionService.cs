using System;
using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class DonacionService
    {
        private readonly RefugioMascotasEntities db;
        private readonly ContabilidadService contabilidadService;

        public DonacionService()
        {
            db = new RefugioMascotasEntities();
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
                contabilidadService.RegistrarMovimiento(
                    "Ingreso",
                    monto,
                    "Donaciones",
                    $"Donación {(anonima ? "anónima" : "de usuario")} - {frecuencia}",
                    usuarioId.Value,
                    $"DON-{donacion.DonacionId}"
                );
            }

            return donacion;
        }

        // Registrar apadrinamiento
        public Apadrinamientos RegistrarApadrinamiento(int usuarioId, int mascotaId, decimal montoMensual,
            DateTime fechaInicio, int duracionMeses, string metodoPago)
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
                MetodoPago = metodoPago
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            // Registrar primer pago
            RegistrarPagoApadrinamiento(apadrinamiento.ApadrinamientoId, montoMensual, DateTime.Now, "Completado");

            return apadrinamiento;
        }

        // Registrar pago de apadrinamiento
        public PagosApadrinamiento RegistrarPagoApadrinamiento(int apadrinamientoId, decimal monto,
            DateTime fechaPago, string estado)
        {
            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamientoId,
                Monto = monto,
                FechaPago = fechaPago,
                Estado = estado
            };

            db.PagosApadrinamiento.Add(pago);
            db.SaveChanges();

            // Registrar en contabilidad
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);
            if (apadrinamiento != null && estado == "Completado")
            {
                contabilidadService.RegistrarMovimiento(
                    "Ingreso",
                    monto,
                    "Apadrinamientos",
                    $"Pago apadrinamiento mascota ID: {apadrinamiento.MascotaId}",
                    apadrinamiento.UsuarioId,
                    $"PAD-{pago.PagoId}"
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

        // Obtener apadrinamientos activos
        public List<Apadrinamientos> ObtenerApadrinamientosActivos()
        {
            return db.Apadrinamientos
                .Where(a => a.Estado == "Activo")
                .OrderBy(a => a.FechaInicio)
                .ToList();
        }

        // Obtener apadrinamientos de un usuario
        public List<Apadrinamientos> ObtenerApadrinamientosUsuario(int usuarioId)
        {
            return db.Apadrinamientos
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.FechaInicio)
                .ToList();
        }

        // Obtener apadrinamientos de una mascota
        public List<Apadrinamientos> ObtenerApadrinamientosMascota(int mascotaId)
        {
            return db.Apadrinamientos
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
                apadrinamiento.MotivoCancelacion = motivo;
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
                           d.FechaDonacion.Value.Year == anio)
                .Sum(d => (decimal?)d.Monto) ?? 0;
        }

        // Obtener número de donantes del mes
        public int ObtenerNumeroDonantesMes(int mes, int anio)
        {
            return db.Donaciones
                .Where(d => d.FechaDonacion.HasValue &&
                           d.FechaDonacion.Value.Month == mes &&
                           d.FechaDonacion.Value.Year == anio &&
                           d.UsuarioId.HasValue)
                .Select(d => d.UsuarioId.Value)
                .Distinct()
                .Count();
        }

        // Obtener últimas donaciones
        public List<Donaciones> ObtenerUltimasDonaciones(int cantidad = 10)
        {
            return db.Donaciones
                .OrderByDescending(d => d.FechaDonacion)
                .Take(cantidad)
                .ToList();
        }

        // Generar comprobante electrónico
        private string GenerarComprobanteElectronico()
        {
            var fecha = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"CE-{fecha}-{random}";
        }

        // Verificar si mascota tiene apadrinamiento activo
        public bool TieneApadrinamientoActivo(int mascotaId)
        {
            return db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId && a.Estado == "Activo");
        }

        public void Dispose()
        {
            db?.Dispose();
            contabilidadService?.Dispose();
        }
    }
}