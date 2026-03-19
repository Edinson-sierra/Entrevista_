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

        public async Task<ActionResult> Chat(int id)
        {
            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .FirstOrDefault(e => e.id_entrevista == id);

            if (entrevista == null)
                return RedirectToAction("Index", "Simulacion");

            if (!entrevista.Preguntas.Any())
            {
                string preguntaIA = await _ia.PreguntarAsync("Haz la primera pregunta técnica");

                entrevista.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = preguntaIA
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
                NumeroPregunta = entrevista.Respuestas.Count
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Responder(EntrevistaViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .FirstOrDefault(e => e.id_entrevista == model.EntrevistaId);

            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .FirstOrDefault();

            // Guardar respuesta
            entrevista.Respuestas.Add(new Respuestas
            {
                preguntas_id_pregunta = ultimaPregunta.id_pregunta,
                respuesta_usuario = model.RespuestaUsuario
            });

            _context.SaveChanges();

            int totalRespuestas = entrevista.Respuestas.Count;

            if (totalRespuestas >= 5)
                return RedirectToAction("Index", "Resultado", new { id = model.EntrevistaId });

            // Generar siguiente pregunta
            string nuevaPregunta = await _ia.PreguntarAsync(
                $"Basado en esta respuesta genera otra pregunta: {model.RespuestaUsuario}"
            );

            entrevista.Preguntas.Add(new Preguntas
            {
                texto_pregunta = nuevaPregunta
            });

            _context.SaveChanges();

            return RedirectToAction("Chat", new { id = model.EntrevistaId });
        }
    }
}