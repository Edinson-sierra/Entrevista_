using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System.Linq;
using System.Web.Mvc;
namespace Entrevista.Controllers
{
    public class HomeController : Controller
    {

        private readonly SDEEntities _context = new SDEEntities();

        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var resultados = _context.Resultado
                .Where(r => r.Entrevista.usuarios_id_usuarios == usuarioId)
                .OrderByDescending(r => r.fecha_resultado)
                .Take(5)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalEntrevistas = resultados.Count,
                PromedioPuntaje = resultados.Any() ? (int)resultados.Average(r => r.puntaje_total) : 0,
                UltimosResultados = resultados.Select(r => new ResultadoViewModel
                {
                    EntrevistaId = r.entrevista_id_entrevista,
                    Puntaje = r.puntaje_total ?? 0,
                    Observaciones = r.observaciones
                }).ToList()
            };

            return View(model);
        }
    }
}