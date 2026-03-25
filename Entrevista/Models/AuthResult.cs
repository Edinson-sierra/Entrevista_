using Entrevista_DATA;

namespace Entrevista.Models
{
    /// <summary>
    /// Modelo de resultado devuelto por AuthService.Login() y AuthService.Registrar().
    /// Encapsula el resultado de una operación de autenticación.
    /// </summary>
    public class AuthResult
    {
        // ────────────────────────────────────────────────────────────
        // Resultado de la operación
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Indica si la operación de autenticación fue exitosa.
        /// true  → login/registro correcto.
        /// false → credenciales inválidas, usuario bloqueado, email duplicado, etc.
        /// </summary>
        public bool Exito { get; set; }

        /// <summary>
        /// Mensaje descriptivo del resultado.
        /// En caso de error, se muestra en la vista de Login/Register.
        /// </summary>
        public string Mensaje { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos del usuario autenticado (solo cuando Exito = true)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Entidad del usuario autenticado.
        /// Null si la autenticación falló.
        /// </summary>
        public Usuarios Usuario { get; set; }

        /// <summary>
        /// Token de sesión generado por AuthService.
        /// Se almacena en Session["TOKEN"] y en la tabla Tokens_login.
        /// </summary>
        public string Token { get; set; }

        // ────────────────────────────────────────────────────────────
        // Factory methods — para construir resultados de forma legible
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Crea un resultado de error con el mensaje indicado.
        /// </summary>
        public static AuthResult Error(string mensaje)
            => new AuthResult { Exito = false, Mensaje = mensaje };

        /// <summary>
        /// Crea un resultado exitoso con el usuario y token.
        /// </summary>
        public static AuthResult Ok(Usuarios usuario, string token)
            => new AuthResult { Exito = true, Usuario = usuario, Token = token };
    }
}