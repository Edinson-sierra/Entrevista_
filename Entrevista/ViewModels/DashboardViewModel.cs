using System.Collections.Generic;

namespace Entrevista.ViewModels

{
    public class DashboardViewModel
    {

        public int TotalEntrevistas { get; set; }

        public int PromedioPuntaje { get; set; }

        public List<ResultadoViewModel> UltimosResultados { get; set; }
    }
}