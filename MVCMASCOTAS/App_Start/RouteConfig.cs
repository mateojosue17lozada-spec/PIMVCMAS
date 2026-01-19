using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MVCMASCOTAS
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Ruta para detalle de mascota con nombre amigable
            routes.MapRoute(
                name: "MascotaDetalle",
                url: "Mascotas/Detalle/{id}/{nombre}",
                defaults: new { controller = "Mascotas", action = "Detalle", nombre = UrlParameter.Optional }
            );

            // Ruta para solicitud de adopción
            routes.MapRoute(
                name: "SolicitarAdopcion",
                url: "Adopcion/Solicitar/{mascotaId}",
                defaults: new { controller = "Adopcion", action = "Solicitar" }
            );

            // Ruta para producto detalle
            routes.MapRoute(
                name: "ProductoDetalle",
                url: "Tienda/Producto/{id}/{nombre}",
                defaults: new { controller = "Tienda", action = "Detalle", nombre = UrlParameter.Optional }
            );

            // Ruta para apadrinar mascota
            routes.MapRoute(
                name: "ApadrinarMascota",
                url: "Donaciones/Apadrinar/{mascotaId}",
                defaults: new { controller = "Donaciones", action = "Apadrinar" }
            );

            // Ruta para actividades de voluntariado
            routes.MapRoute(
                name: "ActividadDetalle",
                url: "Voluntariado/Actividad/{id}",
                defaults: new { controller = "Voluntariado", action = "DetalleActividad" }
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