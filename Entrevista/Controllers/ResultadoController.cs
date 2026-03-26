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
    /// Controlador de resultados de entrevista.
    /// Muestra el análisis detallado: desglose por pregunta,
    /// puntaje promedio, nivel técnico y recomendación final de la IA.
    ///
    /// Rutas expuestas:
    ///   GET /Resultado/Index/{id}
    /// </summary>
    [AuthFilter]
    public class ResultadoController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();

        // ====================================================================
        // GET: /Resultado/Index/{id}
        // ====================================================================

        /// <summary>
        /// Construye el ViewModel de resultados para una entrevista finalizada.
        ///
        /// Lógica de emparejamiento pregunta–resultado:
        ///   Los resultados (evaluaciones por pregunta) se guardan en el mismo
        ///   orden que las preguntas, por lo que se emparejan por índice posicional.
        ///   El último resultado es el "resumen final" generado al terminar la entrevista.
        ///
        /// Validaciones:
        ///   - La entrevista debe existir
        ///   - La entrevista debe pertenecer al usuario autenticado
        /// </summary>
        /// <param name="id">ID de la entrevista a mostrar.</param>
        public ActionResult Index(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // ── Cargar entrevista con todas sus relaciones ───────────────────
            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);

            // ── Validaciones de seguridad ────────────────────────────────────
            if (entrevista == null)
                return RedirectToAction("Index", "Home");

            if (entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            // ── Ordenar preguntas y resultados cronológicamente ───────────────
            var preguntas = entrevista.Preguntas
                .OrderBy(p => p.id_pregunta)
                .ToList();

            // Los N primeros resultados corresponden a evaluaciones por pregunta.
            // El último es el resumen global (si existe).
            var resultados = entrevista.Resultado
                .OrderBy(r => r.id_resultado)
                .ToList();

            // ── Construir detalles por pregunta ──────────────────────────────
            var detalles = preguntas.Select((pregunta, index) =>
            {
                // Respuesta del usuario para esta pregunta
                var respuesta = entrevista.Respuestas
                    .FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);

                // Resultado de evaluación emparejado por índice posicional
                var resultado = index < resultados.Count ? resultados[index] : null;

                return new ResultadoDetalleViewModel
                {
                    Pregunta = pregunta.texto_pregunta,
                    Respuesta = respuesta?.respuesta_usuario,
                    Puntaje = resultado?.puntaje_total ?? 0,
                    Observacion = resultado?.observaciones
                };
            }).ToList();

            // ── Calcular promedio solo de las evaluaciones por pregunta ───────
            // Excluir el último resultado (resumen global) del promedio
            var evaluacionesPorPregunta = detalles.Take(preguntas.Count).ToList();

            double promedio = evaluacionesPorPregunta.Any()
                ? Math.Round(evaluacionesPorPregunta.Average(d => d.Puntaje), 1)
                : 0.0;

            // ── Calcular nivel usando NivelCalculator centralizado ────────────
            string nivel = NivelCalculator.Calcular(promedio);

            // ── Obtener recomendación final (último resultado = resumen IA) ───
            // Puede contener el análisis completo o el plan de entrenamiento
            var ultimoResultado = resultados.LastOrDefault();
            string recomendacion = ultimoResultado?.observaciones;

            // Si la observación contiene el separador del plan, tomar solo el análisis
            if (!string.IsNullOrEmpty(recomendacion) && recomendacion.Contains("---PLAN---"))
            {
                recomendacion = recomendacion
                    .Split(new[] { "---PLAN---" }, StringSplitOptions.None)[0]
                    .Trim();
            }

            // ── Construir ViewModel final ─────────────────────────────────────
            var model = new ResultadoViewModel
            {
                EntrevistaId = id,
                Detalles = detalles,
                Promedio = promedio,
                Nivel = nivel,
                RecomendacionFinal = recomendacion,
                Fecha = entrevista.fecha_entrevista,
                Tema = entrevista.Temas.nombre_tema,
                Dificultad = entrevista.Dificultad.nombre_dificultad,
                Estado = entrevista.estado_entrevista
            };

            ViewBag.Title = $"Resultado — {entrevista.Temas.nombre_tema}";
            return View(model);
        }
        // En ResultadoController.cs — agregar este método
        public ActionResult Historial()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var resultados = _context.Entrevista
                .Include("Temas")
                .Include("Dificultad")
                .Include("Resultado")
                .Where(e => e.usuarios_id_usuarios == usuarioId
                         && e.estado_entrevista == "FINALIZADA")
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            return View(resultados);
        }
    }
}