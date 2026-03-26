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
    /// Controlador para la configuración e inicio de simulaciones de entrevista.
    /// Requiere autenticación activa (AuthFilter).
    /// </summary>
    [AuthFilter]
    public class SimulacionController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();

        // ================================================================
        // GET: /Simulacion
        // ================================================================

        /// <summary>
        /// Muestra el formulario de configuración de entrevista.
        /// Carga los temas y dificultades disponibles desde la base de datos.
        /// </summary>
        public ActionResult Index()
        {
            var model = new SimulacionViewModel
            {
                Temas = _context.Temas
                    .OrderBy(t => t.nombre_tema)
                    .Select(t => new SelectListItem
                    {
                        Value = t.id_tema.ToString(),
                        Text = t.nombre_tema
                    })
                    .ToList(),

                Dificultades = _context.Dificultad
                    .OrderBy(d => d.id_dificultad)
                    .Select(d => new SelectListItem
                    {
                        Value = d.id_dificultad.ToString(),
                        Text = d.nombre_dificultad
                    })
                    .ToList()
            };

            return View(model);
        }

        // ================================================================
        // POST: /Simulacion/Iniciar
        // ================================================================

        /// <summary>
        /// Crea una nueva entrevista en la base de datos y redirige al chat.
        /// </summary>
        /// <param name="model">ViewModel con el tema y dificultad seleccionados.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Iniciar(SimulacionViewModel model)
        {
            // Recargar listas en caso de error de validación
            if (!ModelState.IsValid)
            {
                model.Temas = _context.Temas
                    .OrderBy(t => t.nombre_tema)
                    .Select(t => new SelectListItem
                    {
                        Value = t.id_tema.ToString(),
                        Text = t.nombre_tema
                    })
                    .ToList();

                model.Dificultades = _context.Dificultad
                    .OrderBy(d => d.id_dificultad)
                    .Select(d => new SelectListItem
                    {
                        Value = d.id_dificultad.ToString(),
                        Text = d.nombre_dificultad
                    })
                    .ToList();

                return View("Index", model);
            }

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // Crear la entrevista con fecha registrada correctamente
            var entrevista = new Entrevista_DATA.Entrevista
            {
                usuarios_id_usuarios = usuarioId,
                temas_id_temas = model.TemaSeleccionado,
                dificultad_id_dificultad = model.DificultadSeleccionada,
                estado_entrevista = "INICIADA",
                fecha_entrevista = DateTime.Now    // ← FIX: fecha era null antes
            };

            _context.Entrevista.Add(entrevista);
            _context.SaveChanges();

            return RedirectToAction("Chat", "Entrevista", new { id = entrevista.id_entrevista });
        }
    }
}