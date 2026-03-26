using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para el formulario de registro de nuevos usuarios.
    /// Incluye validaciones de formato, longitud y coincidencia de contraseñas.
    /// </summary>
    public class RegisterViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Propiedades
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Nombre completo del usuario.
        /// Se usa para mostrar en el sidebar, avatar y perfil.
        /// </summary>
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s\-']+$",
            ErrorMessage = "El nombre solo puede contener letras, espacios y guiones.")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; }

        /// <summary>
        /// Correo electrónico. Debe ser único en el sistema.
        /// Se verifica duplicidad en AuthService.Registrar().
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        /// <summary>
        /// Contraseña en texto plano. Se hashea con BCrypt antes de persistir.
        /// </summary>
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*\d).{6,}$",
            ErrorMessage = "La contraseña debe contener al menos una letra y un número.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        /// <summary>
        /// Confirmación de contraseña. Debe coincidir exactamente con Password.
        /// Solo se valida en cliente — no se persiste.
        /// </summary>
        [Required(ErrorMessage = "Confirma tu contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; }
    }
}