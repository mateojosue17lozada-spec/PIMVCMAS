using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    public class AccountController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Buscar usuario por email - CORREGIDO: Activo puede ser NULL
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == model.Email && u.Activo == true);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                return View(model);
            }

            // Verificar contraseña
            if (!PasswordHelper.VerifyPassword(model.Password, usuario.PasswordHash, usuario.Salt))
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                AuditoriaHelper.RegistrarAccion("Login Fallido", "Account", $"Intento fallido para {model.Email}");
                return View(model);
            }

            // Obtener roles del usuario
            var roles = db.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Select(ur => ur.Roles.NombreRol)
                .ToArray();

            // Crear ticket de autenticación
            string rolesString = string.Join(",", roles);
            var ticket = new FormsAuthenticationTicket(
                1,
                usuario.Email,
                DateTime.Now,
                DateTime.Now.AddMinutes(model.RememberMe ? 43200 : 120),
                model.RememberMe,
                rolesString,
                FormsAuthentication.FormsCookiePath
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            Response.Cookies.Add(cookie);

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.Now;
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Login Exitoso", "Account",
                $"Usuario {usuario.Email} inició sesión", usuario.UsuarioId);

            // Redireccionar
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirigir según rol principal
            if (roles.Contains("Administrador"))
                return RedirectToAction("Dashboard", "Admin");
            else if (roles.Contains("Veterinario"))
                return RedirectToAction("Dashboard", "Veterinario");
            else if (roles.Contains("Contabilidad"))
                return RedirectToAction("Dashboard", "Contabilidad");
            else
                return RedirectToAction("Index", "Home");
        }

        // GET: Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si el email ya existe
            if (db.Usuarios.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Este email ya está registrado.");
                return View(model);
            }

            // Verificar si la cédula ya existe
            if (db.Usuarios.Any(u => u.Cedula == model.Cedula))
            {
                ModelState.AddModelError("Cedula", "Esta cédula ya está registrada.");
                return View(model);
            }

            // Validar contraseña
            if (!PasswordHelper.IsPasswordValid(model.Password))
            {
                ModelState.AddModelError("Password", PasswordHelper.GetPasswordValidationMessage());
                return View(model);
            }

            // Validar imagen si se proporciona
            byte[] imagenBytes = null;
            if (model.ImagenPerfil != null)
            {
                if (!ImageHelper.IsValidImage(model.ImagenPerfil))
                {
                    ModelState.AddModelError("ImagenPerfil", "Archivo de imagen inválido.");
                    return View(model);
                }

                if (!ImageHelper.IsFileSizeValid(model.ImagenPerfil, 5))
                {
                    ModelState.AddModelError("ImagenPerfil", "La imagen no debe exceder 5 MB.");
                    return View(model);
                }

                imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenPerfil);
                imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
            }

            // Generar salt y hash
            string salt = PasswordHelper.GenerateSalt();
            string passwordHash = PasswordHelper.HashPassword(model.Password, salt);

            // Crear usuario
            var usuario = new Usuarios
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                Telefono = model.Telefono,
                Cedula = model.Cedula,
                Direccion = model.Direccion,
                Ciudad = model.Ciudad ?? "Quito",
                Provincia = model.Provincia ?? "Pichincha",
                PasswordHash = passwordHash,
                Salt = salt,
                ImagenPerfil = imagenBytes,
                FechaRegistro = DateTime.Now,
                Activo = true,  // Cuando se crea, siempre es true
                EmailConfirmado = false,
                TelefonoConfirmado = false
            };

            db.Usuarios.Add(usuario);
            db.SaveChanges();

            // Asignar rol de Usuario por defecto
            var rolUsuario = db.Roles.FirstOrDefault(r => r.NombreRol == "Usuario");
            if (rolUsuario != null)
            {
                var usuarioRol = new UsuariosRoles
                {
                    UsuarioId = usuario.UsuarioId,
                    RolId = rolUsuario.RolId,
                    FechaAsignacion = DateTime.Now
                };
                db.UsuariosRoles.Add(usuarioRol);
                db.SaveChanges();
            }

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Registro", "Account",
                $"Nuevo usuario registrado: {usuario.Email}", usuario.UsuarioId);

            // Enviar email de bienvenida (asíncrono)
            _ = EmailHelper.SendRegistrationConfirmationAsync(usuario.Email, usuario.NombreCompleto);

            // Autenticar automáticamente
            var ticket = new FormsAuthenticationTicket(
                1,
                usuario.Email,
                DateTime.Now,
                DateTime.Now.AddHours(2),
                false,
                "Usuario",
                FormsAuthentication.FormsCookiePath
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            Response.Cookies.Add(cookie);

            TempData["SuccessMessage"] = "¡Registro exitoso! Bienvenido al sistema.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // Auditoría
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
                if (usuario != null)
                {
                    AuditoriaHelper.RegistrarAccion("Logout", "Account",
                        $"Usuario {usuario.Email} cerró sesión", usuario.UsuarioId);
                }
            }

            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/MiPerfil (CAMBIADO DE Profile A MiPerfil)
        [Authorize]
        public ActionResult MiPerfil()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout");
            }

            ViewBag.ImagenBase64 = usuario.ImagenPerfil != null
                ? ImageHelper.GetImageDataUri(usuario.ImagenPerfil)
                : null;

            return View(usuario);
        }

        // GET: Account/EditProfile
        [Authorize]
        public ActionResult EditProfile()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout");
            }

            var model = new EditProfileViewModel
            {
                NombreCompleto = usuario.NombreCompleto,
                Telefono = usuario.Telefono,
                Direccion = usuario.Direccion,
                Ciudad = usuario.Ciudad,
                Provincia = usuario.Provincia
            };

            ViewBag.ImagenBase64 = usuario.ImagenPerfil != null
                ? ImageHelper.GetImageDataUri(usuario.ImagenPerfil)
                : null;

            return View(model);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Mantener la imagen actual en la vista si hay error
                var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuarioActual != null)
                {
                    ViewBag.ImagenBase64 = usuarioActual.ImagenPerfil != null
                        ? ImageHelper.GetImageDataUri(usuarioActual.ImagenPerfil)
                        : null;
                }
                return View(model);
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout");
            }

            // Actualizar datos
            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Telefono = model.Telefono;
            usuario.Direccion = model.Direccion;
            usuario.Ciudad = model.Ciudad;
            usuario.Provincia = model.Provincia;

            // Actualizar imagen si se proporciona
            if (model.NuevaImagenPerfil != null)
            {
                if (!ImageHelper.IsValidImage(model.NuevaImagenPerfil))
                {
                    ModelState.AddModelError("NuevaImagenPerfil", "Archivo de imagen inválido.");
                    ViewBag.ImagenBase64 = usuario.ImagenPerfil != null
                        ? ImageHelper.GetImageDataUri(usuario.ImagenPerfil)
                        : null;
                    return View(model);
                }

                if (!ImageHelper.IsFileSizeValid(model.NuevaImagenPerfil, 5))
                {
                    ModelState.AddModelError("NuevaImagenPerfil", "La imagen no debe exceder 5 MB.");
                    ViewBag.ImagenBase64 = usuario.ImagenPerfil != null
                        ? ImageHelper.GetImageDataUri(usuario.ImagenPerfil)
                        : null;
                    return View(model);
                }

                byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(model.NuevaImagenPerfil);
                imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                usuario.ImagenPerfil = imagenBytes;
            }

            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Actualizar Perfil", "Account",
                $"Usuario {usuario.Email} actualizó su perfil", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
            return RedirectToAction("MiPerfil"); // CAMBIADO DE Profile A MiPerfil
        }

        // GET: Account/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout");
            }

            // Verificar contraseña actual
            if (!PasswordHelper.VerifyPassword(model.CurrentPassword, usuario.PasswordHash, usuario.Salt))
            {
                ModelState.AddModelError("CurrentPassword", "La contraseña actual es incorrecta.");
                return View(model);
            }

            // Validar nueva contraseña
            if (!PasswordHelper.IsPasswordValid(model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", PasswordHelper.GetPasswordValidationMessage());
                return View(model);
            }

            // Generar nueva salt y hash
            string newSalt = PasswordHelper.GenerateSalt();
            string newPasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

            // Actualizar contraseña
            usuario.PasswordHash = newPasswordHash;
            usuario.Salt = newSalt;
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cambiar Contraseña", "Account",
                $"Usuario {usuario.Email} cambió su contraseña", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Contraseña cambiada exitosamente.";
            return RedirectToAction("MiPerfil"); // CAMBIADO DE Profile A MiPerfil
        }

        // GET: Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == model.Email && u.Activo == true);

            if (usuario != null)
            {
                // Generar token de recuperación
                string resetToken = Guid.NewGuid().ToString();

                // Guardar token en la base de datos (necesitarías crear una tabla para esto)
                // Por ahora solo auditoría
                AuditoriaHelper.RegistrarAccion("Solicitud Recuperación", "Account",
                    $"Usuario {usuario.Email} solicitó recuperación de contraseña", usuario.UsuarioId);

                // Enviar email con enlace de recuperación
                _ = EmailHelper.SendPasswordResetAsync(usuario.Email, usuario.NombreCompleto, resetToken);
            }

            // Mostrar mismo mensaje aunque el email no exista por seguridad
            TempData["InfoMessage"] = "Si el email existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña.";
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}