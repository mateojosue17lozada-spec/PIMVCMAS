using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Atributo de autorización personalizado que verifica roles
    /// </summary>
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        private readonly string[] _roles;

        public AuthorizeRolesAttribute(params string[] roles)
        {
            _roles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Verificar si el usuario está autenticado
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            // Si no se especificaron roles, solo verificar autenticación
            if (_roles == null || _roles.Length == 0)
            {
                return true;
            }

            // Verificar si el usuario tiene alguno de los roles especificados
            return _roles.Any(role => httpContext.User.IsInRole(role));
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Usuario autenticado pero sin permisos
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Unauthorized.cshtml"
                };
            }
            else
            {
                // Usuario no autenticado, redirigir a login
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
