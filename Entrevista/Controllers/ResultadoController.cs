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
        private readonly SDEEntities _context = new SDEEntities();

        // ============================================================
        // RESULTADO INDIVIDUAL
        // ============================================================
        public ActionResult Index(int? id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // 🚨 SI NO HAY ID → IR A HISTORIAL
            if (id == null)
                return RedirectToAction("Historial");

            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Historial");

            // ─────────────────────────────────────────────
            // DATOS
            // ─────────────────────────────────────────────
            var preguntas = entrevista.Preguntas
                .OrderBy(p => p.id_pregunta)
                .ToList();

            var resultados = entrevista.Resultado
                .OrderBy(r => r.id_resultado)
                .ToList();

            var detalles = preguntas.Select((pregunta, index) =>
            {
                var respuesta = entrevista.Respuestas
                    .FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);

                var resultado = index < resultados.Count ? resultados[index] : null;

                return new ResultadoDetalleViewModel
                {
                    Pregunta = pregunta.texto_pregunta,
                    Respuesta = respuesta?.respuesta_usuario,
                    Puntaje = resultado?.puntaje_total ?? 0,
                    Observacion = resultado?.observaciones
                };
            }).ToList();

            double promedio = detalles.Any()
                ? Math.Round(detalles.Average(d => d.Puntaje), 1)
                : 0.0;

            string nivel = NivelCalculator.Calcular(promedio);

            var ultimoResultado = resultados.LastOrDefault();
            string recomendacion = ultimoResultado?.observaciones;

            if (!string.IsNullOrEmpty(recomendacion) && recomendacion.Contains("---PLAN---"))
            {
                recomendacion = recomendacion
                    .Split(new[] { "---PLAN---" }, StringSplitOptions.None)[0]
                    .Trim();
            }

            var model = new ResultadoViewModel
            {
                EntrevistaId = id.Value,
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

        // ============================================================
        // HISTORIAL
        // ============================================================
        public ActionResult Historial()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Include("Temas")
                .Include("Dificultad")
                .Include("Resultado")
                .Where(e => e.usuarios_id_usuarios == usuarioId
                         && e.estado_entrevista == "FINALIZADA")
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            var model = entrevistas.Select(e =>
            {
                double promedio = e.Resultado.Any()
                    ? Math.Round(e.Resultado.Average(r => (double?)r.puntaje_total ?? 0), 1)
                    : 0;

                return new ResultadoViewModel
                {
                    EntrevistaId = e.id_entrevista,
                    Tema = e.Temas.nombre_tema,
                    Dificultad = e.Dificultad.nombre_dificultad,
                    Fecha = e.fecha_entrevista,
                    Promedio = promedio,
                    Nivel = NivelCalculator.Calcular(promedio)
                };
            }).ToList();

            return View(model);
        }
    }
}