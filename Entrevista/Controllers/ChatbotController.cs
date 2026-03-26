using Entrevista.Filter;
using Entrevista.Helpers;
using Entrevista.Services;
using Entrevista_DATA;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Entrevista.Controllers
{
    /// <summary>
    /// Controlador del Chatbot inteligente de EntrevistIA.
    ///
    /// Expone:
    ///   GET  /Chatbot          → Vista principal del chat
    ///   POST /Chatbot/Enviar   → Procesa mensaje y devuelve respuesta (JSON)
    ///   GET  /Chatbot/Historial → Carga mensajes anteriores (JSON)
    ///   POST /Chatbot/NuevoChat → Crea una nueva conversación
    /// </summary>
    [AuthFilter]
    public class ChatbotController : Controller
    {
        private readonly SDEEntities _db = new SDEEntities();

        // ====================================================================
        // GET: /Chatbot
        // ====================================================================

        /// <summary>
        /// Vista principal del chatbot.
        /// Crea o recupera el chat activo del día.
        /// </summary>
        public async Task<ActionResult> Index()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var service = new ChatbotService(_db);

            int chatId = await service.ObtenerOCrearChatAsync(usuarioId);
            ViewBag.ChatId = chatId;

            // Estadísticas rápidas para el sidebar
            ViewBag.TotalEntrevistas = _db.Entrevista
                .Count(e => e.usuarios_id_usuarios == usuarioId);

            ViewBag.EntrevistasFinalizadas = _db.Entrevista
                .Count(e => e.usuarios_id_usuarios == usuarioId
                         && e.estado_entrevista == "FINALIZADA");

            ViewBag.PromedioGeneral = ObtenerPromedioGeneral(usuarioId);

            var usuario = _db.Usuarios.Find(usuarioId);
            ViewBag.NombreUsuario = usuario?.nombre_usuario ?? "Usuario";

            return View();
        }

        // ====================================================================
        // POST: /Chatbot/Enviar
        // ====================================================================

        /// <summary>
        /// Recibe el mensaje del usuario, lo procesa con IA + SQL y responde en JSON.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Enviar(string mensaje, int chatId)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return Json(new { error = "Mensaje vacío" });

            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var service = new ChatbotService(_db);

            try
            {
                var resultado = await service.ProcesarMensajeAsync(
                    mensaje.Trim(),
                    usuarioId,
                    chatId
                );

                return Json(new
                {
                    respuesta = resultado.Respuesta,
                    timestamp = resultado.Timestamp.ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Error: {ex.Message}" });
            }
        }

        // ====================================================================
        // GET: /Chatbot/Historial
        // ====================================================================

        /// <summary>
        /// Devuelve el historial de mensajes del chat actual en JSON.
        /// </summary>
        public JsonResult Historial(int chatId)
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);
            var service = new ChatbotService(_db);

            var mensajes = service.ObtenerHistorial(usuarioId, chatId);
            return Json(mensajes, JsonRequestBehavior.AllowGet);
        }

        // ====================================================================
        // POST: /Chatbot/NuevoChat
        // ====================================================================

        /// <summary>
        /// Fuerza la creación de un nuevo chat (nueva conversación).
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> NuevoChat()
        {
            int usuarioId = SessionHelper.ObtenerUsuarioId(this);

            var nuevoChat = new Chat_conversacion
            {
                usuarios_id_usuarios = usuarioId,
                fecha_inicio_conversacion = DateTime.Now,
                titulo_conversacion = $"Chat {DateTime.Now:dd/MM/yyyy HH:mm}"
            };

            _db.Chat_conversacion.Add(nuevoChat);
            await _db.SaveChangesAsync();

            return Json(new { chatId = nuevoChat.id_chat_conversacion });
        }

        // ====================================================================
        // HELPERS PRIVADOS
        // ====================================================================

        private double ObtenerPromedioGeneral(int usuarioId)
        {
            try
            {
                var puntajes = _db.Database.SqlQuery<double?>(
                    @"SELECT AVG(CAST(r.puntaje_total AS FLOAT)) 
                      FROM Resultado r 
                      JOIN Entrevista e ON r.entrevista_id_entrevista = e.id_entrevista
                      WHERE e.usuarios_id_usuarios = @p0
                        AND r.puntaje_total IS NOT NULL",
                    usuarioId
                ).FirstOrDefault();

                return Math.Round(puntajes ?? 0, 1);
            }
            catch
            {
                return 0;
            }
        }
    }
}