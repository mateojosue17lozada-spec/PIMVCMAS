using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MVCMASCOTAS.Helpers
{
    /// <summary>
    /// Helper para envío de correos electrónicos
    /// </summary>
    public static class EmailHelper
    {
        private static bool EmailEnabled => bool.Parse(ConfigurationManager.AppSettings["EmailHabilitado"] ?? "false");
        private static string SmtpHost => ConfigurationManager.AppSettings["SMTPHost"];
        private static int SmtpPort => int.Parse(ConfigurationManager.AppSettings["SMTPPort"] ?? "587");
        private static string SmtpUser => ConfigurationManager.AppSettings["SMTPUsuario"];
        private static string SmtpPassword => ConfigurationManager.AppSettings["SMTPPassword"];
        private static bool SmtpEnableSSL => bool.Parse(ConfigurationManager.AppSettings["SMTPEnableSSL"] ?? "true");
        private static string EmailRefugio => ConfigurationManager.AppSettings["EmailRefugio"];

        /// <summary>
        /// Envía un email de forma asíncrona
        /// </summary>
        public static async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            if (!EmailEnabled)
            {
                // Email deshabilitado, solo registrar en log
                Console.WriteLine($"[EMAIL SIMULADO] Para: {to}, Asunto: {subject}");
                return true;
            }

            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(SmtpUser, "Refugio de Animales Quito");
                    message.To.Add(new MailAddress(to));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;

                    using (var client = new SmtpClient(SmtpHost, SmtpPort))
                    {
                        client.EnableSsl = SmtpEnableSSL;
                        client.Credentials = new NetworkCredential(SmtpUser, SmtpPassword);

                        await client.SendMailAsync(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Registrar error en log
                Console.WriteLine($"Error al enviar email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email de confirmación de registro
        /// </summary>
        public static async Task<bool> SendRegistrationConfirmationAsync(string email, string userName)
        {
            string subject = "Bienvenido al Refugio de Animales Quito";
            string body = $@"
                <h2>¡Bienvenido {userName}!</h2>
                <p>Gracias por registrarte en nuestro sistema.</p>
                <p>Ahora puedes:</p>
                <ul>
                    <li>Solicitar adopciones</li>
                    <li>Apadrinar mascotas</li>
                    <li>Realizar donaciones</li>
                    <li>Participar como voluntario</li>
                    <li>Comprar en nuestra tienda solidaria</li>
                </ul>
                <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>
                <br/>
                <p>Saludos,<br/>Equipo del Refugio</p>
            ";

            return await SendEmailAsync(email, subject, body);
        }

        /// <summary>
        /// Envía notificación de adopción aprobada
        /// </summary>
        public static async Task<bool> SendAdoptionApprovedAsync(string email, string userName, string mascotaNombre)
        {
            string subject = $"¡Tu solicitud de adopción de {mascotaNombre} ha sido aprobada!";
            string body = $@"
                <h2>¡Felicitaciones {userName}!</h2>
                <p>Tu solicitud de adopción de <strong>{mascotaNombre}</strong> ha sido aprobada.</p>
                <p>Por favor, ingresa al sistema para revisar y firmar el contrato de adopción.</p>
                <p>Nos pondremos en contacto contigo pronto para coordinar la entrega.</p>
                <br/>
                <p>Gracias por darle un hogar a {mascotaNombre}.</p>
                <p>Saludos,<br/>Equipo del Refugio</p>
            ";

            return await SendEmailAsync(email, subject, body);
        }

        /// <summary>
        /// Envía notificación de solicitud de adopción recibida
        /// </summary>
        public static async Task<bool> SendAdoptionRequestReceivedAsync(string email, string userName, string mascotaNombre)
        {
            string subject = $"Hemos recibido tu solicitud de adopción de {mascotaNombre}";
            string body = $@"
                <h2>Hola {userName}</h2>
                <p>Hemos recibido tu solicitud de adopción de <strong>{mascotaNombre}</strong>.</p>
                <p>Nuestro equipo la revisará en las próximas 48 horas y te contactaremos con los resultados.</p>
                <p>Puedes ver el estado de tu solicitud en tu panel de usuario.</p>
                <br/>
                <p>Saludos,<br/>Equipo del Refugio</p>
            ";

            return await SendEmailAsync(email, subject, body);
        }
    }
}