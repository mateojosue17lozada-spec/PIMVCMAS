using System.Web.Mvc;

namespace MVCMASCOTAS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Filtro de manejo de errores
            filters.Add(new HandleErrorAttribute());

            // Filtro para requerir HTTPS en producción (comentado por defecto)
            // filters.Add(new RequireHttpsAttribute());

            // Filtro de validación de token anti-falsificación
            filters.Add(new ValidateInputAttribute(true));
        }
    }
}
