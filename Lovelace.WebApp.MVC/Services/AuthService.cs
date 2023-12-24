using Lovelace.WebApp.MVC.Extensions;
using Lovelace.WebApp.MVC.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Lovelace.WebApp.MVC.Services
{
    public class AuthService : Service, IAuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient, IOptions<AppSettings> settings)
        {
            httpClient.BaseAddress = new Uri(settings.Value.AutenticacaoUrl);
            _httpClient = httpClient;
        }

        public async Task<LoginServiceResponseViewModel> Login(LoginViewModel loginViewModel)
        {
            var loginContent = ObterConteudo(loginViewModel);

            var response = await _httpClient.PostAsync("/api/auth/login", loginContent);

            if (!TratarErrosResponse(response))
            {
                return new LoginServiceResponseViewModel
                {
                    ResponseResult = await DeserializarObjetoResponse<ResponseResult>(response)
                };
            }

            return await DeserializarObjetoResponse<LoginServiceResponseViewModel>(response);
        }

        public async Task<LoginServiceResponseViewModel> Register(RegisterViewModel registerViewModel)
        {
            var registroContent = ObterConteudo(registerViewModel);

            var response = await _httpClient.PostAsync("/api/auth/register", registroContent);

            if (!TratarErrosResponse(response))
            {
                return new LoginServiceResponseViewModel
                {
                    ResponseResult = await DeserializarObjetoResponse<ResponseResult>(response)
                };
            }

            return await DeserializarObjetoResponse<LoginServiceResponseViewModel>(response);
        }
    }
}
