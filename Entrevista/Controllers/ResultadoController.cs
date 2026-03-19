using Entrevista.Services;
using Entrevista.ViewModels;
using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    public class ResultadoController : Controller
    {
        private readonly SDEEntities _context = new SDEEntities();
        private readonly IIAService _ia = new IAService();

        public async Task<ActionResult> Index(int id)
        {
            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .FirstOrDefault(e => e.id_entrevista == id);

            var sb = new StringBuilder();

            foreach (var pregunta in entrevista.Preguntas)
            {
                sb.AppendLine($"P: {pregunta.texto_pregunta}");

                var respuesta = entrevista.Respuestas
                    .FirstOrDefault(r => r.preguntas_id_pregunta == pregunta.id_pregunta);

                if (respuesta != null)
                    sb.AppendLine($"R: {respuesta.respuesta_usuario}");
            }

            string evaluacion = await _ia.PreguntarAsync(
                $"Evalúa esta entrevista:\n{sb}\nFormato:\nPUNTAJE: numero\nOBSERVACIONES: texto"
            );

            int puntaje = 0;
            string observaciones = evaluacion;

            // Parseo básico
            try
            {
                var partes = evaluacion.Split(new[] { "OBSERVACIONES:" }, System.StringSplitOptions.None);
                puntaje = int.Parse(partes[0].Replace("PUNTAJE:", "").Trim());
                observaciones = partes[1].Trim();
            }
            catch { }

            var resultado = new Resultado
            {
                entrevista_id_entrevista = id,
                puntaje_total = puntaje,
                observaciones = observaciones
            };

            _context.Resultado.Add(resultado);
            _context.SaveChanges();

            var model = new ResultadoViewModel
            {
                EntrevistaId = id,
                Puntaje = puntaje,
                Observaciones = observaciones
            };

            return View(model);
        }
    }
}