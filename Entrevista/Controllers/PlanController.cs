using Entrevista.Helpers;
using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    public class PlanController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        public async Task<ActionResult> Generar(int entrevistaId)
        {
            // 🔥 VALIDAR USUARIO
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // 🔥 TRAER RESULTADOS DE ESA ENTREVISTA
            var resultados = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevistaId)
                .ToList();

            if (!resultados.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // 🔥 UNIR TODAS LAS OBSERVACIONES
            string observaciones = string.Join("\n",
                resultados.Select(r => r.observaciones));

            // 🔥 IA GENERA PLAN PRO
            string planIA = await _ia.GenerarPlanAsync(observaciones);

            // 🔥 GUARDAR EN BD
            var plan = new Planes_entrenamiento
            {
                usuarios_id_usuarios = usuarioId,
                recomendacion = planIA
            };

            _context.Planes_entrenamiento.Add(plan);
            _context.SaveChanges();

            // 🔥 VIEWMODEL
            var model = new PlanViewModel
            {
                Recomendacion = planIA
            };

            return View("Index", model);
        }
    }
}