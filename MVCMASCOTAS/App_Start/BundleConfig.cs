using System.Web.Optimization;

namespace MVCMASCOTAS
{
    public class BundleConfig
    {
        // Para obtener más información sobre las uniones, visite https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
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
                      "~/Scripts/bootstrap.js"));

            // Scripts personalizados
            bundles.Add(new ScriptBundle("~/bundles/custom").Include(
                      "~/Scripts/site.js"));

            // CSS de Bootstrap
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));

            // CSS personalizado
            bundles.Add(new StyleBundle("~/Content/custom").Include(
                      "~/Content/custom.css"));

            // Habilitar optimización en producción
            // BundleTable.EnableOptimizations = true;
        }
    }
}
