using System.Web;
using System.Web.Optimization;

namespace MVCMASCOTAS
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            // ===========================
            // BUNDLES DE JAVASCRIPT
            // ===========================

            // jQuery
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // jQuery Validation
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Modernizr
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Bootstrap
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.js"));

            // Scripts personalizados
            bundles.Add(new ScriptBundle("~/bundles/custom").Include(
                      "~/Scripts/custom/site.js",
                      "~/Scripts/custom/validacion-formularios.js"));

            // Scripts de animaciones Canvas
            bundles.Add(new ScriptBundle("~/bundles/canvas").Include(
                      "~/Scripts/custom/animaciones-canvas.js",
                      "~/Scripts/custom/carrusel-mascotas.js"));

            // Scripts de tienda
            bundles.Add(new ScriptBundle("~/bundles/tienda").Include(
                      "~/Scripts/custom/carrito-compras.js"));

            // ===========================
            // BUNDLES DE CSS
            // ===========================

            // Bootstrap y estilos base
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/css/bootstrap.min.css",
                      "~/Content/css/site.css"));

            // Estilos de mascotas
            bundles.Add(new StyleBundle("~/Content/mascotas").Include(
                      "~/Content/css/mascotas.css"));

            // Estilos de adopción
            bundles.Add(new StyleBundle("~/Content/adopcion").Include(
                      "~/Content/css/adopcion.css"));

            // Estilos de tienda
            bundles.Add(new StyleBundle("~/Content/tienda").Include(
                      "~/Content/css/tienda.css"));

#if DEBUG
            BundleTable.EnableOptimizations = false;
#else
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}