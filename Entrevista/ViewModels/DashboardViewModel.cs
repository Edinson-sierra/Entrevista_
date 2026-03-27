using System;
using System.Collections.Generic;
using System.Linq;

namespace Entrevista.ViewModels
{
    // ====================================================================
    // DASHBOARD PRINCIPAL
    // ====================================================================
    public class DashboardViewModel
    {
        // ── Métricas ─────────────────────────────────────────────────
        public int TotalEntrevistas { get; set; }
        public double PromedioPuntaje { get; set; }
        public string Nivel { get; set; }
        public DateTime? UltimaFecha { get; set; }

        // ── Listas ───────────────────────────────────────────────────
        public List<DashboardEntrevistaItemViewModel> UltimasEntrevistas { get; set; }
            = new List<DashboardEntrevistaItemViewModel>();

        /// <summary>
        /// Datos serializados a JSON para los gráficos Chart.js.
        /// Se llena en HomeController con TODAS las entrevistas (no solo las últimas 5).
        /// </summary>
        public List<DashboardGraficoItem> DatosGraficos { get; set; }
            = new List<DashboardGraficoItem>();

        // ── Propiedades calculadas ────────────────────────────────────
        public string NivelBadgeClass =>
            Nivel == "Senior" ? "sde-badge-green" :
            Nivel == "Mid" ? "sde-badge-yellow" :
                                "sde-badge-red";

        public string NivelIcon =>
            Nivel == "Senior" ? "bi-award-fill" :
            Nivel == "Mid" ? "bi-lightning-fill" :
                                "bi-person-fill";

        public int PromedioComoEntero => (int)Math.Round(PromedioPuntaje * 10);

        public string ScoreColor =>
            PromedioPuntaje >= 8 ? "var(--sde-success)" :
            PromedioPuntaje >= 5 ? "var(--sde-warning)" :
                                   "var(--sde-danger)";

        public bool TieneEntrevistas => TotalEntrevistas > 0;

        public string UltimaFechaTexto =>
            UltimaFecha.HasValue
                ? UltimaFecha.Value.ToString("dd MMM yyyy")
                : "Sin actividad";
    }

    // ====================================================================
    // ÍTEM DE TABLA (últimas entrevistas)
    // ====================================================================
    public class DashboardEntrevistaItemViewModel
    {
        public int EntrevistaId { get; set; }
        public string Tema { get; set; }
        public string Dificultad { get; set; }
        public double Puntaje { get; set; }
        public DateTime? Fecha { get; set; }

        // ── Propiedades calculadas ────────────────────────────────────
        public string DificultadBadgeClass =>
            Dificultad == "Alta" ? "sde-badge-red" :
            Dificultad == "Media" ? "sde-badge-yellow" :
                                    "sde-badge-green";

        public string PuntajeColor =>
            Puntaje >= 8 ? "var(--sde-success)" :
            Puntaje >= 5 ? "var(--sde-warning)" :
                           "var(--sde-danger)";

        public int PuntajePorcentaje => (int)Math.Round(Puntaje * 10);

        public string FechaTexto =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yy") : "—";
    }

    // ====================================================================
    // ÍTEM DE GRÁFICO (Chart.js)
    // Solo vive en DashboardViewModel.DatosGraficos
    // ====================================================================
    public class DashboardGraficoItem
    {
        public string Tema { get; set; }
        public string Dificultad { get; set; }
        public double Puntaje { get; set; }
        public string Fecha { get; set; }
    }
}