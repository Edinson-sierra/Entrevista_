using Entrevista.Services;
using Entrevista_DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Entrevista.Filter
{
    public class AuthFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var token = HttpContext.Current.Session["TOKEN"]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                RedirigirLogin(filterContext);
                return;
            }

            var db = new SDEEntities();
            var authService = new AuthService(db);

            var usuario = authService.ObtenerUsuarioPorToken(token);

            if (usuario == null)
            {
                RedirigirLogin(filterContext);
                return;
            }

            // 👤 Guardar usuario en contexto (opcional)
            HttpContext.Current.Items["Usuario"] = usuario;

            base.OnActionExecuting(filterContext);
        }

        private void RedirigirLogin(ActionExecutingContext filterContext)
        {
            filterContext.Result = new RedirectResult("/Auth/Login");
        }
    }
}