using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Entrevista.Models
{
    public class AuthResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public Usuarios Usuario { get; set; }
        public string Token { get; internal set; }
    }
}