using Entrevista_DATA;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Entrevista.Services
{
    /// <summary>
    /// Servicio del Chatbot de EntrevistIA.
    ///
    /// Arquitectura Text-to-SQL:
    ///   1. Recibe pregunta en lenguaje natural del usuario
    ///   2. Envía el esquema de la BD + pregunta al LLM (Groq)
    ///   3. El LLM genera una consulta SQL segura (solo SELECT)
    ///   4. Se ejecuta la consulta contra SQL Server vía Entity Framework
    ///   5. El resultado crudo se vuelve a enviar al LLM para respuesta natural
    ///   6. Se retorna la respuesta amigable + historial guardado en BD
    /// </summary>
    public class ChatbotService
    {
        // ─────────────────────────────────────────────────────────────
        // Configuración
        // ─────────────────────────────────────────────────────────────    

        private readonly string _apiKey = ConfigurationManager.AppSettings["GroqApiKey"];
        private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";

        private readonly string[] _modelos = {
            "llama-3.3-70b-versatile",
            "llama-3.1-8b-instant"
        };

        private static readonly HttpClient _http = new HttpClient();
        private readonly SDEEntities _db;

        // ─────────────────────────────────────────────────────────────
        // Esquema de la BD (context del LLM para generar SQL correcto)
        // ─────────────────────────────────────────────────────────────

        private const string ESQUEMA_BD = @"
Tablas disponibles en la base de datos SQL Server (SDE):

- Usuarios(id_usuarios PK, nombre_usuario, email_usuario, activo)
- Perfil(id_perfil PK, usuarios_id_usuarios FK, bio, fecha_actualizacion)
- Entrevista(id_entrevista PK, usuarios_id_usuarios FK, temas_id_temas FK, dificultad_id_dificultad FK, fecha_entrevista, estado_entrevista)
  * estado_entrevista: 'INICIADA' o 'FINALIZADA'
- Temas(id_tema PK, nombre_tema, descripcion_tema)
- Dificultad(id_dificultad PK, nombre_dificultad)
- Preguntas(id_pregunta PK, entrevista_id_entrevista FK, texto_pregunta)
- Respuestas(id_respuesta PK, entrevista_id_entrevista FK, preguntas_id_pregunta FK, respuesta_usuario, fecha_respuesta)
- Resultado(id_resultado PK, entrevista_id_entrevista FK, puntaje_total, observaciones, fecha_resultado)
  * puntaje_total: 0 a 10
- Planes_entrenamiento(id_plan_entrenamiento PK, usuarios_id_usuarios FK, recomendacion, fecha_plan)
- Sesiones(id_sesion PK, usuarios_id_usuarios FK, ip_sesion, dispositivo, fecha_inicio_sesion, activa)
- Intentos_login(id_intento PK, usuarios_id_usuarios FK, exito_intento, fecha_intento)
- Chat_conversacion(id_chat_conversacion PK, usuarios_id_usuarios FK, fecha_inicio_conversacion, titulo_conversacion)
- Mensajes_conversacion(id_mensaje PK, chat_conversacion_id FK, remitente, mensaje, fecha_envio)
";

        // ─────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────

        public ChatbotService(SDEEntities db)
        {
            _db = db;
        }

        // ====================================================================
        // MÉTODO PRINCIPAL: Procesar mensaje del usuario
        // ====================================================================

        /// <summary>
        /// Procesa el mensaje del usuario y devuelve una respuesta inteligente.
        ///
        /// Flujo:
        ///   1. Guardar mensaje en BD
        ///   2. Detectar si es pregunta de datos (requiere SQL) o conversacional
        ///   3a. Si requiere SQL → generar SQL → ejecutar → respuesta natural
        ///   3b. Si es conversacional → responder directamente con IA
        ///   4. Guardar respuesta del bot en BD
        /// </summary>
        public async Task<ChatbotRespuestaDto> ProcesarMensajeAsync(
              string mensajeUsuario,
              int usuarioId,
              int chatId)
        {
            await GuardarMensajeAsync(chatId, "usuario", mensajeUsuario);

            string respuestaFinal;

            try
            {
                // 1. Intentar siempre generar SQL primero
                string sql = await GenerarSQLAsync(mensajeUsuario, usuarioId);

                if (!string.IsNullOrWhiteSpace(sql) && !sql.StartsWith("ERROR_NO_SQL") && EsConsultaSegura(sql))
                {
                    // 2. Ejecutar contra la BD
                    string resultadoRaw = EjecutarSQL(sql);

                    // 3. Convertir resultado a lenguaje natural
                    respuestaFinal = await ResultadoALenguajeNaturalAsync(mensajeUsuario, sql, resultadoRaw);
                }
                else
                {
                    // 4. Solo si el LLM decidió que no aplica SQL → respuesta conversacional
                    respuestaFinal = await ProcesarConversacional(mensajeUsuario, usuarioId);
                }
            }
            catch (Exception ex)
            {
                respuestaFinal = $"Lo siento, ocurrió un error. Por favor intenta de nuevo. ({ex.Message})";
            }

            await GuardarMensajeAsync(chatId, "bot", respuestaFinal);

            return new ChatbotRespuestaDto
            {
                Respuesta = respuestaFinal,
                Timestamp = DateTime.Now
            };
        }

        // ====================================================================
        // FLUJO SQL: Pregunta → SQL → Resultado → Respuesta natural
        // ====================================================================

        private async Task<string> ProcesarConSQL(string pregunta, int usuarioId)
        {
            // ── Paso 1: Generar SQL con IA ────────────────────────────────
            string sql = await GenerarSQLAsync(pregunta, usuarioId);

            if (string.IsNullOrWhiteSpace(sql) || sql.StartsWith("ERROR"))
                return "No pude generar una consulta válida para esa pregunta. ¿Podrías reformularla?";

            // ── Paso 2: Validar seguridad (solo SELECT) ───────────────────
            if (!EsConsultaSegura(sql))
                return "⚠️ Por seguridad, solo puedo realizar consultas de lectura (SELECT). No puedo modificar datos.";

            // ── Paso 3: Ejecutar consulta ─────────────────────────────────
            string resultadoRaw;
            try
            {
                resultadoRaw = EjecutarSQL(sql);
            }
            catch (Exception)
            {
                return "No pude ejecutar esa consulta. Es posible que la pregunta sea muy compleja o que los datos no existan.";
            }

            // ── Paso 4: Convertir resultado a lenguaje natural ────────────
            return await ResultadoALenguajeNaturalAsync(pregunta, sql, resultadoRaw);
        }

        // ====================================================================
        // FLUJO CONVERSACIONAL: Preguntas generales sobre EntrevistIA
        // ====================================================================

        private async Task<string> ProcesarConversacional(string pregunta, int usuarioId)
        {
            // Obtener contexto del usuario para personalizar respuesta
            var usuario = _db.Usuarios.FirstOrDefault(u => u.id_usuarios == usuarioId);
            string nombreUsuario = usuario?.nombre_usuario ?? "usuario";

            int totalEntrevistas = _db.Entrevista.Count(e => e.usuarios_id_usuarios == usuarioId);
            int entrevistasFinalizadas = _db.Entrevista.Count(e =>
                e.usuarios_id_usuarios == usuarioId &&
                e.estado_entrevista == "FINALIZADA");

            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = $@"
Eres EntrevistBot, el asistente inteligente de SDE Entrevista — una plataforma colombiana de preparación para entrevistas laborales con IA.

CONTEXTO DEL USUARIO ACTUAL:
- Nombre: {nombreUsuario}
- Total entrevistas realizadas: {totalEntrevistas}
- Entrevistas finalizadas: {entrevistasFinalizadas}

TUS CAPACIDADES:
1. Responder preguntas sobre cómo funciona la plataforma
2. Consultar datos del sistema (usuarios, entrevistas, resultados, estadísticas)
3. Dar consejos de preparación para entrevistas
4. Explicar resultados y planes de entrenamiento
5. Orientar sobre temas y dificultades disponibles

INSTRUCCIONES:
- Sé amigable, profesional y motivador
- Responde siempre en español
- Si te preguntan por datos específicos, indica que puedes consultarlos
- Usa emojis moderadamente para hacer la conversación más amigable
- Máximo 3 párrafos por respuesta
"
                },
                new { role = "user", content = pregunta }
            };

            return await LlamarGroqAsync(mensajes, 0.7);
        }

        // ====================================================================
        // GENERAR SQL CON IA
        // ====================================================================

        private async Task<string> GenerarSQLAsync(string pregunta, int usuarioId)
        {
            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = $@"
Eres un experto en SQL Server. Tu ÚNICA tarea es generar consultas SQL SELECT válidas.

{ESQUEMA_BD}

REGLAS CRÍTICAS:
1. SOLO genera consultas SELECT — NUNCA INSERT, UPDATE, DELETE, DROP, ALTER
2. Cuando el usuario hable en primera persona ('mis entrevistas', 'mi perfil'), usa WHERE con usuarios_id_usuarios = {usuarioId}
3. Usa JOINs cuando sean necesarios para obtener nombres en lugar de IDs
4. Limita resultados con TOP 20 para consultas generales
5. Si la pregunta no se puede responder con SQL, responde exactamente: ERROR_NO_SQL
6. SOLO devuelve el SQL puro, sin explicaciones ni markdown
7. Usa alias descriptivos en inglés para las columnas

EJEMPLOS:
- 'cuántos usuarios hay' → SELECT COUNT(*) AS total_usuarios FROM Usuarios WHERE activo = 1
- 'mis entrevistas' → SELECT TOP 20 e.id_entrevista, t.nombre_tema, d.nombre_dificultad, e.estado_entrevista, e.fecha_entrevista FROM Entrevista e JOIN Temas t ON e.temas_id_temas = t.id_tema JOIN Dificultad d ON e.dificultad_id_dificultad = d.id_dificultad WHERE e.usuarios_id_usuarios = {usuarioId} ORDER BY e.fecha_entrevista DESC
- 'mi puntaje promedio' → SELECT AVG(CAST(r.puntaje_total AS FLOAT)) AS promedio_puntaje FROM Resultado r JOIN Entrevista e ON r.entrevista_id_entrevista = e.id_entrevista WHERE e.usuarios_id_usuarios = {usuarioId}
"
                },
                new { role = "user", content = pregunta }
            };

            var sql = await LlamarGroqAsync(mensajes, 0.1);
            return sql?.Trim()
                .Replace("```sql", "")
                .Replace("```", "")
                .Trim() ?? "ERROR_NO_SQL";
        }

        // ====================================================================
        // EJECUTAR SQL CONTRA SQL SERVER
        // ====================================================================

        private string EjecutarSQL(string sql)
        {

            try
            {
                // Usamos la conexión subyacente del SDEEntities que ya tienes inyectado
                var conn = _db.Database.Connection;

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 15;

                    using (var reader = cmd.ExecuteReader())
                    {
                        var resultados = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var fila = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                fila[reader.GetName(i)] = reader.IsDBNull(i)
                                    ? null
                                    : reader.GetValue(i);
                            }
                            resultados.Add(fila);
                        }

                        if (!resultados.Any())
                            return "SIN_RESULTADOS";

                        return JsonConvert.SerializeObject(resultados, Formatting.Indented);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error ejecutando SQL: {ex.Message}");
            }
        }

        // ====================================================================
        // CONVERTIR RESULTADO A LENGUAJE NATURAL
        // ====================================================================

        private async Task<string> ResultadoALenguajeNaturalAsync(
            string preguntaOriginal,
            string sql,
            string resultadoRaw)
        {
            string contextoResultado = resultadoRaw == "SIN_RESULTADOS"
                ? "La consulta no devolvió resultados."
                : $"Datos obtenidos:\n{resultadoRaw}";

            var mensajes = new object[]
            {
                new {
                    role = "system",
                    content = @"
Eres EntrevistBot, asistente de EntrevistIA. 
Convierte resultados de base de datos en respuestas amigables en español.

INSTRUCCIONES:
- Responde directamente con los datos de forma clara y organizada
- Si son listas, usa formato con viñetas o numeración
- Si son números, interprétalos con contexto
- Si no hay resultados, sugiere qué podría buscar el usuario
- Usa emojis moderados para hacer la respuesta más visual
- Máximo 5 líneas de respuesta
- NO menciones SQL ni base de datos en tu respuesta
"
                },
                new {
                    role = "user",
                    content = $"Pregunta del usuario: {preguntaOriginal}\n\n{contextoResultado}"
                }
            };

            return await LlamarGroqAsync(mensajes, 0.4);
        }


        // ====================================================================
        // SEGURIDAD: VALIDAR QUE SOLO SEA SELECT
        // ====================================================================

        private bool EsConsultaSegura(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false;

            string sqlUpper = sql.ToUpperInvariant().Trim();

            // Solo permitir SELECT
            if (!sqlUpper.StartsWith("SELECT")) return false;

            // Bloquear palabras peligrosas
            var peligrosas = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER",
                                     "TRUNCATE", "EXEC", "EXECUTE", "GRANT", "REVOKE",
                                     "CREATE", "MERGE", "BULK", "OPENROWSET" };

            return !peligrosas.Any(p => sqlUpper.Contains(p));
        }

        // ====================================================================
        // CREAR O RECUPERAR CHAT ACTIVO DEL USUARIO
        // ====================================================================

        public async Task<int> ObtenerOCrearChatAsync(int usuarioId)
        {
            // Buscar chat activo del día
            var chatHoy = _db.Chat_conversacion
                .Where(c => c.usuarios_id_usuarios == usuarioId
                         && DbFunctions.TruncateTime(c.fecha_inicio_conversacion) == DbFunctions.TruncateTime(DateTime.Today))
                .OrderByDescending(c => c.fecha_inicio_conversacion)
                .FirstOrDefault();

            if (chatHoy != null)
                return chatHoy.id_chat_conversacion;

            // Crear nuevo chat
            var nuevoChat = new Chat_conversacion
            {
                usuarios_id_usuarios = usuarioId,
                fecha_inicio_conversacion = DateTime.Now,
                titulo_conversacion = $"Chat {DateTime.Now:dd/MM/yyyy HH:mm}"
            };

            _db.Chat_conversacion.Add(nuevoChat);
            await _db.SaveChangesAsync();

            return nuevoChat.id_chat_conversacion;
        }

        // ====================================================================
        // CARGAR HISTORIAL DE MENSAJES
        // ====================================================================

        public List<MensajeChatDto> ObtenerHistorial(int usuarioId, int chatId)
        {
            return _db.Mensajes_conversacion
                .Where(m => m.chat_conversacion_id == chatId
                         && m.Chat_conversacion.usuarios_id_usuarios == usuarioId)
                .OrderBy(m => m.fecha_envio)
                .Select(m => new MensajeChatDto
                {
                    Remitente = m.remitente,
                    Mensaje = m.mensaje,
                    FechaEnvio = m.fecha_envio ?? DateTime.Now
                })
                .ToList();
        }

        // ====================================================================
        // GUARDAR MENSAJE EN BD
        // ====================================================================

        private async Task GuardarMensajeAsync(int chatId, string remitente, string mensaje)
        {
            _db.Mensajes_conversacion.Add(new Mensajes_conversacion
            {
                chat_conversacion_id = chatId,
                remitente = remitente,
                mensaje = mensaje,
                fecha_envio = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        // ====================================================================
        // LLAMAR A GROQ CON FALLBACK DE MODELOS
        // ====================================================================

        private async Task<string> LlamarGroqAsync(object mensajes, double temperatura)
        {
            foreach (var modelo in _modelos)
            {
                try
                {
                    var body = new
                    {
                        model = modelo,
                        messages = mensajes,
                        temperature = temperatura,
                        max_tokens = 1000
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(body),
                        Encoding.UTF8,
                        "application/json"
                    );

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                        var response = await client.PostAsync(API_URL, content);
                        var json = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode) continue;

                        dynamic data = JsonConvert.DeserializeObject(json);
                        return data.choices[0].message.content.ToString();
                    }
                }
                catch { }
            }

            return "Lo siento, el servicio de IA no está disponible en este momento.";
        }
    }

    // ====================================================================
    // DTOs
    // ====================================================================

    public class ChatbotRespuestaDto
    {
        public string Respuesta { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MensajeChatDto
    {
        public string Remitente { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaEnvio { get; set; }
    }
}