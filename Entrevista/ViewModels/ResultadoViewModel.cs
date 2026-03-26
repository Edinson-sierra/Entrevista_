using System;
using System.Collections.Generic;
using System.Linq;

namespace Entrevista.ViewModels
{
    // ====================================================================
    // RESULTADO COMPLETO DE ENTREVISTA
    // ====================================================================

    /// <summary>
    /// ViewModel principal de la pantalla de resultados.
    /// Contiene el desglose por pregunta, métricas agregadas
    /// y la recomendación final generada por la IA.
    /// </summary>
    public class ResultadoViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Metadatos de la entrevista
        // ────────────────────────────────────────────────────────────

        /// <summary>ID de la entrevista para enlaces de navegación.</summary>
        public int EntrevistaId { get; set; }

        /// <summary>Fecha y hora en que se realizó la entrevista.</summary>
        public DateTime? Fecha { get; set; }

        /// <summary>Nombre del tema técnico evaluado.</summary>
        public string Tema { get; set; }

        /// <summary>Nivel de dificultad de la entrevista.</summary>
        public string Dificultad { get; set; }

        /// <summary>Estado final de la entrevista (FINALIZADA, INICIADA, etc.).</summary>
        public string Estado { get; set; }

        // ────────────────────────────────────────────────────────────
        // Métricas de rendimiento
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Promedio de puntajes de todas las preguntas (0.0 – 10.0).
        /// </summary>
        public double Promedio { get; set; }

        /// <summary>
        /// Nivel técnico estimado según el promedio.
        /// Calculado en ResultadoController usando NivelCalculator.
        /// </summary>
        public string Nivel { get; set; }

        /// <summary>
        /// Análisis final generado por la IA con fortalezas, debilidades y plan.
        /// Contiene HTML formateado para renderizar con @Html.Raw().
        /// </summary>
        public string RecomendacionFinal { get; set; }

        // ────────────────────────────────────────────────────────────
        // Desglose por pregunta
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista de preguntas con sus respuestas, puntajes y feedback.
        /// </summary>
        public List<ResultadoDetalleViewModel> Detalles { get; set; }
            = new List<ResultadoDetalleViewModel>();

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>Clase CSS del header según el nivel alcanzado.</summary>
        public string HeaderClass =>
            Nivel == "Senior" ? "senior" :
            Nivel == "Mid" ? "mid" :
                                "junior";

        /// <summary>Clase CSS del badge de nivel.</summary>
        public string NivelBadgeClass =>
            Nivel == "Senior" ? "sde-badge-green" :
            Nivel == "Mid" ? "sde-badge-yellow" :
                                "sde-badge-red";

        /// <summary>Clase CSS del círculo de score.</summary>
        public string ScoreCircleClass =>
            Promedio >= 8 ? "high" :
            Promedio >= 5 ? "medium" :
                            "low";

        /// <summary>Color CSS del puntaje.</summary>
        public string ScoreColor =>
            Promedio >= 8 ? "var(--sde-success)" :
            Promedio >= 5 ? "var(--sde-warning)" :
                            "var(--sde-danger)";

        /// <summary>Promedio formateado con un decimal.</summary>
        public string PromedioFormateado => Promedio.ToString("0.0");

        /// <summary>Total de preguntas respondidas.</summary>
        public int TotalPreguntas => Detalles?.Count ?? 0;

        /// <summary>Fecha formateada para mostrar en pantalla.</summary>
        public string FechaFormateada =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy HH:mm") : "—";

        /// <summary>
        /// Puntaje más alto obtenido en una pregunta individual.
        /// </summary>
        public int MejorPuntaje =>
            Detalles != null && Detalles.Any() ? Detalles.Max(d => d.Puntaje) : 0;

        /// <summary>
        /// Puntaje más bajo obtenido en una pregunta individual.
        /// </summary>
        public int PeorPuntaje =>
            Detalles != null && Detalles.Any() ? Detalles.Min(d => d.Puntaje) : 0;
    }

    // ====================================================================
    // DETALLE DE UNA PREGUNTA
    // ====================================================================

    /// <summary>
    /// Representa el resultado de una pregunta individual dentro de una entrevista.
    /// Incluye la pregunta, la respuesta del candidato, el puntaje y el feedback de la IA.
    /// </summary>
    public class ResultadoDetalleViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Datos del resultado
        // ────────────────────────────────────────────────────────────

        /// <summary>Texto de la pregunta realizada por la IA.</summary>
        public string Pregunta { get; set; }

        /// <summary>Texto de la respuesta enviada por el usuario.</summary>
        public string Respuesta { get; set; }

        /// <summary>
        /// Puntaje asignado por la IA a esta respuesta (0 – 10).
        /// </summary>
        public int Puntaje { get; set; }

        /// <summary>
        /// Feedback textual de la IA sobre la respuesta.
        /// Explica por qué se asignó ese puntaje y qué mejorar.
        /// </summary>
        public string Observacion { get; set; }

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>Clase CSS del badge de puntaje.</summary>
        public string PuntajeBadgeClass =>
            Puntaje >= 8 ? "sde-badge-green" :
            Puntaje >= 5 ? "sde-badge-yellow" :
                           "sde-badge-red";

        /// <summary>Color CSS para la barra de progreso del puntaje.</summary>
        public string PuntajeBarColor =>
            Puntaje >= 8 ? "var(--sde-success)" :
            Puntaje >= 5 ? "var(--sde-warning)" :
                           "var(--sde-danger)";

        /// <summary>Porcentaje del puntaje (0–100) para la barra visual.</summary>
        public int PuntajePorcentaje => Puntaje * 10;

        /// <summary>
        /// Indica si hay feedback disponible para mostrar.
        /// </summary>
        public bool TieneFeedback =>
            !string.IsNullOrWhiteSpace(Observacion);

        /// <summary>
        /// Indica si el usuario respondió esta pregunta.
        /// </summary>
        public bool TieneRespuesta =>
            !string.IsNullOrWhiteSpace(Respuesta);
    }
}