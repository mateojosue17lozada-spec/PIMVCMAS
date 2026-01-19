using System;
using System.Web;

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
        public static void RegistrarAccion(string accion, string modulo, string descripcion, int? usuarioId = null)
        {
            try
            {
                using (var db = new Models.RefugioMascotasEntities())
                {
                    var auditoria = new Models.AuditoriaAcciones
                    {
                        UsuarioId = usuarioId,
                        Accion = accion,
                        Modulo = modulo,
                        Descripcion = descripcion,
                        FechaAccion = DateTime.Now,
                        DireccionIP = GetClientIP()
                    };

                    db.AuditoriaAcciones.Add(auditoria);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // No lanzar excepción si falla la auditoría
                Console.WriteLine($"Error al registrar auditoría: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la dirección IP del cliente
        /// </summary>
        private static string GetClientIP()
        {
            try
            {
                string ip = HttpContext.Current?.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (string.IsNullOrEmpty(ip))
                {
                    ip = HttpContext.Current?.Request.ServerVariables["REMOTE_ADDR"];
                }

                return ip ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}