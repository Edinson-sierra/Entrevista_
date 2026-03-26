using Entrevista.Models;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;

namespace Entrevista.Services
{
    /// <summary>
    /// Servicio de autenticación y gestión de sesiones.
    ///
    /// Responsabilidades:
    ///   - Validar credenciales con protección anti-fuerza-bruta
    ///   - Crear y revocar tokens de sesión (Tokens_login + Sesiones)
    ///   - Registrar intentos de login para auditoría
    ///   - Registrar nuevos usuarios con hash BCrypt
    ///   - Validar tokens para el AuthFilter
    /// </summary>
    public class AuthService
    {
        // ────────────────────────────────────────────────────────────
        // Constantes de política de seguridad
        // ────────────────────────────────────────────────────────────

        /// <summary>Intentos fallidos antes de bloqueo temporal.</summary>
        private const int MAX_INTENTOS = 5;

        /// <summary>Minutos de ventana para contar intentos fallidos.</summary>
        private const int VENTANA_MINUTOS = 15;

        /// <summary>Horas de vigencia del token de sesión.</summary>
        private const int TOKEN_HORAS = 8;

        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context;

        // ────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────

        public AuthService(SDEEntities context)
        {
            _context = context;
        }

        // ====================================================================
        // LOGIN
        // ====================================================================

        /// <summary>
        /// Autentica al usuario validando credenciales y política anti-brute-force.
        ///
        /// Flujo:
        ///   1. Validar que los datos no estén vacíos
        ///   2. Buscar usuario por email
        ///   3. Verificar si está bloqueado (>= MAX_INTENTOS en últimos VENTANA_MINUTOS)
        ///   4. Verificar contraseña con BCrypt
        ///   5. Registrar intento (exitoso o fallido)
        ///   6. Si es exitoso → crear token + sesión
        /// </summary>
        /// <param name="model">Credenciales del usuario.</param>
        /// <param name="ip">IP del cliente para registro de sesión.</param>
        /// <param name="dispositivo">User-Agent del cliente.</param>
        /// <returns>AuthResult con Exito=true y Token, o Exito=false con Mensaje.</returns>
        public AuthResult Login(LoginViewModel model, string ip, string dispositivo)
        {
            // ── Validación de entrada ─────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
                return AuthResult.Error("Completa todos los campos.");

            // ── Buscar usuario por email ──────────────────────────────────────
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.email_usuario == model.Email
                                  && u.activo == true);

            if (usuario == null)
            {
                // Registrar intento fallido sin usuario (email no existe)
                RegistrarIntento(null, false);
                // Mensaje genérico para no revelar si el email existe
                return AuthResult.Error("Credenciales incorrectas.");
            }

            // ── Verificar bloqueo por intentos fallidos ───────────────────────
            if (EstaBloquadoPorIntentos(usuario.id_usuarios))
            {
                return AuthResult.Error(
                    $"Cuenta bloqueada temporalmente por múltiples intentos fallidos. " +
                    $"Espera {VENTANA_MINUTOS} minutos e intenta de nuevo."
                );
            }

            // ── Verificar contraseña con BCrypt ───────────────────────────────
            bool passwordValido = BCrypt.Net.BCrypt.Verify(
                model.Password,
                usuario.password_hash
            );

            if (!passwordValido)
            {
                RegistrarIntento(usuario.id_usuarios, false);

                // Calcular intentos restantes para el mensaje
                int intentosFallidos = ContarIntentosFallidos(usuario.id_usuarios);
                int intentosRestantes = Math.Max(0, MAX_INTENTOS - intentosFallidos);

                string mensaje = intentosRestantes > 0
                    ? $"Credenciales incorrectas. Te quedan {intentosRestantes} intentos."
                    : "Cuenta bloqueada temporalmente.";

                return AuthResult.Error(mensaje);
            }

            // ── Login exitoso ─────────────────────────────────────────────────
            RegistrarIntento(usuario.id_usuarios, true);

            string token = CrearSesionCompleta(usuario.id_usuarios, ip, dispositivo);

            return AuthResult.Ok(usuario, token);
        }

        // ====================================================================
        // LOGOUT
        // ====================================================================

        /// <summary>
        /// Invalida el token activo del usuario.
        /// Marca el token como inactivo y registra la hora de cierre de sesión.
        /// </summary>
        /// <param name="token">Token a invalidar.</param>
        public void Logout(string token)
        {
            if (string.IsNullOrEmpty(token)) return;

            // ── Desactivar token de autenticación ─────────────────────────────
            var tokenLogin = _context.Tokens_login
                .FirstOrDefault(t => t.token == token && t.token_activo);

            if (tokenLogin != null)
                tokenLogin.token_activo = false;

            // ── Cerrar sesión de tracking ─────────────────────────────────────
            var sesion = _context.Sesiones
                .FirstOrDefault(s => s.token_sesion == token && s.activa);

            if (sesion != null)
            {
                sesion.activa = false;
                sesion.fecha_cierre_sesion = DateTime.Now;
            }

            _context.SaveChanges();
        }

        // ====================================================================
        // REGISTRO
        // ====================================================================

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// Verifica que el email no esté duplicado antes de crear el registro.
        /// La contraseña se hashea con BCrypt (work factor 12).
        /// </summary>
        /// <param name="model">Datos del nuevo usuario.</param>
        /// <returns>true si el registro fue exitoso, false si el email ya existe.</returns>
        public bool Registrar(RegisterViewModel model)
        {
            // ── Verificar email duplicado ─────────────────────────────────────
            bool emailExiste = _context.Usuarios
                .Any(u => u.email_usuario == model.Email);

            if (emailExiste) return false;

            // ── Crear usuario con contraseña hasheada ─────────────────────────
            var nuevoUsuario = new Usuarios
            {
                nombre_usuario = model.Nombre.Trim(),
                email_usuario = model.Email.Trim().ToLowerInvariant(),
                password_hash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12),
                activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();

            return true;
        }

        // ====================================================================
        // VALIDAR TOKEN (para AuthFilter)
        // ====================================================================

        /// <summary>
        /// Busca el usuario propietario de un token activo y no expirado.
        /// Usado por AuthFilter en cada request protegido.
        /// </summary>
        /// <param name="token">Token a validar.</param>
        /// <returns>Entidad Usuarios si el token es válido, null en caso contrario.</returns>
        public Usuarios ObtenerUsuarioPorToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var tokenValido = _context.Tokens_login
                .FirstOrDefault(t =>
                    t.token == token &&
                    t.token_activo == true &&
                    t.fecha_expiracion > DateTime.Now
                );

            if (tokenValido == null) return null;

            return _context.Usuarios
                .FirstOrDefault(u => u.id_usuarios == tokenValido.usuarios_id_usuarios
                                  && u.activo == true);
        }

        // ====================================================================
        // MÉTODOS PRIVADOS
        // ====================================================================

        /// <summary>
        /// Crea el token de autenticación y el registro de sesión activa en BD.
        /// </summary>
        /// <param name="usuarioId">ID del usuario autenticado.</param>
        /// <param name="ip">IP del cliente.</param>
        /// <param name="dispositivo">User-Agent del cliente.</param>
        /// <returns>Token generado como GUID string.</returns>
        private string CrearSesionCompleta(int usuarioId, string ip, string dispositivo)
        {
            string token = Guid.NewGuid().ToString();
            var ahora = DateTime.Now;

            // ── Token de autenticación ────────────────────────────────────────
            _context.Tokens_login.Add(new Tokens_login
            {
                usuarios_id_usuarios = usuarioId,
                token = token,
                fecha_creacion = ahora,
                fecha_expiracion = ahora.AddHours(TOKEN_HORAS),
                token_activo = true
            });

            // ── Registro de sesión (auditoría / tracking) ─────────────────────
            _context.Sesiones.Add(new Sesiones
            {
                usuarios_id_usuarios = usuarioId,
                ip_sesion = ip,
                dispositivo = dispositivo,
                fecha_inicio_sesion = ahora,
                token_sesion = token,
                activa = true
            });

            _context.SaveChanges();

            return token;
        }

        /// <summary>
        /// Registra un intento de login en la tabla Intentos_login.
        /// </summary>
        /// <param name="usuarioId">ID del usuario (null si el email no existe).</param>
        /// <param name="exito">true = login exitoso, false = fallido.</param>
        private void RegistrarIntento(int? usuarioId, bool exito)
        {
            _context.Intentos_login.Add(new Intentos_login
            {
                usuarios_id_usuarios = usuarioId,
                exito_intento = exito,
                fecha_intento = DateTime.Now
            });

            _context.SaveChanges();
        }

        /// <summary>
        /// Verifica si el usuario está bloqueado por exceso de intentos fallidos.
        /// </summary>
        /// <param name="usuarioId">ID del usuario a verificar.</param>
        private bool EstaBloquadoPorIntentos(int usuarioId)
        {
            return ContarIntentosFallidos(usuarioId) >= MAX_INTENTOS;
        }

        /// <summary>
        /// Cuenta los intentos fallidos recientes dentro de la ventana de tiempo.
        /// </summary>
        /// <param name="usuarioId">ID del usuario.</param>
        /// <returns>Número de intentos fallidos en los últimos VENTANA_MINUTOS minutos.</returns>
        private int ContarIntentosFallidos(int usuarioId)
        {
            var fechaLimite = DateTime.Now.AddMinutes(-VENTANA_MINUTOS);

            return _context.Intentos_login
                .Count(i =>
                    i.usuarios_id_usuarios == usuarioId &&
                    i.exito_intento == false &&
                    i.fecha_intento.HasValue &&
                    i.fecha_intento > fechaLimite
                );
        }
    }
}