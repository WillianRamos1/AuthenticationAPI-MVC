using Lovelace.UserManagerAPI.Extensions;
using Lovelace.UserManagerAPI.Models;
using Lovelace.UserManagerAPI.Services.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Lovelace.UserManagerAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogService logService, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logService = logService;
            _configuration = configuration;
        }

        public async Task<UserInfoResultViewModel> GetUserDetailsByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var userInfo = GenerateUserInfo(user, roles);
            await _logService.SaveNewLog(user.Email, $"Usuario Buscado por email: {email}");
            return userInfo;
        }

        public async Task<IEnumerable<string>> GetUsernamesListAsync()
        {
            var userNames = await _userManager.Users
                .Select(q => q.UserName)
                .ToListAsync();
            await _logService.SaveNewLog("ADMIN", "Lista de Nome de Usuarios Gerada");
            return userNames;
        }

        public async Task<IEnumerable<UserInfoResultViewModel>> GetUsersListAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            List<UserInfoResultViewModel> userInfoResults = new List<UserInfoResultViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userInfo = GenerateUserInfo(user, roles);
                userInfoResults.Add(userInfo);
            }
            await _logService.SaveNewLog("ADMIN", "Lista de Usuarios Gerada");
            return userInfoResults;
        }

        public async Task<LoginServiceResponseViewModel?> LoginAsync(LoginViewModel loginViewModel)
        {
            var user = await _userManager.FindByEmailAsync(loginViewModel.Email);
            if (user == null) return null;

            var passwordCheck = await _userManager.CheckPasswordAsync(user,loginViewModel.Password);
            if(!passwordCheck) return null;

            var newToken = await GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var userInfo = GenerateUserInfo(user, roles);
            
            await _logService.SaveNewLog(user.Email, "Novo Login");

            return new LoginServiceResponseViewModel()
            {
                NewToken = newToken,
                UserInfo = userInfo,
            };
        }

        public async Task<ResponseViewModel> UpdateRoleAsync(ClaimsPrincipal user, UpdateViewModel updateViewModel)
        {
            var userFind = await _userManager.FindByEmailAsync(updateViewModel.Email);
            if (userFind == null)
                return new ResponseViewModel()
                {
                    IsSucceed = false,
                    StatusCode = 404,
                    Message = "Usuario Invalido"
                };

            if (!await _roleManager.RoleExistsAsync(updateViewModel.NewRole))
            {
                return new ResponseViewModel
                {
                    IsSucceed = false,
                    StatusCode = 400,
                    Message = "A nova role especificada não é válida."
                };
            }

            // Remover roles antigas e adicionar nova role
            var userRoles = await _userManager.GetRolesAsync(userFind);
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(userFind, userRoles);
            var addToRoleResult = await _userManager.AddToRoleAsync(userFind, updateViewModel.NewRole);

            if (removeRolesResult.Succeeded && addToRoleResult.Succeeded)
            {
                await _logService.SaveNewLog(userFind.Email, "Permissões Alteradas");

                return new ResponseViewModel
                {
                    IsSucceed = true,
                    StatusCode = 200,
                    Message = "Role do usuário atualizada com sucesso."
                };

                
            }
            else
            {
                // Tratar erros ao remover ou adicionar roles
                return new ResponseViewModel
                {
                    IsSucceed = false,
                    StatusCode = 500,
                    Message = "Ocorreu um erro ao atualizar a role do usuário."
                };
            }
        }

        public async Task<ResponseViewModel> RegisterAsync(RegisterViewModel registerViewModel)
        {
            var userExist = await _userManager.FindByEmailAsync(registerViewModel.Email);
            if (userExist != null)
                return new ResponseViewModel()
                {
                    IsSucceed = false,
                    StatusCode = 409,
                    Message = "Usuario ja Existe"
                };

            if (registerViewModel.Roles != null && registerViewModel.Roles.Any())
            {
                foreach (var roleName in registerViewModel.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        return new ResponseViewModel()
                        {
                            IsSucceed = false,
                            StatusCode = 401,
                            Message = $"A role '{roleName}' não existe."
                        };
                    }
                }
            }

            ApplicationUser user = new ApplicationUser()
            {
                FirstName = registerViewModel.FirstName,
                LastName = registerViewModel.LastName,
                Email = registerViewModel.Email,
                UserName = registerViewModel.UserName,
                Address = registerViewModel.Address,
                Roles = registerViewModel.Roles,
                SecurityStamp = Guid.NewGuid().ToString()
                };

                var userResult = await _userManager.CreateAsync(user, registerViewModel.Password);

                if (!userResult.Succeeded)
                {
                    var errorString = "Erro ao criar usuario, motivo: ";
                    foreach (var item in userResult.Errors)
                    {
                        errorString += " # " + item.Description;
                    }

                    return new ResponseViewModel()
                    {
                        IsSucceed = false,
                        StatusCode = 400,
                        Message = errorString
                    };
                }

            if (registerViewModel.Roles != null && registerViewModel.Roles.Any())
            {
                foreach (var roleName in registerViewModel.Roles)
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }

            await _logService.SaveNewLog(user.Email, "Registrado no Site");

            return new ResponseViewModel()
            {
                IsSucceed = true,
                StatusCode = 201,
                Message = "Usuario criado com sucesso."
            };
        }


        private async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var signingCredentials = new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256);

            var tokenObject = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: signingCredentials
                );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);
            return token;
        }

        private UserInfoResultViewModel GenerateUserInfo(ApplicationUser user, IEnumerable<string> roles)
        {
            // Instead of this, You can use Automapper packages. But i don't want it in this project
            return new UserInfoResultViewModel()
            {
                ID = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            };
        }
    }
}
