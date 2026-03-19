using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    public class RegisterViewModel
    {


        [Required]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
       /* [Required]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }*/

    }
}