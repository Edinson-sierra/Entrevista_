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
        private const string SEP_PLAN = "\n\n---PLAN---\n\n";

        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        // =========================
        // GET: Chat
        // =========================
        public async Task<ActionResult> Chat(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var entrevista = CargarEntrevistaCompleta(id);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            if (entrevista.estado_entrevista == ESTADO_FINALIZADA)
                return RedirectToAction("Index", "Resultado", new { id });

            var puntajes = ObtenerPuntajesDeSesion(id);
            string dificultadBase = entrevista.Dificultad.nombre_dificultad;
            string dificultadActual = IAService.AdaptarDificultad(dificultadBase, puntajes);

            // Generar primera pregunta
            if (!entrevista.Preguntas.Any())
            {
                string primera = await _ia.GenerarPreguntaAsync(
                    entrevista.Temas.nombre_tema,
                    dificultadBase,
                    new List<MensajeViewModel>(),
                    puntajes
                );

                _context.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = primera,
                    entrevista_id_entrevista = entrevista.id_entrevista
                });

                _context.SaveChanges();
                entrevista = CargarEntrevistaCompleta(id);
            }

            var preguntaActiva = entrevista.Preguntas
                .OrderByDescending(preg => preg.id_pregunta)
                .First();

            bool yaRespondida = entrevista.Respuestas
                .Any(resp => resp.preguntas_id_pregunta == preguntaActiva.id_pregunta);

            if (yaRespondida)
            {
                int totalRespuestas = _context.Respuestas
                    .Count(resp => resp.entrevista_id_entrevista == entrevista.id_entrevista);

                if (totalRespuestas >= TOTAL_PREGUNTAS)
                {
                    entrevista = CargarEntrevistaCompleta(id);
                    return await FinalizarEntrevista(entrevista, puntajes);
                }

                var historialRec = ConstruirHistorialCompleto(entrevista);

                string sigPregunta = await _ia.GenerarPreguntaAsync(
                    entrevista.Temas.nombre_tema,
                    dificultadBase,
                    historialRec,
                    puntajes
                );

                _context.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = sigPregunta,
                    entrevista_id_entrevista = entrevista.id_entrevista
                });

                _context.SaveChanges();
                entrevista = CargarEntrevistaCompleta(id);

                preguntaActiva = entrevista.Preguntas
                    .OrderByDescending(preg => preg.id_pregunta)
                    .First();
            }

            var historialUI = ConstruirHistorialSinPreguntaActiva(
                entrevista, preguntaActiva.id_pregunta
            );

            int numeroPregunta = entrevista.Respuestas.Count;

            string ultimaEval = TempData["UltimaEvaluacion"] as string;
            string ultimaMejora = TempData["UltimaMejora"] as string;
            int ultimoPuntaje = TempData["UltimoPuntaje"] is int puntajeTemp ? puntajeTemp : -1;
            string ultimaCategoria = TempData["UltimaCategoria"] as string;

            var model = new EntrevistaViewModel
            {
                EntrevistaId = id,
                PreguntaActual = preguntaActiva.texto_pregunta,
                NumeroPregunta = numeroPregunta,
                Historial = historialUI,
                DificultadBase = dificultadBase,
                DificultadActual = dificultadActual,
                NivelActual = IAService.CalcularNivelActual(puntajes),
                PuntajesAcumulados = puntajes,
                UltimaEvaluacion = ultimaEval,
                UltimaMejora = ultimaMejora,
                UltimoPuntaje = ultimoPuntaje,
                UltimaCategoria = ultimaCategoria
            };

            ViewBag.Title = $"Pregunta {numeroPregunta + 1} de {TOTAL_PREGUNTAS}";
            return View(model);
        }

        // =========================
        // POST: Responder
        // =========================
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

            var preguntaActiva = entrevista.Preguntas
                .OrderByDescending(preg => preg.id_pregunta)
                .FirstOrDefault();

            if (preguntaActiva == null)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            bool yaRespondida = _context.Respuestas
                .Any(resp => resp.preguntas_id_pregunta == preguntaActiva.id_pregunta);

            if (yaRespondida)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            var puntajes = ObtenerPuntajesDeSesion(model.EntrevistaId);
            string dificultadBase = entrevista.Dificultad.nombre_dificultad;
            string dificultadActual = IAService.AdaptarDificultad(dificultadBase, puntajes);

            // Guardar respuesta
            _context.Respuestas.Add(new Respuestas
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                preguntas_id_pregunta = preguntaActiva.id_pregunta,
                respuesta_usuario = model.RespuestaUsuario.Trim(),
                fecha_respuesta = DateTime.Now
            });

            _context.SaveChanges();

            // Evaluación IA
            var evaluacion = await _ia.EvaluarRespuestaAsync(
                preguntaActiva.texto_pregunta,
                model.RespuestaUsuario.Trim(),
                dificultadActual
            );

            string observaciones =
                $"{evaluacion.Feedback}|NIVEL:{evaluacion.Nivel}|CAT:{evaluacion.Categoria}|MEJORA:{evaluacion.Mejora}";

            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = evaluacion.Puntaje,
                observaciones = observaciones,
                fecha_resultado = DateTime.Now
            });

            _context.SaveChanges();

            puntajes.Add(evaluacion.Puntaje);
            GuardarPuntajesEnSesion(model.EntrevistaId, puntajes);

            TempData["UltimaEvaluacion"] = evaluacion.Feedback;
            TempData["UltimaMejora"] = evaluacion.Mejora;
            TempData["UltimoPuntaje"] = evaluacion.Puntaje;
            TempData["UltimaCategoria"] = evaluacion.Categoria;

            int total = _context.Respuestas
                .Count(resp => resp.entrevista_id_entrevista == entrevista.id_entrevista);

            if (total >= TOTAL_PREGUNTAS)
            {
                entrevista = CargarEntrevistaCompleta(model.EntrevistaId);
                return await FinalizarEntrevista(entrevista, puntajes);
            }

            entrevista = CargarEntrevistaCompleta(model.EntrevistaId);
            var historial = ConstruirHistorialCompleto(entrevista);

            string nuevaPregunta = await _ia.GenerarPreguntaAsync(
                entrevista.Temas.nombre_tema,
                dificultadBase,
                historial,
                puntajes
            );

            _context.Preguntas.Add(new Preguntas
            {
                texto_pregunta = nuevaPregunta,
                entrevista_id_entrevista = entrevista.id_entrevista
            });

            _context.SaveChanges();

            return RedirectToAction("Chat", new { id = model.EntrevistaId });
        }

        // =========================
        // FINALIZAR
        // =========================
        private async Task<ActionResult> FinalizarEntrevista(
            Entrevista_DATA.Entrevista entrevista,
            List<int> puntajes)
        {
            var historial = ConstruirHistorialCompleto(entrevista);

            string analisis = await _ia.GenerarResultadoFinalAsync(historial);
            string plan = await _ia.GenerarPlanAsync(analisis);

            var resultados = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevista.id_entrevista)
                .OrderBy(r => r.id_resultado)
                .Take(TOTAL_PREGUNTAS)
                .ToList();

            double promedio = resultados.Any()
                ? Math.Round(resultados.Average(r => (double)(r.puntaje_total ?? 0)), 1)
                : 0.0;

            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = (int)Math.Round(promedio),
                observaciones = analisis + SEP_PLAN + plan,
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

            LimpiarPuntajesDeSesion(entrevista.id_entrevista);

            return RedirectToAction("Index", "Resultado", new { id = entrevista.id_entrevista });
        }

        // =========================
        // HELPERS
        // =========================
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

        private List<MensajeViewModel> ConstruirHistorialCompleto(Entrevista_DATA.Entrevista entrevista)
        {
            var hist = new List<MensajeViewModel>();

            foreach (var preg in entrevista.Preguntas.OrderBy(x => x.id_pregunta))
            {
                hist.Add(new MensajeViewModel { Tipo = MensajeViewModel.TIPO_IA, Texto = preg.texto_pregunta });

                var resp = entrevista.Respuestas
                    .FirstOrDefault(x => x.preguntas_id_pregunta == preg.id_pregunta);

                if (resp != null)
                    hist.Add(new MensajeViewModel { Tipo = MensajeViewModel.TIPO_USUARIO, Texto = resp.respuesta_usuario });
            }

            return hist;
        }

        private List<MensajeViewModel> ConstruirHistorialSinPreguntaActiva(
            Entrevista_DATA.Entrevista entrevista,
            int idPreguntaActiva)
        {
            var hist = new List<MensajeViewModel>();

            foreach (var preg in entrevista.Preguntas
                .Where(x => x.id_pregunta != idPreguntaActiva)
                .OrderBy(x => x.id_pregunta))
            {
                hist.Add(new MensajeViewModel { Tipo = MensajeViewModel.TIPO_IA, Texto = preg.texto_pregunta });

                var resp = entrevista.Respuestas
                    .FirstOrDefault(x => x.preguntas_id_pregunta == preg.id_pregunta);

                if (resp != null)
                    hist.Add(new MensajeViewModel { Tipo = MensajeViewModel.TIPO_USUARIO, Texto = resp.respuesta_usuario });
            }

            return hist;
        }

        private static string ClavePuntajes(int id) => $"puntajes_{id}";

        private List<int> ObtenerPuntajesDeSesion(int id)
        {
            var raw = Session[ClavePuntajes(id)] as string;

            if (string.IsNullOrEmpty(raw))
                return new List<int>();

            return raw.Split(',')
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }

        private void GuardarPuntajesEnSesion(int id, List<int> puntajes)
        {
            Session[ClavePuntajes(id)] = string.Join(",", puntajes);
        }

        private void LimpiarPuntajesDeSesion(int id)
        {
            Session.Remove(ClavePuntajes(id));
        }
    }
}