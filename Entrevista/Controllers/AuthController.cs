using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.Models;
using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    /// <summary>
    /// Controlador de autenticación.
    /// Gestiona el ciclo de vida de la sesión del usuario:
    /// Login → Sesión activa → Logout.
    ///
    /// Rutas expuestas:
    ///   GET  /Auth/Login
    ///   POST /Auth/Login
    ///   GET  /Auth/Register
    ///   POST /Auth/Register
    ///   GET  /Auth/Logout
    /// </summary>
    public class AuthController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly AuthService _authService;
        private readonly SDEEntities _context;

        // ────────────────────────────────────────────────────────────
        // Constructor
        // ────────────────────────────────────────────────────────────

        public AuthController()
        {
            _context = new SDEEntities();
            _authService = new AuthService(_context);
        }

        // ====================================================================
        // LOGIN
        // ====================================================================

        /// <summary>
        /// GET /Auth/Login
        /// Muestra el formulario de inicio de sesión.
        /// Redirige al Dashboard si ya hay sesión activa.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Login()
        {
            // Evitar que un usuario ya autenticado vea el login
            if (SessionHelper.EstaAutenticado(this))
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        /// <summary>
        /// POST /Auth/Login
        /// Procesa las credenciales enviadas por el formulario.
        /// Si son válidas, crea la sesión y redirige al Dashboard.
        /// </summary>
        /// <param name="model">Credenciales del usuario (email + password).</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(LoginViewModel model)
        {
            // ── Validación del modelo (anotaciones DataAnnotations) ──────────
            if (!ModelState.IsValid)
                return View(model);

            // ── Capturar metadatos de red para el registro de intento ────────
            string ip = Request.UserHostAddress;
            string dispositivo = Request.UserAgent;

            // ── Delegar autenticación al servicio ────────────────────────────
            AuthResult result = _authService.Login(model, ip, dispositivo);

            if (!result.Exito)
            {
                ModelState.AddModelError(string.Empty, result.Mensaje);
                return View(model);
            }

            // ── Establecer sesión ────────────────────────────────────────────
            EstablecerSesion(result);

            return RedirectToAction("Index", "Home");
        }

        // ====================================================================
        // REGISTER
        // ====================================================================

        /// <summary>
        /// GET /Auth/Register
        /// Muestra el formulario de registro de nuevo usuario.
        /// </summary>
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (SessionHelper.EstaAutenticado(this))
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// POST /Auth/Register
        /// Crea una nueva cuenta de usuario.
        /// Si el email ya existe, retorna error sin exponer qué dato es duplicado.
        /// </summary>
        /// <param name="model">Datos del nuevo usuario.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool registrado = _authService.Registrar(model);

            if (!registrado)
            {
                // Mensaje genérico — no revelar si el email existe por seguridad
                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible crear la cuenta. El correo ya está registrado."
                );
                return View(model);
            }

            // Registro exitoso → redirigir al login con mensaje de confirmación
            TempData["SuccessMessage"] = "Cuenta creada exitosamente. Inicia sesión para continuar.";
            return RedirectToAction("Login");
        }

        // ====================================================================
        // LOGOUT
        // ====================================================================

        /// <summary>
        /// GET /Auth/Logout
        /// Invalida el token en BD, limpia la sesión del servidor y redirige al login.
        /// </summary>
        [AuthFilter]
        public ActionResult Logout()
        {
            string token = SessionHelper.ObtenerToken(this);

            // Invalidar token en BD si existe
            if (!string.IsNullOrEmpty(token))
                _authService.Logout(token);

            // Destruir sesión del servidor completamente
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        // ====================================================================
        // MÉTODOS PRIVADOS
        // ====================================================================

        /// <summary>
        /// Establece todas las variables de sesión necesarias tras un login exitoso.
        /// Incluye datos del usuario y URL del avatar (con creación automática si no existe).
        /// </summary>
        /// <param name="result">Resultado exitoso del AuthService.</param>
        private void EstablecerSesion(AuthResult result)
        {
            // ── Datos base de sesión ─────────────────────────────────────────
            Session[SessionHelper.KEY_TOKEN] = result.Token;
            Session[SessionHelper.KEY_EMAIL] = result.Usuario.email_usuario;
            Session[SessionHelper.KEY_NOMBRE] = result.Usuario.nombre_usuario;
            Session[SessionHelper.KEY_USER_ID] = result.Usuario.id_usuarios;

            // ── Cargar o generar avatar ──────────────────────────────────────
            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == result.Usuario.id_usuarios);

            if (perfil != null && !string.IsNullOrEmpty(perfil.avatar_url))
            {
                // Usar avatar guardado en BD
                Session[SessionHelper.KEY_AVATAR] = perfil.avatar_url;
            }
            else
            {
                // Generar avatar determinista con la semilla del nombre
                string seed = result.Usuario.nombre_usuario
                    .Replace(" ", "")
                    .ToLowerInvariant();

                string avatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={seed}";

                Session[SessionHelper.KEY_AVATAR] = avatarUrl;

                // Crear o actualizar el perfil si no existe avatar
                if (perfil == null)
                {
                    _context.Perfil.Add(new Perfil
                    {
                        usuarios_id_usuarios = result.Usuario.id_usuarios,
                        avatar_seed = seed,
                        avatar_estilo = "adventurer",
                        avatar_url = avatarUrl
                    });
                }
                else
                {
                    // Perfil existe pero sin avatar — completarlo
                    perfil.avatar_seed = seed;
                    perfil.avatar_estilo = "adventurer";
                    perfil.avatar_url = avatarUrl;
                }

                _context.SaveChanges();
            }
        }

        // ====================================================================
        // RECUPERACIÓN DE CONTRASEÑA
        // ====================================================================

        /// <summary>GET /Auth/OlvideContrasena — Formulario de email</summary>
        [AllowAnonymous]
        public ActionResult OlvideContrasena()
        {
            if (SessionHelper.EstaAutenticado(this))
                return RedirectToAction("Index", "Home");

            return View(new OlvideContrasenaViewModel());
        }

        /// <summary>POST /Auth/OlvideContrasena — Solicitar enlace</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult OlvideContrasena(OlvideContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                _authService.SolicitarRecuperacion(model.Email);
            }
            catch
            {
                // No propagar errores SMTP al usuario
            }

            // Mensaje genérico siempre (no revelar si el email existe)
            TempData["SuccessMessage"] =
                "Si el correo está registrado, recibirás un enlace de recuperación en breve.";

            return RedirectToAction("OlvideContrasena");
        }

        /// <summary>GET /Auth/Restablecer?token=... — Formulario nueva contraseña</summary>
        [AllowAnonymous]
        public ActionResult Restablecer(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToLogin("Enlace inválido.");

            var usuario = _authService.ValidarTokenRecuperacion(token);
            if (usuario == null)
                return RedirectToLogin("El enlace ha expirado o ya fue utilizado.");

            return View(new RestablecerContrasenaViewModel { Token = token });
        }

        /// <summary>POST /Auth/Restablecer — Aplicar nueva contraseña</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Restablecer(RestablecerContrasenaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool ok = _authService.RestablecerPassword(model.Token, model.NuevaPassword);

            if (!ok)
                return RedirectToLogin("El enlace ha expirado o ya fue utilizado.");

            TempData["SuccessMessage"] = "Contraseña restablecida correctamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        // ── Helper privado ──────────────────────────────────────────────────
        private ActionResult RedirectToLogin(string error)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction("Login");
        }
    }
}