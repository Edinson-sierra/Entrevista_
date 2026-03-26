using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    // ====================================================================
    // CHAT IA — Para conversación libre con el asistente
    // ====================================================================

    /// <summary>
    /// ViewModel para el componente de chat libre con la IA.
    /// Se usa en el módulo de Chat_conversacion (diferente a la entrevista estructurada).
    /// </summary>
    public class ChatIAViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Mensaje entrante del usuario
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto del mensaje enviado por el usuario en el chat libre.
        /// </summary>
        [Required(ErrorMessage = "Escribe un mensaje para enviar.")]
        [StringLength(2000, MinimumLength = 1,
            ErrorMessage = "El mensaje debe tener entre 1 y 2000 caracteres.")]
        [Display(Name = "Mensaje")]
        public string MensajeUsuario { get; set; }

        // ────────────────────────────────────────────────────────────
        // Historial de la conversación
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Historial completo de la conversación (para mostrar en la UI y enviar como contexto a la IA).
        /// </summary>
        public List<MensajeViewModel> Conversacion { get; set; }
            = new List<MensajeViewModel>();

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Indica si existe historial previo de conversación.
        /// </summary>
        public bool TieneHistorial =>
            Conversacion != null && Conversacion.Count > 0;

        /// <summary>
        /// Número total de turnos en la conversación.
        /// </summary>
        public int TotalMensajes => Conversacion?.Count ?? 0;
    }

    // ====================================================================
    // CONFIGURACIÓN IA — Parámetros de contexto para el asistente
    // ====================================================================

    /// <summary>
    /// ViewModel con los parámetros de configuración que se envían a la IA
    /// antes de iniciar una sesión de entrevista.
    /// </summary>
    
}