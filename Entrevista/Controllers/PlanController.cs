using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    public class PlanController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities ();
        private readonly IIAService _ia = new IAService();

        public async Task<ActionResult> Generar(int entrevistaId)
        {
            var resultado = _context.Resultado
                .FirstOrDefault(r => r.entrevista_id_entrevista == entrevistaId);

            int usuarioId = (int)Session["usuario"];

            string planIA = await _ia.PreguntarAsync(
                $"Basado en estas observaciones genera un plan de entrenamiento:\n{resultado.observaciones}"
            );

            var plan = new Planes_entrenamiento
            {
                usuarios_id_usuarios = usuarioId,
                recomendacion = planIA
            };

            _context.Planes_entrenamiento.Add(plan);
            _context.SaveChanges();

            var model = new PlanViewModel
            {
                Recomendacion = planIA
            };

            return View("Index", model);
        }
    }
}