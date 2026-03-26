using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Entrevista.ViewModels
{
    // ====================================================================
    // DASHBOARD PRINCIPAL
    // ====================================================================

    /// <summary>
    /// ViewModel principal del Dashboard.
    /// Contiene las métricas agregadas del usuario y el listado
    /// de sus últimas entrevistas realizadas.
    /// </summary>
    public class DashboardViewModel
    {
        // ────────────────────────────────────────────────────────────
        // Métricas principales
        // ────────────────────────────────────────────────────────────

        /// <summary>Total de entrevistas realizadas por el usuario.</summary>
        public int TotalEntrevistas { get; set; }

        /// <summary>
        /// Puntaje promedio de todas las entrevistas (0.0 – 10.0).
        /// Se calcula promediando los puntajes de todos los resultados.
        /// </summary>
        public double PromedioPuntaje { get; set; }

        /// <summary>
        /// Nivel técnico estimado del usuario según su promedio.
        /// Calculado en HomeController mediante NivelCalculator.
        /// </summary>
        public string Nivel { get; set; }

        /// <summary>
        /// Fecha de la entrevista más reciente.
        /// Null si el usuario no tiene entrevistas.
        /// </summary>
        public DateTime? UltimaFecha { get; set; }

        /// <summary>
        /// Listado de las últimas 5 entrevistas para mostrar en la tabla del dashboard.
        /// </summary>
        public List<DashboardEntrevistaItemViewModel> UltimasEntrevistas { get; set; }
            = new List<DashboardEntrevistaItemViewModel>();

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>
        /// Clase CSS del badge de nivel (para colorear según rendimiento).
        /// </summary>
        public string NivelBadgeClass =>
            Nivel == "Senior" ? "sde-badge-green" :
            Nivel == "Mid" ? "sde-badge-yellow" :
                                "sde-badge-red";

        /// <summary>
        /// Icono Bootstrap del nivel.
        /// </summary>
        public string NivelIcon =>
            Nivel == "Senior" ? "bi-award-fill" :
            Nivel == "Mid" ? "bi-lightning-fill" :
                                "bi-person-fill";

        /// <summary>
        /// Porcentaje del promedio (0–100) para la barra de progreso visual.
        /// </summary>
        public int PromedioComoEntero => (int)Math.Round(PromedioPuntaje * 10);

        /// <summary>
        /// Color CSS del score según rendimiento.
        /// </summary>
        public string ScoreColor =>
            PromedioPuntaje >= 8 ? "var(--sde-success)" :
            PromedioPuntaje >= 5 ? "var(--sde-warning)" :
                                   "var(--sde-danger)";

        /// <summary>
        /// Indica si el usuario tiene entrevistas registradas.
        /// </summary>
        public bool TieneEntrevistas => TotalEntrevistas > 0;

        /// <summary>
        /// Texto amigable de la última actividad.
        /// </summary>
        public string UltimaFechaTexto =>
            UltimaFecha.HasValue
                ? UltimaFecha.Value.ToString("dd MMM yyyy")
                : "Sin actividad";

        public List<DashboardGraficoItem> DatosGraficos { get; internal set; }
    }

    // ====================================================================
    // ÍTEM DE ENTREVISTA EN EL LISTADO
    // ====================================================================

    /// <summary>
    /// Representa una entrevista resumida para mostrar en la tabla del dashboard.
    /// </summary>
    public class DashboardEntrevistaItemViewModel
    {
        /// <summary>ID de la entrevista (para el enlace "Ver resultado").</summary>
        public int EntrevistaId { get; set; }

        /// <summary>Nombre del tema técnico evaluado.</summary>
        public string Tema { get; set; }

        /// <summary>Nombre del nivel de dificultad (Baja/Media/Alta).</summary>
        public string Dificultad { get; set; }

        /// <summary>Puntaje promedio de esa entrevista (0.0 – 10.0).</summary>
        public double Puntaje { get; set; }

        /// <summary>Fecha en que se realizó la entrevista.</summary>
        public DateTime? Fecha { get; set; }

        // 🔥 NUEVO: datos para gráficos
        public List<DashboardGraficoItem> DatosGraficos { get; set; } = new List<DashboardGraficoItem>();

        // ────────────────────────────────────────────────────────────
        // Propiedades calculadas
        // ────────────────────────────────────────────────────────────

        /// <summary>Clase CSS del badge de dificultad.</summary>
        public string DificultadBadgeClass =>
            Dificultad == "Alta" ? "sde-badge-red" :
            Dificultad == "Media" ? "sde-badge-yellow" :
                                    "sde-badge-green";

        /// <summary>Color del puntaje según rendimiento.</summary>
        public string PuntajeColor =>
            Puntaje >= 8 ? "var(--sde-success)" :
            Puntaje >= 5 ? "var(--sde-warning)" :
                           "var(--sde-danger)";

        /// <summary>Porcentaje del puntaje (0–100) para la barra visual.</summary>
        public int PuntajePorcentaje => (int)Math.Round(Puntaje * 10);

        /// <summary>Fecha formateada para mostrar en tabla.</summary>
        public string FechaTexto =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yy") : "—";
    }
    public class DashboardGraficoItem
    {
        public string tema { get; set; }
        public string dificultad { get; set; }
        public double puntaje { get; set; }
        public string fecha { get; set; }
    }
}