using System;
using System.Web.Mvc;

namespace Entrevista.Helpers
{
    /// <summary>
    /// Helper estático para operaciones sobre la sesión del usuario.
    /// Centraliza el acceso a las variables de sesión para evitar
    /// duplicación de claves y manejo inconsistente de nulos.
    ///
    /// CLAVES DE SESIÓN usadas en la aplicación:
    ///   "TOKEN"          → string  — Token de autenticación activo
    ///   "USUARIO"        → string  — Email del usuario
    ///   "nombre_usuario" → string  — Nombre completo
    ///   "usuario_id"     → int     — ID primario del usuario
    ///   "avatar"         → string  — URL del avatar SVG
    /// </summary>
    public static class SessionHelper
    {
        // ────────────────────────────────────────────────────────────
        // Constantes de claves de sesión
        // ────────────────────────────────────────────────────────────

        public const string KEY_TOKEN = "TOKEN";
        public const string KEY_EMAIL = "USUARIO";
        public const string KEY_NOMBRE = "nombre_usuario";
        public const string KEY_USER_ID = "usuario_id";
        public const string KEY_AVATAR = "avatar";

        // ====================================================================
        // MÉTODOS DE LECTURA
        // ====================================================================

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde la sesión.
        /// </summary>
        /// <param name="controller">Controlador actual (para acceder a Session).</param>
        /// <returns>ID del usuario como entero.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Si la sesión expiró o el usuario no está autenticado.
        /// </exception>
        public static int ObtenerUsuarioId(Controller controller)
        {
            var id = controller.Session[KEY_USER_ID];

            if (id == null)
                throw new UnauthorizedAccessException(
                    "La sesión ha expirado. Por favor inicia sesión nuevamente."
                );

            return (int)id;
        }

        /// <summary>
        /// Intenta obtener el ID del usuario sin lanzar excepción.
        /// </summary>
        /// <returns>ID del usuario o 0 si no está autenticado.</returns>
        public static int ObtenerUsuarioIdSafe(Controller controller)
        {
            var id = controller.Session[KEY_USER_ID];
            return id != null ? (int)id : 0;
        }

        /// <summary>
        /// Obtiene el email del usuario autenticado.
        /// </summary>
        public static string ObtenerEmail(Controller controller)
            => controller.Session[KEY_EMAIL]?.ToString();

        /// <summary>
        /// Obtiene el nombre completo del usuario autenticado.
        /// </summary>
        public static string ObtenerNombre(Controller controller)
            => controller.Session[KEY_NOMBRE]?.ToString() ?? "Usuario";

        /// <summary>
        /// Obtiene el token de sesión activo.
        /// </summary>
        public static string ObtenerToken(Controller controller)
            => controller.Session[KEY_TOKEN]?.ToString();

        /// <summary>
        /// Obtiene la URL del avatar del usuario.
        /// Retorna un avatar por defecto si no hay ninguno configurado.
        /// </summary>
        public static string ObtenerAvatar(Controller controller)
            => controller.Session[KEY_AVATAR]?.ToString()
               ?? "https://api.dicebear.com/7.x/adventurer/svg?seed=default";

        // ====================================================================
        // MÉTODOS DE VERIFICACIÓN
        // ====================================================================

        /// <summary>
        /// Indica si existe una sesión activa con datos de usuario.
        /// </summary>
        public static bool EstaAutenticado(Controller controller)
            => controller.Session[KEY_TOKEN] != null
               && controller.Session[KEY_USER_ID] != null;
    }
}