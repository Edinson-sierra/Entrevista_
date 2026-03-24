using System;
using System.Collections.Generic;

namespace Entrevista.ViewModels

{
    public class DashboardViewModel
    {
        public int TotalEntrevistas { get; set; }

        public double PromedioPuntaje { get; set; }

        public string Nivel { get; set; }

        public DateTime? UltimaFecha { get; set; }

        public List<DashboardEntrevistaItemViewModel> UltimasEntrevistas { get; set; }
    }

    public class DashboardEntrevistaItemViewModel
    {
        public int EntrevistaId { get; set; }

        public string Tema { get; set; }

        public string Dificultad { get; set; }

        public double Puntaje { get; set; }

        public DateTime? Fecha { get; set; }
    }
}