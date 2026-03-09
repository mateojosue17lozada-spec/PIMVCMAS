using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using MVCMASCOTAS.Models.ViewModels;  // ← NUEVO: para SeguimientoAdopcionViewModel
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para gestión de mascotas del refugio
    /// Incluye CRUD, cambio de estados, archivado y seguimiento de adopciones
    /// VERSIÓN CORREGIDA - Fase 2 (dynamic → ViewModel tipado)
    /// </summary>
    public class MascotaService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;

        // ✅ Estados válidos según constraint de BD
        private static readonly string[] ESTADOS_VALIDOS = new[]
        {
            "Rescatada",
            "En revisión veterinaria",
            "Pendiente de exámenes",
            "En cuarentena",
            "En tratamiento",
            "No disponible",
            "Disponible para adopción",
            "Adoptada",
            "Fallecida",
            "Archivada"
        };

        // ✅ Transiciones de estado permitidas
        private static readonly Dictionary<string, List<string>> TRANSICIONES_PERMITIDAS = new Dictionary<string, List<string>>
        {
            ["Rescatada"] = new List<string> { "En revisión veterinaria", "Fallecida", "Archivada" },
            ["En revisión veterinaria"] = new List<string> { "Pendiente de exámenes", "En cuarentena", "En tratamiento", "Disponible para adopción", "Fallecida", "Archivada" },
            ["Pendiente de exámenes"] = new List<string> { "En tratamiento", "Disponible para adopción", "Fallecida", "Archivada" },
            ["En cuarentena"] = new List<string> { "En tratamiento", "Disponible para adopción", "Fallecida", "Archivada" },
            ["En tratamiento"] = new List<string> { "Disponible para adopción", "En cuarentena", "Fallecida", "Archivada" },
            ["No disponible"] = new List<string> { "Disponible para adopción", "Fallecida", "Archivada" },
            ["Disponible para adopción"] = new List<string> { "No disponible", "Adoptada", "En tratamiento", "Fallecida", "Archivada" },
            ["Adoptada"] = new List<string> { "Fallecida", "Archivada" },
            ["Fallecida"] = new List<string> { "Archivada" },
            ["Archivada"] = new List<string> { "Rescatada" }
        };

        public MascotaService()
        {
            db = new RefugioMascotasDBEntities();
        }

        #region CRUD Básico

        /// <summary>
        /// Obtener mascotas disponibles para adopción (vista pública)
        /// </summary>
        public List<Mascotas> ObtenerMascotasDisponibles()
        {
            return db.Mascotas
                .Where(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false))
                .OrderByDescending(m => m.FechaIngreso)
                .ToList();
        }

        /// <summary>
        /// Obtener mascotas con filtros y paginación (vista administrativa)
        /// </summary>
        public PaginacionModel<Mascotas> ObtenerMascotasFiltradas(FiltroMascotasModel filtro, bool incluirArchivadas = false)
        {
            IQueryable<Mascotas> query = db.Mascotas;

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            if (!string.IsNullOrEmpty(filtro.Busqueda))
            {
                string busqueda = filtro.Busqueda.Trim();
                query = query.Where(m =>
                    m.Nombre.Contains(busqueda) ||
                    (m.Raza != null && m.Raza.Contains(busqueda)) ||
                    (m.Microchip != null && m.Microchip.Contains(busqueda)) ||
                    (m.DescripcionGeneral != null && m.DescripcionGeneral.Contains(busqueda)));
            }

            if (!string.IsNullOrEmpty(filtro.Especie))
                query = query.Where(m => m.Especie == filtro.Especie);

            if (!string.IsNullOrEmpty(filtro.Sexo))
                query = query.Where(m => m.Sexo == filtro.Sexo);

            if (!string.IsNullOrEmpty(filtro.Tamanio))
                query = query.Where(m => m.Tamanio == filtro.Tamanio);

            if (!string.IsNullOrEmpty(filtro.Categoria))
                query = query.Where(m => m.Categoria == filtro.Categoria);

            if (!string.IsNullOrEmpty(filtro.Estado))
                query = query.Where(m => m.Estado == filtro.Estado);

            if (!string.IsNullOrEmpty(filtro.EdadAproximada))
                query = query.Where(m => m.EdadAproximada == filtro.EdadAproximada);

            if (filtro.Esterilizado.HasValue)
                query = query.Where(m => m.Esterilizado == filtro.Esterilizado.Value);

            if (!string.IsNullOrEmpty(filtro.TipoEspecial))
                query = query.Where(m => m.TipoEspecial == filtro.TipoEspecial);

            query = query.OrderBy(m => m.Estado == "Archivada" ? 1 : 0)
                         .ThenByDescending(m => m.FechaIngreso);

            var totalElementos = query.Count();
            var mascotas = query
                .Skip((filtro.Pagina - 1) * filtro.ElementosPorPagina)
                .Take(filtro.ElementosPorPagina)
                .ToList();

            return new PaginacionModel<Mascotas>(mascotas, totalElementos, filtro.Pagina, filtro.ElementosPorPagina);
        }

        /// <summary>
        /// Obtener mascota por ID
        /// </summary>
        public Mascotas ObtenerMascotaPorId(int mascotaId)
        {
            return db.Mascotas.Find(mascotaId);
        }

        /// <summary>
        /// Crear nueva mascota en el sistema
        /// </summary>
        public Mascotas CrearMascota(Mascotas mascota, int usuarioId)
        {
            if (!string.IsNullOrEmpty(mascota.Microchip))
            {
                var existeMicrochip = db.Mascotas.Any(m => m.Microchip == mascota.Microchip);
                if (existeMicrochip)
                    throw new InvalidOperationException($"Ya existe una mascota con el microchip {mascota.Microchip}");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    mascota.FechaIngreso = DateTime.Now;
                    mascota.Activo = true;
                    mascota.Estado = "Rescatada";

                    db.Mascotas.Add(mascota);
                    db.SaveChanges();

                    var historial = new HistorialEstadosMascota
                    {
                        MascotaId = mascota.MascotaId,
                        EstadoAnterior = null,
                        EstadoNuevo = "Rescatada",
                        Motivo = "Creación de mascota",
                        UsuarioId = usuarioId,
                        Observaciones = "Mascota creada en el sistema"
                    };
                    db.HistorialEstadosMascota.Add(historial);

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Crear Mascota",
                        controlador: "Mascotas",
                        detalles: $"Mascota creada: {mascota.Nombre} (ID: {mascota.MascotaId}), Especie: {mascota.Especie}",
                        usuarioId: usuarioId
                    );

                    db.SaveChanges();
                    transaction.Commit();
                    return mascota;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Actualizar datos de mascota existente
        /// </summary>
        public void ActualizarMascota(Mascotas mascota, int usuarioId)
        {
            var mascotaExistente = db.Mascotas.Find(mascota.MascotaId);
            if (mascotaExistente == null)
                throw new InvalidOperationException("Mascota no encontrada");

            if (!string.IsNullOrEmpty(mascota.Microchip) && mascota.Microchip != mascotaExistente.Microchip)
            {
                var existeMicrochip = db.Mascotas.Any(m => m.Microchip == mascota.Microchip && m.MascotaId != mascota.MascotaId);
                if (existeMicrochip)
                    throw new InvalidOperationException($"Ya existe otra mascota con el microchip {mascota.Microchip}");
            }

            if (mascota.Estado != mascotaExistente.Estado)
            {
                if (!ESTADOS_VALIDOS.Contains(mascota.Estado))
                    throw new InvalidOperationException($"Estado '{mascota.Estado}' no es válido");

                if (!EsTransicionValida(mascotaExistente.Estado, mascota.Estado))
                {
                    var estadosPermitidos = TRANSICIONES_PERMITIDAS.ContainsKey(mascotaExistente.Estado)
                        ? string.Join(", ", TRANSICIONES_PERMITIDAS[mascotaExistente.Estado])
                        : "ninguno";

                    throw new InvalidOperationException(
                        $"Transición no permitida: '{mascotaExistente.Estado}' → '{mascota.Estado}'. " +
                        $"Estados permitidos desde '{mascotaExistente.Estado}': {estadosPermitidos}");
                }
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    string cambios = "";
                    string estadoAnterior = mascotaExistente.Estado;

                    if (mascotaExistente.Nombre != mascota.Nombre)
                    {
                        cambios += $"Nombre: {mascotaExistente.Nombre} → {mascota.Nombre}; ";
                        mascotaExistente.Nombre = mascota.Nombre;
                    }

                    if (mascotaExistente.Estado != mascota.Estado)
                    {
                        cambios += $"Estado: {mascotaExistente.Estado} → {mascota.Estado}; ";

                        var historial = new HistorialEstadosMascota
                        {
                            MascotaId = mascota.MascotaId,
                            EstadoAnterior = estadoAnterior,
                            EstadoNuevo = mascota.Estado,
                            Motivo = "Actualización manual",
                            UsuarioId = usuarioId,
                            Observaciones = $"Estado cambiado desde {estadoAnterior} a {mascota.Estado}"
                        };
                        db.HistorialEstadosMascota.Add(historial);

                        if (mascota.Estado == "Disponible para adopción")
                            mascotaExistente.FechaDisponible = DateTime.Now;
                        else if (mascota.Estado == "Adoptada")
                            mascotaExistente.FechaAdopcion = DateTime.Now;
                        else if (mascota.Estado == "Archivada")
                            mascotaExistente.Activo = false;

                        mascotaExistente.Estado = mascota.Estado;
                    }

                    mascotaExistente.Especie = mascota.Especie;
                    mascotaExistente.Raza = mascota.Raza;
                    mascotaExistente.Sexo = mascota.Sexo;
                    mascotaExistente.EdadAproximada = mascota.EdadAproximada;
                    mascotaExistente.Tamanio = mascota.Tamanio;
                    mascotaExistente.Color = mascota.Color;
                    mascotaExistente.Categoria = mascota.Categoria;
                    mascotaExistente.TipoEspecial = mascota.TipoEspecial;
                    mascotaExistente.DescripcionGeneral = mascota.DescripcionGeneral;
                    mascotaExistente.CaracteristicasComportamiento = mascota.CaracteristicasComportamiento;
                    mascotaExistente.HistoriaRescate = mascota.HistoriaRescate;
                    mascotaExistente.Esterilizado = mascota.Esterilizado;
                    mascotaExistente.Microchip = mascota.Microchip;
                    mascotaExistente.VeterinarioAsignado = mascota.VeterinarioAsignado;
                    mascotaExistente.RescatistaId = mascota.RescatistaId;

                    if (mascota.ImagenPrincipal != null && mascota.ImagenPrincipal.Length > 0)
                    {
                        mascotaExistente.ImagenPrincipal = mascota.ImagenPrincipal;
                        cambios += "Imagen actualizada; ";
                    }

                    db.SaveChanges();

                    if (!string.IsNullOrEmpty(cambios))
                    {
                        AuditoriaHelper.RegistrarAccion(
                            accion: "Actualizar Mascota",
                            controlador: "Mascotas",
                            detalles: $"Mascota actualizada: {mascota.Nombre} (ID: {mascota.MascotaId}). Cambios: {cambios}",
                            usuarioId: usuarioId
                        );
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Validar si una transición de estado es permitida
        /// </summary>
        private bool EsTransicionValida(string estadoActual, string estadoNuevo)
        {
            if (string.IsNullOrEmpty(estadoActual) || string.IsNullOrEmpty(estadoNuevo))
                return false;

            if (!TRANSICIONES_PERMITIDAS.ContainsKey(estadoActual))
                return false;

            return TRANSICIONES_PERMITIDAS[estadoActual].Contains(estadoNuevo);
        }

        #endregion

        #region Archivado y Restauración

        /// <summary>
        /// Archivar mascota (soft delete)
        /// </summary>
        public void ArchivarMascota(int mascotaId, int usuarioId, string motivo)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota == null)
                throw new InvalidOperationException("Mascota no encontrada");

            var validacion = ValidarArchivado(mascotaId);
            if (!validacion.PuedeArchivar)
                throw new InvalidOperationException(validacion.Razon);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    string estadoAnterior = mascota.Estado;
                    mascota.Estado = "Archivada";
                    mascota.Activo = false;

                    var historial = new HistorialEstadosMascota
                    {
                        MascotaId = mascotaId,
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = "Archivada",
                        Motivo = motivo,
                        UsuarioId = usuarioId,
                        Observaciones = $"Archivado: {motivo}"
                    };
                    db.HistorialEstadosMascota.Add(historial);

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Archivar Mascota",
                        controlador: "Mascotas",
                        detalles: $"Mascota archivada: {mascota.Nombre} (ID: {mascotaId}). Estado anterior: {estadoAnterior}, Motivo: {motivo}",
                        usuarioId: usuarioId
                    );

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Validar si se puede archivar una mascota
        /// </summary>
        public ResultadoValidacion ValidarArchivado(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota == null)
                return new ResultadoValidacion { PuedeArchivar = false, Razon = "Mascota no encontrada" };

            if (mascota.Estado == "Archivada")
                return new ResultadoValidacion { PuedeArchivar = false, Razon = "La mascota ya está archivada" };

            if (mascota.Estado == "Adoptada" && mascota.FechaAdopcion.HasValue)
            {
                var tiempoAdopcion = DateTime.Now - mascota.FechaAdopcion.Value;
                if (tiempoAdopcion.TotalDays < 365)
                    return new ResultadoValidacion
                    {
                        PuedeArchivar = false,
                        Razon = "No se puede archivar una mascota adoptada antes de 1 año."
                    };
            }

            var tieneTratamientosActivos = db.Tratamientos
                .Any(t => t.MascotaId == mascotaId && t.Estado == "En curso");

            if (tieneTratamientosActivos)
                return new ResultadoValidacion
                {
                    PuedeArchivar = false,
                    Razon = "La mascota tiene tratamientos activos. Complete o suspenda los tratamientos antes de archivar."
                };

            var tieneSolicitudesActivas = db.SolicitudAdopcion
                .Any(s => s.MascotaId == mascotaId &&
                       (s.Estado == "Pendiente" || s.Estado == "En evaluación"));

            if (tieneSolicitudesActivas)
                return new ResultadoValidacion
                {
                    PuedeArchivar = false,
                    Razon = "La mascota tiene solicitudes de adopción activas."
                };

            return new ResultadoValidacion { PuedeArchivar = true, Razon = null };
        }

        /// <summary>
        /// Restaurar mascota archivada
        /// </summary>
        public void RestaurarMascotaArchivada(int mascotaId, int usuarioId, string motivo)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota == null || mascota.Estado != "Archivada")
                throw new InvalidOperationException("Solo se pueden restaurar mascotas archivadas");

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    mascota.Estado = "Rescatada";
                    mascota.Activo = true;
                    mascota.FechaIngreso = DateTime.Now;

                    var historial = new HistorialEstadosMascota
                    {
                        MascotaId = mascotaId,
                        EstadoAnterior = "Archivada",
                        EstadoNuevo = "Rescatada",
                        Motivo = $"Restauración: {motivo}",
                        UsuarioId = usuarioId,
                        Observaciones = motivo
                    };
                    db.HistorialEstadosMascota.Add(historial);

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Restaurar Mascota Archivada",
                        controlador: "Mascotas",
                        detalles: $"Mascota restaurada: {mascota.Nombre} (ID: {mascotaId})",
                        usuarioId: usuarioId
                    );

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Eliminar físicamente mascota (SOLO para casos extremos - NO USAR normalmente)
        /// </summary>
        public void EliminarFisicamente(int mascotaId, int usuarioIdAutorizado, string justificacion)
        {
            var usuario = db.Usuarios
                .Include(u => u.UsuariosRoles.Select(ur => ur.Roles))
                .FirstOrDefault(u => u.UsuarioId == usuarioIdAutorizado);

            if (usuario == null || !usuario.UsuariosRoles.Any(ur => ur.Roles.NombreRol == "Administrador"))
                throw new UnauthorizedAccessException("Solo los Administradores pueden eliminar físicamente mascotas");

            if (string.IsNullOrWhiteSpace(justificacion) || justificacion.Length < 20)
                throw new InvalidOperationException("Debe proporcionar una justificación detallada (mínimo 20 caracteres)");

            var validacion = ValidarEliminacionFisica(mascotaId);
            if (!validacion.PuedeEliminar)
                throw new InvalidOperationException(validacion.Razon);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var mascota = db.Mascotas.Find(mascotaId);

                    AuditoriaHelper.RegistrarAccion(
                        accion: "⚠️ ELIMINACIÓN FÍSICA - MASCOTA ⚠️",
                        controlador: "Mascotas",
                        detalles: $"ID: {mascotaId}, Nombre: {mascota.Nombre}, Especie: {mascota.Especie}, " +
                                  $"Estado: {mascota.Estado}, Justificación: {justificacion}, " +
                                  $"Usuario: {usuario.NombreCompleto} (ID: {usuarioIdAutorizado})",
                        usuarioId: usuarioIdAutorizado
                    );

                    db.Mascotas.Remove(mascota);
                    db.SaveChanges();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Validar si se puede eliminar físicamente una mascota
        /// </summary>
        public ResultadoValidacion ValidarEliminacionFisica(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota == null)
                return new ResultadoValidacion { PuedeEliminar = false, Razon = "Mascota no encontrada" };

            if (mascota.Estado != "Fallecida")
                return new ResultadoValidacion
                {
                    PuedeEliminar = false,
                    Razon = "⛔ Solo se pueden eliminar físicamente mascotas en estado 'Fallecida'. Use ARCHIVAR para otros casos."
                };

            var tiempoFallecida = db.HistorialEstadosMascota
                .Where(h => h.MascotaId == mascotaId && h.EstadoNuevo == "Fallecida")
                .OrderByDescending(h => h.FechaCambio)
                .FirstOrDefault();

            if (tiempoFallecida != null && tiempoFallecida.FechaCambio.HasValue)
            {
                var diasDesdeFallecimiento = (DateTime.Now - tiempoFallecida.FechaCambio.Value).TotalDays;
                if (diasDesdeFallecimiento < 730)
                    return new ResultadoValidacion
                    {
                        PuedeEliminar = false,
                        Razon = $"⛔ Deben pasar al menos 2 años desde el fallecimiento. Faltan {Math.Ceiling(730 - diasDesdeFallecimiento)} días."
                    };
            }

            var dependencias = new List<string>();

            if (db.SolicitudAdopcion.Any(s => s.MascotaId == mascotaId))
                dependencias.Add("Solicitudes de adopción");

            if (db.HistorialMedico.Any(h => h.MascotaId == mascotaId))
                dependencias.Add("Historial médico");

            if (db.MascotaVacunas.Any(v => v.MascotaId == mascotaId))
                dependencias.Add("Vacunas");

            if (db.Tratamientos.Any(t => t.MascotaId == mascotaId))
                dependencias.Add("Tratamientos");

            if (db.Apadrinamientos.Any(a => a.MascotaId == mascotaId))
                dependencias.Add("Apadrinamientos");

            if (db.HistorialEstadosMascota.Any(h => h.MascotaId == mascotaId))
                dependencias.Add("Historial de estados");

            if (db.ImagenesAdicionales.Any(i => i.EntidadTipo == "Mascota" && i.EntidadId == mascotaId))
                dependencias.Add("Imágenes adicionales");

            if (dependencias.Any())
                return new ResultadoValidacion
                {
                    PuedeEliminar = false,
                    Razon = $"⛔ ELIMINACIÓN FÍSICA BLOQUEADA. La mascota tiene registros relacionados: {string.Join(", ", dependencias)}. " +
                            "Estos registros tienen valor histórico y de auditoría. Use ARCHIVAR en lugar de eliminar."
                };

            return new ResultadoValidacion
            {
                PuedeEliminar = true,
                Razon = "⚠️ ADVERTENCIA: Esta acción es IRREVERSIBLE y eliminará permanentemente todos los datos de la mascota."
            };
        }

        #endregion

        #region Cambio de Estado

        /// <summary>
        /// Cambiar estado de mascota con validación de transiciones
        /// </summary>
        public void CambiarEstado(int mascotaId, string nuevoEstado, int usuarioId, string motivo = null)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota == null)
                throw new InvalidOperationException("Mascota no encontrada");

            if (!EsTransicionValida(mascota.Estado, nuevoEstado))
                throw new InvalidOperationException($"No se puede cambiar de '{mascota.Estado}' a '{nuevoEstado}'");

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    string estadoAnterior = mascota.Estado;
                    mascota.Estado = nuevoEstado;

                    switch (nuevoEstado)
                    {
                        case "Disponible para adopción":
                            mascota.FechaDisponible = DateTime.Now;
                            break;
                        case "Adoptada":
                            mascota.FechaAdopcion = DateTime.Now;
                            break;
                        case "Archivada":
                            mascota.Activo = false;
                            break;
                        default:
                            mascota.Activo = true;
                            break;
                    }

                    var historial = new HistorialEstadosMascota
                    {
                        MascotaId = mascotaId,
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = nuevoEstado,
                        Motivo = motivo ?? "Cambio manual de estado",
                        UsuarioId = usuarioId,
                        Observaciones = $"Cambiado de {estadoAnterior} a {nuevoEstado}"
                    };
                    db.HistorialEstadosMascota.Add(historial);

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Cambio de Estado",
                        controlador: "Mascotas",
                        detalles: $"Estado de {mascota.Nombre} (ID: {mascotaId}) cambiado de '{estadoAnterior}' a '{nuevoEstado}'",
                        usuarioId: usuarioId
                    );

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Obtener historial de cambios de estado de una mascota
        /// </summary>
        public List<HistorialEstadosMascota> ObtenerHistorialEstados(int mascotaId)
        {
            return db.HistorialEstadosMascota
                .Where(h => h.MascotaId == mascotaId)
                .OrderByDescending(h => h.FechaCambio)
                .Include(h => h.Usuarios)
                .ToList();
        }

        #endregion

        #region Seguimiento de Adopciones

        /// <summary>
        /// Crear seguimientos automáticos cuando se firma un contrato de adopción
        /// </summary>
        public void CrearSeguimientosParaContrato(int contratoId, int responsableId)
        {
            using (var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var seguimientosExistentes = db.SeguimientoAdopcion
                        .Any(s => s.ContratoId == contratoId);

                    if (seguimientosExistentes)
                        throw new InvalidOperationException("Ya existen seguimientos para este contrato");

                    var contrato = db.ContratoAdopcion.Find(contratoId);
                    if (contrato == null)
                        throw new InvalidOperationException("Contrato no encontrado");

                    var fechaContrato = contrato.FechaContrato ?? DateTime.Now;

                    var seguimientos = new[]
                    {
                        new { Dias = 15,  Tipo = "Inicial"     },
                        new { Dias = 45,  Tipo = "Primer Mes"  },
                        new { Dias = 90,  Tipo = "Tercer Mes"  },
                        new { Dias = 180, Tipo = "Sexto Mes"   },
                        new { Dias = 365, Tipo = "Anual"       }
                    };

                    foreach (var seg in seguimientos)
                    {
                        var seguimiento = new SeguimientoAdopcion
                        {
                            ContratoId = contratoId,
                            TipoSeguimiento = seg.Tipo,
                            ResponsableSeguimiento = responsableId,
                            ProximoSeguimiento = fechaContrato.AddDays(seg.Dias),
                            EstadoMascota = "Pendiente",
                            CondicionesVivienda = "Pendiente",
                            RelacionConAdoptante = "Pendiente",
                            RequiereIntervencion = false
                        };
                        db.SeguimientoAdopcion.Add(seguimiento);
                    }

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Crear Seguimientos",
                        controlador: "Mascotas",
                        detalles: $"Seguimientos creados para contrato ID: {contratoId}. Total: {seguimientos.Length}",
                        usuarioId: responsableId
                    );

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Obtener mascotas adoptadas que necesitan seguimiento
        /// </summary>
        public List<Mascotas> ObtenerMascotasAdoptadasConSeguimiento()
        {
            return db.Mascotas
                .Where(m => m.Estado == "Adoptada" && (m.Activo ?? false))
                .OrderByDescending(m => m.FechaAdopcion)
                .ToList();
        }

        /// <summary>
        /// Obtener todos los seguimientos de una mascota específica
        /// </summary>
        public List<SeguimientoAdopcion> ObtenerSeguimientosDeMascota(int mascotaId)
        {
            return db.SeguimientoAdopcion
                .Include(s => s.ContratoAdopcion)
                .Include(s => s.ContratoAdopcion.SolicitudAdopcion)
                .Include(s => s.Usuarios)
                .Where(s => s.ContratoAdopcion != null &&
                            s.ContratoAdopcion.SolicitudAdopcion != null &&
                            s.ContratoAdopcion.SolicitudAdopcion.MascotaId == mascotaId)
                .OrderBy(s => s.ProximoSeguimiento)
                .ToList();
        }

        /// <summary>
        /// Obtener mascotas adoptadas que necesitan seguimiento próximo.
        /// ✅ CORREGIDO: retorna List tipado en lugar de List dynamic
        /// </summary>
        public List<SeguimientoAdopcionViewModel> ObtenerMascotasNecesitanSeguimiento()
        {
            var hoy = DateTime.Today;
            var proximaSemana = hoy.AddDays(7);

            var mascotasAdoptadas = db.Mascotas
                .Where(m => m.Estado == "Adoptada" && (m.Activo ?? false))
                .ToList();

            var resultado = new List<SeguimientoAdopcionViewModel>();

            foreach (var mascota in mascotasAdoptadas)
            {
                var contrato = db.ContratoAdopcion
                    .Include("SolicitudAdopcion")
                    .FirstOrDefault(c => c.SolicitudAdopcion.MascotaId == mascota.MascotaId);

                string numeroContrato = contrato?.NumeroContrato ?? "Sin contrato";
                string adoptanteNombre = contrato?.AdoptanteNombre ?? "No registrado";
                int? contratoId = contrato?.ContratoId;

                var seguimientos = contratoId.HasValue
                    ? db.SeguimientoAdopcion
                        .Where(s => s.ContratoId == contratoId.Value)
                        .OrderByDescending(s => s.FechaSeguimiento)
                        .ToList()
                    : new List<SeguimientoAdopcion>();

                var ultimoSeguimiento = seguimientos
                    .Where(s => s.FechaSeguimiento != null)
                    .OrderByDescending(s => s.FechaSeguimiento)
                    .FirstOrDefault();

                var proximoPendiente = seguimientos
                    .Where(s => s.FechaSeguimiento == null && s.ProximoSeguimiento != null)
                    .OrderBy(s => s.ProximoSeguimiento)
                    .FirstOrDefault();

                bool requiereIntervencion = seguimientos.Any(s => s.RequiereIntervencion);
                DateTime? proximaFecha = proximoPendiente?.ProximoSeguimiento;

                string estadoSeguimiento;
                if (!contratoId.HasValue)
                    estadoSeguimiento = "Sin contrato";
                else if (!seguimientos.Any())
                    estadoSeguimiento = "Sin seguimientos";
                else if (proximaFecha == null)
                    estadoSeguimiento = "Sin programar";
                else if (proximaFecha < hoy)
                    estadoSeguimiento = "Vencido";
                else if (proximaFecha <= proximaSemana)
                    estadoSeguimiento = "Próximo";
                else
                    estadoSeguimiento = "Programado";

                resultado.Add(new SeguimientoAdopcionViewModel
                {
                    MascotaId = mascota.MascotaId,
                    Nombre = mascota.Nombre,
                    Especie = mascota.Especie,
                    Raza = mascota.Raza,
                    NumeroContrato = numeroContrato,
                    AdoptanteNombre = adoptanteNombre,
                    ContratoId = contratoId,
                    UltimoSeguimiento = ultimoSeguimiento?.FechaSeguimiento,
                    ProximoSeguimiento = proximaFecha,
                    RequiereIntervencion = requiereIntervencion,
                    EstadoSeguimiento = estadoSeguimiento,
                    TieneSeguimientos = seguimientos.Any(),
                    TieneContrato = contratoId.HasValue
                });
            }

            return resultado
                .OrderBy(x => x.EstadoSeguimiento == "Vencido" ? 0 :
                              x.EstadoSeguimiento == "Sin contrato" ? 1 :
                              x.EstadoSeguimiento == "Sin seguimientos" ? 2 :
                              x.EstadoSeguimiento == "Próximo" ? 3 :
                              x.EstadoSeguimiento == "Sin programar" ? 4 : 5)
                .ThenBy(x => x.ProximoSeguimiento ?? DateTime.MaxValue)
                .ToList();
        }

        #endregion

        #region Consultas Auxiliares

        /// <summary>
        /// Obtener mascotas filtradas por estado específico
        /// </summary>
        public List<Mascotas> ObtenerMascotasPorEstado(string estado, bool incluirArchivadas = false)
        {
            var query = db.Mascotas.Where(m => m.Estado == estado);

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            return query.OrderBy(m => m.Nombre).ToList();
        }

        /// <summary>
        /// Obtener las mascotas más recientes disponibles para adopción
        /// </summary>
        public List<Mascotas> ObtenerMascotasRecientes(int cantidad = 6)
        {
            return db.Mascotas
                .Where(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false))
                .OrderByDescending(m => m.FechaIngreso)
                .Take(cantidad)
                .ToList();
        }

        /// <summary>
        /// Buscar mascotas por término de búsqueda
        /// </summary>
        public List<Mascotas> BuscarMascotas(string termino, bool incluirArchivadas = false)
        {
            var query = db.Mascotas.AsQueryable();

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            return query
                .Where(m => m.Nombre.Contains(termino) ||
                            m.Especie.Contains(termino) ||
                            (m.Raza != null && m.Raza.Contains(termino)) ||
                            (m.Microchip != null && m.Microchip.Contains(termino)) ||
                            (m.DescripcionGeneral != null && m.DescripcionGeneral.Contains(termino)))
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        /// <summary>
        /// Obtener total de mascotas en el sistema
        /// </summary>
        public int ObtenerTotalMascotas(bool incluirArchivadas = false)
        {
            if (incluirArchivadas)
                return db.Mascotas.Count();

            return db.Mascotas.Count(m => m.Activo == true);
        }

        /// <summary>
        /// Obtener conteo de mascotas por estado específico
        /// </summary>
        public int ObtenerMascotasPorEstadoCount(string estado, bool incluirArchivadas = false)
        {
            var query = db.Mascotas.Where(m => m.Estado == estado);

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            return query.Count();
        }

        /// <summary>
        /// Obtener conteo de mascotas disponibles para adopción
        /// </summary>
        public int ObtenerMascotasDisponiblesCount()
        {
            return db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false));
        }

        /// <summary>
        /// Obtener lista única de especies registradas
        /// </summary>
        public List<string> ObtenerEspecies(bool incluirArchivadas = false)
        {
            var query = db.Mascotas.AsQueryable();

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            return query
                .Where(m => !string.IsNullOrEmpty(m.Especie))
                .Select(m => m.Especie)
                .Distinct()
                .OrderBy(e => e)
                .ToList();
        }

        /// <summary>
        /// Obtener razas disponibles para una especie específica
        /// </summary>
        public List<string> ObtenerRazasPorEspecie(string especie, bool incluirArchivadas = false)
        {
            var query = db.Mascotas.Where(m => m.Especie == especie);

            if (!incluirArchivadas)
                query = query.Where(m => m.Activo == true);

            return query
                .Where(m => !string.IsNullOrEmpty(m.Raza))
                .Select(m => m.Raza)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }

        /// <summary>
        /// Obtener estadísticas completas del refugio
        /// </summary>
        public Dictionary<string, int> ObtenerEstadisticasCompletas()
        {
            var estadisticas = new Dictionary<string, int>();

            foreach (var estado in ESTADOS_VALIDOS)
                estadisticas[estado] = db.Mascotas.Count(m => m.Estado == estado);

            estadisticas["Total"] = db.Mascotas.Count();
            estadisticas["Activas"] = db.Mascotas.Count(m => m.Activo == true);
            estadisticas["Archivadas"] = db.Mascotas.Count(m => m.Estado == "Archivada");

            return estadisticas;
        }

        #endregion

        /// <summary>
        /// Liberar recursos del DbContext
        /// </summary>
        public void Dispose()
        {
            db?.Dispose();
        }
    }

    /// <summary>
    /// Clase auxiliar para resultados de validación de operaciones de negocio
    /// </summary>
    public class ResultadoValidacion
    {
        public bool PuedeArchivar { get; set; }
        public bool PuedeEliminar { get; set; }
        public string Razon { get; set; }
    }
}