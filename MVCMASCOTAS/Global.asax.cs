using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Linq;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                try
                {
                    FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                    if (authTicket != null && !authTicket.Expired)
                    {
                        string[] roles = authTicket.UserData.Split(',');
                        HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(
                            new System.Security.Principal.GenericIdentity(authTicket.Name, "Forms"),
                            roles
                        );
                    }
                }
                catch
                {
                    // Si hay error descifrando el ticket, limpiar la cookie
                    FormsAuthentication.SignOut();
                }
            }
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();

            // Log del error
            System.Diagnostics.Debug.WriteLine($"Error en aplicación: {exception?.Message}");

            // Aquí puedes agregar logging a archivo o base de datos
        }
    }
}
