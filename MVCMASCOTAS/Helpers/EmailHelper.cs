using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Configuration;
using System.Text;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para envío de emails del sistema
    /// Soporta: Registro, Recuperación de contraseña, Notificaciones de adopción, Donaciones
    /// </summary>
    public static class EmailHelper
    {
        #region CONFIGURACIÓN

        // ⚡ OPTIMIZADO: Lazy loading de configuraciones
        private static readonly Lazy<EmailConfig> _config = new Lazy<EmailConfig>(() => new EmailConfig());

        private class EmailConfig
        {
            public string SmtpServer { get; }
            public int SmtpPort { get; }
            public string SmtpUsername { get; }
            public string SmtpPassword { get; }
            public bool EnableSsl { get; }
            public string FromEmail { get; }
            public string FromName { get; }
            public bool EmailEnabled { get; }
            public int TimeoutSeconds { get; }

            public EmailConfig()
            {
                SmtpServer = ConfigurationManager.AppSettings["SMTPHost"] ?? "smtp.gmail.com";
                SmtpPort = int.Parse(ConfigurationManager.AppSettings["SMTPPort"] ?? "587");
                SmtpUsername = ConfigurationManager.AppSettings["SMTPUsuario"] ?? "";
                SmtpPassword = ConfigurationManager.AppSettings["SMTPPassword"] ?? "";
                EnableSsl = bool.Parse(ConfigurationManager.AppSettings["SMTPEnableSSL"] ?? "true");
                FromEmail = ConfigurationManager.AppSettings["EmailRefugio"] ?? "noreply@refugiomascotas.com";
                FromName = ConfigurationManager.AppSettings["NombreRefugio"] ?? "Refugio de Mascotas";
                EmailEnabled = bool.Parse(ConfigurationManager.AppSettings["EmailHabilitado"] ?? "false");
                TimeoutSeconds = int.Parse(ConfigurationManager.AppSettings["EmailTimeoutSeconds"] ?? "30");

                // LOG DE CONFIGURACIÓN PARA DEBUG
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] Cargando configuración:");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] EmailEnabled: {EmailEnabled}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] SmtpServer: {SmtpServer}:{SmtpPort}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] SmtpUsername: {SmtpUsername}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] SmtpPassword: {(string.IsNullOrEmpty(SmtpPassword) ? "NO CONFIGURADA" : "CONFIGURADA")}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] EnableSsl: {EnableSsl}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] FromEmail: {FromEmail}");
                System.Diagnostics.Debug.WriteLine($"[EmailConfig] FromName: {FromName}");
            }
        }

        #endregion

        #region MÉTODOS DE ENVÍO PRINCIPALES

        /// <summary>
        /// ⚡ OPTIMIZADO: Envía un email genérico con reintentos
        /// </summary>
        private static async Task<bool> SendEmailAsync(string toEmail, string subject, string body, int maxRetries = 2)
        {
            // LOG DE INICIO
            System.Diagnostics.Debug.WriteLine($"[EmailHelper.SendEmailAsync] Iniciando envío a: {toEmail}");
            System.Diagnostics.Debug.WriteLine($"[EmailHelper.SendEmailAsync] Subject: {subject}");

            // Verificar si los emails están habilitados
            if (!_config.Value.EmailEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR: Emails deshabilitados en configuración (EmailHabilitado = false)");
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Para habilitar, cambia EmailHabilitado a 'true' en Web.config");
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Email no enviado a: {toEmail}");
                return true; // Retornar true para no romper el flujo de la aplicación
            }

            // Validar email de destino
            if (string.IsNullOrWhiteSpace(toEmail) || !IsValidEmail(toEmail))
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR: Email inválido: {toEmail}");
                return false;
            }

            // Validar configuración SMTP
            if (string.IsNullOrWhiteSpace(_config.Value.SmtpServer))
            {
                System.Diagnostics.Debug.WriteLine("[EmailHelper] ERROR: SMTPHost no configurado en Web.config");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.Value.SmtpUsername))
            {
                System.Diagnostics.Debug.WriteLine("[EmailHelper] ERROR: SMTPUsuario no configurado en Web.config");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.Value.SmtpPassword))
            {
                System.Diagnostics.Debug.WriteLine("[EmailHelper] ERROR: SMTPPassword no configurado en Web.config");
                System.Diagnostics.Debug.WriteLine("[EmailHelper] INFO: Usa una 'Contraseña de aplicación' de Google, no tu contraseña normal");
                return false;
            }

            int attempt = 0;
            Exception lastException = null;

            while (attempt < maxRetries)
            {
                attempt++;
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Intento {attempt}/{maxRetries} para: {toEmail}");

                try
                {
                    using (var smtpClient = CreateSmtpClient())
                    using (var mailMessage = CreateMailMessage(toEmail, subject, body))
                    {
                        System.Diagnostics.Debug.WriteLine($"[EmailHelper] Conectando a {_config.Value.SmtpServer}:{_config.Value.SmtpPort}...");
                        await smtpClient.SendMailAsync(mailMessage);

                        System.Diagnostics.Debug.WriteLine($"[EmailHelper] SUCCESS: Email enviado exitosamente a: {toEmail}");
                        return true;
                    }
                }
                catch (SmtpException smtpEx)
                {
                    lastException = smtpEx;
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR SMTP (Intento {attempt}/{maxRetries}):");
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] Mensaje: {smtpEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] StatusCode: {smtpEx.StatusCode}");

                    if (smtpEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EmailHelper] InnerException: {smtpEx.InnerException.Message}");
                    }

                    // Esperar antes de reintentar
                    if (attempt < maxRetries)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EmailHelper] Esperando {2000 * attempt}ms antes de reintentar...");
                        await Task.Delay(2000 * attempt); // 2s, 4s, etc.
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR general enviando email:");
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] Tipo: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] Mensaje: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[EmailHelper] StackTrace: {ex.StackTrace}");
                    break; // No reintentar en errores generales
                }
            }

            // Log del error final
            if (lastException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR FINAL: Fallo al enviar email después de {attempt} intentos");
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Último error: {lastException.Message}");

                // Registrar en auditoría
                try
                {
                    AuditoriaHelper.RegistrarError("EmailHelper",
                        $"Error al enviar email a {toEmail}",
                        lastException);
                }
                catch { /* Ignorar errores de auditoría */ }
            }

            return false;
        }

        #endregion

        #region EMAILS DE AUTENTICACIÓN

        /// <summary>
        /// Envía email de confirmación de registro
        /// </summary>
        public static async Task<bool> SendRegistrationConfirmationAsync(string toEmail, string nombreUsuario)
        {
            System.Diagnostics.Debug.WriteLine($"[EmailHelper] Llamando SendRegistrationConfirmationAsync para: {toEmail}");

            try
            {
                string subject = "¡Bienvenido al Refugio de Mascotas!";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .button {{ background-color: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐾 ¡Bienvenido al Refugio de Mascotas!</h1>
        </div>
        <div class='content'>
            <h2>¡Hola {nombreUsuario}!</h2>
            <p>Tu registro en nuestro sistema ha sido <strong>exitoso</strong>.</p>
            <p>Ahora puedes acceder a todas nuestras funcionalidades:</p>
            <ul>
                <li>✅ Solicitar adopciones</li>
                <li>✅ Realizar donaciones</li>
                <li>✅ Apadrinar mascotas</li>
                <li>✅ Participar como voluntario</li>
                <li>✅ Reportar mascotas perdidas o encontradas</li>
            </ul>
            <p>Gracias por unirte a nuestra comunidad y ayudarnos a dar un hogar a los animales que lo necesitan.</p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR en SendRegistrationConfirmationAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email para recuperación de contraseña
        /// </summary>
        public static async Task<bool> SendPasswordResetAsync(string toEmail, string nombreUsuario, string resetLink)
        {
            System.Diagnostics.Debug.WriteLine($"[EmailHelper] Llamando SendPasswordResetAsync para: {toEmail}");
            System.Diagnostics.Debug.WriteLine($"[EmailHelper] Reset Link: {resetLink}");

            try
            {
                string subject = "Recuperación de Contraseña - Refugio de Mascotas";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .button {{ background-color: #2196F3; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Recuperación de Contraseña</h1>
        </div>
        <div class='content'>
            <h2>Hola {nombreUsuario},</h2>
            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
            <p>Para crear una nueva contraseña, haz clic en el siguiente botón:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Restablecer Contraseña</a>
            </p>
            <p style='font-size: 12px; color: #666;'>O copia y pega este enlace en tu navegador:<br/>
            <a href='{resetLink}'>{resetLink}</a></p>
            
            <div class='warning'>
                <strong>⚠️ Importante:</strong>
                <ul style='margin: 5px 0;'>
                    <li>Este enlace expirará en <strong>1 hora</strong></li>
                    <li>Si no solicitaste este cambio, ignora este mensaje</li>
                    <li>Nunca compartas este enlace con nadie</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] ERROR en SendPasswordResetAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ⚡ NUEVO: Envía email de confirmación de email
        /// </summary>
        public static async Task<bool> SendEmailConfirmationAsync(string toEmail, string nombreUsuario, string confirmLink)
        {
            try
            {
                string subject = "Confirma tu dirección de email - Refugio de Mascotas";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .button {{ background-color: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✉️ Confirma tu Email</h1>
        </div>
        <div class='content'>
            <h2>Hola {nombreUsuario},</h2>
            <p>Gracias por registrarte en el Refugio de Mascotas.</p>
            <p>Para completar tu registro, necesitamos que confirmes tu dirección de email haciendo clic en el botón:</p>
            <p style='text-align: center;'>
                <a href='{confirmLink}' class='button'>Confirmar Email</a>
            </p>
            <p style='font-size: 12px; color: #666;'>O copia y pega este enlace en tu navegador:<br/>
            <a href='{confirmLink}'>{confirmLink}</a></p>
            <p><strong>Este enlace expirará en 24 horas.</strong></p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendEmailConfirmationAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region EMAILS DE ADOPCIÓN

        /// <summary>
        /// Envía email de solicitud de adopción recibida
        /// </summary>
        public static async Task<bool> SendAdoptionRequestReceivedAsync(string toEmail, string nombreUsuario, string nombreMascota)
        {
            try
            {
                string subject = $"Solicitud de Adopción Recibida - {nombreMascota}";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .timeline {{ background-color: white; padding: 15px; border-left: 4px solid #FF9800; margin: 15px 0; }}
        .timeline li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐾 Solicitud de Adopción Recibida</h1>
        </div>
        <div class='content'>
            <h2>¡Hola {nombreUsuario}!</h2>
            <p>Hemos recibido tu solicitud de adopción para <strong>{nombreMascota}</strong>.</p>
            <p>¡Gracias por querer darle un hogar a un animal necesitado! 💚</p>
            
            <div class='timeline'>
                <h3>📋 Proceso de Adopción:</h3>
                <ol>
                    <li>✅ <strong>Solicitud recibida</strong> (completado)</li>
                    <li>⏳ Evaluación inicial (2-3 días hábiles)</li>
                    <li>📞 Entrevista telefónica o virtual</li>
                    <li>🏠 Evaluación del hogar (si aplica)</li>
                    <li>📝 Firma de contrato de adopción</li>
                    <li>🎉 Entrega de tu nueva mascota</li>
                </ol>
            </div>
            
            <p>Nuestro equipo está revisando tu solicitud y te contactaremos pronto.</p>
            <p><strong>Tiempo estimado de respuesta:</strong> 2-3 días hábiles</p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendAdoptionRequestReceivedAsync: {ex.Message}");
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
                string subject = $"¡Felicidades! Adopción Aprobada - {nombreMascota} 🎉";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .success-box {{ background-color: #d4edda; border: 2px solid #28a745; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .next-steps {{ background-color: white; padding: 15px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎊 ¡FELICIDADES! 🎊</h1>
        </div>
        <div class='content'>
            <div class='success-box'>
                <h2 style='color: #28a745; margin: 0;'>¡Tu solicitud ha sido APROBADA!</h2>
            </div>
            
            <h2>Hola {nombreUsuario},</h2>
            <p>Tenemos excelentes noticias: ¡Tu solicitud de adopción para <strong>{nombreMascota}</strong> ha sido <strong>APROBADA</strong>! 🐾</p>
            <p>Estamos muy felices de que {nombreMascota} tenga un nuevo hogar lleno de amor.</p>
            
            <div class='next-steps'>
                <h3>📋 Próximos pasos:</h3>
                <ol>
                    <li>📞 Nos contactaremos contigo en las próximas <strong>48 horas</strong> para coordinar la entrega</li>
                    <li>📄 Prepara los documentos necesarios:
                        <ul>
                            <li>Copia de cédula</li>
                            <li>Recibo de servicios básicos</li>
                        </ul>
                    </li>
                    <li>📝 Firma del contrato de adopción</li>
                    <li>🏡 ¡Recibe a {nombreMascota} en tu hogar!</li>
                </ol>
            </div>
            
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0;'>
                <strong>⚠️ Importante:</strong>
                <ul style='margin: 5px 0;'>
                    <li>Compromiso de esterilización dentro de los 3 meses (si aplica)</li>
                    <li>Seguimiento post-adopción obligatorio</li>
                    <li>Actualizar cartilla de vacunación</li>
                </ul>
            </div>
            
            <p>¡Gracias por darle una segunda oportunidad a {nombreMascota}!</p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendAdoptionApprovedAsync: {ex.Message}");
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
                string subject = $"Actualización sobre tu solicitud de adopción - {nombreMascota}";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .info-box {{ background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Actualización de Solicitud</h1>
        </div>
        <div class='content'>
            <h2>Hola {nombreUsuario},</h2>
            <p>Lamentamos informarte que tu solicitud de adopción para <strong>{nombreMascota}</strong> no ha sido aprobada en esta ocasión.</p>
            
            <div class='info-box'>
                <strong>📋 Motivo:</strong>
                <p style='margin: 10px 0;'>{motivo}</p>
            </div>
            
            <p>Esto <strong>no significa</strong> que no puedas adoptar con nosotros en el futuro. Te invitamos a:</p>
            <ul>
                <li>🐕 Considerar otras mascotas disponibles que puedan adaptarse mejor a tu situación</li>
                <li>📝 Mejorar las condiciones señaladas en la evaluación</li>
                <li>🔄 Volver a aplicar cuando estés listo</li>
                <li>📞 Contactarnos para más información sobre el proceso</li>
            </ul>
            
            <p>Agradecemos sinceramente tu interés en adoptar y esperamos poder ayudarte a encontrar la mascota perfecta para ti en el futuro.</p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendAdoptionRejectedAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region EMAILS DE DONACIONES

        /// <summary>
        /// ⚡ NUEVO: Envía email de confirmación de donación
        /// </summary>
        public static async Task<bool> SendDonationConfirmationAsync(string toEmail, string nombreUsuario, decimal monto, string tipoDonacion)
        {
            try
            {
                string subject = $"Confirmación de Donación - ${monto:N2}";
                string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
        .donation-box {{ background-color: white; border: 2px solid #9C27B0; padding: 20px; text-align: center; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💝 ¡Gracias por tu Donación!</h1>
        </div>
        <div class='content'>
            <h2>Hola {nombreUsuario},</h2>
            <p>¡Gracias por tu generosa donación! Tu apoyo hace la diferencia en la vida de nuestros animales.</p>
            
            <div class='donation-box'>
                <h3 style='color: #9C27B0; margin-top: 0;'>Detalles de tu Donación</h3>
                <p style='font-size: 24px; margin: 10px 0;'><strong>${monto:N2}</strong></p>
                <p>Tipo: {tipoDonacion}</p>
                <p>Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            </div>
            
            <p>Tu donación nos ayuda a:</p>
            <ul>
                <li>🏥 Brindar atención veterinaria</li>
                <li>🍖 Alimentar a los animales</li>
                <li>🏠 Mantener las instalaciones</li>
                <li>💉 Vacunar y esterilizar</li>
            </ul>
            
            <p><strong>Tu comprobante de donación ha sido generado y lo recibirás en un email por separado.</strong></p>
            <p style='text-align: center; margin-top: 20px;'>
                <em>¡Gracias por ser parte del cambio! 🐾</em>
            </p>
        </div>
        <div class='footer'>
            <p>Este es un email automático, por favor no responder.</p>
            <p>&copy; {DateTime.Now.Year} Refugio de Mascotas - Quito, Ecuador</p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendDonationConfirmationAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region EMAILS GENERALES

        /// <summary>
        /// Envía email de notificación general
        /// </summary>
        public static async Task<bool> SendNotificationAsync(string toEmail, string subject, string body)
        {
            try
            {
                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmailHelper] Error en SendNotificationAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        /// <summary>
        /// ⚡ NUEVO: Crea y configura el cliente SMTP
        /// </summary>
        private static SmtpClient CreateSmtpClient()
        {
            var smtpClient = new SmtpClient(_config.Value.SmtpServer, _config.Value.SmtpPort)
            {
                Credentials = new NetworkCredential(_config.Value.SmtpUsername, _config.Value.SmtpPassword),
                EnableSsl = _config.Value.EnableSsl,
                Timeout = _config.Value.TimeoutSeconds * 1000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            return smtpClient;
        }

        /// <summary>
        /// ⚡ NUEVO: Crea el mensaje de email
        /// </summary>
        private static MailMessage CreateMailMessage(string toEmail, string subject, string body)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config.Value.FromEmail, _config.Value.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                Priority = MailPriority.Normal
            };

            mailMessage.To.Add(toEmail);

            return mailMessage;
        }

        /// <summary>
        /// ⚡ NUEVO: Valida formato de email
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region MÉTODO DE PRUEBA PARA DEBUG

        /// <summary>
        /// Método para probar la configuración de email
        /// </summary>
        public static async Task<bool> TestEmailConfigurationAsync(string toEmail = null)
        {
            System.Diagnostics.Debug.WriteLine("=== PRUEBA DE CONFIGURACIÓN DE EMAIL ===");

            if (string.IsNullOrEmpty(toEmail))
            {
                toEmail = _config.Value.SmtpUsername; // Enviar al mismo email de configuración
            }

            string subject = "Prueba de configuración - Refugio de Mascotas";
            string body = $@"
<!DOCTYPE html>
<html>
<body>
    <h2>✅ Configuración de Email Exitosa</h2>
    <p>Este es un email de prueba enviado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
    <p>Si recibes este email, la configuración SMTP con Gmail está funcionando correctamente.</p>
    <p><strong>Configuración usada:</strong></p>
    <ul>
        <li>Servidor: {_config.Value.SmtpServer}:{_config.Value.SmtpPort}</li>
        <li>Usuario: {_config.Value.SmtpUsername}</li>
        <li>SSL: {_config.Value.EnableSsl}</li>
        <li>Remitente: {_config.Value.FromName} &lt;{_config.Value.FromEmail}&gt;</li>
    </ul>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        #endregion
    }
}