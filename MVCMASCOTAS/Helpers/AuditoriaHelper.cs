using System;
using System.Web;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para registro de auditoría de acciones
    /// </summary>
    public static class AuditoriaHelper
    {
        /// <summary>
        /// Registra una acción en la tabla de auditoría
        /// </summary>
        public static void RegistrarAccion(string accion, string controlador, string detalles, int? usuarioId = null)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    var auditoria = new AuditoriaAcciones
                    {
                        UsuarioId = usuarioId,
                        Accion = accion,
                        Modulo = controlador,
                        Detalles = detalles,
                        FechaAccion = DateTime.Now,
                        DireccionIP = ObtenerDireccionIP()
                    };

                    db.AuditoriaAcciones.Add(auditoria);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la operación principal
                System.Diagnostics.Debug.WriteLine($"Error en auditoría: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la dirección IP del cliente
        /// </summary>
        private static string ObtenerDireccionIP()
        {
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    string ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                    if (string.IsNullOrEmpty(ip))
                    {
                        ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                    }

                    return ip;
                }
            }
            catch
            {
                // Ignorar errores
            }

            return "Unknown";
        }

        /// <summary>
        /// Registra un inicio de sesión exitoso
        /// </summary>
        public static void RegistrarLoginExitoso(string email, int usuarioId)
        {
            RegistrarAccion("Login Exitoso", "Account", $"Usuario {email} inició sesión", usuarioId);
        }

        /// <summary>
        /// Registra un intento de login fallido
        /// </summary>
        public static void RegistrarLoginFallido(string email)
        {
            RegistrarAccion("Login Fallido", "Account", $"Intento fallido para {email}");
        }

        /// <summary>
        /// Registra un cierre de sesión
        /// </summary>
        public static void RegistrarLogout(string email, int usuarioId)
        {
            RegistrarAccion("Logout", "Account", $"Usuario {email} cerró sesión", usuarioId);
        }
    }
}
