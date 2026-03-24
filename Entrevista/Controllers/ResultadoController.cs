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

        public ActionResult Index(int id)
        {
            var entrevista = _context.Entrevista
                .Include("Preguntas")
                .Include("Respuestas")
                .Include("Resultado")
                .Include("Temas")
                .Include("Dificultad")
                .FirstOrDefault(e => e.id_entrevista == id);

            if (entrevista == null)
                return RedirectToAction("Index", "Home");

            var detalles = entrevista.Preguntas
                .OrderBy(p => p.id_pregunta)
                .Select((p, index) =>
                {
                    var respuesta = entrevista.Respuestas
                        .FirstOrDefault(r => r.preguntas_id_pregunta == p.id_pregunta);

                    var resultado = entrevista.Resultado
                        .Skip(index)
                        .FirstOrDefault();

                    return new ResultadoDetalleViewModel
                    {
                        Pregunta = p.texto_pregunta,
                        Respuesta = respuesta?.respuesta_usuario,
                        Puntaje = resultado?.puntaje_total ?? 0,
                        Observacion = resultado?.observaciones
                    };
                }).ToList();

            var promedio = detalles.Any() ? detalles.Average(x => x.Puntaje) : 0;

            // 🔥 NIVEL AUTOMÁTICO
            string nivel = "Junior";

            if (promedio >= 8) nivel = "Senior";
            else if (promedio >= 5) nivel = "Mid";

            var recomendacion = entrevista.Resultado
                .OrderByDescending(r => r.id_resultado)
                .FirstOrDefault()?.observaciones;

            var model = new ResultadoViewModel
            {
                Detalles = detalles,
                Promedio = promedio,
                Nivel = nivel,
                RecomendacionFinal = recomendacion,

                // 🔥 NUEVO
                Fecha = entrevista.fecha_entrevista,
                Tema = entrevista.Temas.nombre_tema,
                Dificultad = entrevista.Dificultad.nombre_dificultad,
                Estado = entrevista.estado_entrevista
            };

            return View(model);
        }
    }
}