using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace MVCMASCOTAS.Helpers
{
    public static class EmailHelper
    {
        private static string SmtpServer = WebConfigurationManager.AppSettings["SmtpServer"];
        private static int SmtpPort = int.Parse(WebConfigurationManager.AppSettings["SmtpPort"] ?? "587");
        private static string SmtpUsername = WebConfigurationManager.AppSettings["SmtpUsername"];
        private static string SmtpPassword = WebConfigurationManager.AppSettings["SmtpPassword"];
        private static bool EnableSsl = bool.Parse(WebConfigurationManager.AppSettings["EnableSsl"] ?? "true");
        private static string FromEmail = WebConfigurationManager.AppSettings["FromEmail"] ?? "noreply@refugiomascotas.com";

        /// <summary>
        /// Envía email de confirmación de registro
        /// </summary>
        public static async Task<bool> SendRegistrationConfirmationAsync(string toEmail, string nombreUsuario)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = "¡Bienvenido al Refugio de Mascotas!",
                        Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>¡Bienvenido {nombreUsuario}!</h2>
    <p>Tu registro en el Refugio de Mascotas ha sido exitoso.</p>
    <p>Ahora puedes acceder a todas las funcionalidades de nuestro sistema.</p>
    <br/>
    <p>Gracias por unirte a nuestra comunidad.</p>
    <p>El equipo del Refugio de Mascotas</p>
</body>
</html>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email para recuperación de contraseña
        /// </summary>
        public static async Task<bool> SendPasswordResetAsync(string toEmail, string nombreUsuario, string resetToken)
        {
            try
            {
                // En una aplicación real, aquí construirías la URL completa
                string resetLink = $"{WebConfigurationManager.AppSettings["AppUrl"]}/Account/ResetPassword?token={resetToken}";

                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = "Recuperación de Contraseña - Refugio de Mascotas",
                        Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombreUsuario},</h2>
    <p>Hemos recibido una solicitud para restablecer tu contraseña.</p>
    <p>Para crear una nueva contraseña, haz clic en el siguiente enlace:</p>
    <p><a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Restablecer Contraseña</a></p>
    <br/>
    <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
    <p>Este enlace expirará en 24 horas.</p>
    <br/>
    <p>El equipo del Refugio de Mascotas</p>
</body>
</html>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email de recuperación: {ex.Message}");
                return false;
            }
        }
        // Agrega estos métodos a tu clase EmailHelper (en Helpers/EmailHelper.cs)

        /// <summary>
        /// Envía email de solicitud de adopción recibida
        /// </summary>
        public static async Task<bool> SendAdoptionRequestReceivedAsync(string toEmail, string nombreUsuario, string nombreMascota)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = $"Solicitud de Adopción Recibida - {nombreMascota}",
                        Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>¡Hola {nombreUsuario}!</h2>
    <p>Hemos recibido tu solicitud de adopción para <strong>{nombreMascota}</strong>.</p>
    <p>Nuestro equipo está revisando tu solicitud y te contactaremos pronto con los siguientes pasos.</p>
    <br/>
    <h3>Proceso de Adopción:</h3>
    <ol>
        <li>Evaluación inicial de tu solicitud (2-3 días)</li>
        <li>Entrevista telefónica o virtual</li>
        <li>Evaluación del hogar (si aplica)</li>
        <li>Firma de contrato de adopción</li>
        <li>Entrega de tu nueva mascota 🐾</li>
    </ol>
    <br/>
    <p>Gracias por querer darle un hogar a un animal necesitado.</p>
    <p>El equipo del Refugio de Mascotas</p>
</body>
</html>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email de adopción recibida: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email de adopción aprobada
        /// </summary>
        public static async Task<bool> SendAdoptionApprovedAsync(string toEmail, string nombreUsuario, string nombreMascota)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = $"¡Felicidades! Adopción Aprobada - {nombreMascota}",
                        Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #4CAF50;'>¡Felicidades {nombreUsuario}! 🎉</h2>
    <p>Tenemos excelentes noticias: ¡Tu solicitud de adopción para <strong>{nombreMascota}</strong> ha sido <strong>APROBADA</strong>!</p>
    <br/>
    <h3>Próximos pasos:</h3>
    <ol>
        <li>Nos contactaremos contigo en las próximas 48 horas para coordinar la entrega</li>
        <li>Prepara los documentos necesarios (copia de cédula y recibo de servicios)</li>
        <li>Firma del contrato de adopción</li>
        <li>¡Recibe a tu nueva mascota en casa!</li>
    </ol>
    <br/>
    <p><strong>Importante:</strong> Recuerda que debes esterilizar a {nombreMascota} dentro de los 3 meses siguientes a la adopción.</p>
    <br/>
    <p>¡Estamos muy felices de que {nombreMascota} tenga un nuevo hogar!</p>
    <p>El equipo del Refugio de Mascotas</p>
</body>
</html>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email de adopción aprobada: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email de adopción rechazada
        /// </summary>
        public static async Task<bool> SendAdoptionRejectedAsync(string toEmail, string nombreUsuario, string nombreMascota, string motivo)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = $"Actualización sobre tu solicitud de adopción - {nombreMascota}",
                        Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Hola {nombreUsuario},</h2>
    <p>Lamentamos informarte que tu solicitud de adopción para <strong>{nombreMascota}</strong> no ha sido aprobada en esta ocasión.</p>
    <p><strong>Motivo:</strong> {motivo}</p>
    <br/>
    <p>Esto no significa que no puedas adoptar con nosotros. Te invitamos a:</p>
    <ul>
        <li>Considerar otras mascotas disponibles</li>
        <li>Mejorar las condiciones señaladas</li>
        <li>Volver a aplicar en el futuro</li>
    </ul>
    <br/>
    <p>Agradecemos tu interés en adoptar y esperamos poder ayudarte a encontrar la mascota perfecta para ti.</p>
    <p>El equipo del Refugio de Mascotas</p>
</body>
</html>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email de adopción rechazada: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email genérico (método general para reemplazar SendEmailAsync)
        /// </summary>
        public static async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando email: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Envía email de notificación general
        /// </summary>
        public static async Task<bool> SendNotificationAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var smtpClient = new SmtpClient(SmtpServer, SmtpPort))
                {
                    smtpClient.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword);
                    smtpClient.EnableSsl = EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(FromEmail, "Refugio de Mascotas"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando notificación: {ex.Message}");
                return false;
            }
        }
    }
}