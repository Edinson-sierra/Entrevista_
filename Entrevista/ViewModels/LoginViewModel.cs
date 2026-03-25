using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para el formulario de inicio de sesión.
    /// Contiene las credenciales del usuario y sus reglas de validación.
    /// </summary>
    public class LoginViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Propiedades
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Correo electrónico del usuario.
        /// Usado como identificador único en el sistema.
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña del usuario en texto plano.
        /// Se compara contra el hash BCrypt almacenado.
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }
    }
}