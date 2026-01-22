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

            // Buscar usuario por email - Activo puede ser NULL o true
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == model.Email &&
                (u.Activo == null || u.Activo == true));

            if (usuario == null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                AuditoriaHelper.RegistrarAccion("Login Fallido", "Account",
                    $"Intento fallido para {model.Email}");
                return View(model);
            }

            // Verificar contraseña
            if (!PasswordHelper.VerifyPassword(model.Password, usuario.PasswordHash, usuario.Salt))
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                AuditoriaHelper.RegistrarAccion("Login Fallido", "Account",
                    $"Contraseña incorrecta para {model.Email}", usuario.UsuarioId);
                return View(model);
            }

            // Verificar si el email está confirmado (opcional)
            if (usuario.EmailConfirmado == false)
            {
                ViewBag.EmailNoConfirmado = true;
                ModelState.AddModelError("", "Su email no ha sido confirmado. Revise su correo.");
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
                DateTime.Now.AddMinutes(model.RememberMe ? 43200 : 120), // 30 días o 2 horas
                model.RememberMe,
                rolesString,
                FormsAuthentication.FormsCookiePath
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection
            };

            if (model.RememberMe)
            {
                cookie.Expires = DateTime.Now.AddDays(30);
            }

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
            else if (roles.Contains("Voluntario"))
                return RedirectToAction("Dashboard", "Voluntario");
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
            if (!string.IsNullOrEmpty(model.Cedula) &&
                db.Usuarios.Any(u => u.Cedula == model.Cedula))
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
                    ModelState.AddModelError("ImagenPerfil",
                        "Archivo de imagen inválido. Formatos permitidos: JPG, PNG, GIF.");
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
                Activo = true,
                EmailConfirmado = false,
                TelefonoConfirmado = false
            };

            try
            {
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
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al registrar usuario. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarAccion("Error Registro", "Account",
                    $"Error: {ex.Message}");
                return View(model);
            }
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // Auditoría
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name &&
                    (u.Activo == null || u.Activo == true));

                if (usuario != null)
                {
                    AuditoriaHelper.RegistrarAccion("Logout", "Account",
                        $"Usuario {usuario.Email} cerró sesión", usuario.UsuarioId);
                }
            }

            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            // Limpiar cookies
            if (Response.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                Response.Cookies[FormsAuthentication.FormsCookieName].Expires = DateTime.Now.AddDays(-1);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/MiPerfil
        [Authorize]
        public ActionResult MiPerfil()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name &&
                (u.Activo == null || u.Activo == true));

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                return RedirectToAction("Logout");
            }

            // Convertir imagen a base64 para mostrar
            if (usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0)
            {
                ViewBag.ImagenBase64 = Convert.ToBase64String(usuario.ImagenPerfil);
            }
            else
            {
                ViewBag.ImagenBase64 = null;
            }

            // Obtener roles del usuario
            var roles = db.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Select(ur => ur.Roles.NombreRol)
                .ToList();

            ViewBag.Roles = roles;
            ViewBag.FechaRegistroFormateada = usuario.FechaRegistro?.ToString("dd/MM/yyyy HH:mm");

            return View(usuario);
        }

        // GET: Account/EditProfile
        [Authorize]
        public ActionResult EditProfile()
        {
            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name &&
                (u.Activo == null || u.Activo == true));

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                return RedirectToAction("Logout");
            }

            // Crear ViewModel usando el método FromUsuario
            var model = new EditProfileViewModel
            {
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                Cedula = usuario.Cedula,
                Direccion = usuario.Direccion,
                Ciudad = usuario.Ciudad,
                Provincia = usuario.Provincia,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo,
                EmailConfirmado = usuario.EmailConfirmado,
                TelefonoConfirmado = usuario.TelefonoConfirmado
            };

            // Convertir imagen a base64 para el ViewModel
            if (usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0)
            {
                model.ImagenPerfilBase64 = Convert.ToBase64String(usuario.ImagenPerfil);
            }

            // También pasar en ViewBag para compatibilidad
            ViewBag.ImagenBase64 = model.ImagenPerfilBase64;

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
                var usuarioActual = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name &&
                    (u.Activo == null || u.Activo == true));

                if (usuarioActual != null && usuarioActual.ImagenPerfil != null)
                {
                    model.ImagenPerfilBase64 = Convert.ToBase64String(usuarioActual.ImagenPerfil);
                    ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                }
                return View(model);
            }

            var usuario = db.Usuarios.FirstOrDefault(u =>
                u.Email == User.Identity.Name &&
                (u.Activo == null || u.Activo == true));

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                return RedirectToAction("Logout");
            }

            try
            {
                // Actualizar solo propiedades editables (no Email, Cedula, FechaRegistro, etc.)
                usuario.NombreCompleto = model.NombreCompleto;
                usuario.Telefono = model.Telefono;
                usuario.Direccion = model.Direccion;
                usuario.Ciudad = model.Ciudad;
                usuario.Provincia = model.Provincia;

                // Actualizar imagen si se proporciona una nueva
                if (model.NuevaImagenPerfil != null)
                {
                    if (!ImageHelper.IsValidImage(model.NuevaImagenPerfil))
                    {
                        ModelState.AddModelError("NuevaImagenPerfil",
                            "Archivo de imagen inválido. Formatos permitidos: JPG, PNG, GIF.");
                        model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                            ? Convert.ToBase64String(usuario.ImagenPerfil)
                            : null;
                        ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                        return View(model);
                    }

                    if (!ImageHelper.IsFileSizeValid(model.NuevaImagenPerfil, 5))
                    {
                        ModelState.AddModelError("NuevaImagenPerfil", "La imagen no debe exceder 5 MB.");
                        model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                            ? Convert.ToBase64String(usuario.ImagenPerfil)
                            : null;
                        ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                        return View(model);
                    }

                    byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(model.NuevaImagenPerfil);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                    usuario.ImagenPerfil = imagenBytes;

                    // Actualizar la imagen en el modelo para mostrar en la vista
                    model.ImagenPerfilBase64 = Convert.ToBase64String(imagenBytes);
                }

                db.SaveChanges();

                // Actualizar propiedades de solo lectura en el modelo para mostrar en la vista
                model.Email = usuario.Email;
                model.Cedula = usuario.Cedula;
                model.FechaRegistro = usuario.FechaRegistro;
                model.Activo = usuario.Activo;
                model.EmailConfirmado = usuario.EmailConfirmado;
                model.TelefonoConfirmado = usuario.TelefonoConfirmado;

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Actualizar Perfil", "Account",
                    $"Usuario {usuario.Email} actualizó su perfil", usuario.UsuarioId);

                TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
                return RedirectToAction("MiPerfil");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al actualizar el perfil. Por favor intente nuevamente.");

                // Mantener la imagen actual
                model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                    ? Convert.ToBase64String(usuario.ImagenPerfil)
                    : null;
                ViewBag.ImagenBase64 = model.ImagenPerfilBase64;

                AuditoriaHelper.RegistrarAccion("Error Actualizar Perfil", "Account",
                    $"Error: {ex.Message}", usuario.UsuarioId);
                return View(model);
            }
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
                u.Email == User.Identity.Name &&
                (u.Activo == null || u.Activo == true));

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
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

            // Verificar que la nueva contraseña sea diferente a la actual
            if (PasswordHelper.VerifyPassword(model.NewPassword, usuario.PasswordHash, usuario.Salt))
            {
                ModelState.AddModelError("NewPassword",
                    "La nueva contraseña debe ser diferente a la actual.");
                return View(model);
            }

            try
            {
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
                return RedirectToAction("MiPerfil");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al cambiar la contraseña. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarAccion("Error Cambiar Contraseña", "Account",
                    $"Error: {ex.Message}", usuario.UsuarioId);
                return View(model);
            }
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
                u.Email == model.Email &&
                (u.Activo == null || u.Activo == true));

            if (usuario != null)
            {
                // Generar token de recuperación
                string resetToken = Guid.NewGuid().ToString();

                // Guardar token temporalmente en Session
                Session["ResetToken_" + usuario.UsuarioId] = resetToken;
                Session["ResetTokenExpiry_" + usuario.UsuarioId] = DateTime.Now.AddHours(1);

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Solicitud Recuperación", "Account",
                    $"Usuario {usuario.Email} solicitó recuperación de contraseña", usuario.UsuarioId);

                // Enviar email con enlace de recuperación
                string resetLink = Url.Action("ResetPassword", "Account",
                    new { token = resetToken, userId = usuario.UsuarioId },
                    Request.Url.Scheme);

                _ = EmailHelper.SendPasswordResetAsync(usuario.Email, usuario.NombreCompleto, resetLink);
            }

            // Mostrar mismo mensaje aunque el email no exista por seguridad
            TempData["InfoMessage"] = "Si el email existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña.";
            return RedirectToAction("Login");
        }

        // GET: Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string token, int userId)
        {
            // Verificar token válido
            var storedToken = Session["ResetToken_" + userId] as string;
            var expiry = Session["ResetTokenExpiry_" + userId] as DateTime?;

            if (string.IsNullOrEmpty(token) || token != storedToken ||
                expiry == null || DateTime.Now > expiry.Value)
            {
                TempData["ErrorMessage"] = "El enlace de recuperación es inválido o ha expirado.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                UserId = userId
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar token nuevamente
            var storedToken = Session["ResetToken_" + model.UserId] as string;
            var expiry = Session["ResetTokenExpiry_" + model.UserId] as DateTime?;

            if (string.IsNullOrEmpty(model.Token) || model.Token != storedToken ||
                expiry == null || DateTime.Now > expiry.Value)
            {
                TempData["ErrorMessage"] = "El enlace de recuperación es inválido o ha expirado.";
                return RedirectToAction("Login");
            }

            var usuario = db.Usuarios.Find(model.UserId);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Login");
            }

            try
            {
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

                // Limpiar tokens de sesión
                Session.Remove("ResetToken_" + model.UserId);
                Session.Remove("ResetTokenExpiry_" + model.UserId);

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Restablecer Contraseña", "Account",
                    $"Usuario {usuario.Email} restableció su contraseña", usuario.UsuarioId);

                TempData["SuccessMessage"] = "Contraseña restablecida exitosamente. Puede iniciar sesión con su nueva contraseña.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al restablecer la contraseña. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarAccion("Error Restablecer Contraseña", "Account",
                    $"Error: {ex.Message}", usuario.UsuarioId);
                return View(model);
            }
        }

        // GET: Account/ConfirmEmail
        [AllowAnonymous]
        public ActionResult ConfirmEmail(string token, int userId)
        {
            var usuario = db.Usuarios.Find(userId);
            if (usuario == null || usuario.EmailConfirmado == true)
            {
                TempData["ErrorMessage"] = "Enlace de confirmación inválido o email ya confirmado.";
                return RedirectToAction("Login");
            }

            // Aquí deberías verificar el token (depende de tu implementación)
            // Por ahora simplemente marcamos como confirmado
            usuario.EmailConfirmado = true;
            db.SaveChanges();

            AuditoriaHelper.RegistrarAccion("Confirmar Email", "Account",
                $"Usuario {usuario.Email} confirmó su email", usuario.UsuarioId);

            TempData["SuccessMessage"] = "¡Email confirmado exitosamente! Ya puede iniciar sesión.";
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