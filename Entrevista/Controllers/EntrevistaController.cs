using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    [AuthFilter]
    public class EntrevistaController : Controller
    {
        private const int TOTAL_PREGUNTAS = 5;
        private const string ESTADO_FINALIZADA = "FINALIZADA";

        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        // =====================================================
        // GET: Chat
        // =====================================================
        public async Task<ActionResult> Chat(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var entrevista = CargarEntrevistaCompleta(id);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            if (entrevista.estado_entrevista == ESTADO_FINALIZADA)
                return RedirectToAction("Index", "Resultado", new { id });

            // 🔥 PRIMERA PREGUNTA (ADAPTATIVA)
            if (!entrevista.Preguntas.Any())
            {
                string primeraPregunta = await _ia.GenerarPreguntaAsync(
                    entrevista.Temas.nombre_tema,
                    new List<MensajeViewModel>(),
                    new List<int>()
                );

                entrevista.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = primeraPregunta,
                    entrevista_id_entrevista = entrevista.id_entrevista
                });

                _context.SaveChanges();
            }

            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .First();

            var historial = ConstruirHistorial(entrevista);

            int numeroPregunta = entrevista.Respuestas.Count;

            var model = new EntrevistaViewModel
            {
                EntrevistaId = id,
                PreguntaActual = ultimaPregunta.texto_pregunta,
                NumeroPregunta = numeroPregunta,
                Historial = historial
            };

            ViewBag.Title = $"Entrevista — Pregunta {numeroPregunta + 1}/{TOTAL_PREGUNTAS}";
            return View(model);
        }

        // =====================================================
        // POST: Responder
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Responder(EntrevistaViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var entrevista = CargarEntrevistaCompleta(model.EntrevistaId);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            if (entrevista.estado_entrevista == ESTADO_FINALIZADA)
                return RedirectToAction("Index", "Resultado", new { id = model.EntrevistaId });

            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .FirstOrDefault();

            if (ultimaPregunta == null)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            // 🧠 GUARDAR RESPUESTA
            var respuesta = new Respuestas
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                preguntas_id_pregunta = ultimaPregunta.id_pregunta,
                respuesta_usuario = model.RespuestaUsuario.Trim(),
                fecha_respuesta = DateTime.Now
            };

            entrevista.Respuestas.Add(respuesta);
            _context.SaveChanges();

            // 🔥 EVALUACIÓN PRO
            var (puntaje, feedback, nivel, categoria) = await _ia.EvaluarRespuestaAsync(
                ultimaPregunta.texto_pregunta,
                model.RespuestaUsuario
            );

            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = puntaje,
                observaciones = $"Nivel: {nivel}\nCategoría: {categoria}\nFeedback: {feedback}",
                fecha_resultado = DateTime.Now
            });

            _context.SaveChanges();

            int totalRespuestas = entrevista.Respuestas.Count;

            // 🔥 TERMINAR ENTREVISTA
            if (totalRespuestas >= TOTAL_PREGUNTAS)
            {
                return await FinalizarEntrevista(entrevista);
            }

            // 🔥 DIFICULTAD DINÁMICA
            var puntajes = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevista.id_entrevista)
                .OrderBy(r => r.fecha_resultado)
                .Select(r => r.puntaje_total ?? 5)
                .ToList();

            string dificultad = CalcularDificultad(puntajes);

            // 🔥 SIGUIENTE PREGUNTA ADAPTATIVA
            entrevista = CargarEntrevistaCompleta(model.EntrevistaId);
            var historial = ConstruirHistorial(entrevista);

            string nuevaPregunta = await _ia.GenerarPreguntaAsync(
                entrevista.Temas.nombre_tema,
                historial,
                puntajes
            );

            entrevista.Preguntas.Add(new Preguntas
            {
                texto_pregunta = nuevaPregunta,
                entrevista_id_entrevista = entrevista.id_entrevista
            });

            _context.SaveChanges();

            return RedirectToAction("Chat", new { id = model.EntrevistaId });
        }

        // =====================================================
        // FINALIZAR ENTREVISTA
        // =====================================================
        private async Task<ActionResult> FinalizarEntrevista(Entrevista_DATA.Entrevista entrevista)
        {
            var historialFinal = ConstruirHistorial(entrevista);

            string resultadoFinal = await _ia.GenerarResultadoFinalAsync(historialFinal);
            string plan = await _ia.GenerarPlanAsync(resultadoFinal);

            var resultados = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevista.id_entrevista)
                .ToList();

            double promedio = resultados.Any()
                ? resultados.Average(r => r.puntaje_total ?? 0)
                : 0;

            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = (int)Math.Round(promedio),
                observaciones = resultadoFinal + "\n\n---PLAN---\n\n" + plan,
                fecha_resultado = DateTime.Now
            });

            _context.Planes_entrenamiento.Add(new Planes_entrenamiento
            {
                usuarios_id_usuarios = entrevista.usuarios_id_usuarios,
                recomendacion = plan,
                fecha_plan = DateTime.Now
            });

            entrevista.estado_entrevista = ESTADO_FINALIZADA;

            _context.SaveChanges();

            return RedirectToAction("Index", "Resultado", new { id = entrevista.id_entrevista });
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private string CalcularDificultad(List<int> puntajes)
        {
            if (!puntajes.Any()) return "basico";

            double promedio = puntajes.Average();

            if (promedio < 4) return "basico";
            if (promedio < 7) return "intermedio";
            return "avanzado";
        }

        private Entrevista_DATA.Entrevista CargarEntrevistaCompleta(int id)
        {
            return _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);
        }

        private List<MensajeViewModel> ConstruirHistorial(Entrevista_DATA.Entrevista entrevista)
        {
            var historial = new List<MensajeViewModel>();

            var preguntasOrdenadas = entrevista.Preguntas
                .OrderBy(p => p.id_pregunta)
                .ToList();

            // ❌ QUITAR LA ÚLTIMA PREGUNTA (porque se muestra aparte)
            if (preguntasOrdenadas.Any())
                preguntasOrdenadas.RemoveAt(preguntasOrdenadas.Count - 1);

            foreach (var pregunta in preguntasOrdenadas)
            {
                historial.Add(new MensajeViewModel
                {
                    Tipo = MensajeViewModel.TIPO_IA,
                    Texto = pregunta.texto_pregunta
                });

                var respuesta = entrevista.Respuestas
                    .FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);

                if (respuesta != null)
                {
                    historial.Add(new MensajeViewModel
                    {
                        Tipo = MensajeViewModel.TIPO_USUARIO,
                        Texto = respuesta.respuesta_usuario
                    });
                }
            }

            return historial;
        }
    }
}