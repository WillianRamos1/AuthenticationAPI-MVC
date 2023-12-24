using Lovelace.UserManagerAPI.Extensions;
using Lovelace.UserManagerAPI.Models;
using Lovelace.UserManagerAPI.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lovelace.UserManagerAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(IAuthService authService, RoleManager<IdentityRole> roleManager)
        {
            _authService = authService;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Route("get-roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();

            return Ok(roles);
        }

        [HttpPost]
        [Route("create-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] IdentityRole roles)
        {
            if (ModelState.IsValid)
            {
                var roleExist = await _roleManager.RoleExistsAsync(roles.Name);
                if (!roleExist)
                {
                    var role = new IdentityRole()
                    {   Id = Guid.NewGuid().ToString(),
                        Name = roles.Name,
                        NormalizedName = roles.Name
                    };
                    await _roleManager.CreateAsync(role);
                    return Ok(role);
                }
                else
                {
                    return BadRequest("Role already exists.");
                }
            }
            return BadRequest("Invalid model state.");
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel registerViewModel)
        {
            var registerResult = await _authService.RegisterAsync(registerViewModel);
            return StatusCode(registerResult.StatusCode, registerResult.Message);
        }

        // Route -> Login
        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<LoginServiceResponseViewModel>> Login([FromBody] LoginViewModel loginViewModel)
        {
            var loginResult = await _authService.LoginAsync(loginViewModel);

            if (loginResult is null)
            {
                return Unauthorized("Your credentials are invalid. Please contact to an Admin");
            }

            return Ok(loginResult);
        }

        [HttpPost]
        [Route("update-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateViewModel updateViewModel)
        {
            var updateRoleResult = await _authService.UpdateRoleAsync(User, updateViewModel);

            if (updateRoleResult.IsSucceed)
            {
                return Ok(updateRoleResult.Message);
            }
            else
            {
                return StatusCode(updateRoleResult.StatusCode, updateRoleResult.Message);
            }
        }

        [HttpGet]
        [Route("get-users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserInfoResultViewModel>>> GetUsersList()
        {
            var usersList = await _authService.GetUsersListAsync();

            return Ok(usersList);
        }

        [HttpGet]
        [Route("get-user-email")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserInfoResultViewModel>> GetUserDetailsByUserName([FromQuery] string email)
        {
            var user = await _authService.GetUserDetailsByEmailAsync(email);
            if (user is not null)
            {
                return Ok(user);
            }
            else
            {
                return NotFound("UserName not found");
            }
        }

        [HttpGet]
        [Route("get-usernames")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetUserNamesList()
        {
            var usernames = await _authService.GetUsernamesListAsync();

            return Ok(usernames);
        }
    }
}
