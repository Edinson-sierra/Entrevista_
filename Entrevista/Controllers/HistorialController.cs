using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    [AuthFilter]
    public class HistorialController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();

        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Include("Temas")
                .Include("Resultado")
                .Where(e => e.usuarios_id_usuarios == usuarioId && e.estado_entrevista == "FINALIZADA")
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            var model = new HistorialViewModel
            {
                Entrevistas = entrevistas.Select(e =>
                {
                    var ultimoResultado = e.Resultado
                        .OrderByDescending(r => r.id_resultado)
                        .FirstOrDefault();

                    return new HistorialItemViewModel
                    {
                        EntrevistaId = e.id_entrevista,
                        Tema = e.Temas?.nombre_tema ?? "—",
                        Fecha = e.fecha_entrevista,
                        Puntaje = ultimoResultado?.puntaje_total,
                        Estado = e.estado_entrevista
                    };
                }).ToList()
            };

            ViewBag.Title = "Historial de Simulaciones";
            return View(model);
        }

        public ActionResult Detalle(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevista = _context.Entrevista
                .Include("Temas")
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .FirstOrDefault(e => e.id_entrevista == id && e.usuarios_id_usuarios == usuarioId);

            if (entrevista == null)
                return RedirectToAction("Index");

            var ultimoResultado = entrevista.Resultado
                .OrderByDescending(r => r.id_resultado)
                .FirstOrDefault();

            var model = new HistorialDetalleViewModel
            {
                EntrevistaId = entrevista.id_entrevista,
                Tema = entrevista.Temas?.nombre_tema ?? "—",
                Fecha = entrevista.fecha_entrevista,
                Puntaje = ultimoResultado?.puntaje_total,
                Preguntas = new List<PreguntaDetalleViewModel>()
            };

            var preguntas = entrevista.Preguntas.OrderBy(p => p.id_pregunta).ToList();
            var respuestas = entrevista.Respuestas.OrderBy(r => r.id_respuesta).ToList();
            var resultados = entrevista.Resultado.OrderBy(r => r.id_resultado).ToList();

            for (int i = 0; i < preguntas.Count; i++)
            {
                var pregunta = preguntas[i];
                var respuesta = i < respuestas.Count ? respuestas[i] : null;
                var resultado = i < resultados.Count ? resultados[i] : null;

                string respuestaCorrecta = resultado?.observaciones ?? "";
                if (respuestaCorrecta.Contains("---PLAN---"))
                {
                    respuestaCorrecta = respuestaCorrecta.Split(new[] { "---PLAN---" }, StringSplitOptions.None)[0].Trim();
                }

                model.Preguntas.Add(new PreguntaDetalleViewModel
                {
                    Numero = i + 1,
                    Pregunta = pregunta.texto_pregunta,
                    RespuestaUsuario = respuesta?.respuesta_usuario ?? "",
                    RespuestaCorrecta = respuestaCorrecta,
                    Puntaje = resultado?.puntaje_total,
                    Observacion = resultado?.observaciones
                });
            }

            ViewBag.Title = $"Detalle - {model.Tema}";
            return View(model);
        }

        [HttpPost]
        public ActionResult ExportarPDF(List<int> entrevistasSeleccionadas)
        {
            if (entrevistasSeleccionadas == null || !entrevistasSeleccionadas.Any())
            {
                TempData["Error"] = "Seleccione al menos una simulación para exportar.";
                return RedirectToAction("Index");
            }

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Include("Temas")
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Where(e => entrevistasSeleccionadas.Contains(e.id_entrevista) && e.usuarios_id_usuarios == usuarioId)
                .ToList();

            if (!entrevistas.Any())
            {
                TempData["Error"] = "No se encontraron simulaciones seleccionadas.";
                return RedirectToAction("Index");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                BaseColor primaryColor = new BaseColor(59, 130, 246);
                BaseColor successColor = new BaseColor(16, 185, 129);
                BaseColor dangerColor = new BaseColor(239, 68, 68);
                BaseColor warningColor = new BaseColor(245, 158, 11);

                Font titleFont = new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, primaryColor);
                Font headerFont = new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.DARK_GRAY);
                Font normalFont = new Font(Font.FontFamily.HELVETICA, 11, Font.NORMAL, BaseColor.DARK_GRAY);
                Font boldFont = new Font(Font.FontFamily.HELVETICA, 11, Font.BOLD, BaseColor.DARK_GRAY);
                Font smallFont = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.GRAY);

                foreach (var entrevista in entrevistas.OrderByDescending(e => e.fecha_entrevista))
                {
                    var tema = entrevista.Temas?.nombre_tema ?? "—";
                    var fecha = entrevista.fecha_entrevista?.ToString("dd/MM/yyyy HH:mm") ?? "—";
                    var ultimoResultado = entrevista.Resultado.OrderByDescending(r => r.id_resultado).FirstOrDefault();
                    var puntaje = ultimoResultado?.puntaje_total?.ToString() ?? "—";

                    document.Add(new Paragraph(" ", normalFont));

                    PdfPTable headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    PdfPCell titleCell = new PdfPCell(new Phrase("SDE Interview - Resultados de Simulación", titleFont));
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingBottom = 10;
                    headerTable.AddCell(titleCell);

                    PdfPCell dateCell = new PdfPCell(new Phrase($"Fecha: {fecha}", normalFont));
                    dateCell.Border = Rectangle.NO_BORDER;
                    dateCell.HorizontalAlignment = PdfPCell.ALIGN_RIGHT;
                    dateCell.PaddingBottom = 10;
                    headerTable.AddCell(dateCell);

                    document.Add(headerTable);

                    PdfPTable infoTable = new PdfPTable(2);
                    infoTable.WidthPercentage = 50;
                    infoTable.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoTable.SetWidths(new float[] { 1f, 1f });

                    PdfPCell topicCell = new PdfPCell(new Phrase($"Tema: {tema}", boldFont));
                    topicCell.Border = Rectangle.BOTTOM_BORDER;
                    topicCell.BorderColor = BaseColor.LIGHT_GRAY;
                    topicCell.Padding = 5;
                    infoTable.AddCell(topicCell);

                    PdfPCell scoreCell = new PdfPCell(new Phrase($"Puntaje: {puntaje}/10", boldFont));
                    scoreCell.Border = Rectangle.BOTTOM_BORDER;
                    scoreCell.BorderColor = BaseColor.LIGHT_GRAY;
                    scoreCell.Padding = 5;
                    scoreCell.HorizontalAlignment = PdfPCell.ALIGN_RIGHT;
                    infoTable.AddCell(scoreCell);

                    document.Add(infoTable);

                    var preguntas = entrevista.Preguntas.OrderBy(p => p.id_pregunta).ToList();
                    var respuestas = entrevista.Respuestas.OrderBy(r => r.id_respuesta).ToList();
                    var resultados = entrevista.Resultado.OrderBy(r => r.id_resultado).ToList();

                    for (int i = 0; i < preguntas.Count; i++)
                    {
                        var pregunta = preguntas[i];
                        var respuesta = i < respuestas.Count ? respuestas[i] : null;
                        var resultado = i < resultados.Count ? resultados[i] : null;

                        var esCorrecta = (resultado?.puntaje_total ?? 0) >= 5;

                        document.Add(new Paragraph(" ", smallFont));
                        document.Add(new Paragraph($"Pregunta {i + 1}:", boldFont));

                        Paragraph questionPara = new Paragraph(pregunta.texto_pregunta, normalFont);
                        questionPara.IndentationLeft = 10;
                        document.Add(questionPara);

                        document.Add(new Paragraph("Tu respuesta:", boldFont));
                        string userAnswer = !string.IsNullOrWhiteSpace(respuesta?.respuesta_usuario) 
                            ? respuesta.respuesta_usuario 
                            : "Sin respuesta";
                        Paragraph answerPara = new Paragraph(userAnswer, normalFont);
                        answerPara.IndentationLeft = 10;
                        document.Add(answerPara);

                        if (!string.IsNullOrWhiteSpace(resultado?.observaciones))
                        {
                            string feedback = resultado.observaciones;
                            if (feedback.Contains("---PLAN---"))
                            {
                                feedback = feedback.Split(new[] { "---PLAN---" }, StringSplitOptions.None)[0].Trim();
                            }
                            document.Add(new Paragraph("Feedback:", boldFont));
                            Paragraph feedbackPara = new Paragraph(feedback, normalFont);
                            feedbackPara.IndentationLeft = 10;
                            document.Add(feedbackPara);
                        }

                        string estadoTexto = esCorrecta ? "Correcta" : "Incorrecta";
                        BaseColor estadoColor = esCorrecta ? successColor : dangerColor;

                        document.Add(new Paragraph($"Estado: {estadoTexto}", new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, estadoColor)));

                        if (i < preguntas.Count - 1)
                        {
                            PdfPTable separator = new PdfPTable(1);
                            separator.WidthPercentage = 30;
                            PdfPCell sepCell = new PdfPCell(new Phrase(" "));
                            sepCell.Border = Rectangle.TOP_BORDER;
                            sepCell.BorderColor = BaseColor.LIGHT_GRAY;
                            sepCell.PaddingTop = 5;
                            separator.AddCell(sepCell);
                            document.Add(separator);
                        }
                    }

                    if (entrevistas.IndexOf(entrevista) < entrevistas.Count - 1)
                    {
                        document.Add(new Paragraph(" "));
                        PdfPTable pageBreak = new PdfPTable(1);
                        pageBreak.WidthPercentage = 100;
                        PdfPCell breakCell = new PdfPCell(new Phrase(" "));
                        breakCell.Border = Rectangle.TOP_BORDER;
                        breakCell.BorderColor = primaryColor;
                        breakCell.PaddingTop = 10;
                        pageBreak.AddCell(breakCell);
                        document.Add(pageBreak);
                    }
                }

                document.Close();
                return File(ms.ToArray(), "application/pdf", $"Historial_SDE_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
        }

        public ActionResult ExportarPDFAll()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Include("Temas")
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Where(e => e.usuarios_id_usuarios == usuarioId && e.estado_entrevista == "FINALIZADA")
                .ToList();

            if (!entrevistas.Any())
            {
                TempData["Error"] = "No hay simulaciones para exportar.";
                return RedirectToAction("Index");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                BaseColor primaryColor = new BaseColor(59, 130, 246);
                BaseColor successColor = new BaseColor(16, 185, 129);
                BaseColor dangerColor = new BaseColor(239, 68, 68);

                Font titleFont = new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, primaryColor);
                Font normalFont = new Font(Font.FontFamily.HELVETICA, 11, Font.NORMAL, BaseColor.DARK_GRAY);
                Font boldFont = new Font(Font.FontFamily.HELVETICA, 11, Font.BOLD, BaseColor.DARK_GRAY);
                Font smallFont = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.GRAY);

                foreach (var entrevista in entrevistas.OrderByDescending(e => e.fecha_entrevista))
                {
                    var tema = entrevista.Temas?.nombre_tema ?? "—";
                    var fecha = entrevista.fecha_entrevista?.ToString("dd/MM/yyyy HH:mm") ?? "—";
                    var ultimoResultado = entrevista.Resultado.OrderByDescending(r => r.id_resultado).FirstOrDefault();
                    var puntaje = ultimoResultado?.puntaje_total?.ToString() ?? "—";

                    document.Add(new Paragraph(" ", normalFont));

                    PdfPTable headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    PdfPCell titleCell = new PdfPCell(new Phrase("SDE Interview - Resultados de Simulación", titleFont));
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingBottom = 10;
                    headerTable.AddCell(titleCell);

                    PdfPCell dateCell = new PdfPCell(new Phrase($"Fecha: {fecha}", normalFont));
                    dateCell.Border = Rectangle.NO_BORDER;
                    dateCell.HorizontalAlignment = PdfPCell.ALIGN_RIGHT;
                    dateCell.PaddingBottom = 10;
                    headerTable.AddCell(dateCell);

                    document.Add(headerTable);

                    PdfPTable infoTable = new PdfPTable(2);
                    infoTable.WidthPercentage = 50;
                    infoTable.HorizontalAlignment = Element.ALIGN_LEFT;
                    infoTable.SetWidths(new float[] { 1f, 1f });

                    PdfPCell topicCell = new PdfPCell(new Phrase($"Tema: {tema}", boldFont));
                    topicCell.Border = Rectangle.BOTTOM_BORDER;
                    topicCell.BorderColor = BaseColor.LIGHT_GRAY;
                    topicCell.Padding = 5;
                    infoTable.AddCell(topicCell);

                    PdfPCell scoreCell = new PdfPCell(new Phrase($"Puntaje: {puntaje}/10", boldFont));
                    scoreCell.Border = Rectangle.BOTTOM_BORDER;
                    scoreCell.BorderColor = BaseColor.LIGHT_GRAY;
                    scoreCell.Padding = 5;
                    scoreCell.HorizontalAlignment = PdfPCell.ALIGN_RIGHT;
                    infoTable.AddCell(scoreCell);

                    document.Add(infoTable);

                    var preguntas = entrevista.Preguntas.OrderBy(p => p.id_pregunta).ToList();
                    var respuestas = entrevista.Respuestas.OrderBy(r => r.id_respuesta).ToList();
                    var resultados = entrevista.Resultado.OrderBy(r => r.id_resultado).ToList();

                    for (int i = 0; i < preguntas.Count; i++)
                    {
                        var pregunta = preguntas[i];
                        var respuesta = i < respuestas.Count ? respuestas[i] : null;
                        var resultado = i < resultados.Count ? resultados[i] : null;

                        var esCorrecta = (resultado?.puntaje_total ?? 0) >= 5;

                        document.Add(new Paragraph(" ", smallFont));
                        document.Add(new Paragraph($"Pregunta {i + 1}:", boldFont));

                        Paragraph questionPara = new Paragraph(pregunta.texto_pregunta, normalFont);
                        questionPara.IndentationLeft = 10;
                        document.Add(questionPara);

                        document.Add(new Paragraph("Tu respuesta:", boldFont));
                        string userAnswer = !string.IsNullOrWhiteSpace(respuesta?.respuesta_usuario) 
                            ? respuesta.respuesta_usuario 
                            : "Sin respuesta";
                        Paragraph answerPara = new Paragraph(userAnswer, normalFont);
                        answerPara.IndentationLeft = 10;
                        document.Add(answerPara);

                        if (!string.IsNullOrWhiteSpace(resultado?.observaciones))
                        {
                            string feedback = resultado.observaciones;
                            if (feedback.Contains("---PLAN---"))
                            {
                                feedback = feedback.Split(new[] { "---PLAN---" }, StringSplitOptions.None)[0].Trim();
                            }
                            document.Add(new Paragraph("Feedback:", boldFont));
                            Paragraph feedbackPara = new Paragraph(feedback, normalFont);
                            feedbackPara.IndentationLeft = 10;
                            document.Add(feedbackPara);
                        }

                        string estadoTexto = esCorrecta ? "Correcta" : "Incorrecta";
                        BaseColor estadoColor = esCorrecta ? successColor : dangerColor;

                        document.Add(new Paragraph($"Estado: {estadoTexto}", new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, estadoColor)));

                        if (i < preguntas.Count - 1)
                        {
                            PdfPTable separator = new PdfPTable(1);
                            separator.WidthPercentage = 30;
                            PdfPCell sepCell = new PdfPCell(new Phrase(" "));
                            sepCell.Border = Rectangle.TOP_BORDER;
                            sepCell.BorderColor = BaseColor.LIGHT_GRAY;
                            sepCell.PaddingTop = 5;
                            separator.AddCell(sepCell);
                            document.Add(separator);
                        }
                    }

                    if (entrevistas.IndexOf(entrevista) < entrevistas.Count - 1)
                    {
                        document.Add(new Paragraph(" "));
                        PdfPTable pageBreak = new PdfPTable(1);
                        pageBreak.WidthPercentage = 100;
                        PdfPCell breakCell = new PdfPCell(new Phrase(" "));
                        breakCell.Border = Rectangle.TOP_BORDER;
                        breakCell.BorderColor = primaryColor;
                        breakCell.PaddingTop = 10;
                        pageBreak.AddCell(breakCell);
                        document.Add(pageBreak);
                    }
                }

                document.Close();
                return File(ms.ToArray(), "application/pdf", $"Historial_SDE_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
        }
    }
}