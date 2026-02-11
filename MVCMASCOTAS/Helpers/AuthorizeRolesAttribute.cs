using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Helpers;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// ⚡ OPTIMIZADO Y CORREGIDO: Atributo de autorización personalizado con caché y auditoría
    /// Evita páginas en blanco forzando siempre una respuesta válida
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        #region PROPIEDADES
        private readonly string[] _requiredRoles;
        /// <summary>
        /// Indica si se debe auditar el acceso denegado
        /// </summary>
        public bool AuditDeniedAccess { get; set; } = true;
        /// <summary>
        /// Indica si se debe usar caché de roles
        /// </summary>
        public bool UseCache { get; set; } = true;
        /// <summary>
        /// Tiempo de expiración del caché en minutos
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Constructor del atributo de autorización
        /// </summary>
        /// <param name="roles">Roles permitidos (el usuario debe tener AL MENOS UNO)</param>
        public AuthorizeRolesAttribute(params string[] roles)
        {
            _requiredRoles = roles ?? new string[0];
        }
        #endregion

        #region AUTORIZACIÓN
        /// <summary>
        /// ⚡ OPTIMIZADO: Verifica si el usuario tiene autorización con caché
        /// </summary>
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            try
            {
                // 1. Verificar autenticación básica
                if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
                {
                    LogDebug("Usuario no autenticado");
                    return false;
                }

                // 2. Si no hay roles especificados, solo verificar autenticación
                if (_requiredRoles == null || _requiredRoles.Length == 0)
                {
                    LogDebug("No hay roles requeridos, acceso permitido");
                    return true;
                }

                string userEmail = httpContext.User.Identity.Name;
                if (string.IsNullOrEmpty(userEmail))
                {
                    LogDebug("Email de usuario vacío");
                    return false;
                }

                // 3. Obtener roles del usuario (con o sin caché)
                List<string> userRoles = UseCache
                    ? GetUserRolesWithCache(userEmail, httpContext)
                    : GetUserRolesFromDatabase(userEmail);

                if (userRoles == null || userRoles.Count == 0)
                {
                    LogDebug($"Usuario {userEmail} no tiene roles asignados");
                    if (AuditDeniedAccess)
                    {
                        AuditAccessDenied(userEmail, null, "Usuario sin roles asignados");
                    }
                    return false;
                }

                // 4. Verificar si tiene alguno de los roles requeridos
                bool hasPermission = userRoles.Any(userRole =>
                    _requiredRoles.Any(requiredRole =>
                        string.Equals(userRole.Trim(), requiredRole.Trim(), StringComparison.OrdinalIgnoreCase)));

                // 5. Log y auditoría
                LogDebug($"Usuario: {userEmail}");
                LogDebug($"Roles del usuario: {string.Join(", ", userRoles)}");
                LogDebug($"Roles requeridos: {string.Join(", ", _requiredRoles)}");
                LogDebug($"Acceso {(hasPermission ? "PERMITIDO" : "DENEGADO")}");

                if (!hasPermission && AuditDeniedAccess)
                {
                    AuditAccessDenied(userEmail, userRoles, "Roles insuficientes");
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                // Log detallado del error
                LogError($"Error en AuthorizeCore: {ex.Message}", ex);

                // Auditar el error
                try
                {
                    AuditoriaHelper.RegistrarError(
                        "AuthorizeRoles",
                        "Error en verificación de autorización",
                        ex);
                }
                catch { /* Ignorar errores de auditoría */ }

                // En modo debug, lanzar excepción para debugging
                if (httpContext?.IsDebuggingEnabled ?? false)
                {
                    throw new HttpException(500, $"Error en AuthorizeRolesAttribute: {ex.Message}", ex);
                }

                // Por seguridad, denegar acceso si hay error
                return false;
            }
        }
        #endregion

        #region GESTIÓN DE ROLES
        /// <summary>
        /// ⚡ NUEVO: Obtiene roles del usuario con caché de sesión
        /// </summary>
        private List<string> GetUserRolesWithCache(string userEmail, HttpContextBase httpContext)
        {
            try
            {
                string cacheKey = $"UserRoles_{userEmail}";
                string cacheExpiryKey = $"UserRolesExpiry_{userEmail}";

                // Verificar si existe en caché y no ha expirado
                if (httpContext.Session != null)
                {
                    var cachedRoles = httpContext.Session[cacheKey] as List<string>;
                    var expiryTime = httpContext.Session[cacheExpiryKey] as DateTime?;

                    if (cachedRoles != null && expiryTime.HasValue && DateTime.Now < expiryTime.Value)
                    {
                        LogDebug($"Roles obtenidos de caché para {userEmail}");
                        return cachedRoles;
                    }
                }

                // Si no está en caché o expiró, obtener de BD
                var roles = GetUserRolesFromDatabase(userEmail);

                // Guardar en caché
                if (roles != null && httpContext.Session != null)
                {
                    httpContext.Session[cacheKey] = roles;
                    httpContext.Session[cacheExpiryKey] = DateTime.Now.AddMinutes(CacheExpirationMinutes);
                    LogDebug($"Roles guardados en caché para {userEmail}");
                }

                return roles ?? new List<string>();
            }
            catch (Exception ex)
            {
                LogError($"Error en GetUserRolesWithCache: {ex.Message}", ex);
                return GetUserRolesFromDatabase(userEmail);
            }
        }

        /// <summary>
        /// ⚡ OPTIMIZADO: Obtiene roles del usuario desde la base de datos
        /// </summary>
        private List<string> GetUserRolesFromDatabase(string userEmail)
        {
            try
            {
                using (var db = new RefugioMascotasDBEntities())
                {
                    // Buscar usuario
                    var usuario = db.Usuarios
                        .Where(u => u.Email.ToLower() == userEmail.ToLower() &&
                                   (u.Activo == null || u.Activo == true))
                        .Select(u => new { u.UsuarioId })
                        .FirstOrDefault();

                    if (usuario == null)
                    {
                        LogDebug($"Usuario no encontrado: {userEmail}");
                        return new List<string>();
                    }

                    // Obtener roles del usuario (consulta optimizada)
                    var roles = db.UsuariosRoles
                        .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                        .Select(ur => ur.Roles.NombreRol)
                        .ToList();

                    LogDebug($"Roles obtenidos de BD para {userEmail}: {string.Join(", ", roles)}");
                    return roles ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error en GetUserRolesFromDatabase: {ex.Message}", ex);
                return new List<string>();
            }
        }
        #endregion

        #region MANEJO DE ACCESO NO AUTORIZADO - CORREGIDO ANTI-BLANCO
        /// <summary>
        /// ⚡ CORREGIDO: Maneja las solicitudes no autorizadas - nunca deja blanco
        /// </summary>
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            try
            {
                bool isAuthenticated = filterContext.HttpContext.User.Identity?.IsAuthenticated ?? false;

                // Siempre establecer status code
                filterContext.HttpContext.Response.StatusCode = isAuthenticated ? 403 : 401;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;

                if (isAuthenticated)
                {
                    HandleForbiddenAccess(filterContext);
                }
                else
                {
                    HandleUnauthenticatedAccess(filterContext);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error crítico en HandleUnauthorizedRequest: {ex.Message}", ex);

                // ANTI-BLANCO: fallback extremo si todo falla
                filterContext.Result = new ContentResult
                {
                    Content = "<!DOCTYPE html><html lang='es'><head><meta charset='utf-8'><title>Error de Autorización</title></head>" +
                              "<body style='background:#111;color:#f00;font-family:Arial;padding:50px;text-align:center;'>" +
                              "<h1 style='font-size:60px;'>ERROR EN AUTORIZACIÓN</h1>" +
                              "<p style='font-size:24px;'>No se pudo procesar el acceso. Contacta al administrador.</p>" +
                              "<p style='font-size:18px;'>Detalles: " + HttpUtility.HtmlEncode(ex.Message) + "</p>" +
                              "</body></html>",
                    ContentType = "text/html"
                };
            }
        }

        /// <summary>
        /// Maneja acceso prohibido (usuario autenticado sin permisos)
        /// </summary>
        private void HandleForbiddenAccess(AuthorizationContext filterContext)
        {
            if (IsAjaxRequest(filterContext.HttpContext.Request))
            {
                filterContext.Result = new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        error = "No tienes permisos para acceder a este recurso",
                        statusCode = 403
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }

            string viewPath = "~/Views/Error/Unauthorized.cshtml";

            if (ViewExists(filterContext, "Unauthorized"))
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = viewPath,
                    ViewData = new ViewDataDictionary
                    {
                        { "Message", "No tienes permisos suficientes para acceder a esta página." },
                        { "RolesRequeridos", string.Join(", ", _requiredRoles) },
                        { "ReturnUrl", filterContext.HttpContext.Request.Url?.PathAndQuery ?? "/" }
                    }
                };
            }
            else
            {
                // Fallback seguro: HTML directo
                filterContext.Result = new ContentResult
                {
                    Content = GenerateUnauthorizedHtml(),
                    ContentType = "text/html"
                };
            }
        }

        /// <summary>
        /// Maneja acceso no autenticado (redirige a login)
        /// </summary>
        private void HandleUnauthenticatedAccess(AuthorizationContext filterContext)
        {
            string returnUrl = filterContext.HttpContext.Request.Url?.PathAndQuery ?? "/";

            if (IsAjaxRequest(filterContext.HttpContext.Request))
            {
                filterContext.Result = new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        error = "Debes iniciar sesión para acceder a este recurso",
                        redirectUrl = "/Account/Login?returnUrl=" + HttpUtility.UrlEncode(returnUrl),
                        statusCode = 401
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }

            // Redirección más robusta y segura
            filterContext.Result = new RedirectResult("/Account/Login?returnUrl=" + HttpUtility.UrlEncode(returnUrl));
        }
        #endregion

        #region AUDITORÍA
        private void AuditAccessDenied(string userEmail, List<string> userRoles, string reason)
        {
            try
            {
                string rolesText = userRoles != null && userRoles.Any()
                    ? string.Join(", ", userRoles)
                    : "ninguno";

                string detalles = $"Usuario {userEmail} intentó acceder a recurso que requiere roles: {string.Join(", ", _requiredRoles)}. " +
                                 $"Roles del usuario: {rolesText}. Razón: {reason}";

                int? userId = null;
                try
                {
                    using (var db = new RefugioMascotasDBEntities())
                    {
                        userId = db.Usuarios
                            .Where(u => u.Email.ToLower() == userEmail.ToLower())
                            .Select(u => (int?)u.UsuarioId)
                            .FirstOrDefault();
                    }
                }
                catch { }

                AuditoriaHelper.RegistrarAccesoNoAutorizado(
                    $"Roles requeridos: {string.Join(", ", _requiredRoles)}",
                    userId);
            }
            catch (Exception ex)
            {
                LogError($"Error en AuditAccessDenied: {ex.Message}", ex);
            }
        }
        #endregion

        #region MÉTODOS AUXILIARES
        private bool IsAjaxRequest(HttpRequestBase request)
        {
            if (request == null) return false;

            return (request["X-Requested-With"] != null &&
                    request["X-Requested-With"].Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase)) ||
                   (request.Headers["X-Requested-With"] != null &&
                    request.Headers["X-Requested-With"].Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase));
        }

        private bool ViewExists(AuthorizationContext context, string viewName)
        {
            try
            {
                var result = ViewEngines.Engines.FindView(context, viewName, null);
                return result.View != null;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateUnauthorizedHtml()
        {
            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Acceso Denegado - Refugio de Mascotas</title>
    <style>
        body {{ font-family: 'Segoe UI', sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); margin:0; padding:0; display:flex; justify-content:center; align-items:center; min-height:100vh; }}
        .container {{ background:white; padding:40px; border-radius:10px; box-shadow:0 10px 25px rgba(0,0,0,0.2); text-align:center; max-width:500px; }}
        .icon {{ font-size:80px; color:#f44336; margin-bottom:20px; }}
        h1 {{ color:#333; margin:0 0 15px 0; }}
        p {{ color:#666; margin:10px 0; line-height:1.6; }}
        .btn {{ display:inline-block; padding:12px 30px; margin:20px 10px 0; background:#4CAF50; color:white; text-decoration:none; border-radius:5px; transition:0.3s; }}
        .btn:hover {{ background:#45a049; }}
        .btn-secondary {{ background:#2196F3; }}
        .btn-secondary:hover {{ background:#0b7dda; }}
        .roles {{ background:#fff3cd; border-left:4px solid #ffc107; padding:10px; margin:20px 0; text-align:left; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>🚫</div>
        <h1>Acceso Denegado</h1>
        <p>No tienes los permisos necesarios para acceder a esta página.</p>
        <div class='roles'>
            <strong>Roles requeridos:</strong><br>
            {string.Join(", ", _requiredRoles)}
        </div>
        <p>Si crees que deberías tener acceso, contacta con el administrador del sistema.</p>
        <a href='/Home/Index' class='btn'>Volver al Inicio</a>
        <a href='/Account/Logout' class='btn btn-secondary'>Cerrar Sesión</a>
    </div>
</body>
</html>";
        }

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthorizeRoles] {message}");
        }

        private void LogError(string message, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthorizeRoles ERROR] {message}");
            if (ex != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
        #endregion

        #region MÉTODOS ESTÁTICOS PÚBLICOS
        public static void ClearUserRolesCache(string userEmail)
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    string cacheKey = $"UserRoles_{userEmail}";
                    string cacheExpiryKey = $"UserRolesExpiry_{userEmail}";
                    HttpContext.Current.Session.Remove(cacheKey);
                    HttpContext.Current.Session.Remove(cacheExpiryKey);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar caché de roles: {ex.Message}");
            }
        }

        public static void ClearAllRolesCache()
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    var keysToRemove = new List<string>();
                    foreach (string key in HttpContext.Current.Session.Keys)
                    {
                        if (key.StartsWith("UserRoles_") || key.StartsWith("UserRolesExpiry_"))
                            keysToRemove.Add(key);
                    }
                    foreach (var key in keysToRemove)
                        HttpContext.Current.Session.Remove(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar caché global: {ex.Message}");
            }
        }
        #endregion
    }
}