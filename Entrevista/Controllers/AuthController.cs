using Entrevista.Filter;
using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System.Linq;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly SDEEntities _context;
        public AuthController()
        {
            _context = new SDEEntities();
            _authService = new AuthService(new SDEEntities());
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {

            if (!ModelState.IsValid)
                return View(model);

            string ip = Request.UserHostAddress;
            string dispositivo = Request.UserAgent;

            var result = _authService.Login(model, ip, dispositivo);
            

            if (!result.Exito)
            {
                ModelState.AddModelError("", result.Mensaje);
                return View(model);
            }

            // 🔥 DATOS BASE
            Session["TOKEN"] = result.Token;
            Session["USUARIO"] = result.Usuario.email_usuario;
            Session["nombre_usuario"] = result.Usuario.nombre_usuario;
            Session["usuario_id"] = result.Usuario.id_usuarios;

            // 🔥 PERFIL + AVATAR
            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == result.Usuario.id_usuarios);
            if (perfil != null && !string.IsNullOrEmpty(perfil.avatar_url))
            {
                Session["avatar"] = perfil.avatar_url;
            }
            else
            {
                // Avatar por defecto dinámico
                string seed = result.Usuario.nombre_usuario.Replace(" ", "").ToLower();
                string defaultAvatar = $"https://api.dicebear.com/7.x/adventurer/svg?seed={seed}";

                Session["avatar"] = defaultAvatar;

                // 🔥 Crear perfil automáticamente si no existe
                if (perfil == null)
                {
                    var nuevoPerfil = new Perfil
                    {
                        usuarios_id_usuarios = result.Usuario.id_usuarios,
                        avatar_seed = seed,
                        avatar_estilo = "adventurer",
                        avatar_url = defaultAvatar
                    };

                    _context.Perfil.Add(nuevoPerfil);
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            var token = Session["TOKEN"]?.ToString();

            if (token != null)
                _authService.Logout(token);

            Session.Clear();

            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = _authService.Registrar(model);

            if (!result)
            {
                ModelState.AddModelError("", "El email ya existe");
                return View(model);
            }

            return RedirectToAction("Login");
        }
    }
}