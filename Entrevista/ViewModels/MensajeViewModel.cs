using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// Representa un mensaje individual dentro del historial de conversación
    /// de una entrevista técnica.
    /// </summary>
    public class MensajeViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Constantes de tipo de mensaje
        // ────────────────────────────────────────────────────────────

        /// <summary>Mensaje generado por la IA (pregunta del entrevistador).</summary>
        public const string TIPO_IA = "IA";

        /// <summary>Mensaje enviado por el usuario (respuesta del candidato).</summary>
        public const string TIPO_USUARIO = "Usuario";

        // ────────────────────────────────────────────────────────────
        // Propiedades
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Origen del mensaje: "IA" o "Usuario".
        /// Se usa para determinar la alineación y estilo del bubble en el chat.
        /// </summary>
        [Required]
        public string Tipo { get; set; }

        /// <summary>
        /// Contenido textual del mensaje.
        /// </summary>
        [Required]
        [StringLength(5000)]
        public string Texto { get; set; }

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Indica si el mensaje fue generado por la IA.
        /// </summary>
        public bool EsDeIA => Tipo == TIPO_IA;

        /// <summary>
        /// Indica si el mensaje fue enviado por el usuario.
        /// </summary>
        public bool EsDelUsuario => Tipo == TIPO_USUARIO;
    }
}