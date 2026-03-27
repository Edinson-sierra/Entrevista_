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
    /// Contiene el desglose detallado por pregunta con evaluación extendida,
    /// métricas agregadas y el análisis final generado por la IA.
    /// </summary>
    public class ResultadoViewModel
    {
        // ── Metadatos de la entrevista ────────────────────────────────────

        public int EntrevistaId { get; set; }
        public DateTime? Fecha { get; set; }
        public string Tema { get; set; }
        public string Dificultad { get; set; }
        public string Estado { get; set; }

        // ── Métricas de rendimiento ───────────────────────────────────────

        /// <summary>Promedio de puntajes de las 5 preguntas (0.0 – 10.0).</summary>
        public double Promedio { get; set; }

        /// <summary>Nivel técnico estimado por NivelCalculator.</summary>
        public string Nivel { get; set; }

        /// <summary>Análisis final HTML generado por la IA.</summary>
        public string RecomendacionFinal { get; set; }

        // ── Desglose por pregunta ─────────────────────────────────────────

        public List<ResultadoDetalleViewModel> Detalles { get; set; }
            = new List<ResultadoDetalleViewModel>();

        // ── Propiedades calculadas ────────────────────────────────────────

        public string HeaderClass =>
            Nivel == "Senior" ? "senior" : Nivel == "Mid" ? "mid" : "junior";

        public string NivelBadgeClass =>
            Nivel == "Senior" ? "sde-badge-green" :
            Nivel == "Mid" ? "sde-badge-yellow" :
                                "sde-badge-red";

        public string ScoreCircleClass =>
            Promedio >= 8 ? "high" : Promedio >= 5 ? "medium" : "low";

        public string ScoreColor =>
            Promedio >= 8 ? "var(--sde-success)" :
            Promedio >= 5 ? "var(--sde-warning)" :
                            "var(--sde-danger)";

        public string PromedioFormateado => Promedio.ToString("0.0");

        public string FechaFormateada =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy HH:mm") : "—";

        public int TotalPreguntas => Detalles?.Count ?? 0;

        public int MejorPuntaje =>
            Detalles?.Any() == true ? Detalles.Max(d => d.Puntaje) : 0;

        public int PeorPuntaje =>
            Detalles?.Any() == true ? Detalles.Min(d => d.Puntaje) : 0;

        /// <summary>
        /// Número de respuestas con puntaje >= 7 (consideradas buenas).
        /// </summary>
        public int RespuestasAprobadas =>
            Detalles?.Count(d => d.Puntaje >= 7) ?? 0;
    }

    // ====================================================================
    // DETALLE DE UNA PREGUNTA (con evaluación extendida)
    // ====================================================================

    /// <summary>
    /// Resultado individual de una pregunta con la evaluación extendida
    /// que incluye nivel detectado, categoría y consejo de mejora.
    /// </summary>
    public class ResultadoDetalleViewModel
    {
        // ── Datos base ────────────────────────────────────────────────────

        public string Pregunta { get; set; }
        public string Respuesta { get; set; }

        /// <summary>Puntaje asignado por la IA (0–10).</summary>
        public int Puntaje { get; set; }

        /// <summary>Feedback constructivo de la IA.</summary>
        public string Observacion { get; set; }

        // ── Campos extendidos (NUEVOS) ────────────────────────────────────

        /// <summary>
        /// Nivel técnico detectado en esta respuesta específica.
        /// "junior" | "mid" | "senior"
        /// </summary>
        public string Nivel { get; set; }

        /// <summary>
        /// Categoría de la pregunta.
        /// "teoria" | "practica" | "algoritmos" | "arquitectura"
        /// </summary>
        public string Categoria { get; set; }

        /// <summary>
        /// Consejo específico de la IA sobre cómo mejorar esta respuesta.
        /// </summary>
        public string Mejora { get; set; }

        // ── Propiedades calculadas ────────────────────────────────────────

        public string PuntajeBadgeClass =>
            Puntaje >= 8 ? "sde-badge-green" :
            Puntaje >= 5 ? "sde-badge-yellow" :
                           "sde-badge-red";

        public string PuntajeBarColor =>
            Puntaje >= 8 ? "var(--sde-success)" :
            Puntaje >= 5 ? "var(--sde-warning)" :
                           "var(--sde-danger)";

        public int PuntajePorcentaje => Puntaje * 10;

        public bool TieneFeedback => !string.IsNullOrWhiteSpace(Observacion);
        public bool TieneRespuesta => !string.IsNullOrWhiteSpace(Respuesta);
        public bool TieneMejora => !string.IsNullOrWhiteSpace(Mejora);

        /// <summary>Icono para la categoría de la pregunta.</summary>
        public string CategoriaIcono
        {
            get
            {
                switch (Categoria?.ToLower())
                {
                    case "practica": return "bi-code-slash";
                    case "algoritmos": return "bi-diagram-3";
                    case "arquitectura": return "bi-buildings";
                    default: return "bi-book";
                }
            }
        }

        /// <summary>Etiqueta legible de la categoría.</summary>
        public string CategoriaLabel
        {
            get
            {
                switch (Categoria?.ToLower())
                {
                    case "practica": return "Práctica";
                    case "algoritmos": return "Algoritmos";
                    case "arquitectura": return "Arquitectura";
                    default: return "Teoría";
                }
            }
        }

        /// <summary>Clase CSS para el badge del nivel detectado.</summary>
        public string NivelBadgeClass
        {
            get
            {
                switch (Nivel?.ToLower())
                {
                    case "senior": return "sde-badge-green";
                    case "mid": return "sde-badge-yellow";
                    default: return "sde-badge-red";
                }
            }
        }
    }
}