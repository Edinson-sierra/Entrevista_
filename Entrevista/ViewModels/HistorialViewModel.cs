using System;
using System.Collections.Generic;

namespace Entrevista.ViewModels
{
    public class HistorialViewModel
    {
        public List<HistorialItemViewModel> Entrevistas { get; set; } = new List<HistorialItemViewModel>();
    }

    public class HistorialItemViewModel
    {
        public int EntrevistaId { get; set; }
        public string Tema { get; set; }
        public DateTime? Fecha { get; set; }
        public int? Puntaje { get; set; }
        public string Estado { get; set; }

        public string FechaFormateada =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy HH:mm") : "—";

        public string PuntajeDisplay =>
            Puntaje.HasValue ? $"{Puntaje}/10" : "—";

        public string PuntajeClass =>
            Puntaje >= 8 ? "sde-badge-green" :
            Puntaje >= 5 ? "sde-badge-yellow" :
                           "sde-badge-red";
    }

    public class HistorialDetalleViewModel
    {
        public int EntrevistaId { get; set; }
        public string Tema { get; set; }
        public DateTime? Fecha { get; set; }
        public int? Puntaje { get; set; }
        public List<PreguntaDetalleViewModel> Preguntas { get; set; } = new List<PreguntaDetalleViewModel>();

        public string FechaFormateada =>
            Fecha.HasValue ? Fecha.Value.ToString("dd/MM/yyyy HH:mm") : "—";

        public string PuntajeDisplay =>
            Puntaje.HasValue ? $"{Puntaje}/10" : "—";
    }

    public class PreguntaDetalleViewModel
    {
        public int Numero { get; set; }
        public string Pregunta { get; set; }
        public string RespuestaUsuario { get; set; }
        public string RespuestaCorrecta { get; set; }
        public int? Puntaje { get; set; }
        public string Observacion { get; set; }

        public bool EsCorrecta => Puntaje >= 5;

        public string EstadoClass => EsCorrecta ? "correct" : "incorrect";

        public string EstadoTexto => EsCorrecta ? "Correcta" : "Incorrecta";

        public string EstadoIcon => EsCorrecta ? "bi-check-circle-fill" : "bi-x-circle-fill";
    }
}