using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    public class SimulacionController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();

        // GET: Simulacion
        public ActionResult Index()
        {
            var model = new SimulacionViewModel
            {
                Temas = _context.Temas
                    .Select(t => new SelectListItem
                    {
                        Value = t.id_tema.ToString(),
                        Text = t.nombre_tema
                    }).ToList(),

                Dificultades = _context.Dificultad
                    .Select(d => new SelectListItem
                    {
                        Value = d.id_dificultad.ToString(),
                        Text = d.nombre_dificultad
                    }).ToList()
            };

            return View(model);
        }
        [HttpPost]
        public ActionResult Iniciar(SimulacionViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevista = new Entrevista_DATA.Entrevista
            {
                usuarios_id_usuarios = usuarioId,
                temas_id_temas = model.TemaSeleccionado,
                dificultad_id_dificultad = model.DificultadSeleccionada,
                estado_entrevista = "INICIADA"
            };

            _context.Entrevista.Add(entrevista);
            _context.SaveChanges();

            return RedirectToAction("Chat", "Entrevista", new { id = entrevista.id_entrevista });
        }
    }
}