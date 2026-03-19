using System.Web.Mvc;

namespace Entrevista.Helpers
{
    public class SessionHelper
    {
        public static int ObtenerUsuarioId(Controller controller)
        {
            if (controller.Session["usuario_id"] == null)
                throw new System.Exception("Sesión expirada");

            return (int)controller.Session["usuario_id"];
        }
    }
}