using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Entrevista.Controllers
{
    public class EvaluacionItemViewModels
    {
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public int Puntaje { get; set; }
        public string Feedback { get; set; }
        public string Nivel { get; set; }
        public string Categoria { get; set; }
    }
}