using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [AllowAnonymous]
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            return View();
        }
    }
}
