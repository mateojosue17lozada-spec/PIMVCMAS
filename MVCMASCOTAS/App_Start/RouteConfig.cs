using System.Web.Mvc;
using System.Web.Routing;

namespace MVCMASCOTAS
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Ruta para detalles de mascota
            routes.MapRoute(
                name: "MascotaDetalle",
                url: "Mascotas/Detalle/{id}",
                defaults: new { controller = "Mascotas", action = "Detalle" },
                constraints: new { id = @"\d+" }
            );

            // Ruta para solicitar adopción
            routes.MapRoute(
                name: "SolicitarAdopcion",
                url: "Adopcion/Solicitar/{mascotaId}",
                defaults: new { controller = "Adopcion", action = "Solicitar" },
                constraints: new { mascotaId = @"\d+" }
            );

            // Ruta para historial médico
            routes.MapRoute(
                name: "HistorialMedico",
                url: "Veterinario/HistorialMedico/{mascotaId}",
                defaults: new { controller = "Veterinario", action = "HistorialMedico" },
                constraints: new { mascotaId = @"\d+" }
            );

            // Ruta para detalles de solicitud de adopción
            routes.MapRoute(
                name: "DetallesSolicitud",
                url: "Admin/DetallesSolicitud/{id}",
                defaults: new { controller = "Admin", action = "DetallesSolicitud" },
                constraints: new { id = @"\d+" }
            );

            // Ruta para productos de tienda
            routes.MapRoute(
                name: "TiendaProducto",
                url: "Tienda/Detalle/{id}",
                defaults: new { controller = "Tienda", action = "Detalle" },
                constraints: new { id = @"\d+" }
            );

            // Ruta para detalle de reporte de rescate
            routes.MapRoute(
                name: "DetalleReporte",
                url: "Rescate/DetalleReporte/{id}",
                defaults: new { controller = "Rescate", action = "DetalleReporte" },
                constraints: new { id = @"\d+" }
            );

            // Ruta por defecto
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
