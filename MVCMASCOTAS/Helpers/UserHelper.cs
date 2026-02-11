using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Razor.Tokenizer;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper optimizado para obtener información del usuario actual
    /// Incluye caché de sesión para mejorar rendimiento
    /// </summary>
    public static class UserHelper
    {
        #region CONSTANTES

        private const string SESSION_USER_KEY = "CurrentUser";
        private const string SESSION_USERID_KEY = "CurrentUserId";
        private const string SESSION_USERROLES_KEY = "CurrentUserRoles";

        #endregion

        #region USUARIO ACTUAL

        /// <summary>
        /// ⚡ OPTIMIZADO: Obtiene el usuario actual con caché de sesión
        /// </summary>
        public static Usuarios GetCurrentUser(RefugioMascotasDBEntities db = null)
        {
            // Verificar autenticación
            if (HttpContext.Current?.User == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            try
            {
                // Intentar obtener de caché de sesión
                if (HttpContext.Current.Session != null)
                {
                    var cachedUser = HttpContext.Current.Session[SESSION_USER_KEY] as Usuarios;
                    if (cachedUser != null)
                        return cachedUser;
                }

                // Si no está en caché, buscar en BD
                string email = HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(email))
                    return null;

                bool shouldDisposeDb = false;
                if (db == null)
                {
                    db = new RefugioMascotasDBEntities();
                    shouldDisposeDb = true;
                }

                try
                {
                    var user = db.Usuarios.FirstOrDefault(u =>
                        u.Email.ToLower() == email.ToLower() &&
                        (u.Activo == null || u.Activo == true));

                    // Guardar en caché de sesión
                    if (user != null && HttpContext.Current.Session != null)
                    {
                        HttpContext.Current.Session[SESSION_USER_KEY] = user;
                        HttpContext.Current.Session[SESSION_USERID_KEY] = user.UsuarioId;
                    }

                    return user;
                }
                finally
                {
                    if (shouldDisposeDb)
                        db?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en GetCurrentUser: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ⚡ OPTIMIZADO: Obtiene el ID del usuario actual con caché
        /// </summary>
        public static int? GetCurrentUserId()
        {
            try
            {
                // Intentar obtener de caché de sesión
                if (HttpContext.Current?.Session != null)
                {
                    var cachedUserId = HttpContext.Current.Session[SESSION_USERID_KEY];
                    if (cachedUserId != null)
                        return (int)cachedUserId;
                }

                // Si no está en caché, obtener del usuario
                var user = GetCurrentUser();
                return user?.UsuarioId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en GetCurrentUserId: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene el email del usuario actual sin consultar BD
        /// </summary>
        public static string GetCurrentUserEmail()
        {
            try
            {
                if (HttpContext.Current?.User == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                    return null;

                return HttpContext.Current.User.Identity.Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene el nombre completo del usuario actual
        /// </summary>
        public static string GetCurrentUserFullName()
        {
            try
            {
                var user = GetCurrentUser();
                return user?.NombreCompleto ?? GetCurrentUserEmail();
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region ROLES DEL USUARIO

        /// <summary>
        /// ⚡ OPTIMIZADO: Obtiene los roles del usuario actual con caché
        /// </summary>
        public static List<string> GetCurrentUserRoles()
        {
            try
            {
                // Intentar obtener de caché de sesión
                if (HttpContext.Current?.Session != null)
                {
                    var cachedRoles = HttpContext.Current.Session[SESSION_USERROLES_KEY] as List<string>;
                    if (cachedRoles != null)
                        return cachedRoles;
                }

                // Si no está en caché, buscar en BD
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return new List<string>();

                using (var db = new RefugioMascotasDBEntities())
                {
                    var roles = db.UsuariosRoles
                        .Where(ur => ur.UsuarioId == userId.Value)
                        .Select(ur => ur.Roles.NombreRol)
                        .ToList();

                    // Guardar en caché de sesión
                    if (HttpContext.Current?.Session != null)
                    {
                        HttpContext.Current.Session[SESSION_USERROLES_KEY] = roles;
                    }

                    return roles;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en GetCurrentUserRoles: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario actual tiene un rol específico
        /// </summary>
        public static bool IsInRole(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return false;

                var roles = GetCurrentUserRoles();
                return roles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario tiene alguno de los roles especificados
        /// </summary>
        public static bool IsInAnyRole(params string[] roleNames)
        {
            try
            {
                if (roleNames == null || roleNames.Length == 0)
                    return false;

                var userRoles = GetCurrentUserRoles();
                return roleNames.Any(roleName =>
                    userRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario es Administrador
        /// </summary>
        public static bool IsAdmin()
        {
            return IsInRole("Administrador");
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario es Veterinario
        /// </summary>
        public static bool IsVeterinario()
        {
            return IsInRole("Veterinario");
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario es Administrador o Veterinario
        /// </summary>
        public static bool IsAdminOrVeterinario()
        {
            return IsInAnyRole("Administrador", "Veterinario");
        }

        #endregion

        #region CACHÉ Y SESIÓN

        /// <summary>
        /// ⚡ NUEVO: Limpia el caché del usuario actual (útil después de actualizar datos)
        /// </summary>
        public static void ClearUserCache()
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    HttpContext.Current.Session.Remove(SESSION_USER_KEY);
                    HttpContext.Current.Session.Remove(SESSION_USERID_KEY);
                    HttpContext.Current.Session.Remove(SESSION_USERROLES_KEY);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en ClearUserCache: {ex.Message}");
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Refresca los datos del usuario en caché
        /// </summary>
        public static void RefreshUserCache()
        {
            try
            {
                ClearUserCache();

                // Forzar recarga desde BD
                using (var db = new RefugioMascotasDBEntities())
                {
                    var user = GetCurrentUser(db);
                    if (user != null)
                    {
                        // Los datos ya se guardan en caché automáticamente
                        GetCurrentUserRoles(); // Recargar roles también
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en RefreshUserCache: {ex.Message}");
            }
        }

        #endregion

        #region VALIDACIONES

        /// <summary>
        /// ⚡ NUEVO: Verifica si hay un usuario autenticado
        /// </summary>
        public static bool IsAuthenticated()
        {
            try
            {
                return HttpContext.Current?.User?.Identity?.IsAuthenticated ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario actual es el propietario de un recurso
        /// </summary>
        public static bool IsOwner(int? resourceOwnerId)
        {
            if (!resourceOwnerId.HasValue)
                return false;

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return false;

            return currentUserId.Value == resourceOwnerId.Value;
        }

        /// <summary>
        /// ⚡ NUEVO: Verifica si el usuario puede acceder a un recurso (es propietario o admin)
        /// </summary>
        public static bool CanAccessResource(int? resourceOwnerId)
        {
            return IsAdmin() || IsOwner(resourceOwnerId);
        }

        #endregion

        #region INFORMACIÓN ADICIONAL

        /// <summary>
        /// ⚡ NUEVO: Obtiene la imagen de perfil del usuario actual en Base64
        /// </summary>
        public static string GetCurrentUserProfileImageBase64()
        {
            try
            {
                var user = GetCurrentUser();
                if (user?.ImagenPerfil != null && user.ImagenPerfil.Length > 0)
                {
                    return Convert.ToBase64String(user.ImagenPerfil);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtiene información resumida del usuario actual
        /// </summary>
        public static UserSummary GetCurrentUserSummary()
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                    return null;

                var roles = GetCurrentUserRoles();

                return new UserSummary
                {
                    UsuarioId = user.UsuarioId,
                    NombreCompleto = user.NombreCompleto,
                    Email = user.Email,
                    Roles = roles,
                    FechaRegistro = user.FechaRegistro,
                    UltimoAcceso = user.UltimoAcceso,
                    TieneImagenPerfil = user.ImagenPerfil != null && user.ImagenPerfil.Length > 0
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region ESTADÍSTICAS DEL USUARIO

        /// <summary>
        /// ⚡ NUEVO: Obtiene estadísticas del usuario actual
        /// </summary>
        public static UserStatistics GetCurrentUserStatistics()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return null;

                using (var db = new RefugioMascotasDBEntities())
                {
                    return new UserStatistics
                    {
                        TotalSolicitudesAdopcion = db.SolicitudAdopcion.Count(s => s.UsuarioId == userId.Value),
                        SolicitudesAprobadas = db.SolicitudAdopcion.Count(s => s.UsuarioId == userId.Value && s.Estado == "Aprobada"),
                        SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.UsuarioId == userId.Value && s.Estado == "Pendiente"),
                        TotalDonaciones = db.Donaciones.Count(d => d.UsuarioId == userId.Value && d.Estado == "Completada"),
                        MontoTotalDonado = db.Donaciones
                            .Where(d => d.UsuarioId == userId.Value && d.Estado == "Completada")
                            .Sum(d => (decimal?)d.Monto) ?? 0,
                        TotalApadrinamientos = db.Apadrinamientos.Count(a => a.UsuarioId == userId.Value && a.Estado == "Activo"),
                        TotalVoluntariado = db.InscripcionesActividades.Count(i => i.UsuarioId == userId.Value && i.Estado == "Confirmada"),
                        TotalPedidos = db.Pedidos.Count(p => p.UsuarioId == userId.Value),
                        TotalReportes = db.ReportesRescate.Count(r => r.UsuarioReportante == userId.Value)
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserHelper] Error en GetCurrentUserStatistics: {ex.Message}");
                return null;
            }
        }

        #endregion
    }

    #region CLASES AUXILIARES

    /// <summary>
    /// ⚡ NUEVO: Resumen de información del usuario
    /// </summary>
    public class UserSummary
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public bool TieneImagenPerfil { get; set; }

        public string RolesPrincipal => Roles?.FirstOrDefault() ?? "Usuario";
        public string RolesFormateados => Roles != null ? string.Join(", ", Roles) : "Usuario";
        public bool EsAdmin => Roles?.Contains("Administrador") ?? false;
        public bool EsVeterinario => Roles?.Contains("Veterinario") ?? false;
    }

    /// <summary>
    /// ⚡ NUEVO: Estadísticas del usuario
    /// </summary>
    public class UserStatistics
    {
        public int TotalSolicitudesAdopcion { get; set; }
        public int SolicitudesAprobadas { get; set; }
        public int SolicitudesPendientes { get; set; }
        public int TotalDonaciones { get; set; }
        public decimal MontoTotalDonado { get; set; }
        public int TotalApadrinamientos { get; set; }
        public int TotalVoluntariado { get; set; }
        public int TotalPedidos { get; set; }
        public int TotalReportes { get; set; }

        public int TotalActividades => TotalSolicitudesAdopcion + TotalDonaciones +
                                       TotalApadrinamientos + TotalVoluntariado +
                                       TotalPedidos + TotalReportes;
    }

    #endregion
}
