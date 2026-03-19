using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    public class PerfilController : Controller
    {
        // GET: Perifl
        private readonly SDEEntities _context = new SDEEntities();

        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.id_usuarios == usuarioId);

            var model = new PerfilViewModel
            {
                Nombre = usuario.nombre_usuario,
                Email = usuario.email_usuario,
                Bio = perfil?.bio,
                AvatarUrl = perfil?.avatar_url
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Guardar(PerfilViewModel model)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

            if (perfil == null)
            {
                perfil = new Perfil
                {
                    usuarios_id_usuarios = usuarioId
                };
                _context.Perfil.Add(perfil);
            }

            // Generar avatar automático si no existe
            if (string.IsNullOrEmpty(perfil.avatar_url))
            {
                string seed = model.Nombre.Replace(" ", "").ToLower();
                string url = $"https://api.dicebear.com/7.x/adventurer/svg?seed={seed}";

                perfil.avatar_seed = seed;
                perfil.avatar_estilo = "adventurer";
                perfil.avatar_url = url;
            }

            perfil.bio = model.Bio;

            _context.SaveChanges();

            return RedirectToAction("Index");


        }
        public ActionResult GenerarAvatar()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

            if (perfil != null)
            {
                var random = new System.Random();
                string seed = random.Next(1000, 9999).ToString();

                string url = $"https://api.dicebear.com/7.x/adventurer/svg?seed={seed}";

                perfil.avatar_seed = seed;
                perfil.avatar_url = url;

                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}