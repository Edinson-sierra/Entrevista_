using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Entrevista.ViewModels
{
    public class ConfiguracionIAViewModel
    {
        /// <summary>
        /// Nombre del tema técnico que se evaluará.
        /// </summary>
        [Required(ErrorMessage = "El tema es obligatorio.")]
        [StringLength(100)]
        public string Tema { get; set; }

        /// <summary>
        /// Nivel de dificultad de la sesión.
        /// </summary>
        [Required(ErrorMessage = "La dificultad es obligatoria.")]
        [StringLength(50)]
        public string Dificultad { get; set; }
    }
}