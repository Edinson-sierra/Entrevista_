using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de chat de entrevista técnica.
    /// Transporta la pregunta actual, el historial de conversación
    /// y la respuesta que el usuario está escribiendo.
    /// </summary>
    public class EntrevistaViewModel : IValidatableObject
    {
        // ────────────────────────────────────────────────────────────
        // Identificadores
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// ID de la entrevista activa (FK → Entrevista.id_entrevista).
        /// Se transmite como campo oculto en el formulario del chat.
        /// </summary>
        [Required(ErrorMessage = "La entrevista no es válida.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de entrevista inválido.")]
        public int EntrevistaId { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos de la pregunta actual
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto de la pregunta generada por la IA para mostrar en el chat.
        /// Solo lectura — se genera en el servidor.
        /// </summary>
        public string PreguntaActual { get; set; }

        /// <summary>
        /// Número de respuestas ya enviadas (0-indexed).
        /// Determina el progreso visual (1/5, 2/5, etc.).
        /// </summary>
        public int NumeroPregunta { get; set; }

        // ────────────────────────────────────────────────────────────
        // Respuesta del usuario
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto de la respuesta ingresada por el usuario.
        /// Puede venir del textarea o del Speech-to-Text.
        /// </summary>
        [Required(ErrorMessage = "Debes escribir o dictar una respuesta antes de continuar.")]
        [StringLength(5000, MinimumLength = 5,
            ErrorMessage = "La respuesta debe tener entre 5 y 5000 caracteres.")]
        [Display(Name = "Tu respuesta")]
        public string RespuestaUsuario { get; set; }

        // ────────────────────────────────────────────────────────────
        // Historial de la conversación
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Historial completo de preguntas y respuestas de la sesión.
        /// Se usa para renderizar el chat y como contexto para la IA.
        /// </summary>
        public List<MensajeViewModel> Historial { get; set; } = new List<MensajeViewModel>();

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas (solo lectura)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Número de pregunta mostrado al usuario (base 1).
        /// Ejemplo: NumeroPregunta=2 → "Pregunta 3 de 5".
        /// </summary>
        public int NumeroPreguntaDisplay => NumeroPregunta + 1;

        /// <summary>
        /// Preguntas restantes para completar la entrevista.
        /// </summary>
        public int PreguntasRestantes => 5 - NumeroPregunta;

        /// <summary>
        /// Porcentaje de progreso de la entrevista (0–100).
        /// </summary>
        public int PorcentajeProgreso => NumeroPregunta * 20;

        /// <summary>
        /// Indica si la entrevista está completa (5 respuestas enviadas).
        /// </summary>
        public bool EstaCompleta => NumeroPregunta >= 5;

        // ────────────────────────────────────────────────────────────
        // Validación personalizada
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Validaciones de negocio que no pueden expresarse con atributos simples.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Evitar respuestas que solo contienen espacios o caracteres repetidos
            if (!string.IsNullOrWhiteSpace(RespuestaUsuario))
            {
                var trimmed = RespuestaUsuario.Trim();

                if (trimmed.Length < 5)
                {
                    yield return new ValidationResult(
                        "La respuesta es demasiado corta para ser evaluada.",
                        new[] { nameof(RespuestaUsuario) }
                    );
                }

                // Detectar respuesta de "relleno" (mismo carácter repetido)
                bool esSoloRepeticion = trimmed.Length > 0 &&
                    trimmed.Replace(trimmed[0].ToString(), "").Length == 0;

                if (esSoloRepeticion && trimmed.Length < 20)
                {
                    yield return new ValidationResult(
                        "Ingresa una respuesta real para continuar.",
                        new[] { nameof(RespuestaUsuario) }
                    );
                }
            }
        }
    }
}