using System.Linq;
using System.Web;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Helpers
{
    public static class UserHelper
    {
        public static Usuarios GetCurrentUser(RefugioMascotasDBEntities db)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            string email = HttpContext.Current.User.Identity.Name;
            return db.Usuarios.FirstOrDefault(u => u.Email == email);
        }

        public static int? GetCurrentUserId()
        {
            var user = GetCurrentUser(new RefugioMascotasDBEntities());
            return user?.UsuarioId;
        }
    }
}