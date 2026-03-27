using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    [AuthFilter]
    public class ResultadoController : Controller
    {
        private const int TOTAL_PREGUNTAS = 5;
        private const string SEP_PLAN = "---PLAN---";

        private const string SEP_NIVEL = "|NIVEL:";
        private const string SEP_CAT = "|CAT:";
        private const string SEP_MEJORA = "|MEJORA:";

        private readonly SDEEntities _context = new SDEEntities();

        public ActionResult Index(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            // ─────────────────────────────────────────────
            // Orden base
            // ─────────────────────────────────────────────
            var preguntas = entrevista.Preguntas
                .OrderBy(p => p.id_pregunta)
                .ToList();

            var resultadosOrdenados = entrevista.Resultado
                .OrderBy(r => r.id_resultado)
                .ToList();

            // ─────────────────────────────────────────────
            // Separar resultados reales vs maestro
            // ─────────────────────────────────────────────
            var resultadosPorPregunta = resultadosOrdenados
                .Where(r => r.observaciones != null && r.observaciones.Contains(SEP_NIVEL))
                .Take(TOTAL_PREGUNTAS)
                .ToList();

            var resultadoMaestro = resultadosOrdenados
                .FirstOrDefault(r => r.observaciones != null && r.observaciones.Contains(SEP_PLAN));

            // ─────────────────────────────────────────────
            // Construir detalles robustos
            // ─────────────────────────────────────────────
            var detalles = preguntas.Select(pregunta =>
            {
                var respuesta = entrevista.Respuestas
                    .FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);

                var resultado = resultadosPorPregunta
                    .ElementAtOrDefault(detallesIndexSafe(resultadosPorPregunta, pregunta.id_pregunta));

                var (feedback, nivel, categoria, mejora) =
                    ParsearObservaciones(resultado?.observaciones);

                return new ResultadoDetalleViewModel
                {
                    Pregunta = pregunta.texto_pregunta,
                    Respuesta = respuesta?.respuesta_usuario,
                    Puntaje = resultado?.puntaje_total ?? 0,
                    Observacion = feedback,
                    Nivel = nivel,
                    Categoria = categoria,
                    Mejora = mejora
                };
            }).ToList();

            // ─────────────────────────────────────────────
            // Promedio
            // ─────────────────────────────────────────────
            double promedio = resultadosPorPregunta.Any()
                ? Math.Round(resultadosPorPregunta.Average(r => (double)(r.puntaje_total ?? 0)), 1)
                : 0.0;

            string nivelFinal = NivelCalculator.Calcular(promedio);

            // ─────────────────────────────────────────────
            // Recomendación final
            // ─────────────────────────────────────────────
            string recomendacion = null;

            if (resultadoMaestro != null && !string.IsNullOrWhiteSpace(resultadoMaestro.observaciones))
            {
                var obs = resultadoMaestro.observaciones;

                if (obs.Contains(SEP_PLAN))
                {
                    recomendacion = obs
                        .Split(new[] { SEP_PLAN }, StringSplitOptions.None)[0]
                        .Trim();
                }
                else
                {
                    recomendacion = obs.Trim();
                }
            }

            var model = new ResultadoViewModel
            {
                EntrevistaId = id,
                Detalles = detalles,
                Promedio = promedio,
                Nivel = nivelFinal,
                RecomendacionFinal = recomendacion,
                Fecha = entrevista.fecha_entrevista,
                Tema = entrevista.Temas.nombre_tema,
                Dificultad = entrevista.Dificultad.nombre_dificultad,
                Estado = entrevista.estado_entrevista
            };

            ViewBag.Title = $"Resultado — {entrevista.Temas.nombre_tema}";
            return View(model);
        }

        // ─────────────────────────────────────────────
        // Helpers seguros
        // ─────────────────────────────────────────────

        private static int detallesIndexSafe(System.Collections.Generic.List<Resultado> lista, int idPregunta)
        {
            if (lista == null || !lista.Any())
                return -1;

            // fallback simple: usar índice secuencial
            return Math.Min(lista.Count - 1, idPregunta - 1);
        }

        private static (string feedback, string nivel, string categoria, string mejora)
            ParsearObservaciones(string observaciones)
        {
            if (string.IsNullOrWhiteSpace(observaciones))
                return ("Sin evaluación disponible.", "", "", "");

            if (!observaciones.Contains(SEP_NIVEL))
                return (observaciones.Trim(), "", "", "");

            try
            {
                int posNivel = observaciones.IndexOf(SEP_NIVEL);
                if (posNivel < 0)
                    return (observaciones.Trim(), "", "", "");

                string feedback = observaciones.Substring(0, posNivel).Trim();

                string nivel = ExtraerCampo(observaciones, SEP_NIVEL, SEP_CAT);
                string categoria = ExtraerCampo(observaciones, SEP_CAT, SEP_MEJORA);
                string mejora = ExtraerCampoFinal(observaciones, SEP_MEJORA);

                return (feedback, nivel, categoria, mejora);
            }
            catch
            {
                return (observaciones.Trim(), "", "", "");
            }
        }

        private static string ExtraerCampo(string texto, string sepInicio, string sepFin)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            int ini = texto.IndexOf(sepInicio);
            if (ini < 0) return "";

            ini += sepInicio.Length;

            int fin = texto.IndexOf(sepFin, ini);
            if (fin < 0) return texto.Substring(ini).Trim();

            return texto.Substring(ini, fin - ini).Trim();
        }

        private static string ExtraerCampoFinal(string texto, string sepInicio)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            int ini = texto.IndexOf(sepInicio);
            if (ini < 0) return "";

            ini += sepInicio.Length;
            return texto.Substring(ini).Trim();
        }
    }
}