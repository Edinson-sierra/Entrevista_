using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Entrevista.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de configuración de una nueva entrevista.
    /// Contiene la selección de tema y dificultad, junto con sus listas de opciones.
    /// </summary>
    public class SimulacionViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Selecciones del usuario
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// ID del tema técnico seleccionado (FK → Temas.id_tema).
        /// </summary>
        [Required(ErrorMessage = "Debes seleccionar un tema para la entrevista.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un tema válido de la lista.")]
        [Display(Name = "Tema técnico")]
        public int TemaSeleccionado { get; set; }

        /// <summary>
        /// ID de la dificultad seleccionada (FK → Dificultad.id_dificultad).
        /// </summary>
        [Required(ErrorMessage = "Debes seleccionar una dificultad.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una dificultad válida.")]
        [Display(Name = "Nivel de dificultad")]
        public int DificultadSeleccionada { get; set; }

        // ────────────────────────────────────────────────────────────
        // Listas para los controles de selección
        // (se cargan en el controlador, no se validan)
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Lista de temas disponibles. Se carga desde la BD en SimulacionController.Index().
        /// </summary>
        public List<SelectListItem> Temas { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Lista de dificultades disponibles. Se carga desde la BD.
        /// </summary>
        public List<SelectListItem> Dificultades { get; set; } = new List<SelectListItem>();
    }
}