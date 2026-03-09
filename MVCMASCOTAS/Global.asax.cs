using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace MVCMASCOTAS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Registrar todas las áreas
            AreaRegistration.RegisterAllAreas();

            // Registrar filtros globales
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);

            // Registrar rutas
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Registrar bundles
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Optimizar para MVC
            MvcHandler.DisableMvcResponseHeader = true;


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
                catch (Exception ex)
                {
                    // Log del error de autenticación
                    LogError("Authentication Error", ex);

                    // Si hay error descifrando el ticket, limpiar la cookie
                    FormsAuthentication.SignOut();

                    // Eliminar cookie inválida
                    authCookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(authCookie);
                }
            }
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            HttpException httpException = exception as HttpException;

            // Log del error
            LogError("Application Error", exception);

            // Si es error 404, manejarlo específicamente
            if (httpException != null && httpException.GetHttpCode() == 404)
            {
                Response.Redirect("~/Error/NotFound");
                Server.ClearError();
                return;
            }

            // Para otros errores, usar el manejo configurado en web.config
            // El web.config ya tiene customErrors configurado
            // Solo asegurarse de limpiar el error
            Server.ClearError();

            // Opcional: Redirigir a página de error general
            // Response.Redirect("~/Error/General");
        }

        protected void Application_EndRequest()
        {
            // Limpiar recursos si es necesario
            // Puedes agregar lógica aquí para limpieza de recursos
        }

        protected void Application_BeginRequest()
        {
            // Eliminar cabecera X-AspNetMvc-Version por seguridad
            Response.Headers.Remove("X-AspNetMvc-Version");
            Response.Headers.Remove("X-AspNet-Version");
            Response.Headers.Remove("Server");

            // Forzar UTF-8
            Response.ContentType = "text/html; charset=utf-8";
            Response.Charset = "utf-8";
            Response.HeaderEncoding = System.Text.Encoding.UTF8;
        }

        // Método para logging de errores
        private void LogError(string errorType, Exception exception)
        {
            try
            {
                // Obtener configuración del web.config
                bool logToFile = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["LogErrorsToFile"] ?? "false");
                string logPath = System.Configuration.ConfigurationManager.AppSettings["LogFilePath"] ?? "~/App_Data/ErrorLog.txt";

                if (logToFile)
                {
                    string fullPath = Server.MapPath(logPath);
                    string logMessage = $@"
========================================
Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Tipo: {errorType}
Error: {exception?.Message}
Tipo Excepción: {exception?.GetType().FullName}
URL: {Request.Url}
Usuario: {(User?.Identity?.Name ?? "Anónimo")}
IP: {Request.UserHostAddress}
User Agent: {Request.UserAgent}
Stack Trace:
{exception?.StackTrace}

Inner Exception: {(exception?.InnerException?.Message ?? "Ninguno")}
========================================
";

                    // Crear directorio si no existe
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Escribir en archivo
                    File.AppendAllText(fullPath, logMessage);
                }

                // Siempre escribir en Output de Visual Studio (para desarrollo)
                System.Diagnostics.Debug.WriteLine($"{errorType.ToUpper()}: {exception?.Message}");

                if (exception?.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"INNER: {exception.InnerException.Message}");
                }
            }
            catch (Exception logEx)
            {
                // Si falla el logging, al menos mostrar en consola
                System.Diagnostics.Debug.WriteLine($"Error en logging: {logEx.Message}");
            }
        }
    }
}