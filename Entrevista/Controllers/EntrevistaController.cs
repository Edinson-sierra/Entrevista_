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
    /// <summary>
    /// Controlador del flujo principal de entrevista técnica con IA.
    ///
    /// Flujo de una entrevista:
    ///   1. Chat()     → Muestra la pregunta actual y el historial
    ///   2. Responder() → Guarda respuesta, evalúa con IA, genera siguiente pregunta
    ///   3. Al llegar a 5 respuestas → genera resultado final y plan → redirige a Resultado
    ///
    /// Rutas expuestas:
    ///   GET  /Entrevista/Chat/{id}
    ///   POST /Entrevista/Responder
    /// </summary>
    [AuthFilter]
    public class EntrevistaController : Controller
    {
        // ────────────────────────────────────────────────────────────
        // Constantes de negocio
        // ────────────────────────────────────────────────────────────

        /// <summary>Número total de preguntas por entrevista.</summary>
        private const int TOTAL_PREGUNTAS = 5;

        /// <summary>Estado de entrevista al finalizarse.</summary>
        private const string ESTADO_FINALIZADA = "FINALIZADA";

        // ────────────────────────────────────────────────────────────
        // Dependencias
        // ────────────────────────────────────────────────────────────

        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        // ====================================================================
        // GET: /Entrevista/Chat/{id}
        // ====================================================================

        /// <summary>
        /// Muestra la pantalla de chat de entrevista.
        ///
        /// Si la entrevista no tiene preguntas aún (primer acceso),
        /// genera la primera pregunta con la IA antes de mostrar la vista.
        ///
        /// Validaciones:
        ///   - La entrevista debe existir en BD
        ///   - La entrevista debe pertenecer al usuario autenticado
        ///   - La entrevista no debe estar FINALIZADA
        /// </summary>
        /// <param name="id">ID de la entrevista activa.</param>
        public async Task<ActionResult> Chat(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // ── Cargar entrevista con todas sus relaciones necesarias ─────────
            var entrevista = CargarEntrevistaCompleta(id);

            // ── Validaciones de seguridad ────────────────────────────────────
            if (entrevista == null)
                return RedirectToAction("Index", "Home");

            // Impedir acceso a entrevistas de otros usuarios
            if (entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            // Impedir continuar una entrevista ya finalizada
            if (entrevista.estado_entrevista == ESTADO_FINALIZADA)
                return RedirectToAction("Index", "Resultado", new { id });

            // ── Generar primera pregunta si la entrevista es nueva ───────────
            if (!entrevista.Preguntas.Any())
            {
                var historialVacio = new List<MensajeViewModel>();

                string primeraPregunta = await _ia.GenerarPreguntaAsync(
                    entrevista.Temas.nombre_tema,
                    entrevista.Dificultad.nombre_dificultad,
                    historialVacio
                );

                entrevista.Preguntas.Add(new Preguntas
                {
                    texto_pregunta = primeraPregunta,
                    entrevista_id_entrevista = entrevista.id_entrevista
                });

                _context.SaveChanges();
            }

            // ── Obtener la pregunta más reciente ─────────────────────────────
            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .First();

            // ── Construir historial de conversación ──────────────────────────
            var historial = ConstruirHistorial(entrevista);

            // ── Contar respuestas ya enviadas (progreso) ──────────────────────
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

        // ====================================================================
        // POST: /Entrevista/Responder
        // ====================================================================

        /// <summary>
        /// Procesa la respuesta enviada por el usuario.
        ///
        /// Flujo interno:
        ///   1. Validar modelo
        ///   2. Guardar respuesta en BD
        ///   3. Evaluar respuesta con IA (puntaje + feedback)
        ///   4. Guardar resultado de evaluación en BD
        ///   5a. Si se completaron las 5 preguntas → finalizar entrevista
        ///   5b. Si quedan preguntas → generar siguiente pregunta y redirigir al chat
        /// </summary>
        /// <param name="model">ViewModel con EntrevistaId y RespuestaUsuario.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Responder(EntrevistaViewModel model)
        {
            // ── Validar modelo (incluye IValidatableObject) ──────────────────
            if (!ModelState.IsValid)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            // ── Cargar entrevista ────────────────────────────────────────────
            var entrevista = CargarEntrevistaCompleta(model.EntrevistaId);

            if (entrevista == null || entrevista.usuarios_id_usuarios != usuarioId)
                return RedirectToAction("Index", "Home");

            if (entrevista.estado_entrevista == ESTADO_FINALIZADA)
                return RedirectToAction("Index", "Resultado", new { id = model.EntrevistaId });

            // ── Obtener la pregunta actual (la más reciente sin respuesta) ───
            var ultimaPregunta = entrevista.Preguntas
                .OrderByDescending(p => p.id_pregunta)
                .FirstOrDefault();

            if (ultimaPregunta == null)
                return RedirectToAction("Chat", new { id = model.EntrevistaId });

            // ── 1. Guardar respuesta del usuario ─────────────────────────────
            var respuesta = new Respuestas
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                preguntas_id_pregunta = ultimaPregunta.id_pregunta,
                respuesta_usuario = model.RespuestaUsuario.Trim(),
                fecha_respuesta = DateTime.Now
            };

            entrevista.Respuestas.Add(respuesta);
            _context.SaveChanges();

            // ── 2. Evaluar respuesta con IA ──────────────────────────────────
            var (puntaje, feedback) = await _ia.EvaluarRespuestaAsync(
                ultimaPregunta.texto_pregunta,
                model.RespuestaUsuario,
                entrevista.Dificultad.nombre_dificultad
            );

            // ── 3. Guardar resultado de esta pregunta ────────────────────────
            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = puntaje,
                observaciones = feedback,
                fecha_resultado = DateTime.Now
            });

            _context.SaveChanges();

            // ── 4. Verificar si se completaron todas las preguntas ───────────
            int totalRespuestas = entrevista.Respuestas.Count;

            if (totalRespuestas >= TOTAL_PREGUNTAS)
            {
                return await FinalizarEntrevista(entrevista);
            }

            // ── 5. Generar siguiente pregunta ────────────────────────────────
            // Recargar entrevista para incluir la respuesta recién guardada
            entrevista = CargarEntrevistaCompleta(model.EntrevistaId);
            var historialActualizado = ConstruirHistorial(entrevista);

            string nuevaPregunta = await _ia.GenerarPreguntaAsync(
                entrevista.Temas.nombre_tema,
                entrevista.Dificultad.nombre_dificultad,
                historialActualizado
            );

            entrevista.Preguntas.Add(new Preguntas
            {
                texto_pregunta = nuevaPregunta,
                entrevista_id_entrevista = entrevista.id_entrevista
            });

            _context.SaveChanges();

            return RedirectToAction("Chat", new { id = model.EntrevistaId });
        }

        // ====================================================================
        // MÉTODOS PRIVADOS
        // ====================================================================

        /// <summary>
        /// Finaliza la entrevista: genera el análisis global con IA,
        /// genera el plan de entrenamiento, persiste el resumen final
        /// y marca la entrevista como FINALIZADA.
        /// </summary>
        /// <param name="entrevista">Entidad de entrevista cargada con todas sus relaciones.</param>
        /// <returns>Redirección a la pantalla de resultados.</returns>
        private async Task<ActionResult> FinalizarEntrevista(Entrevista_DATA.Entrevista entrevista)
        {
            // Construir historial completo para el análisis final
            var historialFinal = ConstruirHistorial(entrevista);

            // ── Análisis global de la entrevista ─────────────────────────────
            string resultadoFinal = await _ia.GenerarResultadoFinalAsync(historialFinal);

            // ── Plan de entrenamiento personalizado ──────────────────────────
            string plan = await _ia.GenerarPlanAsync(resultadoFinal);

            // ── Calcular promedio final de todos los resultados ───────────────
            var todosLosResultados = _context.Resultado
                .Where(r => r.entrevista_id_entrevista == entrevista.id_entrevista)
                .ToList();

            double promedioFinal = todosLosResultados.Any()
                ? todosLosResultados.Average(r => r.puntaje_total ?? 0)
                : 0.0;

            // ── Guardar resumen final en Resultado ───────────────────────────
            // Este registro es el "resultado maestro" — tiene el análisis completo
            _context.Resultado.Add(new Resultado
            {
                entrevista_id_entrevista = entrevista.id_entrevista,
                puntaje_total = (int)Math.Round(promedioFinal),
                observaciones = resultadoFinal + "\n\n---PLAN---\n\n" + plan,
                fecha_resultado = DateTime.Now
            });

            // ── Guardar plan en Planes_entrenamiento ─────────────────────────
            int usuarioId = entrevista.usuarios_id_usuarios;

            _context.Planes_entrenamiento.Add(new Planes_entrenamiento
            {
                usuarios_id_usuarios = usuarioId,
                recomendacion = plan,
                fecha_plan = DateTime.Now
            });

            // ── Marcar entrevista como finalizada ────────────────────────────
            entrevista.estado_entrevista = ESTADO_FINALIZADA;

            _context.SaveChanges();

            return RedirectToAction("Index", "Resultado", new { id = entrevista.id_entrevista });
        }

        /// <summary>
        /// Carga una entrevista desde la BD incluyendo todas las entidades
        /// de navegación necesarias: Preguntas, Respuestas, Resultado, Temas, Dificultad.
        /// </summary>
        /// <param name="entrevistaId">ID de la entrevista a cargar.</param>
        /// <returns>Entidad Entrevista completa, o null si no existe.</returns>
        private Entrevista_DATA.Entrevista CargarEntrevistaCompleta(int entrevistaId)
        {
            return _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == entrevistaId);
        }

        /// <summary>
        /// Construye el historial de conversación de la entrevista
        /// intercalando preguntas (tipo "IA") y respuestas (tipo "Usuario")
        /// en orden cronológico.
        /// </summary>
        /// <param name="entrevista">Entidad con Preguntas y Respuestas cargadas.</param>
        /// <returns>Lista de mensajes ordenados para renderizar el chat.</returns>
        private List<MensajeViewModel> ConstruirHistorial(Entrevista_DATA.Entrevista entrevista)
        {
            var historial = new List<MensajeViewModel>();

            // Iterar preguntas en orden de creación
            foreach (var pregunta in entrevista.Preguntas.OrderBy(p => p.id_pregunta))
            {
                // Añadir pregunta de la IA
                historial.Add(new MensajeViewModel
                {
                    Tipo = MensajeViewModel.TIPO_IA,
                    Texto = pregunta.texto_pregunta
                });

                // Añadir respuesta del usuario si existe para esta pregunta
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