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

            // ── Cargar entrevistas del usuario con sus métricas ──────────────
            // Se proyecta a un tipo anónimo para evitar cargar entidades
            // completas con todas sus navegaciones
            var entrevistas = _context.Entrevista
                .Where(e => e.usuarios_id_usuarios == usuarioId)
                .Select(e => new
                {
                    e.id_entrevista,
                    e.fecha_entrevista,
                    Tema = e.Temas.nombre_tema,
                    Dificultad = e.Dificultad.nombre_dificultad,

                    // Promedio de puntajes de esa entrevista (0 si no tiene resultados)
                    Promedio = e.Resultado.Any()
                        ? e.Resultado.Average(r => (double?)r.puntaje_total) ?? 0.0
                        : 0.0
                })
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            // ── Métricas agregadas ───────────────────────────────────────────
            int totalEntrevistas = entrevistas.Count;
            double promedioGeneral = totalEntrevistas > 0
                ? Math.Round(entrevistas.Average(e => e.Promedio), 1)
                : 0.0;

            DateTime? ultimaFecha = entrevistas.FirstOrDefault()?.fecha_entrevista;

            // ── Nivel técnico usando NivelCalculator centralizado ────────────
            string nivel = NivelCalculator.Calcular(promedioGeneral);

            // ── Últimas 5 entrevistas para la tabla ──────────────────────────
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

            // ── Construir ViewModel ──────────────────────────────────────────
            var model = new DashboardViewModel
            {
                TotalEntrevistas = totalEntrevistas,
                PromedioPuntaje = promedioGeneral,
                Nivel = nivel,
                UltimaFecha = ultimaFecha,
                UltimasEntrevistas = ultimas
            };

            ViewBag.Title = "Dashboard";
            return View(model);
        }
    }
}