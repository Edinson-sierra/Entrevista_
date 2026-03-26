using System;
using System.Collections.Generic;

namespace Entrevista.ViewModels
{
    public class DetalleHistorialViewModel
    {
        public int EntrevistaId { get; set; }
        public string Tema { get; set; }
        public DateTime? Fecha { get; set; }
        public double Promedio { get; set; }
        public List<DetallePreguntaHistorial> Detalles { get; set; } = new List<DetallePreguntaHistorial>();
    }

    public class DetallePreguntaHistorial
    {
        public string Pregunta { get; set; }
        public string Respuesta { get; set; }
        public int Puntaje { get; set; }
        public bool EsCorrecto { get; set; }
        public string Observacion { get; set; }
    }
}