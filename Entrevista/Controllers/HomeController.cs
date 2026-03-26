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
    /// Controlador del Dashboard principal.
    /// Muestra las métricas agregadas del usuario y sus últimas entrevistas.
    ///
    /// Rutas expuestas:
    ///   GET /Home/Index  (raíz de la app autenticada)
    /// </summary>
    [AuthFilter]
    public class HomeController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();

        // ====================================================================
        // GET: /Home  o  /Home/Index
        // ====================================================================

        /// <summary>
        /// Construye el ViewModel del dashboard con:
        ///   - Total de entrevistas del usuario
        ///   - Promedio general de puntajes
        ///   - Nivel técnico calculado (Junior / Mid / Senior)
        ///   - Fecha de última actividad
        ///   - Lista de las últimas 5 entrevistas
        /// </summary>
        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Where(e => e.usuarios_id_usuarios == usuarioId)
                .Select(e => new
                {
                    e.id_entrevista,
                    e.fecha_entrevista,
                    Tema = e.Temas.nombre_tema,
                    Dificultad = e.Dificultad.nombre_dificultad,
                    Promedio = e.Resultado.Any()
                        ? e.Resultado.Average(r => (double?)r.puntaje_total) ?? 0.0
                        : 0.0
                })
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            int totalEntrevistas = entrevistas.Count;

            double promedioGeneral = totalEntrevistas > 0
                ? Math.Round(entrevistas.Average(e => e.Promedio), 1)
                : 0.0;

            DateTime? ultimaFecha = entrevistas.FirstOrDefault()?.fecha_entrevista;

            string nivel = NivelCalculator.Calcular(promedioGeneral);

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

            // 🔥 NUEVO: datos para gráficos
            var datosGraficos = entrevistas
                .Select(e => new DashboardGraficoItem
                {
                    tema = e.Tema,
                    dificultad = e.Dificultad,
                    puntaje = Math.Round(e.Promedio, 1),
                    fecha = e.fecha_entrevista.HasValue
                        ? e.fecha_entrevista.Value.ToString("dd/MM")
                        : ""
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalEntrevistas = totalEntrevistas,
                PromedioPuntaje = promedioGeneral,
                Nivel = nivel,
                UltimaFecha = ultimaFecha,
                UltimasEntrevistas = ultimas,
                DatosGraficos = datosGraficos // 🔥 IMPORTANTE
            };

            ViewBag.Title = "Dashboard";
            return View(model);
        }
    }
}