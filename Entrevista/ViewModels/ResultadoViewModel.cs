using System;

namespace Entrevista.ViewModels
{
    public class ResultadoViewModel
    {
        public int EntrevistaId { get; set; }

        public int Puntaje { get; set; }

        public string Observaciones { get; set; }

        public DateTime Fecha { get; set; }
    }
}