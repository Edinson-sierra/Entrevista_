using Entrevista.Models;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;

namespace Entrevista.Services
{
    public class AuthService
    {
        private readonly SDEEntities _context;

        private const int MAX_INTENTOS = 5;
        private const int BLOQUEO_MINUTOS = 15;

        public AuthService(SDEEntities context)
        {
            _context = context;
        }

        public AuthResult Login(LoginViewModel model, string ip, string dispositivo)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return new AuthResult { Exito = false, Mensaje = "Datos inválidos" };

            var usuario = _context.Usuarios
                .FirstOrDefault(x => x.email_usuario == model.Email);

            if (usuario == null)
            {
                RegistrarIntento(null, false);
                return new AuthResult { Exito = false, Mensaje = "Credenciales incorrectas" };
            }

            // 🔒 VALIDAR BLOQUEO DINÁMICO
            var fechaLimite = DateTime.Now.AddMinutes(-BLOQUEO_MINUTOS);

            var intentosFallidos = _context.Intentos_login
                .Where(x =>
                    x.usuarios_id_usuarios == usuario.id_usuarios &&
                    x.exito_intento == false &&
                    x.fecha_intento.HasValue &&
                    x.fecha_intento > fechaLimite
                )
                .Count();

            if (intentosFallidos >= MAX_INTENTOS)
            {
                return new AuthResult
                {
                    Exito = false,
                    Mensaje = "Usuario bloqueado temporalmente"
                };
            }

            // 🔐 PASSWORD
            if (!BCrypt.Net.BCrypt.Verify(model.Password, usuario.password_hash))
            {
                RegistrarIntento(usuario.id_usuarios, false);

                return new AuthResult
                {
                    Exito = false,
                    Mensaje = "Credenciales incorrectas"
                };
            }

            // ✅ LOGIN OK
            RegistrarIntento(usuario.id_usuarios, true);

            var token = CrearSesionCompleta(usuario.id_usuarios, ip, dispositivo);

            return new AuthResult
            {
                Exito = true,
                Usuario = usuario,
                Token = token
            };
        }

        // 🔥 MÉTODO CLAVE (TOKEN + SESIÓN)
        private string CrearSesionCompleta(int usuarioId, string ip, string dispositivo)
        {
            var token = Guid.NewGuid().ToString();

            // 🔑 TOKEN (AUTH)
            var tokenLogin = new Tokens_login
            {
                usuarios_id_usuarios = usuarioId,
                token = token,
                fecha_creacion = DateTime.Now,
                fecha_expiracion = DateTime.Now.AddHours(2),
                token_activo = true
            };

            _context.Tokens_login.Add(tokenLogin);

            // 📊 SESIÓN (TRACKING)
            var sesion = new Sesiones
            {
                usuarios_id_usuarios = usuarioId,
                ip_sesion = ip,
                dispositivo = dispositivo,
                fecha_inicio_sesion = DateTime.Now,
                token_sesion = token,
                activa = true
            };

            _context.Sesiones.Add(sesion);

            _context.SaveChanges();

            return token;
        }

        public void Logout(string token)
        {
            // 🔑 Desactivar token
            var tokenLogin = _context.Tokens_login
                .FirstOrDefault(x => x.token == token && x.token_activo);

            if (tokenLogin != null)
                tokenLogin.token_activo = false;

            // 📊 Cerrar sesión
            var sesion = _context.Sesiones
                .FirstOrDefault(x => x.token_sesion == token && x.activa);

            if (sesion != null)
            {
                sesion.activa = false;
                sesion.fecha_cierre_sesion = DateTime.Now;
            }

            _context.SaveChanges();
        }

        private void RegistrarIntento(int? usuarioId, bool exito)
        {
            var intento = new Intentos_login
            {
                usuarios_id_usuarios = usuarioId,
                exito_intento = exito,
                fecha_intento = DateTime.Now
            };

            _context.Intentos_login.Add(intento);
            _context.SaveChanges();
        }

        // 🔍 VALIDAR TOKEN (para filtros)
        public Usuarios ObtenerUsuarioPorToken(string token)
        {
            var tokenValido = _context.Tokens_login
                .FirstOrDefault(x =>
                    x.token == token &&
                    x.token_activo &&
                    x.fecha_expiracion > DateTime.Now
                );

            if (tokenValido == null)
                return null;

            return _context.Usuarios
                .FirstOrDefault(x => x.id_usuarios == tokenValido.usuarios_id_usuarios);
        }
        public bool Registrar(RegisterViewModel model)
        {
            if (_context.Usuarios.Any(x => x.email_usuario == model.Email))
                return false;

            var usuario = new Usuarios
            {
                nombre_usuario = model.Nombre,
                email_usuario = model.Email,
                password_hash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                activo = true
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return true;
        }
    }
}