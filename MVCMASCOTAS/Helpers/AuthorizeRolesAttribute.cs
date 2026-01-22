using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Atributo de autorización personalizado que verifica roles específicos
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        private readonly string[] _roles;

        /// <summary>
        /// Constructor que acepta uno o más roles
        /// </summary>
        /// <param name="roles">Roles permitidos para acceder</param>
        public AuthorizeRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            try
            {
                if (httpContext == null)
                {
                    throw new ArgumentNullException(nameof(httpContext));
                }

                // 1. Verificar si el usuario está autenticado
                if (!httpContext.User.Identity.IsAuthenticated)
                {
                    return false;
                }

                // 2. Si no se especificaron roles, solo verificar autenticación
                if (_roles == null || _roles.Length == 0)
                {
                    return true;
                }

                // 3. Verificar si el usuario tiene alguno de los roles especificados
                bool hasAnyRole = _roles.Any(role => httpContext.User.IsInRole(role));

                // 4. Log de auditoría si no tiene permisos (opcional)
                if (!hasAnyRole && httpContext.User.Identity.IsAuthenticated)
                {
                    // Puedes agregar log de auditoría aquí si lo necesitas
                    // Por ejemplo: AuditoriaHelper.RegistrarAccion("Acceso Denegado", "AuthorizeRoles", 
                    //     $"Usuario {httpContext.User.Identity.Name} intentó acceder sin permisos");
                }

                return hasAnyRole;
            }
            catch (Exception ex)
            {
                // Log del error pero devolver false por seguridad
                // Por ejemplo: Logger.Error("Error en AuthorizeCore", ex);
                System.Diagnostics.Debug.WriteLine($"AuthorizeRoles Error: {ex.Message}");

                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            try
            {
                if (filterContext.HttpContext.User.Identity.IsAuthenticated)
                {
                    // Usuario autenticado pero sin permisos - Mostrar página de no autorizado
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Home" },
                            { "action", "Unauthorized" },
                            { "area", "" }
                        });
                }
                else
                {
                    // Usuario no autenticado - Redirigir a login con returnUrl
                    string returnUrl = filterContext.HttpContext.Request.Url?.PathAndQuery ?? "/";

                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Account" },
                            { "action", "Login" },
                            { "returnUrl", returnUrl },
                            { "area", "" }
                        });
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usar el manejo por defecto
                // Por ejemplo: Logger.Error("Error en HandleUnauthorizedRequest", ex);
                System.Diagnostics.Debug.WriteLine($"HandleUnauthorizedRequest Error: {ex.Message}");

                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}