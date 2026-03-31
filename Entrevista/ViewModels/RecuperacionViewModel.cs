using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    // ====================================================================
    // PASO 1 — Solicitar enlace de recuperación (formulario de email)
    // ====================================================================
    public class OlvideContrasenaViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }
    }

    // ====================================================================
    // PASO 2 — Establecer nueva contraseña (formulario con token)
    // ====================================================================
    public class RestablecerContrasenaViewModel
    {
        /// <summary>Token de un solo uso recibido por URL.</summary>
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres.")]
        [Display(Name = "Nueva contraseña")]
        public string NuevaPassword { get; set; }

        [Required(ErrorMessage = "Confirma tu nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarPassword { get; set; }
    }
}
