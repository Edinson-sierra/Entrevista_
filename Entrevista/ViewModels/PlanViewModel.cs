using System;
using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla del plan de entrenamiento.
    /// Contiene la recomendación generada por la IA y metadatos de cuándo fue creada.
    /// </summary>
    public class PlanViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Identificadores
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// ID del plan guardado en Planes_entrenamiento.
        /// Se usa para consultas futuras o regeneración.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID de la entrevista que originó este plan.
        /// </summary>
        public int EntrevistaId { get; set; }

        // ────────────────────────────────────────────────────────────
        // Contenido del plan
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// HTML del plan de entrenamiento generado por Groq AI.
        /// Se renderiza con @Html.Raw() en la vista.
        /// Contiene secciones: diagnóstico, temas, ejercicios, plan 7 días, recomendaciones.
        /// </summary>
        [Required]
        public string Recomendacion { get; set; }

        // ────────────────────────────────────────────────────────────
        // Metadatos
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fecha y hora en que fue generado el plan.
        /// </summary>
        public DateTime Fecha { get; set; } = DateTime.Now;

        /// <summary>
        /// Nombre del tema de la entrevista que originó este plan.
        /// </summary>
        public string Tema { get; set; }

        /// <summary>
        /// Nivel de dificultad de la entrevista que originó este plan.
        /// </summary>
        public string Dificultad { get; set; }

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fecha formateada para mostrar en la vista.
        /// </summary>
        public string FechaFormateada =>
            Fecha != default(DateTime)
                ? Fecha.ToString("dd/MM/yyyy 'a las' HH:mm")
                : DateTime.Now.ToString("dd/MM/yyyy 'a las' HH:mm");

        /// <summary>
        /// Indica si hay contenido de plan disponible para renderizar.
        /// </summary>
        public bool TieneContenido =>
            !string.IsNullOrWhiteSpace(Recomendacion);
    }
}