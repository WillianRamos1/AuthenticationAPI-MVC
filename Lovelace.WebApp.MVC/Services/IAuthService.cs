using Lovelace.WebApp.MVC.Models;

namespace Lovelace.WebApp.MVC.Services
{
    public interface IAuthService
    {
        Task<LoginServiceResponseViewModel> Login(LoginViewModel loginViewModel);
        Task<LoginServiceResponseViewModel> Register(RegisterViewModel registerViewModel);
    }
}
