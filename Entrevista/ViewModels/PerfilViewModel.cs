using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de perfil del usuario.
    /// Permite editar la biografía y gestionar el avatar generado dinámicamente.
    /// Nombre y email son de solo lectura (no se pueden modificar desde el perfil).
    /// </summary>
    public class PerfilViewModel : IValidatableObject
    {
        // ────────────────────────────────────────────────────────────
        // Identificador
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// ID del usuario propietario del perfil.
        /// Se establece en el controlador desde la sesión.
        /// </summary>
        public int UsuarioId { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos de solo lectura (no editables desde esta pantalla)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Nombre completo del usuario. Solo lectura.
        /// Modificable solo desde el sistema de administración.
        /// </summary>
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        /// <summary>
        /// Correo electrónico del usuario. Solo lectura.
        /// Es el identificador de autenticación — no se permite cambiar.
        /// </summary>
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos editables
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Descripción profesional del usuario (opcional).
        /// Se muestra en la pantalla de perfil.
        /// </summary>
        [StringLength(500, ErrorMessage = "La biografía no puede superar los 500 caracteres.")]
        [Display(Name = "Biografía profesional")]
        [DataType(DataType.MultilineText)]
        public string Bio { get; set; }

        // ────────────────────────────────────────────────────────────
        // Datos de avatar (gestionados automáticamente)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// URL completa del avatar SVG de DiceBear.
        /// Se genera automáticamente si no existe.
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// Estilo del avatar en DiceBear (ej. "adventurer").
        /// Se puede ampliar para soportar múltiples estilos.
        /// </summary>
        public string AvatarEstilo { get; set; }

        /// <summary>
        /// Semilla usada para generar el avatar (texto o número aleatorio).
        /// Permite regenerar el mismo avatar de forma determinista.
        /// </summary>
        public string AvatarSeed { get; set; }

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// URL del avatar con fallback al avatar por defecto si no hay URL configurada.
        /// </summary>
        public string AvatarUrlSegura =>
            !string.IsNullOrWhiteSpace(AvatarUrl)
                ? AvatarUrl
                : $"https://api.dicebear.com/7.x/adventurer/svg?seed=default";

        /// <summary>
        /// Indica si el usuario ha completado su biografía.
        /// </summary>
        public bool TieneBio => !string.IsNullOrWhiteSpace(Bio);

        /// <summary>
        /// Iniciales del nombre para usar como fallback de avatar.
        /// </summary>
        public string Iniciales
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Nombre)) return "?";

                var partes = Nombre.Trim().Split(' ');
                return partes.Length >= 2
                    ? $"{partes[0][0]}{partes[partes.Length - 1][0]}".ToUpper()
                    : Nombre.Substring(0, 1).ToUpper();
            }
        }

        // ────────────────────────────────────────────────────────────
        // Validación personalizada
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Validaciones de negocio que no pueden expresarse con atributos simples.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Evitar bios con solo espacios o caracteres especiales repetidos
            if (!string.IsNullOrWhiteSpace(Bio))
            {
                var biaTrimmed = Bio.Trim();

                if (biaTrimmed.Length < 10 && biaTrimmed.Length > 0)
                {
                    yield return new ValidationResult(
                        "La biografía debe tener al menos 10 caracteres si decides completarla.",
                        new[] { nameof(Bio) }
                    );
                }
            }
        }

        // Necesario para IValidatableObject en .NET Framework
        private IEnumerable<ValidationResult> Validate_impl(ValidationContext ctx)
            => Validate(ctx);

        System.Collections.Generic.IEnumerable<ValidationResult>
            IValidatableObject.Validate(ValidationContext validationContext)
            => Validate(validationContext);
    }
}