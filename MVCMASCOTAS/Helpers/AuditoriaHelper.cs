using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para registro completo de auditoría de acciones del sistema
    /// Registra: Logins, Cambios de datos, Acciones críticas, Errores
    /// </summary>
    public static class AuditoriaHelper
    {
        #region REGISTRO DE AUDITORÍA GENERAL

        /// <summary>
        /// Registra una acción en la tabla de auditoría
        /// </summary>
        /// <param name="accion">Nombre de la acción realizada</param>
        /// <param name="controlador">Controlador o módulo donde ocurrió</param>
        /// <param name="detalles">Detalles adicionales de la acción</param>
        /// <param name="usuarioId">ID del usuario que realizó la acción (null si no está autenticado)</param>
        public static void RegistrarAccion(string accion, string controlador, string detalles, int? usuarioId = null)
        {
            try
            {
                // ⚡ NUEVO: Verificar si la auditoría está habilitada en configuración
                bool auditariaHabilitada = bool.Parse(ConfigurationManager.AppSettings["LogAuditoria"] ?? "true");
                if (!auditariaHabilitada)
                    return;

                using (var db = new RefugioMascotasDBEntities())
                {
                    var auditoria = new AuditoriaAcciones
                    {
                        UsuarioId = usuarioId,
                        Accion = TruncarTexto(accion, 100),
                        Modulo = TruncarTexto(controlador, 100),
                        Detalles = TruncarTexto(detalles, 500),
                        FechaAccion = DateTime.Now,
                        DireccionIP = ObtenerDireccionIP()
                    };

                    db.AuditoriaAcciones.Add(auditoria);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log del error pero NO fallar la operación principal
                LogError("Error al registrar auditoría", ex);
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Registra una acción con información adicional del request
        /// </summary>
        public static void RegistrarAccionCompleta(string accion, string controlador, string detalles, int? usuarioId = null)
        {
            try
            {
                bool auditariaHabilitada = bool.Parse(ConfigurationManager.AppSettings["LogAuditoria"] ?? "true");
                if (!auditariaHabilitada)
                    return;

                using (var db = new RefugioMascotasDBEntities())
                {
                    var auditoria = new AuditoriaAcciones
                    {
                        UsuarioId = usuarioId,
                        Accion = TruncarTexto(accion, 100),
                        Modulo = TruncarTexto(controlador, 100),
                        Detalles = ConstruirDetallesCompletos(detalles),
                        FechaAccion = DateTime.Now,
                        DireccionIP = ObtenerDireccionIP()
                    };

                    db.AuditoriaAcciones.Add(auditoria);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LogError("Error al registrar auditoría completa", ex);
            }
        }

        #endregion

        #region REGISTRO DE ACCIONES ESPECÍFICAS

        /// <summary>
        /// Registra un inicio de sesión exitoso
        /// </summary>
        public static void RegistrarLoginExitoso(string email, int usuarioId)
        {
            string detalles = $"Usuario {email} inició sesión exitosamente. IP: {ObtenerDireccionIP()}, Navegador: {ObtenerNavegador()}";
            RegistrarAccion("Login Exitoso", "Account", detalles, usuarioId);
        }

        /// <summary>
        /// Registra un intento de login fallido
        /// </summary>
        public static void RegistrarLoginFallido(string email, string razon = "Credenciales inválidas")
        {
            string detalles = $"Intento fallido para {email}. Razón: {razon}. IP: {ObtenerDireccionIP()}";
            RegistrarAccion("Login Fallido", "Account", detalles);
        }

        /// <summary>
        /// Registra un cierre de sesión
        /// </summary>
        public static void RegistrarLogout(string email, int usuarioId)
        {
            string detalles = $"Usuario {email} cerró sesión. IP: {ObtenerDireccionIP()}";
            RegistrarAccion("Logout", "Account", detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra cambios en datos sensibles
        /// </summary>
        public static void RegistrarCambioDatos(string entidad, int entidadId, string camposModificados, int usuarioId)
        {
            string detalles = $"Modificación en {entidad} ID:{entidadId}. Campos: {camposModificados}";
            RegistrarAccion("Modificación de Datos", entidad, detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra eliminación de registros
        /// </summary>
        public static void RegistrarEliminacion(string entidad, int entidadId, string informacionAdicional, int usuarioId)
        {
            string detalles = $"Eliminación de {entidad} ID:{entidadId}. Info: {informacionAdicional}";
            RegistrarAccion("Eliminación", entidad, detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra creación de registros importantes
        /// </summary>
        public static void RegistrarCreacion(string entidad, int entidadId, string informacionAdicional, int usuarioId)
        {
            string detalles = $"Creación de {entidad} ID:{entidadId}. Info: {informacionAdicional}";
            RegistrarAccion("Creación", entidad, detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra accesos a información sensible
        /// </summary>
        public static void RegistrarAccesoInformacion(string tipo, int registroId, int usuarioId)
        {
            string detalles = $"Acceso a {tipo} ID:{registroId}. IP: {ObtenerDireccionIP()}";
            RegistrarAccion("Acceso a Información", tipo, detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra intentos de acceso no autorizado
        /// </summary>
        public static void RegistrarAccesoNoAutorizado(string recurso, int? usuarioId = null)
        {
            string detalles = $"Intento de acceso no autorizado a {recurso}. IP: {ObtenerDireccionIP()}, Navegador: {ObtenerNavegador()}";
            RegistrarAccion("Acceso Denegado", "Seguridad", detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra errores críticos de la aplicación
        /// </summary>
        public static void RegistrarError(string modulo, string mensajeError, Exception excepcion, int? usuarioId = null)
        {
            try
            {
                bool logErroresHabilitado = bool.Parse(ConfigurationManager.AppSettings["LogErrores"] ?? "true");
                if (!logErroresHabilitado)
                    return;

                string detalles = $"Error: {mensajeError}";
                if (excepcion != null)
                {
                    detalles += $" | Excepción: {excepcion.Message}";
                    if (excepcion.InnerException != null)
                    {
                        detalles += $" | Inner: {excepcion.InnerException.Message}";
                    }
                }

                RegistrarAccion("Error del Sistema", modulo, detalles, usuarioId);
            }
            catch (Exception ex)
            {
                LogError("Error al registrar error en auditoría", ex);
            }
        }

        #endregion

        #region REGISTRO DE ACCIONES DE ADOPCIÓN (ESPECÍFICAS DEL NEGOCIO)

        /// <summary>
        /// ⚡ NUEVO: Registra solicitudes de adopción
        /// </summary>
        public static void RegistrarSolicitudAdopcion(int solicitudId, int usuarioId, int mascotaId, string nombreMascota)
        {
            string detalles = $"Solicitud de adopción ID:{solicitudId} para mascota '{nombreMascota}' (ID:{mascotaId})";
            RegistrarAccion("Solicitud de Adopción", "Adopcion", detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra aprobación de adopciones
        /// </summary>
        public static void RegistrarAprobacionAdopcion(int solicitudId, int aprobadorId, string nombreAdoptante, string nombreMascota)
        {
            string detalles = $"Adopción aprobada ID:{solicitudId}. Adoptante: {nombreAdoptante}, Mascota: {nombreMascota}";
            RegistrarAccion("Aprobación de Adopción", "Adopcion", detalles, aprobadorId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra rechazo de adopciones
        /// </summary>
        public static void RegistrarRechazoAdopcion(int solicitudId, int rechazadorId, string motivo)
        {
            string detalles = $"Adopción rechazada ID:{solicitudId}. Motivo: {motivo}";
            RegistrarAccion("Rechazo de Adopción", "Adopcion", detalles, rechazadorId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra donaciones
        /// </summary>
        public static void RegistrarDonacion(int donacionId, int usuarioId, decimal monto, string tipo)
        {
            string detalles = $"Donación ID:{donacionId}. Tipo: {tipo}, Monto: ${monto:N2}";
            RegistrarAccion("Donación Recibida", "Donaciones", detalles, usuarioId);
        }

        /// <summary>
        /// ⚡ NUEVO: Registra cambios en roles de usuario
        /// </summary>
        public static void RegistrarCambioRol(int usuarioModificadoId, string rolesAntiguos, string rolesNuevos, int adminId)
        {
            string detalles = $"Usuario ID:{usuarioModificadoId}. Roles anteriores: {rolesAntiguos}, Nuevos roles: {rolesNuevos}";
            RegistrarAccion("Cambio de Roles", "Admin", detalles, adminId);
        }

        #endregion

        #region CONSULTAS DE AUDITORÍA

        /// <summary>
        /// ⚡ NUEVO: Obtiene el historial de auditoría de un usuario
        /// </summary>
        public static System.Collections.Generic.List<AuditoriaAcciones> ObtenerHistorialUsuario(int usuarioId, int cantidadRegistros = 50)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    return db.AuditoriaAcciones
                        .Where(a => a.UsuarioId == usuarioId)
                        .OrderByDescending(a => a.FechaAccion)
                        .Take(cantidadRegistros)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                LogError("Error al obtener historial de usuario", ex);
                return new System.Collections.Generic.List<AuditoriaAcciones>();
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene intentos de login fallidos recientes
        /// </summary>
        public static int ObtenerIntentosLoginFallidos(string email, int horasAtras = 2)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    DateTime limite = DateTime.Now.AddHours(-horasAtras);
                    return db.AuditoriaAcciones
                        .Count(a => a.Accion.Contains("Login Fallido") &&
                                   a.Detalles.Contains(email) &&
                                   a.FechaAccion >= limite);
                }
            }
            catch (Exception ex)
            {
                LogError("Error al obtener intentos fallidos", ex);
                return 0;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Limpia registros de auditoría antiguos
        /// </summary>
        public static int LimpiarAuditoriaAntigua(int diasAntiguedad = 90)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    DateTime fechaLimite = DateTime.Now.AddDays(-diasAntiguedad);
                    var registrosAntiguos = db.AuditoriaAcciones
                        .Where(a => a.FechaAccion < fechaLimite)
                        .ToList();

                    int cantidad = registrosAntiguos.Count;
                    db.AuditoriaAcciones.RemoveRange(registrosAntiguos);
                    db.SaveChanges();

                    return cantidad;
                }
            }
            catch (Exception ex)
            {
                LogError("Error al limpiar auditoría antigua", ex);
                return 0;
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        /// <summary>
        /// Obtiene la dirección IP del cliente (maneja proxies)
        /// </summary>
        private static string ObtenerDireccionIP()
        {
            try
            {
                if (HttpContext.Current?.Request == null)
                    return "Unknown";

                var request = HttpContext.Current.Request;

                // Intentar obtener la IP real si está detrás de un proxy
                string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ip))
                {
                    // Si hay múltiples IPs (cadena de proxies), tomar la primera
                    string[] ips = ip.Split(',');
                    if (ips.Length > 0)
                    {
                        ip = ips[0].Trim();
                    }
                }

                // Si no hay IP de proxy, usar la IP remota directa
                if (string.IsNullOrEmpty(ip))
                {
                    ip = request.ServerVariables["REMOTE_ADDR"];
                }

                // Validar que sea una IP válida
                if (string.IsNullOrEmpty(ip))
                    return "Unknown";

                // Truncar si es muy larga
                return TruncarTexto(ip, 45);
            }
            catch (Exception ex)
            {
                LogError("Error al obtener IP", ex);
                return "Unknown";
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene información del navegador del usuario
        /// </summary>
        private static string ObtenerNavegador()
        {
            try
            {
                if (HttpContext.Current?.Request == null)
                    return "Unknown";

                var userAgent = HttpContext.Current.Request.UserAgent;
                return TruncarTexto(userAgent ?? "Unknown", 200);
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene el método HTTP del request actual
        /// </summary>
        private static string ObtenerMetodoHTTP()
        {
            try
            {
                if (HttpContext.Current?.Request == null)
                    return "Unknown";

                return HttpContext.Current.Request.HttpMethod;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Construye detalles completos de la acción
        /// </summary>
        private static string ConstruirDetallesCompletos(string detallesBase)
        {
            try
            {
                var detalles = new System.Text.StringBuilder(detallesBase ?? "");

                if (HttpContext.Current?.Request != null)
                {
                    detalles.Append($" | Método: {ObtenerMetodoHTTP()}");
                    detalles.Append($" | IP: {ObtenerDireccionIP()}");

                    var url = HttpContext.Current.Request.Url?.PathAndQuery;
                    if (!string.IsNullOrEmpty(url))
                    {
                        detalles.Append($" | URL: {TruncarTexto(url, 100)}");
                    }
                }

                return TruncarTexto(detalles.ToString(), 500);
            }
            catch
            {
                return TruncarTexto(detallesBase ?? "", 500);
            }
        }

        /// <summary>
        /// Trunca un texto a una longitud máxima
        /// </summary>
        private static string TruncarTexto(string texto, int longitudMaxima)
        {
            if (string.IsNullOrEmpty(texto))
                return texto;

            if (texto.Length <= longitudMaxima)
                return texto;

            return texto.Substring(0, longitudMaxima - 3) + "...";
        }

        /// <summary>
        /// Log de errores internos del helper (no usar auditoría para evitar recursión)
        /// </summary>
        private static void LogError(string mensaje, Exception ex)
        {
            try
            {
                // Log en consola de debug
                System.Diagnostics.Debug.WriteLine($"[AuditoriaHelper] {mensaje}: {ex?.Message}");

                // Opcionalmente, escribir a un archivo de log
                // System.IO.File.AppendAllText("audit_errors.log", 
                //     $"{DateTime.Now}: {mensaje} - {ex?.Message}\n");
            }
            catch
            {
                // Silenciosamente ignorar errores en el log de errores
            }
        }

        #endregion

        #region ESTADÍSTICAS DE AUDITORÍA

        /// <summary>
        /// ⚡ NUEVO: Obtiene estadísticas de auditoría por periodo
        /// </summary>
        public static dynamic ObtenerEstadisticasAuditoria(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    var registros = db.AuditoriaAcciones
                        .Where(a => a.FechaAccion >= fechaInicio && a.FechaAccion <= fechaFin)
                        .ToList();

                    return new
                    {
                        TotalRegistros = registros.Count,
                        LoginsExitosos = registros.Count(r => r.Accion == "Login Exitoso"),
                        LoginsFallidos = registros.Count(r => r.Accion == "Login Fallido"),
                        Modificaciones = registros.Count(r => r.Accion == "Modificación de Datos"),
                        Eliminaciones = registros.Count(r => r.Accion == "Eliminación"),
                        Errores = registros.Count(r => r.Accion == "Error del Sistema"),
                        AccesosDenegados = registros.Count(r => r.Accion == "Acceso Denegado"),
                        UsuariosActivos = registros.Where(r => r.UsuarioId.HasValue)
                                                  .Select(r => r.UsuarioId.Value)
                                                  .Distinct()
                                                  .Count()
                    };
                }
            }
            catch (Exception ex)
            {
                LogError("Error al obtener estadísticas", ex);
                return null;
            }
        }

        #endregion
    }
}