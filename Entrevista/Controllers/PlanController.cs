using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    /// <summary>
    /// Controlador de planes de entrenamiento generados por IA.
    ///
    /// Un plan se genera al finalizar una entrevista (automáticamente desde
    /// EntrevistaController) o puede regenerarse manualmente desde los resultados.
    ///
    /// Rutas expuestas:
    ///   GET  /Plan/Generar/{entrevistaId}   → Genera y muestra un nuevo plan
    ///   GET  /Plan/Ver/{id}                 → Muestra un plan guardado por su ID
    ///   GET  /Plan/Historial                → Lista todos los planes del usuario
    /// </summary>
    [AuthFilter]
    public class PlanController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        // ====================================================================
        // GET: /Plan/Index
        // ====================================================================

        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var ultimoPlan = _context.Planes_entrenamiento
                .Where(p => p.usuarios_id_usuarios == usuarioId)
                .OrderByDescending(p => p.fecha_plan)
                .FirstOrDefault();

            PlanViewModel model;

            // 🔥 Si NO hay plan → mandamos modelo vacío (NO redirigimos)
            if (ultimoPlan == null)
            {
                model = new PlanViewModel
                {
                    Id = 0,
                    Recomendacion = "{\"dias\":[]}", // JSON vacío válido para tu JS
                    Fecha = DateTime.Now
                };
            }
            else
            {
                model = new PlanViewModel
                {
                    Id = ultimoPlan.id_plan_entrenamiento,
                    Recomendacion = ultimoPlan.recomendacion,
                    Fecha = ultimoPlan.fecha_plan ?? DateTime.Now
                };
            }

            ViewBag.Title = "Plan de Entrenamiento";
            return View("Index", model);
        }

        // ====================================================================
        // GET: /Plan/Generar/{entrevistaId}
        // ====================================================================

        /// <summary>
        /// Genera un nuevo plan de entrenamiento personalizado a partir
        /// de los resultados de una entrevista específica.
        ///
        /// Si la entrevista no tiene resultados registrados, redirige al Dashboard.
        /// El plan se guarda en Planes_entrenamiento para consulta futura.
        /// </summary>
        /// <param name="entrevistaId">ID de la entrevista origen del plan.</param>
        public async Task<ActionResult> Generar(int entrevistaId)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // ── Verificar que la entrevista pertenece al usuario ─────────────
            var entrevista = _context.Entrevista
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == entrevistaId
                                  && e.usuarios_id_usuarios == usuarioId);

            if (entrevista == null)
                return RedirectToAction("Index", "Home");

            // ── Cargar resultados de esa entrevista ───────────────────────────
            var resultados = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevistaId)
                .OrderBy(r => r.id_resultado)
                .ToList();

            if (!resultados.Any())
                return RedirectToAction("Index", "Home");

            // ── Construir contexto para la IA (unir observaciones) ────────────
            // Excluir el resumen final (último resultado) para evitar redundancia
            var observacionesPorPregunta = resultados
                .Take(resultados.Count > 1 ? resultados.Count - 1 : resultados.Count)
                .Where(r => !string.IsNullOrWhiteSpace(r.observaciones))
                .Select(r => r.observaciones);

            string contexto = string.Join("\n\n", observacionesPorPregunta);

            // ── Generar plan con IA ───────────────────────────────────────────
            string planHtml = await _ia.GenerarPlanAsync(contexto);

            // ── Persistir plan en BD ──────────────────────────────────────────
            var nuevoPlan = new Planes_entrenamiento
            {
                usuarios_id_usuarios = usuarioId,
                recomendacion = planHtml,
                fecha_plan = DateTime.Now
            };

            _context.Planes_entrenamiento.Add(nuevoPlan);
            _context.SaveChanges();

            // ── Construir ViewModel ───────────────────────────────────────────
            var model = new PlanViewModel
            {
                Id = nuevoPlan.id_plan_entrenamiento,
                EntrevistaId = entrevistaId,
                Recomendacion = planHtml,
                Fecha = DateTime.Now,
                Tema = entrevista.Temas.nombre_tema,
                Dificultad = entrevista.Dificultad.nombre_dificultad
            };

            ViewBag.Title = "Plan de Entrenamiento";
            return View("Index", model);
        }

        // ====================================================================
        // GET: /Plan/Ver/{id}
        // ====================================================================

        /// <summary>
        /// Muestra un plan de entrenamiento ya guardado por su ID.
        /// Solo el propietario del plan puede verlo.
        /// </summary>
        /// <param name="id">ID del plan en Planes_entrenamiento.</param>
        public ActionResult Ver(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var plan = _context.Planes_entrenamiento
                .FirstOrDefault(p => p.id_plan_entrenamiento == id
                                  && p.usuarios_id_usuarios == usuarioId);

            if (plan == null)
                return RedirectToAction("Index", "Home");

            var model = new PlanViewModel
            {
                Id = plan.id_plan_entrenamiento,
                Recomendacion = plan.recomendacion,
                Fecha = plan.fecha_plan ?? DateTime.Now
            };

            ViewBag.Title = "Plan de Entrenamiento";
            return View("Index", model);
        }

        // ====================================================================
        // GET: /Plan/Historial
        // ====================================================================

        /// <summary>
        /// Lista todos los planes de entrenamiento del usuario autenticado,
        /// ordenados del más reciente al más antiguo.
        /// </summary>
        public ActionResult Historial()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var planes = _context.Planes_entrenamiento
                .Where(p => p.usuarios_id_usuarios == usuarioId)
                .OrderByDescending(p => p.fecha_plan)
                .Select(p => new PlanViewModel
                {
                    Id = p.id_plan_entrenamiento,
                    Recomendacion = p.recomendacion,
                    Fecha = p.fecha_plan ?? DateTime.Now
                })
                .ToList();

            ViewBag.Title = "Historial de Planes";
            return View("Historial", planes);
        }
    }
}