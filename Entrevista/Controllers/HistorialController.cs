using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    [AuthFilter]
    public class HistorialController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();

        // GET: /Historial/
        public ActionResult Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Where(e => e.usuarios_id_usuarios == usuarioId)
                .OrderByDescending(e => e.fecha_entrevista)
                .ToList();

            ViewBag.Title = "Historial — Simulaciones";
            return View(entrevistas);
        }

        // GET: /Historial/Detalle/{id}
        public ActionResult Detalle(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevista = _context.Entrevista
                .Include("Temas")
                .Include("Dificultad")
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .FirstOrDefault(e => e.id_entrevista == id && e.usuarios_id_usuarios == usuarioId);

            if (entrevista == null)
                return HttpNotFound();

            var preguntas = entrevista.Preguntas.OrderBy(p => p.id_pregunta).ToList();
            var resultados = entrevista.Resultado.OrderBy(r => r.id_resultado).ToList();

            var detalles = preguntas.Select((pregunta, index) =>
            {
                var respuesta = entrevista.Respuestas.FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);
                var resultado = index < resultados.Count ? resultados[index] : null;
                var puntaje = resultado?.puntaje_total ?? 0;

                return new DetallePreguntaHistorial
                {
                    Pregunta = pregunta.texto_pregunta,
                    Respuesta = respuesta?.respuesta_usuario,
                    Puntaje = puntaje,
                    EsCorrecto = puntaje >= 5,
                    Observacion = resultado?.observaciones
                };
            }).ToList();

            double promedio = detalles.Any() ? Math.Round(detalles.Average(d => d.Puntaje), 1) : 0;

            ViewBag.Title = $"Detalle — {entrevista.Temas?.nombre_tema}";
            return View("Detalle", new DetalleHistorialViewModel
            {
                EntrevistaId = id,
                Tema = entrevista.Temas?.nombre_tema,
                Fecha = entrevista.fecha_entrevista,
                Promedio = promedio,
                Detalles = detalles
            });
        }

        // POST: /Historial/ExportSelectedToPdf
        [HttpPost]
        public ActionResult ExportSelectedToPdf(int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
                return Content("No se seleccionaron simulaciones.");

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevistas = _context.Entrevista
                .Include("Temas")
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Where(e => selectedIds.Contains(e.id_entrevista) && e.usuarios_id_usuarios == usuarioId)
                .ToList();

            if (!entrevistas.Any())
                return Content("No se encontraron simulaciones.");

            ViewBag.Entrevistas = entrevistas.OrderByDescending(e => e.fecha_entrevista).ToList();
            ViewBag.FechaExportacion = DateTime.Now;

            return View("ExportPdf");
        }

        // GET: /Historial/ExportToCrystal/{id}
        // Exports a Crystal report (.rpt) located at ~/Reports/HistorialReport.rpt to PDF.
        // Uses reflection so the project builds without Crystal dependencies; the runtime must
        // be installed on the server for this to work.
        public ActionResult ExportToCrystal(int id)
        {
            string reportPath = Server.MapPath("~/Reports/HistorialReport.rpt");

            if (!System.IO.File.Exists(reportPath))
            {
                return Content("El archivo de informe 'Reports/HistorialReport.rpt' no fue encontrado. Coloque su .rpt en la carpeta ~/Reports/ y asegúrese de que el runtime de Crystal Reports esté instalado en el servidor.");
            }

            try
            {
                // Load Crystal assemblies dynamically
                var engineAsm = Assembly.Load("CrystalDecisions.CrystalReports.Engine");
                var sharedAsm = Assembly.Load("CrystalDecisions.Shared");

                var reportDocType = engineAsm.GetType("CrystalDecisions.CrystalReports.Engine.ReportDocument");
                var exportFormatType = sharedAsm.GetType("CrystalDecisions.Shared.ExportFormatType");

                if (reportDocType == null || exportFormatType == null)
                    throw new Exception("No se pudieron cargar las librerías de Crystal Reports.");

                var report = Activator.CreateInstance(reportDocType);
                var loadMethod = reportDocType.GetMethod("Load", new[] { typeof(string) });
                loadMethod.Invoke(report, new object[] { reportPath });

                // If you need to pass parameters or set datasource, add reflective calls here.

                var pdfEnum = Enum.Parse(exportFormatType, "PortableDocFormat");
                var exportMethod = reportDocType.GetMethod("ExportToStream", new[] { exportFormatType });
                var streamObj = exportMethod.Invoke(report, new object[] { pdfEnum }) as Stream;

                if (streamObj == null)
                    throw new Exception("La generación del PDF devolvió un stream nulo.");

                if (streamObj.CanSeek) streamObj.Position = 0;

                var closeMethod = reportDocType.GetMethod("Close");
                var disposeMethod = reportDocType.GetMethod("Dispose");
                closeMethod?.Invoke(report, null);
                disposeMethod?.Invoke(report, null);

                return File(streamObj, "application/pdf", $"Historial_{id}.pdf");
            }
            catch (Exception ex)
            {
                return Content("No se pudo exportar el informe. Asegúrese de que Crystal Reports runtime esté instalado y disponible. Detalle: " + ex.Message);
            }
        }

        // GET: /Historial/Print/{id}
        // Returns a print-friendly HTML view for the entrevista so the user can print to PDF from the browser/device.
        public ActionResult Print(int id)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var entrevista = _context.Entrevista
                .Where(e => e.id_entrevista == id && e.usuarios_id_usuarios == usuarioId)
                .FirstOrDefault();

            if (entrevista == null)
                return HttpNotFound();

            ViewBag.Title = "Imprimir entrevista — Historial";
            return View("Print", entrevista);
        }
    }
}
