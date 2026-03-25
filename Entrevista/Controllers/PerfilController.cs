using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    /// <summary>
    /// Controlador del perfil de usuario.
    /// Permite visualizar y editar la información del perfil,
    /// y generar un nuevo avatar de forma aleatoria.
    ///
    /// Rutas expuestas:
    ///   GET  /Perfil/Index
    ///   POST /Perfil/Guardar
    ///   GET  /Perfil/GenerarAvatar
    /// </summary>
    [AuthFilter]
    public class PerfilController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();

        // ── Estilos de avatar disponibles en DiceBear 7.x ─────────────────────
        private static readonly string[] ESTILOS_AVATAR = {
            "adventurer", "avataaars", "bottts", "fun-emoji",
            "lorelei", "micah", "miniavs", "personas"
        };

        // ====================================================================
        // GET: /Perfil
        // ====================================================================

        /// <summary>
        /// Carga y muestra el perfil del usuario autenticado.
        /// Si no existe un perfil en BD, lo crea con valores por defecto.
        /// </summary>
        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // ── Cargar entidades del usuario ──────────────────────────────────
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.id_usuarios == usuarioId);

            if (usuario == null)
                return RedirectToAction("Login", "Auth");

            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

            // ── Crear perfil si no existe ─────────────────────────────────────
            if (perfil == null)
            {
                perfil = CrearPerfilPorDefecto(usuario);
            }

            // ── Construir ViewModel ───────────────────────────────────────────
            var model = new PerfilViewModel
            {
                UsuarioId = usuarioId,
                Nombre = usuario.nombre_usuario,
                Email = usuario.email_usuario,
                Bio = perfil.bio,
                AvatarUrl = perfil.avatar_url,
                AvatarEstilo = perfil.avatar_estilo,
                AvatarSeed = perfil.avatar_seed
            };

            ViewBag.Title = "Mi Perfil";
            return View(model);
        }

        // ====================================================================
        // POST: /Perfil/Guardar
        // ====================================================================

        /// <summary>
        /// Guarda los cambios editables del perfil (actualmente solo la bio).
        /// Nombre y email son de solo lectura.
        /// </summary>
        /// <param name="model">ViewModel con los datos editados por el usuario.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Guardar(PerfilViewModel model)
        {
            // ── Eliminar errores de campos de solo lectura del ModelState ─────
            // Nombre y Email se envían como Hidden y no necesitan re-validarse
            ModelState.Remove("Nombre");
            ModelState.Remove("Email");

            if (!ModelState.IsValid)
            {
                // Recargar datos que no vienen del form
                int usuarioId = SessionHelper.ObtenerUsuarioId(this);

                var usuario = _context.Usuarios
                    .FirstOrDefault(u => u.id_usuarios == usuarioId);

                var perfil = _context.Perfil
                    .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

                model.Nombre = usuario?.nombre_usuario;
                model.Email = usuario?.email_usuario;
                model.AvatarUrl = perfil?.avatar_url;

                ViewBag.Title = "Mi Perfil";
                return View("Index", model);
            }

            int uid = SessionHelper.ObtenerUsuarioId(this);

            var perfilBd = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == uid);

            if (perfilBd == null)
            {
                // Crear perfil si no existe (edge case)
                var usuario = _context.Usuarios.FirstOrDefault(u => u.id_usuarios == uid);
                perfilBd = CrearPerfilPorDefecto(usuario);
            }

            // ── Actualizar solo campos editables ──────────────────────────────
            perfilBd.bio = model.Bio?.Trim();
            perfilBd.fecha_actualizacion = DateTime.Now;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // ====================================================================
        // GET: /Perfil/GenerarAvatar
        // ====================================================================

        /// <summary>
        /// Genera un nuevo avatar aleatorio usando la API de DiceBear.
        /// Usa una semilla aleatoria y rota entre estilos disponibles.
        /// Actualiza la sesión para reflejar el cambio inmediatamente.
        /// </summary>
        public ActionResult GenerarAvatar()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var perfil = _context.Perfil
                .FirstOrDefault(p => p.usuarios_id_usuarios == usuarioId);

            if (perfil == null)
            {
                var usuario = _context.Usuarios.FirstOrDefault(u => u.id_usuarios == usuarioId);
                perfil = CrearPerfilPorDefecto(usuario);
            }

            // ── Generar semilla y estilo aleatorios ───────────────────────────
            var rng = new Random();
            string seed = rng.Next(10000, 99999).ToString();
            string estilo = ESTILOS_AVATAR[rng.Next(ESTILOS_AVATAR.Length)];
            string url = $"https://api.dicebear.com/7.x/{estilo}/svg?seed={seed}";

            // ── Persistir en BD ───────────────────────────────────────────────
            perfil.avatar_seed = seed;
            perfil.avatar_estilo = estilo;
            perfil.avatar_url = url;
            perfil.fecha_actualizacion = DateTime.Now;

            _context.SaveChanges();

            // ── Actualizar sesión para que el sidebar refleje el nuevo avatar ─
            Session[SessionHelper.KEY_AVATAR] = url;

            return RedirectToAction("Index");
        }

        // ====================================================================
        // MÉTODOS PRIVADOS
        // ====================================================================

        /// <summary>
        /// Crea un registro de Perfil con valores por defecto para un usuario nuevo.
        /// El avatar se genera de forma determinista usando el nombre como semilla.
        /// </summary>
        /// <param name="usuario">Entidad del usuario propietario del perfil.</param>
        /// <returns>Entidad Perfil recién creada y guardada en BD.</returns>
        private Perfil CrearPerfilPorDefecto(Usuarios usuario)
        {
            string seed = usuario.nombre_usuario
                .Replace(" ", "")
                .ToLowerInvariant();

            string avatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={seed}";

            var nuevoPerfil = new Perfil
            {
                usuarios_id_usuarios = usuario.id_usuarios,
                avatar_seed = seed,
                avatar_estilo = "adventurer",
                avatar_url = avatarUrl,
                fecha_actualizacion = DateTime.Now
            };

            _context.Perfil.Add(nuevoPerfil);
            _context.SaveChanges();

            return nuevoPerfil;
        }
    }
}