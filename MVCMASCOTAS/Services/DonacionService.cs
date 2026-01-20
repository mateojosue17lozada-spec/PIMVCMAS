using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio de donaciones y apadrinamientos
    /// </summary>
    public class DonacionService
    {
        private RefugioMascotasEntities db;

        public DonacionService()
        {
            db = new RefugioMascotasEntities();
        }

        public DonacionService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Registra una nueva donación
        /// </summary>
        public Donaciones RegistrarDonacion(int? usuarioId, string tipoDonacion, decimal? montoEfectivo,
            string descripcion, string metodoPago, string nombreDonante, string emailDonante,
            string telefonoDonante, bool anonimo, bool publicar)
        {
            var donacion = new Donaciones
            {
                UsuarioId = usuarioId,
                TipoDonacion = tipoDonacion,
                MontoEfectivo = montoEfectivo,
                DescripcionDonacion = descripcion,
                MetodoPago = metodoPago,
                NombreDonante = nombreDonante,
                EmailDonante = emailDonante,
                TelefonoDonante = telefonoDonante,
                AnonimatoDonante = anonimo,
                PublicarEnWeb = publicar,
                FechaDonacion = DateTime.Now,
                EstadoDonacion = "Pendiente"
            };

            db.Donaciones.Add(donacion);
            db.SaveChanges();

            // Si es monetaria, registrar en contabilidad
            if (tipoDonacion == "Monetaria" && montoEfectivo.HasValue)
            {
                RegistrarEnContabilidad(donacion);
            }

            return donacion;
        }

        /// <summary>
        /// Registra la donación en contabilidad
        /// </summary>
        private void RegistrarEnContabilidad(Donaciones donacion)
        {
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Donaciones",
                Monto = donacion.MontoEfectivo.Value,
                Descripcion = $"Donación de {donacion.NombreDonante ?? "Anónimo"}",
                FechaMovimiento = donacion.FechaDonacion,
                MetodoPago = donacion.MetodoPago
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();
        }

        /// <summary>
        /// Obtiene las donaciones de un usuario
        /// </summary>
        public List<Donaciones> ObtenerDonacionesUsuario(int usuarioId)
        {
            return db.Donaciones
                .Where(d => d.UsuarioId == usuarioId)
                .OrderByDescending(d => d.FechaDonacion)
                .ToList();
        }

        /// <summary>
        /// Obtiene estadísticas de donaciones
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticasDonaciones(DateTime? desde = null, DateTime? hasta = null)
        {
            var query = db.Donaciones.AsQueryable();

            if (desde.HasValue)
                query = query.Where(d => d.FechaDonacion >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(d => d.FechaDonacion <= hasta.Value);

            var stats = new Dictionary<string, object>
            {
                ["TotalDonaciones"] = query.Count(),
                ["MontoTotal"] = query.Sum(d => d.MontoEfectivo) ?? 0,
                ["DonacionesMonetarias"] = query.Count(d => d.TipoDonacion == "Monetaria"),
                ["DonacionesEspecie"] = query.Count(d => d.TipoDonacion == "Especie"),
                ["PromedioMonetaria"] = query.Where(d => d.TipoDonacion == "Monetaria")
                    .Average(d => d.MontoEfectivo) ?? 0
            };

            return stats;
        }

        /// <summary>
        /// Crea un apadrinamiento
        /// </summary>
        public Apadrinamientos CrearApadrinamiento(int mascotaId, int padrinoId, decimal montoMensual)
        {
            // Verificar si ya existe apadrinamiento activo
            var existente = db.Apadrinamientos
                .Any(a => a.MascotaId == mascotaId && a.PadrinoId == padrinoId && a.Estado == "Activo");

            if (existente)
            {
                return null;
            }

            var apadrinamiento = new Apadrinamientos
            {
                MascotaId = mascotaId,
                PadrinoId = padrinoId,
                MontoMensual = montoMensual,
                FechaInicio = DateTime.Now,
                Estado = "Activo"
            };

            db.Apadrinamientos.Add(apadrinamiento);
            db.SaveChanges();

            return apadrinamiento;
        }

        /// <summary>
        /// Obtiene los apadrinamientos de un usuario
        /// </summary>
        public List<Apadrinamientos> ObtenerApadrinamientosUsuario(int usuarioId, bool soloActivos = true)
        {
            var query = db.Apadrinamientos.Where(a => a.PadrinoId == usuarioId);

            if (soloActivos)
            {
                query = query.Where(a => a.Estado == "Activo");
            }

            return query.OrderByDescending(a => a.FechaInicio).ToList();
        }

        /// <summary>
        /// Cancela un apadrinamiento
        /// </summary>
        public bool CancelarApadrinamiento(int apadrinamientoId, string motivo)
        {
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);

            if (apadrinamiento == null || apadrinamiento.Estado != "Activo")
            {
                return false;
            }

            apadrinamiento.Estado = "Cancelado";
            apadrinamiento.FechaFin = DateTime.Now;
            apadrinamiento.MotivoFinalizacion = motivo;

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Registra un pago de apadrinamiento
        /// </summary>
        public PagosApadrinamiento RegistrarPagoApadrinamiento(int apadrinamientoId, decimal monto,
            string metodoPago, DateTime? fechaPago = null)
        {
            var pago = new PagosApadrinamiento
            {
                ApadrinamientoId = apadrinamientoId,
                MontoPagado = monto,
                FechaPago = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago
            };

            db.PagosApadrinamiento.Add(pago);

            // Registrar en contabilidad
            var apadrinamiento = db.Apadrinamientos.Find(apadrinamientoId);
            var movimiento = new MovimientosContables
            {
                TipoMovimiento = "Ingreso",
                Categoria = "Apadrinamientos",
                Monto = monto,
                Descripcion = $"Pago apadrinamiento - {apadrinamiento.Mascotas.Nombre}",
                FechaMovimiento = fechaPago ?? DateTime.Now,
                MetodoPago = metodoPago
            };

            db.MovimientosContables.Add(movimiento);
            db.SaveChanges();

            return pago;
        }

        /// <summary>
        /// Obtiene mascotas disponibles para apadrinar
        /// </summary>
        public List<Mascotas> ObtenerMascotasParaApadrinar()
        {
            return db.Mascotas
                .Where(m => m.Activo &&
                       (m.Estado == "Rescatada" || m.Estado == "En tratamiento" ||
                        m.Estado == "Disponible para adopción"))
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
