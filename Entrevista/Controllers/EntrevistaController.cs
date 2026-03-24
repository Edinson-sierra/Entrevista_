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
    public class EntrevistaController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        // ===============================
        // CHAT
        // ===============================
        public async Task<ActionResult> Chat(int id)
        {
            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);

            if (entrevista == null)
                return RedirectToAction("Index", "Simulacion");

            var historial = ConstruirHistorial(entrevista);

            // 🔥 PRIMERA PREGUNTA
            if (!entrevista.Preguntas.Any())
            {
                string pregunta = await _ia.GenerarPreguntaAsync(
                    entrevista.Temas.nombre_tema,
                    entrevista.Dificultad.nombre_dificultad,
                    historial
                );

                entrevista.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = pregunta
                });

                _context.SaveChanges();
            }

            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .FirstOrDefault();

            var model = new EntrevistaViewModel
            {
                EntrevistaId = id,
                PreguntaActual = ultimaPregunta.texto_pregunta,
                NumeroPregunta = entrevista.Respuestas.Count,
                Historial = historial
            };

            return View(model);
        }

        // ===============================
        // RESPONDER
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Responder(EntrevistaViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == model.EntrevistaId);

            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .FirstOrDefault();

            // ===============================
            // GUARDAR RESPUESTA
            // ===============================
            var respuesta = new Respuestas
            {
                preguntas_id_pregunta = ultimaPregunta.id_pregunta,
                respuesta_usuario = model.RespuestaUsuario
            };

            entrevista.Respuestas.Add(respuesta);
            _context.SaveChanges();

            // ===============================
            // EVALUAR RESPUESTA
            // ===============================
            var evaluacion = await _ia.EvaluarRespuestaAsync(
                ultimaPregunta.texto_pregunta,
                model.RespuestaUsuario,
                entrevista.Dificultad.nombre_dificultad
            );

            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = evaluacion.puntaje,
                observaciones = evaluacion.feedback
            });

            _context.SaveChanges();

            int totalRespuestas = entrevista.Respuestas.Count;

            // ===============================
            // FINALIZAR ENTREVISTA
            // ===============================
            if (totalRespuestas >= 5)
            {
                var historial = ConstruirHistorial(entrevista);

                var resultadoFinal = await _ia.GenerarResultadoFinalAsync(historial);

                var plan = await _ia.GenerarPlanAsync(resultadoFinal);

                // Guardar resumen final
                _context.Resultado.Add(new Resultado
                {
                    entrevista_id_entrevista = entrevista.id_entrevista,
                    puntaje_total = (int?)_context.Resultado
                        .Where(r => r.entrevista_id_entrevista == entrevista.id_entrevista)
                        .Average(r => r.puntaje_total),

                    observaciones = resultadoFinal + "\n\nPLAN:\n" + plan
                });

                entrevista.estado_entrevista = "FINALIZADA";

                _context.SaveChanges();

                return RedirectToAction("Index", "Resultado", new { id = model.EntrevistaId });
            }

            // ===============================
            // SIGUIENTE PREGUNTA
            // ===============================
            var historialNext = ConstruirHistorial(entrevista);

            string nuevaPregunta = await _ia.GenerarPreguntaAsync(
                entrevista.Temas.nombre_tema,
                entrevista.Dificultad.nombre_dificultad,
                historialNext
            );

            entrevista.Preguntas.Add(new Preguntas
            {
                texto_pregunta = nuevaPregunta
            });

            _context.SaveChanges();

            return RedirectToAction("Chat", new { id = model.EntrevistaId });
        }

        // ===============================
        // HISTORIAL
        // ===============================
        private List<MensajeViewModel> ConstruirHistorial(Entrevista_DATA.Entrevista entrevista)
        {
            var historial = new List<MensajeViewModel>();

            foreach (var p in entrevista.Preguntas.OrderBy(x => x.id_pregunta))
            {
                historial.Add(new MensajeViewModel
                {
                    Tipo = "IA",
                    Texto = p.texto_pregunta
                });

                var r = entrevista.Respuestas
                    .FirstOrDefault(x => x.preguntas_id_pregunta == p.id_pregunta);

                if (r != null)
                {
                    historial.Add(new MensajeViewModel
                    {
                        Tipo = "Usuario",
                        Texto = r.respuesta_usuario
                    });
                }
            }

            return historial;
        }
    }
}