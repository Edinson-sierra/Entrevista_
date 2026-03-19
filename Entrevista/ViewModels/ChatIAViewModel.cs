using Entrevista.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Entrevista.Services.Chat
{
    public class ChatIAViewModel
    {
        public string MensajeUsuario { get; set; }

        public List<MensajeViewModel> Conversacion { get; set; }
    }
}