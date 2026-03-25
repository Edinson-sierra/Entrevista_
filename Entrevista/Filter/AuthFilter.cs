using Entrevista.Services;
using Entrevista_DATA;
using System.Web;
using System.Web.Mvc;

namespace Entrevista.Filter
{
    /// <summary>
    /// Filtro de acción que protege los controladores que requieren
    /// autenticación activa.
    ///
    /// Funcionamiento:
    ///   1. Lee el token de la sesión del servidor.
    ///   2. Valida el token contra la BD (vigencia + estado activo).
    ///   3. Si el token es inválido o expiró → redirige a /Auth/Login.
    ///   4. Si es válido → almacena el usuario en HttpContext.Items
    ///      para que los controladores puedan accederlo sin re-consultar BD.
    ///
    /// Uso: decorar controladores o acciones con [AuthFilter]
    /// </summary>
    public class AuthFilter : ActionFilterAttribute
    {
        // ====================================================================
        // EJECUCIÓN ANTES DE CADA ACCIÓN
        // ====================================================================

        /// <summary>
        /// Se ejecuta antes de cada acción decorada con [AuthFilter].
        /// Valida el token de sesión y redirige si no es válido.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;

            // ── Verificar que exista token en sesión ──────────────────────────
            var token = session[Helpers.SessionHelper.KEY_TOKEN]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                RedirigirLogin(filterContext);
                return;
            }

            // ── Validar token contra la BD ────────────────────────────────────
            var db = new SDEEntities();
            var authService = new AuthService(db);
            var usuario = authService.ObtenerUsuarioPorToken(token);

            if (usuario == null)
            {
                // Token inválido o expirado — limpiar sesión
                session.Clear();
                RedirigirLogin(filterContext);
                return;
            }

            // ── Exponer usuario al controlador via HttpContext.Items ───────────
            HttpContext.Current.Items["UsuarioActual"] = usuario;

            base.OnActionExecuting(filterContext);
        }

        // ====================================================================
        // MÉTODOS PRIVADOS
        // ====================================================================

        /// <summary>
        /// Cancela la ejecución de la acción y redirige al login.
        /// </summary>
        private static void RedirigirLogin(ActionExecutingContext ctx)
        {
            ctx.Result = new RedirectResult("/Auth/Login");
        }
    }
}