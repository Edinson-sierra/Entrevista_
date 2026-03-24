using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
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

            // 🔥 AGRUPAMOS POR ENTREVISTA (CLAVE)
            var entrevistas = _context.Entrevista
                .Where(e => e.usuarios_id_usuarios == usuarioId)
                .Select(e => new
                {
                    e.id_entrevista,
                    e.fecha_entrevista,
                    Tema = e.Temas.nombre_tema,
                    Dificultad = e.Dificultad.nombre_dificultad,

                    Promedio = e.Resultado.Any()
                        ? e.Resultado.Average(r => (double?)r.puntaje_total) ?? 0
                        : 0
                })
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            // ===========================
            // MÉTRICAS
            // ===========================
            int totalEntrevistas = entrevistas.Count;

            double promedioGeneral = entrevistas.Any()
                ? entrevistas.Average(e => e.Promedio)
                : 0;

            DateTime? ultimaFecha = entrevistas.FirstOrDefault()?.fecha_entrevista;

            // ===========================
            // NIVEL AUTOMÁTICO
            // ===========================
            string nivel = "Junior";

            if (promedioGeneral >= 8)
                nivel = "Senior";
            else if (promedioGeneral >= 5)
                nivel = "Mid";

            // ===========================
            // ÚLTIMAS ENTREVISTAS
            // ===========================
            var ultimas = entrevistas
                .Take(5)
                .Select(e => new DashboardEntrevistaItemViewModel
                {
                    EntrevistaId = e.id_entrevista,
                    Tema = e.Tema,
                    Dificultad = e.Dificultad,
                    Puntaje = Math.Round(e.Promedio, 1),
                    Fecha = e.fecha_entrevista
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalEntrevistas = totalEntrevistas,
                PromedioPuntaje = Math.Round(promedioGeneral, 1),
                Nivel = nivel,
                UltimaFecha = ultimaFecha,
                UltimasEntrevistas = ultimas
            };

            return View(model);
        }
    }
}