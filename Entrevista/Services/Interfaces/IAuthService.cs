using System;
using Entrevista.Models;
using Entrevista.ViewModels;

namespace Entrevista.Services.Interfaces
{
	
		public interface IAuthService
		{
			AuthResult Login(LoginViewModel model, string ip);
			bool Registrar(RegisterViewModel model);
			void Logout(string token);
			string CrearSesion(int usuarioId, string ip);
		}
	
}