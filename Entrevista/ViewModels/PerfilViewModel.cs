using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    public class PerfilViewModel
    {
        public int UsuarioId { get; set; }

        public string Nombre { get; set; }

        public string Email { get; set; }

        [MaxLength(500)]
        public string Bio { get; set; }

        public string AvatarUrl { get; set; }

        public string AvatarEstilo { get; set; }

        public string AvatarSeed { get; set; }
    }
}