using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace Entrevista.Services
{
    /// <summary>
    /// Servicio de envío de emails via SMTP.
    /// Configuración en Web.config: SmtpHost, SmtpPort, SmtpUser, SmtpPassword, SmtpFrom, AppBaseUrl.
    /// </summary>
    public static class EmailService
    {
        private static readonly string _host     = ConfigurationManager.AppSettings["SmtpHost"];
        private static readonly int    _port     = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
        private static readonly string _user     = ConfigurationManager.AppSettings["SmtpUser"];
        private static readonly string _password = ConfigurationManager.AppSettings["SmtpPassword"];
        private static readonly string _from     = ConfigurationManager.AppSettings["SmtpFrom"];

        /// <summary>
        /// Envía el correo de recuperación de contraseña con un enlace de un solo uso.
        /// </summary>
        /// <param name="destinatario">Correo del usuario.</param>
        /// <param name="nombre">Nombre del usuario para personalizar el saludo.</param>
        /// <param name="enlace">URL completa con el token de recuperación.</param>
        public static void EnviarRecuperacion(string destinatario, string nombre, string enlace)
        {
            string cuerpo = CuerpoEmail(nombre, enlace);

            using (var smtp = new SmtpClient(_host, _port))
            {
                smtp.Credentials = new NetworkCredential(_user, _password);
                smtp.EnableSsl   = true;

                using (var mensaje = new MailMessage())
                {
                    mensaje.From       = new MailAddress(_from);
                    mensaje.To.Add(destinatario);
                    mensaje.Subject    = "Recupera tu contraseña — Entrevista SDE";
                    mensaje.Body       = cuerpo;
                    mensaje.IsBodyHtml = true;

                    smtp.Send(mensaje);
                }
            }
        }

        private static string CuerpoEmail(string nombre, string enlace)
        {
            return $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
  <style>
    body  {{ margin:0; padding:0; background:#0d0f14; font-family:'Segoe UI',Arial,sans-serif; color:#e8eaf0; }}
    .wrap {{ max-width:540px; margin:40px auto; background:#13161e; border:1px solid #252a38; border-radius:16px; overflow:hidden; }}
    .hdr  {{ background:linear-gradient(135deg,#1e293b,#2563eb); padding:32px 36px; text-align:center; }}
    .hdr-icon {{ font-size:40px; }}
    .hdr h1  {{ margin:12px 0 4px; font-size:22px; color:#fff; }}
    .hdr p   {{ margin:0; font-size:13px; color:rgba(255,255,255,.6); }}
    .body {{ padding:32px 36px; }}
    .body p  {{ font-size:14px; line-height:1.7; color:#9ca3af; margin:0 0 18px; }}
    .body strong {{ color:#e8eaf0; }}
    .btn  {{ display:block; width:fit-content; margin:24px auto; padding:14px 36px;
             background:#4f8ef7; color:#fff; border-radius:10px; text-decoration:none;
             font-weight:600; font-size:15px; }}
    .note {{ background:#1a1e28; border:1px solid #252a38; border-radius:8px;
             padding:14px 16px; margin-top:20px; }}
    .note p  {{ font-size:12px; color:#6b7280; margin:0; word-break:break-all; }}
    .note a  {{ color:#4f8ef7; }}
    .ftr {{ border-top:1px solid #252a38; padding:18px 36px; text-align:center;
            font-size:11px; color:#374151; }}
  </style>
</head>
<body>
  <div class=""wrap"">
    <div class=""hdr"">
      <div class=""hdr-icon"">🔐</div>
      <h1>Recupera tu contraseña</h1>
      <p>Entrevista SDE — Preparación para entrevistas técnicas</p>
    </div>
    <div class=""body"">
      <p>Hola, <strong>{nombre}</strong>.</p>
      <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta.
         Haz clic en el botón de abajo para crear una nueva contraseña.</p>
      <a href=""{enlace}"" class=""btn"">Restablecer contraseña</a>
      <div class=""note"">
        <p>Si el botón no funciona, copia y pega este enlace en tu navegador:<br/>
           <a href=""{enlace}"">{enlace}</a></p>
      </div>
      <p style=""margin-top:20px;"">
        Este enlace es válido por <strong>2 horas</strong> y solo puede usarse una vez.<br/>
        Si no solicitaste este cambio, ignora este correo.
      </p>
    </div>
    <div class=""ftr"">Entrevista SDE &copy; {DateTime.Now.Year} · Todos los derechos reservados</div>
  </div>
</body>
</html>";
        }
    }
}
