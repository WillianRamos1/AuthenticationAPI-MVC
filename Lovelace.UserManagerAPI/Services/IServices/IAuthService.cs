using Lovelace.UserManagerAPI.Extensions;
using Lovelace.UserManagerAPI.Models;
using System.Security.Claims;

namespace Lovelace.UserManagerAPI.Services.IServices
{
    public interface IAuthService
    {
        Task<ResponseViewModel> RegisterAsync(RegisterViewModel registerViewModel);
        Task<LoginServiceResponseViewModel?> LoginAsync(LoginViewModel loginViewModel);
        Task<ResponseViewModel> UpdateRoleAsync(ClaimsPrincipal user, UpdateViewModel updateViewModel);
        Task<IEnumerable<UserInfoResultViewModel>> GetUsersListAsync();
        Task<UserInfoResultViewModel> GetUserDetailsByEmailAsync(string email);
        Task<IEnumerable<string>> GetUsernamesListAsync();
    }
}
