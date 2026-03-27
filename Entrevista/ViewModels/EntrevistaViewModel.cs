using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de chat de entrevista técnica.
    ///
    /// CAMPOS NUEVOS para la mejora de simulación:
    ///   - PuntajesAcumulados : historial de puntajes para dificultad adaptativa
    ///   - DificultadBase     : dificultad elegida por el usuario al inicio
    ///   - DificultadActual   : dificultad adaptada por rendimiento (puede diferir)
    ///   - NivelActual        : nivel técnico estimado según puntajes
    ///   - UltimaEvaluacion   : feedback de la última respuesta evaluada
    ///   - UltimaMejora       : consejo concreto de mejora de la última evaluación
    /// </summary>
    public class EntrevistaViewModel : IValidatableObject
    {
        // ────────────────────────────────────────────────────────────
        // Identificadores
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// ID de la entrevista activa (FK → Entrevista.id_entrevista).
        /// Se transmite como campo oculto en el formulario.
        /// </summary>
        [Required(ErrorMessage = "La entrevista no es válida.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de entrevista inválido.")]
        public int EntrevistaId { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos de la pregunta actual
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto de la pregunta activa generada por la IA.
        /// Solo lectura — se genera en el servidor.
        /// NO está incluida en el historial (se renderiza por separado).
        /// </summary>
        public string PreguntaActual { get; set; }

        /// <summary>
        /// Número de respuestas ya enviadas (0-indexed).
        /// NumeroPregunta=0 → es la pregunta 1 de 5.
        /// </summary>
        public int NumeroPregunta { get; set; }

        // ────────────────────────────────────────────────────────────
        // Respuesta del usuario
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto de la respuesta ingresada (textarea o Speech-to-Text).
        /// </summary>
        [Required(ErrorMessage = "Debes escribir o dictar una respuesta antes de continuar.")]
        [StringLength(5000, MinimumLength = 5,
            ErrorMessage = "La respuesta debe tener entre 5 y 5000 caracteres.")]
        [Display(Name = "Tu respuesta")]
        public string RespuestaUsuario { get; set; }

        // ────────────────────────────────────────────────────────────
        // Historial de conversación
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Historial de preguntas respondidas + sus respuestas.
        /// IMPORTANTE: NO incluye PreguntaActual — se renderiza por separado
        /// para evitar duplicación visual.
        /// </summary>
        public List<MensajeViewModel> Historial { get; set; } = new List<MensajeViewModel>();

        // ────────────────────────────────────────────────────────────
        // Datos de dificultad adaptativa (NUEVOS)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Dificultad elegida por el usuario al iniciar la simulación.
        /// Ej: "Baja", "Media", "Alta".
        /// </summary>
        public string DificultadBase { get; set; }

        /// <summary>
        /// Dificultad real usada en la pregunta actual.
        /// Puede diferir de DificultadBase si el sistema la ajustó por rendimiento.
        /// Ej: Base="Media" → Actual="Alta" (porque el candidato está rindiendo muy bien).
        /// </summary>
        public string DificultadActual { get; set; }

        /// <summary>
        /// Nivel técnico estimado del candidato según sus puntajes acumulados.
        /// Ej: "Junior", "Mid", "Mid+", "Senior".
        /// Se actualiza después de cada respuesta evaluada.
        /// </summary>
        public string NivelActual { get; set; } = "Por evaluar";

        // ────────────────────────────────────────────────────────────
        // Historial de puntajes (NUEVO)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista de puntajes de cada respuesta evaluada (0–10).
        /// Se usa para calcular la dificultad adaptativa de la siguiente pregunta.
        /// Se persiste como string en la sesión y se parsea al cargar.
        /// </summary>
        public List<int> PuntajesAcumulados { get; set; } = new List<int>();

        // ────────────────────────────────────────────────────────────
        // Feedback de la última evaluación (NUEVO)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Feedback de la IA sobre la última respuesta evaluada.
        /// Se muestra brevemente en la UI antes de pasar a la siguiente pregunta.
        /// Null si es la primera pregunta o no hay evaluación previa.
        /// </summary>
        public string UltimaEvaluacion { get; set; }

        /// <summary>
        /// Consejo de la IA sobre cómo mejorar la última respuesta.
        /// </summary>
        public string UltimaMejora { get; set; }

        /// <summary>
        /// Puntaje de la última respuesta evaluada (0–10).
        /// -1 si no hay evaluación previa (primer turno).
        /// </summary>
        public int UltimoPuntaje { get; set; } = -1;

        /// <summary>
        /// Categoría de la última pregunta evaluada.
        /// Ej: "teoria", "practica", "algoritmos", "arquitectura".
        /// </summary>
        public string UltimaCategoria { get; set; }

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>Número de pregunta mostrado al usuario (base 1). Ej: 1, 2, 3...</summary>
        public int NumeroPreguntaDisplay => NumeroPregunta + 1;

        /// <summary>Preguntas restantes para completar la entrevista.</summary>
        public int PreguntasRestantes => 5 - NumeroPregunta;

        /// <summary>Porcentaje de progreso de 0 a 100.</summary>
        public int PorcentajeProgreso => NumeroPregunta * 20;

        /// <summary>Promedio de puntajes acumulados, o 0 si no hay.</summary>
        public double PromedioActual =>
            PuntajesAcumulados.Any()
                ? System.Math.Round(PuntajesAcumulados.Average(), 1)
                : 0.0;

        /// <summary>
        /// Indica si la dificultad fue ajustada automáticamente por el sistema.
        /// Se usa para mostrar una notificación en la UI.
        /// </summary>
        public bool DificultadFueAjustada =>
            !string.IsNullOrEmpty(DificultadBase) &&
            !string.IsNullOrEmpty(DificultadActual) &&
            !string.Equals(DificultadBase, DificultadActual,
                System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Clase CSS para el badge de nivel actual.
        /// </summary>
        public string NivelBadgeClass
        {
            get
            {
                switch (NivelActual?.ToLower())
                {
                    case "senior": return "sde-badge-green";
                    case "mid+": return "sde-badge-blue";
                    case "mid": return "sde-badge-yellow";
                    case "junior+": return "sde-badge-orange";
                    default: return "sde-badge-gray";
                }
            }
        }

        /// <summary>
        /// Clase CSS para el badge de dificultad actual.
        /// </summary>
        public string DificultadBadgeClass
        {
            get
            {
                switch (DificultadActual?.ToLower())
                {
                    case "alta": return "sde-badge-red";
                    case "media": return "sde-badge-yellow";
                    default: return "sde-badge-green";
                }
            }
        }

        // ────────────────────────────────────────────────────────────
        // Validación personalizada
        // ────────────────────────────────────────────────────────────

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(RespuestaUsuario))
            {
                var t = RespuestaUsuario.Trim();

                if (t.Length < 5)
                    yield return new ValidationResult(
                        "La respuesta es demasiado corta.",
                        new[] { nameof(RespuestaUsuario) }
                    );

                // Detectar relleno (mismo carácter repetido)
                if (t.Length < 20 && t.Distinct().Count() <= 2)
                    yield return new ValidationResult(
                        "Ingresa una respuesta real para continuar.",
                        new[] { nameof(RespuestaUsuario) }
                    );
            }
        }
    }
}