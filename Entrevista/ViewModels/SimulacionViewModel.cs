using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Entrevista.ViewModels
{
    public class SimulacionViewModel
    {
        [Required(ErrorMessage = "Seleccione un tema")]
        public int TemaSeleccionado { get; set; }

        [Required(ErrorMessage = "Seleccione una dificultad")]
        public int DificultadSeleccionada { get; set; }

        public List<SelectListItem> Temas { get; set; }

        public List<SelectListItem> Dificultades { get; set; }
    }
}