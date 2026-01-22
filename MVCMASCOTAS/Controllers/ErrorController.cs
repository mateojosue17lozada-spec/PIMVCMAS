using System;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [AllowAnonymous] // Permitir acceso sin autenticación
    public class ErrorController : Controller
    {
        // GET: Error/NotFound
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            ViewBag.ErrorType = "Página no encontrada";
            ViewBag.ErrorMessage = "La página que buscas no existe o ha sido movida.";
            return View("General");
        }

        // GET: Error/ServerError
        public ActionResult ServerError()
        {
            Response.StatusCode = 500;
            ViewBag.ErrorType = "Error del servidor";
            ViewBag.ErrorMessage = "Ocurrió un error interno en el servidor.";
            return View("General");
        }

        // GET: Error/Forbidden
        public ActionResult Forbidden()
        {
            Response.StatusCode = 403;
            ViewBag.ErrorType = "Acceso denegado";
            ViewBag.ErrorMessage = "No tienes permisos para acceder a esta página.";
            return View("General");
        }

        // GET: Error/Unauthorized
        public ActionResult Unauthorized()
        {
            Response.StatusCode = 401;
            ViewBag.ErrorType = "No autorizado";
            ViewBag.ErrorMessage = "Debes iniciar sesión para acceder a esta página.";
            return View("General");
        }

        // GET: Error/General (manejo por defecto)
        public ActionResult General()
        {
            // Si viene de otro lado sin código, usa 500 por defecto
            if (Response.StatusCode == 200)
                Response.StatusCode = 500;

            ViewBag.ErrorType = "Error";
            ViewBag.ErrorMessage = "Ha ocurrido un error procesando tu solicitud.";
            return View();
        }
    }
}