using System;
using System.Collections.Generic;

namespace Entrevista.ViewModels
{
    public class ResultadoDetalleViewModel
    {
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public int Puntaje { get; set; }
        public string Observacion { get; set; }
    }

    public class ResultadoViewModel
    {
        public List<ResultadoDetalleViewModel> Detalles { get; set; }

        public double Promedio { get; set; }

        public string Nivel { get; set; }

        public string RecomendacionFinal { get; set; }

        // 🔥 NUEVO (PRO)
        public DateTime? Fecha { get; set; }
        public string Tema { get; set; }
        public string Dificultad { get; set; }
        public string Estado { get; set; }
    }
}