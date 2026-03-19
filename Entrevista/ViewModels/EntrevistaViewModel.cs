using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entrevista.ViewModels
{
    public class EntrevistaViewModel
    {
        public int EntrevistaId { get; set; }

        public string PreguntaActual { get; set; }

        [Required(ErrorMessage = "Debes escribir una respuesta")]
        public string RespuestaUsuario { get; set; }

        public int NumeroPregunta { get; set; }

        public List<MensajeViewModel> Historial { get; set; }
    }
}