using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MVCMASCOTAS.Controllers
{
    /// <summary>
    /// Controlador de gestión de cuentas de usuario
    /// Maneja: Login, Register, Logout, Perfil, Cambio de contraseña, Recuperación
    /// Con bloqueo por intentos fallidos y sistema de seguridad mejorado
    /// </summary>
    public class AccountController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        #region LOGIN Y AUTENTICACIÓN

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // Si ya está autenticado, redirigir al home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

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

            try
            {
                // ⚡ CONFIGURACIONES DE SEGURIDAD
                int maxIntentos = int.Parse(ConfigurationManager.AppSettings["MaxIntentosLogin"] ?? "3");
                int tiempoBloqueoHoras = int.Parse(ConfigurationManager.AppSettings["TiempoBloqueoHoras"] ?? "2");
                int tiempoReinicioIntentosHoras = int.Parse(ConfigurationManager.AppSettings["TiempoReinicioIntentosHoras"] ?? "2");

                // Buscar usuario por email
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email.Trim().ToLower() == model.Email.Trim().ToLower());

                // ⚡ NUEVO: Verificar si el usuario existe
                if (usuario == null)
                {
                    ModelState.AddModelError("", "Email o contraseña incorrectos.");
                    AuditoriaHelper.RegistrarAccion("Login Fallido", "Account",
                        $"Intento fallido - Usuario no existe: {model.Email}");
                    return View(model);
                }

                // ⚡ NUEVO: Verificar si el usuario está bloqueado permanentemente
                if (usuario.BloqueadoPermanentemente == true)
                {
                    ModelState.AddModelError("", "Su cuenta ha sido bloqueada permanentemente. Contacte al administrador.");
                    AuditoriaHelper.RegistrarAccion("Login Bloqueado Permanente", "Account",
                        $"Intento de login en cuenta bloqueada permanentemente: {model.Email}");
                    return View(model);
                }

                // ⚡ NUEVO: Verificar si el usuario está activo (bloqueo temporal)
                if (usuario.Activo == false)
                {
                    // Verificar si ya pasó el tiempo de bloqueo
                    if (usuario.FechaDesbloqueo.HasValue && usuario.FechaDesbloqueo > DateTime.Now)
                    {
                        TimeSpan tiempoRestante = usuario.FechaDesbloqueo.Value - DateTime.Now;
                        string tiempoRestanteFormateado = $"{(int)tiempoRestante.TotalHours}h {tiempoRestante.Minutes}m";

                        ModelState.AddModelError("",
                            $"Cuenta bloqueada temporalmente. Intente nuevamente en: {tiempoRestanteFormateado}");
                        AuditoriaHelper.RegistrarAccion("Login Bloqueado Temporal", "Account",
                            $"Intento de login en cuenta bloqueada temporalmente: {model.Email}. Desbloqueo: {usuario.FechaDesbloqueo}");
                        return View(model);
                    }
                    else
                    {
                        // ⚡ NUEVO: Desbloquear automáticamente si pasó el tiempo
                        usuario.Activo = true;
                        usuario.IntentosFallidos = 0;
                        usuario.FechaBloqueoTemporal = null;
                        usuario.FechaDesbloqueo = null;
                        usuario.FechaUltimoIntentoFallido = null;
                        db.SaveChanges();
                    }
                }

                // ⚡ NUEVO: Verificar si el usuario está activo (puede ser null)
                if (usuario.Activo.HasValue && usuario.Activo == false)
                {
                    ModelState.AddModelError("", "Su cuenta está inactiva. Contacte al administrador.");
                    return View(model);
                }

                // ⚡ NUEVO: Reiniciar intentos fallidos si pasó el tiempo
                if (usuario.FechaUltimoIntentoFallido.HasValue)
                {
                    DateTime limiteReinicio = DateTime.Now.AddHours(-tiempoReinicioIntentosHoras);
                    if (usuario.FechaUltimoIntentoFallido < limiteReinicio)
                    {
                        usuario.IntentosFallidos = 0;
                        usuario.FechaUltimoIntentoFallido = null;
                        db.SaveChanges();
                    }
                }

                // Verificar contraseña
                if (!PasswordHelper.VerifyPassword(model.Password, usuario.PasswordHash, usuario.Salt))
                {
                    // ⚡ NUEVO: Incrementar y registrar intentos fallidos
                    usuario.IntentosFallidos = (usuario.IntentosFallidos ?? 0) + 1;
                    usuario.FechaUltimoIntentoFallido = DateTime.Now;

                    // ⚡ NUEVO: Bloquear después de X intentos
                    if (usuario.IntentosFallidos >= maxIntentos)
                    {
                        usuario.Activo = false;
                        usuario.FechaBloqueoTemporal = DateTime.Now;
                        usuario.FechaDesbloqueo = DateTime.Now.AddHours(tiempoBloqueoHoras);

                        ModelState.AddModelError("",
                            $"Cuenta bloqueada por {tiempoBloqueoHoras} horas debido a múltiples intentos fallidos.");

                        AuditoriaHelper.RegistrarAccion("Usuario Bloqueado", "Account",
                            $"Usuario {usuario.Email} bloqueado por {tiempoBloqueoHoras} horas. Intentos: {usuario.IntentosFallidos}",
                            usuario.UsuarioId);
                    }
                    else
                    {
                        int intentosRestantes = maxIntentos - usuario.IntentosFallidos.Value;
                        ModelState.AddModelError("",
                            $"Email o contraseña incorrectos. Intentos restantes: {intentosRestantes}");

                        AuditoriaHelper.RegistrarAccion("Login Fallido", "Account",
                            $"Contraseña incorrecta para {usuario.Email}. Intentos: {usuario.IntentosFallidos}/{maxIntentos}",
                            usuario.UsuarioId);
                    }

                    db.SaveChanges();
                    return View(model);
                }

                // ⚡ NUEVO: Reiniciar intentos fallidos después de login exitoso
                usuario.IntentosFallidos = 0;
                usuario.FechaUltimoIntentoFallido = null;
                usuario.FechaBloqueoTemporal = null;
                usuario.FechaDesbloqueo = null;

                // ⚡ NUEVO: Verificar si el email está confirmado (OPCIONAL - comentar si no usas confirmación)
                if (ConfigurationManager.AppSettings["RequerirConfirmacionEmail"] == "true" && usuario.EmailConfirmado == false)
                {
                    ViewBag.EmailNoConfirmado = true;
                    ViewBag.UsuarioId = usuario.UsuarioId;
                    ModelState.AddModelError("", "Su email no ha sido confirmado. Revise su correo o solicite un nuevo enlace.");
                    return View(model);
                }

                // ⚡ NUEVO: Verificar si necesita cambiar contraseña (cada 90 días)
                int diasCambioPassword = int.Parse(ConfigurationManager.AppSettings["DiasCambioPassword"] ?? "90");
                if (usuario.UltimoAcceso.HasValue)
                {
                    var diasDesdeUltimoAcceso = (DateTime.Now - usuario.UltimoAcceso.Value).TotalDays;
                    if (diasDesdeUltimoAcceso > diasCambioPassword)
                    {
                        TempData["WarningMessage"] = "Por seguridad, debe cambiar su contraseña. Han pasado más de 90 días desde su último cambio.";
                        // Redirigir a cambio de contraseña después del login
                    }
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
                    Secure = Request.IsSecureConnection,
                    SameSite = SameSiteMode.Lax
                };

                if (model.RememberMe)
                {
                    cookie.Expires = DateTime.Now.AddDays(30);
                }

                Response.Cookies.Add(cookie);

                // Actualizar último acceso y reiniciar contadores
                usuario.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                // Auditoría de login exitoso
                AuditoriaHelper.RegistrarAccion("Login Exitoso", "Account",
                    $"Usuario {usuario.Email} inició sesión. Roles: {rolesString}", usuario.UsuarioId);

                // Redireccionar
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirigir según rol principal (el primero en la lista)
                if (roles.Contains("Administrador"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (roles.Contains("Veterinario"))
                    return RedirectToAction("Dashboard", "Veterinario");
                else if (roles.Contains("Contabilidad"))
                    return RedirectToAction("Dashboard", "Contabilidad");
                else if (roles.Contains("Voluntario"))
                    return RedirectToAction("Index", "Voluntariado");
                else
                    return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log del error
                System.Diagnostics.Debug.WriteLine($"Error en Login: {ex.Message}");
                AuditoriaHelper.RegistrarError("AccountController.Login",
                    $"Error al procesar login para {model.Email}", ex);

                ModelState.AddModelError("", "Error al procesar el inicio de sesión. Por favor intente nuevamente.");
                return View(model);
            }
        }

        #endregion

        #region REGISTRO DE USUARIOS

        // GET: Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            // Si ya está autenticado, redirigir al home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model) // CAMBIADO a async Task<ActionResult>
        {
            try
            {
                // ⚡ NUEVO: Validar pregunta de seguridad antes de cualquier cosa
                if (model.PreguntaSeguridad != 4)
                {
                    ModelState.AddModelError("PreguntaSeguridad", "Respuesta incorrecta. Por favor, responda correctamente la pregunta de seguridad.");
                    return View(model);
                }

                if (!ModelState.IsValid)
                {
                    // Log de errores de validación para depuración
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        System.Diagnostics.Debug.WriteLine($"Error de validación: {error.ErrorMessage}");
                    }
                    return View(model);
                }

                // Verificar si el email ya existe (case-insensitive)
                if (db.Usuarios.Any(u => u.Email.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado.");
                    AuditoriaHelper.RegistrarAccion("Registro Fallido", "Account",
                        $"Intento de registro con email existente: {model.Email}");
                    return View(model);
                }

                // Verificar si la cédula ya existe (solo si se proporciona)
                if (!string.IsNullOrEmpty(model.Cedula) &&
                    db.Usuarios.Any(u => u.Cedula == model.Cedula))
                {
                    ModelState.AddModelError("Cedula", "Esta cédula ya está registrada.");
                    AuditoriaHelper.RegistrarAccion("Registro Fallido", "Account",
                        $"Intento de registro con cédula existente: {model.Cedula}");
                    return View(model);
                }

                // ⚡ NUEVO: Validar formato de cédula ecuatoriana
                if (!string.IsNullOrEmpty(model.Cedula))
                {
                    if (!EsCedulaValida(model.Cedula))
                    {
                        ModelState.AddModelError("Cedula", "La cédula no es válida. Verifique que sea una cédula ecuatoriana correcta.");
                        return View(model);
                    }
                }

                // Validar y procesar imagen si se proporciona
                byte[] imagenBytes = null;
                if (model.ImagenPerfil != null && model.ImagenPerfil.ContentLength > 0)
                {
                    // Validar tipo de archivo
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    string fileExtension = System.IO.Path.GetExtension(model.ImagenPerfil.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ImagenPerfil", "Formato de imagen no válido. Formatos permitidos: JPG, PNG, GIF.");
                        return View(model);
                    }

                    // Validar tamaño de archivo (máximo 5 MB)
                    int maxFileSize = 5 * 1024 * 1024; // 5 MB en bytes
                    if (model.ImagenPerfil.ContentLength > maxFileSize)
                    {
                        ModelState.AddModelError("ImagenPerfil", "La imagen es demasiado grande. Tamaño máximo: 5 MB.");
                        return View(model);
                    }

                    try
                    {
                        // Validar que sea una imagen real
                        using (var image = System.Drawing.Image.FromStream(model.ImagenPerfil.InputStream))
                        {
                            // Redimensionar y convertir a byte array
                            imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenPerfil);
                            imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                        }
                        model.ImagenPerfil.InputStream.Position = 0; // Resetear stream
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("ImagenPerfil", "El archivo no es una imagen válida.");
                        return View(model);
                    }
                }

                // Generar salt y hash
                string salt = PasswordHelper.GenerateSalt();
                string passwordHash = PasswordHelper.HashPassword(model.Password, salt);

                // Crear usuario con valores iniciales de seguridad
                var usuario = new Usuarios
                {
                    NombreCompleto = model.NombreCompleto?.Trim(),
                    Email = model.Email?.Trim().ToLower(),
                    Telefono = model.Telefono?.Trim(),
                    Cedula = model.Cedula?.Trim(),
                    Direccion = model.Direccion?.Trim(),
                    Ciudad = model.Ciudad?.Trim() ?? "Quito",
                    Provincia = model.Provincia?.Trim() ?? "Pichincha",
                    PasswordHash = passwordHash,
                    Salt = salt,
                    ImagenPerfil = imagenBytes,
                    FechaRegistro = DateTime.Now,
                    UltimoAcceso = DateTime.Now,
                    Activo = true,
                    // ⚡ NUEVO: Inicializar campos de seguridad
                    IntentosFallidos = 0,
                    FechaBloqueoTemporal = null,
                    FechaDesbloqueo = null,
                    BloqueadoPermanentemente = false,
                    FechaUltimoIntentoFallido = null,
                    EmailConfirmado = ConfigurationManager.AppSettings["RequerirConfirmacionEmail"] != "true", // Confirmación automática si no se requiere
                    TelefonoConfirmado = false,
                    FechaUltimoCambioPassword = DateTime.Now
                };

                db.Usuarios.Add(usuario);
                db.SaveChanges();

                // Asignar rol de "Usuario" por defecto
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

                // ⚡ NUEVO: Enviar email de confirmación si está habilitado
                bool emailHabilitado = bool.Parse(ConfigurationManager.AppSettings["EmailHabilitado"] ?? "false");
                if (emailHabilitado && ConfigurationManager.AppSettings["RequerirConfirmacionEmail"] == "true")
                {
                    try
                    {
                        string token = Guid.NewGuid().ToString("N");
                        Session["EmailConfirmToken_" + usuario.UsuarioId] = token;
                        Session["EmailConfirmTokenExpiry_" + usuario.UsuarioId] = DateTime.Now.AddHours(24);

                        string confirmLink = Url.Action("ConfirmEmail", "Account",
                            new { code = token, userId = usuario.UsuarioId },
                            Request.Url.Scheme);

                        bool emailEnviado = await EmailHelper.SendEmailConfirmationAsync(usuario.Email, usuario.NombreCompleto, confirmLink);

                        if (!emailEnviado)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WARNING] No se pudo enviar email de confirmación a: {usuario.Email}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SUCCESS] Email de confirmación enviado a: {usuario.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // No bloquear el registro si falla el email
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Error al enviar email de confirmación: {emailEx.Message}");
                    }
                }

                // Autenticar automáticamente al usuario si no requiere confirmación
                if (usuario.EmailConfirmado == true)
                {
                    FormsAuthentication.SetAuthCookie(usuario.Email, false);
                    TempData["SuccessMessage"] = "¡Registro exitoso! Bienvenido al sistema.";
                }
                else
                {
                    TempData["InfoMessage"] = "¡Registro exitoso! Por favor revise su correo para confirmar su cuenta.";
                    return RedirectToAction("Login");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log del error completo
                System.Diagnostics.Debug.WriteLine($"Error en registro: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                }

                ModelState.AddModelError("", "Error al registrar usuario. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarError("AccountController.Register",
                    $"Error al registrar usuario con email: {model.Email}", ex);
                return View(model);
            }
        }

        // ⚡ NUEVO: Método para validar cédula ecuatoriana
        private bool EsCedulaValida(string cedula)
        {
            if (cedula.Length != 10 || !Regex.IsMatch(cedula, @"^\d{10}$"))
                return false;

            // Validar provincia (primeros dos dígitos)
            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24)
                return false;

            // Validar tercer dígito
            int tercerDigito = int.Parse(cedula.Substring(2, 1));
            if (tercerDigito > 6)
                return false;

            // Algoritmo de validación de dígito verificador
            int total = 0;
            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int verificador = int.Parse(cedula.Substring(9, 1));

            for (int i = 0; i < coeficientes.Length; i++)
            {
                int valor = int.Parse(cedula.Substring(i, 1)) * coeficientes[i];
                if (valor >= 10)
                    valor -= 9;
                total += valor;
            }

            int residuo = total % 10;
            int digitoCalculado = (residuo == 0) ? 0 : 10 - residuo;

            return digitoCalculado == verificador;
        }

        #endregion

        #region LOGOUT

        // GET: Account/Logout
        public ActionResult Logout()
        {
            try
            {
                // Auditoría ANTES de cerrar sesión
                if (User.Identity.IsAuthenticated)
                {
                    var usuario = db.Usuarios.FirstOrDefault(u =>
                        u.Email == User.Identity.Name);

                    if (usuario != null)
                    {
                        AuditoriaHelper.RegistrarAccion("Logout", "Account",
                            $"Usuario {usuario.Email} cerró sesión", usuario.UsuarioId);
                    }
                }

                // Limpiar autenticación
                FormsAuthentication.SignOut();

                // Limpiar sesión
                Session.Clear();
                Session.Abandon();

                // Limpiar cookies
                if (Response.Cookies[FormsAuthentication.FormsCookieName] != null)
                {
                    Response.Cookies[FormsAuthentication.FormsCookieName].Expires = DateTime.Now.AddDays(-1);
                }

                // ⚡ NUEVO: Limpiar cookie de roles si existe
                if (Response.Cookies[".ASPROLES"] != null)
                {
                    Response.Cookies[".ASPROLES"].Expires = DateTime.Now.AddDays(-1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Logout: {ex.Message}");
                AuditoriaHelper.RegistrarError("AccountController.Logout", "Error en proceso de logout", ex);
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region PERFIL DE USUARIO

        // GET: Account/MiPerfil
        [Authorize]
        public ActionResult MiPerfil()
        {
            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name &&
                    (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                    return RedirectToAction("Logout");
                }

                // Obtener estadísticas para mostrar en la vista
                var userId = usuario.UsuarioId;

                // Usar try-catch para cada estadística en caso de que las tablas no existan
                try { ViewBag.TotalSolicitudes = db.SolicitudAdopcion.Count(s => s.UsuarioId == userId); }
                catch { ViewBag.TotalSolicitudes = 0; }

                try { ViewBag.TotalDonaciones = db.Donaciones.Count(d => d.UsuarioId == userId && d.Estado == "Completada"); }
                catch { ViewBag.TotalDonaciones = 0; }

                try { ViewBag.TotalApadrinamientos = db.Apadrinamientos.Count(a => a.UsuarioId == userId && a.Estado == "Activo"); }
                catch { ViewBag.TotalApadrinamientos = 0; }

                try { ViewBag.TotalVoluntariado = db.InscripcionesActividades.Count(i => i.UsuarioId == userId && i.Estado == "Confirmada"); }
                catch { ViewBag.TotalVoluntariado = 0; }

                try { ViewBag.TotalPedidos = db.Pedidos.Count(p => p.UsuarioId == userId); }
                catch { ViewBag.TotalPedidos = 0; }

                try { ViewBag.TotalReportes = db.ReportesRescate.Count(r => r.UsuarioReportante == userId); }
                catch { ViewBag.TotalReportes = 0; }

                // ⚡ NUEVO: Mostrar estado de seguridad
                ViewBag.IntentosFallidos = usuario.IntentosFallidos ?? 0;
                ViewBag.BloqueadoPermanentemente = usuario.BloqueadoPermanentemente ?? false;
                ViewBag.FechaBloqueoTemporal = usuario.FechaBloqueoTemporal;
                ViewBag.FechaDesbloqueo = usuario.FechaDesbloqueo;

                // Obtener roles del usuario
                var roles = db.UsuariosRoles
                    .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                    .Select(ur => ur.Roles.NombreRol)
                    .ToList();

                ViewBag.Roles = roles;
                ViewBag.FechaRegistroFormateada = usuario.FechaRegistro?.ToString("dd/MM/yyyy HH:mm");
                ViewBag.UltimoAccesoFormateado = usuario.UltimoAcceso?.ToString("dd/MM/yyyy HH:mm");

                // Convertir imagen a Base64 para mostrar
                if (usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0)
                {
                    ViewBag.ImagenBase64 = Convert.ToBase64String(usuario.ImagenPerfil);
                }
                else
                {
                    ViewBag.ImagenBase64 = null;
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MiPerfil: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el perfil.";
                AuditoriaHelper.RegistrarError("AccountController.MiPerfil", "Error al cargar perfil de usuario", ex);
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/EditProfile
        [Authorize]
        public ActionResult EditProfile()
        {
            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name &&
                    (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                    return RedirectToAction("Logout");
                }

                // Crear ViewModel
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

                // Convertir imagen a base64
                if (usuario.ImagenPerfil != null && usuario.ImagenPerfil.Length > 0)
                {
                    model.ImagenPerfilBase64 = Convert.ToBase64String(usuario.ImagenPerfil);
                    ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EditProfile GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el formulario de edición.";
                AuditoriaHelper.RegistrarError("AccountController.EditProfile.GET", "Error al cargar formulario de edición", ex);
                return RedirectToAction("MiPerfil");
            }
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

            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email == User.Identity.Name &&
                    (u.Activo == null || u.Activo == true));

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado o inactivo.";
                    return RedirectToAction("Logout");
                }

                // Actualizar solo propiedades editables
                usuario.NombreCompleto = model.NombreCompleto?.Trim();
                usuario.Telefono = model.Telefono?.Trim();
                usuario.Direccion = model.Direccion?.Trim();
                usuario.Ciudad = model.Ciudad?.Trim();
                usuario.Provincia = model.Provincia?.Trim();

                // Actualizar imagen si se proporciona una nueva
                if (model.NuevaImagenPerfil != null && model.NuevaImagenPerfil.ContentLength > 0)
                {
                    // Validar tipo de archivo
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    string fileExtension = System.IO.Path.GetExtension(model.NuevaImagenPerfil.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("NuevaImagenPerfil", "Formato de imagen no válido. Formatos permitidos: JPG, PNG, GIF.");
                        model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                            ? Convert.ToBase64String(usuario.ImagenPerfil)
                            : null;
                        ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                        return View(model);
                    }

                    // Validar tamaño de archivo (máximo 5 MB)
                    int maxFileSize = 5 * 1024 * 1024; // 5 MB en bytes
                    if (model.NuevaImagenPerfil.ContentLength > maxFileSize)
                    {
                        ModelState.AddModelError("NuevaImagenPerfil", "La imagen es demasiado grande. Tamaño máximo: 5 MB.");
                        model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                            ? Convert.ToBase64String(usuario.ImagenPerfil)
                            : null;
                        ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                        return View(model);
                    }

                    try
                    {
                        // Validar que sea una imagen real
                        using (var image = System.Drawing.Image.FromStream(model.NuevaImagenPerfil.InputStream))
                        {
                            // Convertir y redimensionar
                            byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(model.NuevaImagenPerfil);
                            imagenBytes = ImageHelper.ResizeImage(imagenBytes, 400, 400);
                            usuario.ImagenPerfil = imagenBytes;
                        }
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("NuevaImagenPerfil", "El archivo no es una imagen válida.");
                        model.ImagenPerfilBase64 = usuario.ImagenPerfil != null
                            ? Convert.ToBase64String(usuario.ImagenPerfil)
                            : null;
                        ViewBag.ImagenBase64 = model.ImagenPerfilBase64;
                        return View(model);
                    }
                }

                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Actualizar Perfil", "Account",
                    $"Usuario {usuario.Email} actualizó su perfil", usuario.UsuarioId);

                TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
                return RedirectToAction("MiPerfil");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EditProfile POST: {ex.Message}");
                ModelState.AddModelError("", "Error al actualizar el perfil. Por favor intente nuevamente.");

                AuditoriaHelper.RegistrarError("AccountController.EditProfile.POST",
                    $"Error al actualizar perfil para usuario: {User.Identity.Name}", ex);
                return View(model);
            }
        }

        #endregion

        #region CAMBIO DE CONTRASEÑA

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

            try
            {
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

                    // ⚡ NUEVO: Registrar intento fallido de cambio de contraseña
                    AuditoriaHelper.RegistrarAccion("Cambio Contraseña Fallido", "Account",
                        $"Intento fallido de cambio de contraseña para {usuario.Email}", usuario.UsuarioId);
                    return View(model);
                }

                // Validar nueva contraseña
                int longitudMinima = int.Parse(ConfigurationManager.AppSettings["LongitudMinimaPassword"] ?? "8");
                if (!PasswordHelper.IsPasswordValid(model.NewPassword, longitudMinima))
                {
                    ModelState.AddModelError("NewPassword", PasswordHelper.GetPasswordValidationMessage());
                    return View(model);
                }

                // ⚡ NUEVO: Verificar que la nueva contraseña sea diferente
                if (PasswordHelper.VerifyPassword(model.NewPassword, usuario.PasswordHash, usuario.Salt))
                {
                    ModelState.AddModelError("NewPassword",
                        "La nueva contraseña debe ser diferente a la actual.");
                    return View(model);
                }

                // Generar nueva salt y hash
                string newSalt = PasswordHelper.GenerateSalt();
                string newPasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

                // Actualizar contraseña
                usuario.PasswordHash = newPasswordHash;
                usuario.Salt = newSalt;
                usuario.FechaUltimoCambioPassword = DateTime.Now;
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Cambiar Contraseña", "Account",
                    $"Usuario {usuario.Email} cambió su contraseña exitosamente", usuario.UsuarioId);

                TempData["SuccessMessage"] = "Contraseña cambiada exitosamente.";
                return RedirectToAction("MiPerfil");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ChangePassword: {ex.Message}");
                ModelState.AddModelError("", "Error al cambiar la contraseña. Por favor intente nuevamente.");
                AuditoriaHelper.RegistrarError("AccountController.ChangePassword",
                    $"Error al cambiar contraseña para usuario: {User.Identity.Name}", ex);
                return View(model);
            }
        }

        #endregion

        #region RECUPERACIÓN DE CONTRASEÑA

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
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model) // CAMBIADO a async Task<ActionResult>
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email.ToLower() == model.Email.ToLower() &&
                    (u.Activo == null || u.Activo == true));

                if (usuario != null)
                {
                    // ⚡ NUEVO: Verificar si el usuario está bloqueado
                    if (usuario.Activo == false)
                    {
                        TempData["WarningMessage"] = "Su cuenta está temporalmente bloqueada. Contacte al administrador para recuperar el acceso.";
                        return RedirectToAction("Login");
                    }

                    // Generar token de recuperación único
                    string resetToken = Guid.NewGuid().ToString("N");

                    // Guardar token en Session con expiración de 1 hora
                    Session["ResetToken_" + usuario.UsuarioId] = resetToken;
                    Session["ResetTokenExpiry_" + usuario.UsuarioId] = DateTime.Now.AddHours(1);

                    // Auditoría
                    AuditoriaHelper.RegistrarAccion("Solicitud Recuperación", "Account",
                        $"Usuario {usuario.Email} solicitó recuperación de contraseña", usuario.UsuarioId);

                    // Generar enlace de recuperación
                    string resetLink = Url.Action("ResetPassword", "Account",
                        new { token = resetToken, userId = usuario.UsuarioId },
                        Request.Url.Scheme);

                    // Enviar email con enlace
                    bool emailHabilitado = bool.Parse(ConfigurationManager.AppSettings["EmailHabilitado"] ?? "false");
                    if (emailHabilitado)
                    {
                        // CORREGIDO: Usar await en lugar de _
                        bool emailEnviado = await EmailHelper.SendPasswordResetAsync(usuario.Email, usuario.NombreCompleto, resetLink);

                        if (emailEnviado)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SUCCESS] Email de recuperación enviado a: {usuario.Email}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Falló el envío de email de recuperación a: {usuario.Email}");
                            TempData["WarningMessage"] = "No se pudo enviar el email de recuperación. Contacte al administrador.";
                        }
                    }
                    else
                    {
                        // Para desarrollo: mostrar el enlace en consola
                        System.Diagnostics.Debug.WriteLine($"ENLACE DE RECUPERACIÓN: {resetLink}");
                        TempData["InfoMessage"] = $"Enlace de recuperación (DESARROLLO): {resetLink}";
                    }
                }

                // Mostrar mismo mensaje aunque el email no exista (seguridad)
                TempData["InfoMessage"] = "Si el email existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ForgotPassword: {ex.Message}");
                ModelState.AddModelError("", "Error al procesar la solicitud. Intente nuevamente.");
                AuditoriaHelper.RegistrarError("AccountController.ForgotPassword",
                    $"Error en solicitud de recuperación para email: {model.Email}", ex);
                return View(model);
            }
        }

        // GET: Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string token, int userId)
        {
            try
            {
                // Validar parámetros
                if (string.IsNullOrEmpty(token) || userId <= 0)
                {
                    ViewBag.Error = "El enlace de restablecimiento es inválido o está incompleto.";
                    return View();
                }

                // Verificar token válido
                var storedToken = Session["ResetToken_" + userId] as string;
                var expiry = Session["ResetTokenExpiry_" + userId] as DateTime?;

                if (string.IsNullOrEmpty(storedToken) || token != storedToken ||
                    expiry == null || DateTime.Now > expiry.Value)
                {
                    ViewBag.Error = "El enlace de restablecimiento es inválido o ha expirado.";
                    return View();
                }

                // Verificar que el usuario existe y está activo
                var usuario = db.Usuarios.Find(userId);
                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View();
                }

                if (usuario.Activo == false)
                {
                    ViewBag.Error = "Su cuenta está bloqueada. Contacte al administrador.";
                    return View();
                }

                // Crear modelo con datos del enlace
                var model = new ResetPasswordViewModel
                {
                    Token = token,
                    UserId = userId
                };

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ResetPassword GET: {ex.Message}");
                ViewBag.Error = "Ocurrió un error al cargar el formulario. Por favor, intenta nuevamente.";
                AuditoriaHelper.RegistrarError("AccountController.ResetPassword.GET",
                    $"Error al cargar formulario de restablecimiento", ex);
                return View();
            }
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

            try
            {
                // Verificar token nuevamente
                var storedToken = Session["ResetToken_" + model.UserId] as string;
                var expiry = Session["ResetTokenExpiry_" + model.UserId] as DateTime?;

                if (string.IsNullOrEmpty(model.Token) || model.Token != storedToken ||
                    expiry == null || DateTime.Now > expiry.Value)
                {
                    ViewBag.Error = "El enlace de restablecimiento es inválido o ha expirado.";
                    return View(model);
                }

                var usuario = db.Usuarios.Find(model.UserId);
                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View(model);
                }

                if (usuario.Activo == false)
                {
                    ViewBag.Error = "Su cuenta está bloqueada. Contacte al administrador.";
                    return View(model);
                }

                // Validar nueva contraseña
                int longitudMinima = int.Parse(ConfigurationManager.AppSettings["LongitudMinimaPassword"] ?? "8");
                if (!PasswordHelper.IsPasswordValid(model.NewPassword, longitudMinima))
                {
                    ModelState.AddModelError("NewPassword", PasswordHelper.GetPasswordValidationMessage());
                    return View(model);
                }

                // Verificar que la nueva contraseña sea diferente
                if (PasswordHelper.VerifyPassword(model.NewPassword, usuario.PasswordHash, usuario.Salt))
                {
                    ModelState.AddModelError("NewPassword",
                        "La nueva contraseña debe ser diferente a la actual.");
                    return View(model);
                }

                // Generar nueva salt y hash
                string newSalt = PasswordHelper.GenerateSalt();
                string newPasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);

                // Actualizar contraseña y reiniciar contadores de seguridad
                usuario.PasswordHash = newPasswordHash;
                usuario.Salt = newSalt;
                usuario.FechaUltimoCambioPassword = DateTime.Now;
                // ⚡ NUEVO: Reiniciar intentos fallidos al restablecer contraseña
                usuario.IntentosFallidos = 0;
                usuario.FechaUltimoIntentoFallido = null;
                db.SaveChanges();

                // Limpiar tokens de sesión
                Session.Remove("ResetToken_" + model.UserId);
                Session.Remove("ResetTokenExpiry_" + model.UserId);

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Restablecer Contraseña", "Account",
                    $"Usuario {usuario.Email} restableció su contraseña exitosamente", usuario.UsuarioId);

                // Mostrar mensaje de éxito
                ViewBag.Success = true;
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ResetPassword POST: {ex.Message}");
                ViewBag.Error = "Error al restablecer la contraseña. Por favor intente nuevamente.";
                AuditoriaHelper.RegistrarError("AccountController.ResetPassword.POST",
                    $"Error al restablecer contraseña para usuario ID: {model.UserId}", ex);
                return View(model);
            }
        }

        #endregion

        #region CONFIRMACIÓN DE EMAIL (OPCIONAL)

        // GET: Account/ConfirmEmail
        [AllowAnonymous]
        public ActionResult ConfirmEmail(int userId, string code)
        {
            try
            {
                // Validar parámetros
                if (userId <= 0 || string.IsNullOrEmpty(code))
                {
                    ViewBag.Error = "El enlace de confirmación es inválido o está incompleto.";
                    return View();
                }

                var usuario = db.Usuarios.Find(userId);

                if (usuario == null)
                {
                    ViewBag.Error = "Usuario no encontrado.";
                    return View();
                }

                // Si ya está confirmado
                if (usuario.EmailConfirmado == true)
                {
                    ViewBag.Success = true;
                    ViewBag.Message = "Tu email ya estaba confirmado previamente. Puedes iniciar sesión.";
                    return View();
                }

                // Verificar token
                var storedToken = Session["EmailConfirmToken_" + userId] as string;
                var expiry = Session["EmailConfirmTokenExpiry_" + userId] as DateTime?;

                if (string.IsNullOrEmpty(storedToken) || code != storedToken ||
                    expiry == null || DateTime.Now > expiry.Value)
                {
                    ViewBag.Error = "El enlace de confirmación es inválido o ha expirado.";
                    return View();
                }

                usuario.EmailConfirmado = true;
                db.SaveChanges();

                // Limpiar tokens de sesión
                Session.Remove("EmailConfirmToken_" + userId);
                Session.Remove("EmailConfirmTokenExpiry_" + userId);

                ViewBag.Success = true;
                ViewBag.Message = "¡Email confirmado exitosamente! Ya puedes iniciar sesión.";

                AuditoriaHelper.RegistrarAccion("Confirmar Email", "Account",
                    $"Usuario {usuario.Email} confirmó su email",
                    userId);

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ConfirmEmail: {ex.Message}");
                ViewBag.Error = "Ocurrió un error al confirmar el email. Por favor, intenta nuevamente.";
                AuditoriaHelper.RegistrarError("AccountController.ConfirmEmail",
                    $"Error al confirmar email para usuario ID: {userId}", ex);
                return View();
            }
        }

        // POST: Account/ResendConfirmationEmail
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResendConfirmationEmail(string email) // CAMBIADO a async Task<ActionResult>
        {
            try
            {
                var usuario = db.Usuarios.FirstOrDefault(u =>
                    u.Email.ToLower() == email.ToLower() &&
                    u.EmailConfirmado == false);

                if (usuario != null)
                {
                    // Generar nuevo token
                    string token = Guid.NewGuid().ToString("N");

                    // Guardar en Session o base de datos
                    Session["EmailConfirmToken_" + usuario.UsuarioId] = token;
                    Session["EmailConfirmTokenExpiry_" + usuario.UsuarioId] = DateTime.Now.AddHours(24);

                    // Enviar email
                    bool emailHabilitado = bool.Parse(ConfigurationManager.AppSettings["EmailHabilitado"] ?? "false");
                    if (emailHabilitado)
                    {
                        string confirmLink = Url.Action("ConfirmEmail", "Account",
                            new { code = token, userId = usuario.UsuarioId },
                            Request.Url.Scheme);

                        // CORREGIDO: Usar await
                        bool emailEnviado = await EmailHelper.SendEmailConfirmationAsync(usuario.Email, usuario.NombreCompleto, confirmLink);

                        if (emailEnviado)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SUCCESS] Email de confirmación reenviado a: {usuario.Email}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Falló el reenvío de email de confirmación a: {usuario.Email}");
                        }
                    }

                    AuditoriaHelper.RegistrarAccion("Reenviar Confirmación Email", "Account",
                        $"Usuario {usuario.Email} solicitó reenvío de confirmación", usuario.UsuarioId);
                }

                TempData["InfoMessage"] = "Si el email existe y no está confirmado, recibirás un nuevo enlace de confirmación.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ResendConfirmationEmail: {ex.Message}");
                TempData["ErrorMessage"] = "Error al reenviar confirmación.";
                AuditoriaHelper.RegistrarError("AccountController.ResendConfirmationEmail",
                    $"Error al reenviar confirmación para email: {email}", ex);
                return RedirectToAction("Login");
            }
        }

        #endregion

        #region MÉTODO DE PRUEBA PARA DEBUG

        // GET: Account/TestEmail
        [AllowAnonymous]
        public async Task<ActionResult> TestEmail()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== INICIANDO PRUEBA DE EMAIL ===");

                bool result = await EmailHelper.TestEmailConfigurationAsync();

                if (result)
                {
                    return Content($"<h2>✅ PRUEBA DE EMAIL EXITOSA</h2>" +
                                   $"<p>El email de prueba fue enviado correctamente.</p>" +
                                   $"<p>Revisa la ventana <strong>Output</strong> en Visual Studio para ver los detalles.</p>" +
                                   $"<p>Si no recibes el email, verifica:</p>" +
                                   $"<ul>" +
                                   $"<li>Que <strong>EmailHabilitado = true</strong> en Web.config</li>" +
                                   $"<li>Que las credenciales de Gmail sean correctas</li>" +
                                   $"<li>Que hayas generado una 'Contraseña de aplicación' en Google</li>" +
                                   $"<li>Revisa la carpeta de spam</li>" +
                                   $"</ul>" +
                                   $"<a href='/Account/Login'>Volver al Login</a>");
                }
                else
                {
                    return Content($"<h2>❌ PRUEBA DE EMAIL FALLIDA</h2>" +
                                   $"<p>No se pudo enviar el email de prueba.</p>" +
                                   $"<p>Revisa la ventana <strong>Output</strong> en Visual Studio para ver el error específico.</p>" +
                                   $"<p>Errores comunes:</p>" +
                                   $"<ul>" +
                                   $"<li><strong>EmailHabilitado = false</strong> en Web.config</li>" +
                                   $"<li>Credenciales incorrectas en SMTPPassword</li>" +
                                   $"<li>Falta 'Contraseña de aplicación' de Google</li>" +
                                   $"<li>Verificación en 2 pasos no activada en Google</li>" +
                                   $"</ul>" +
                                   $"<a href='/Account/Login'>Volver al Login</a>");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] TestEmail Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");

                return Content($"<h2>❌ ERROR EN PRUEBA DE EMAIL</h2>" +
                               $"<p><strong>Error:</strong> {ex.Message}</p>" +
                               $"<p><strong>StackTrace:</strong> {ex.StackTrace}</p>" +
                               $"<p>Revisa la ventana <strong>Output</strong> en Visual Studio para más detalles.</p>" +
                               $"<a href='/Account/Login'>Volver al Login</a>");
            }
        }

        #endregion

        #region MÉTODOS DE ADMINISTRACIÓN DE BLOQUEO (SOLO PARA ADMIN)

        /// <summary>
        /// ⚡ NUEVO: Desbloquear usuario manualmente (solo administradores)
        /// </summary>
        [AuthorizeRoles("Administrador")]
        public ActionResult DesbloquearUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Usuarios", "Admin");
                }

                // Desbloquear usuario
                usuario.Activo = true;
                usuario.IntentosFallidos = 0;
                usuario.FechaBloqueoTemporal = null;
                usuario.FechaDesbloqueo = null;
                usuario.FechaUltimoIntentoFallido = null;
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Desbloquear Usuario", "Account",
                    $"Administrador {User.Identity.Name} desbloqueó al usuario {usuario.Email}",
                    GetCurrentUserId());

                TempData["SuccessMessage"] = $"Usuario {usuario.Email} desbloqueado exitosamente.";
                return RedirectToAction("Usuarios", "Admin");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DesbloquearUsuario: {ex.Message}");
                TempData["ErrorMessage"] = "Error al desbloquear usuario.";
                AuditoriaHelper.RegistrarError("AccountController.DesbloquearUsuario",
                    $"Error al desbloquear usuario ID: {id}", ex);
                return RedirectToAction("Usuarios", "Admin");
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Bloquear usuario permanentemente (solo administradores)
        /// </summary>
        [AuthorizeRoles("Administrador")]
        public ActionResult BloquearPermanentemente(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Usuarios", "Admin");
                }

                // Bloquear permanentemente
                usuario.BloqueadoPermanentemente = true;
                usuario.Activo = false;
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Bloquear Usuario Permanentemente", "Account",
                    $"Administrador {User.Identity.Name} bloqueó permanentemente al usuario {usuario.Email}",
                    GetCurrentUserId());

                TempData["SuccessMessage"] = $"Usuario {usuario.Email} bloqueado permanentemente.";
                return RedirectToAction("Usuarios", "Admin");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en BloquearPermanentemente: {ex.Message}");
                TempData["ErrorMessage"] = "Error al bloquear usuario.";
                AuditoriaHelper.RegistrarError("AccountController.BloquearPermanentemente",
                    $"Error al bloquear permanentemente usuario ID: {id}", ex);
                return RedirectToAction("Usuarios", "Admin");
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Quitar bloqueo permanente (solo administradores)
        /// </summary>
        [AuthorizeRoles("Administrador")]
        public ActionResult QuitarBloqueoPermanente(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("Usuarios", "Admin");
                }

                // Quitar bloqueo permanente
                usuario.BloqueadoPermanentemente = false;
                usuario.Activo = true;
                usuario.IntentosFallidos = 0;
                usuario.FechaBloqueoTemporal = null;
                usuario.FechaDesbloqueo = null;
                usuario.FechaUltimoIntentoFallido = null;
                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Quitar Bloqueo Permanente", "Account",
                    $"Administrador {User.Identity.Name} quitó bloqueo permanente al usuario {usuario.Email}",
                    GetCurrentUserId());

                TempData["SuccessMessage"] = $"Bloqueo permanente removido para {usuario.Email}.";
                return RedirectToAction("Usuarios", "Admin");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en QuitarBloqueoPermanente: {ex.Message}");
                TempData["ErrorMessage"] = "Error al quitar bloqueo permanente.";
                AuditoriaHelper.RegistrarError("AccountController.QuitarBloqueoPermanente",
                    $"Error al quitar bloqueo permanente usuario ID: {id}", ex);
                return RedirectToAction("Usuarios", "Admin");
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Obtener ID del usuario actual
        /// </summary>
        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            return usuario?.UsuarioId;
        }

        #endregion

        #region DISPOSE

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}


//holaa holaaaaa