using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new PreventClickjackingAttribute());
        }
    }

    public class PreventClickjackingAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.AddHeader("X-Frame-Options", "SAMEORIGIN");
            base.OnResultExecuting(filterContext);
        }
    }
}