using FinGrid.DTO;
using FinGrid.JwtFeatures;
using FinGrid.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinGrid.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private readonly JwtHandler _jwtHandler;


        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            JwtHandler jwtHandler)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtHandler = jwtHandler;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User { Email = model.Email, UserName = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {

                await _userManager.AddToRoleAsync(user, "User");


                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtHandler.CreateToken(user, roles);

                return Ok(new
                {
                    message = "Реєстрація успішна",
                    token = token,
                    email = user.Email
                });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtHandler.CreateToken(user, roles);

                return Ok(new
                {
                    token = token,
                    isAuthSuccessful = true,
                    email = user.Email
                });
            }

            return Unauthorized(new { message = "Невірний логін або пароль" });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse))
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!result.Succeeded)
                return BadRequest("Помилка автентифікації Google");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email не отримано від Google");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new User { Email = email, UserName = email };
                await _userManager.CreateAsync(user);

                await _userManager.AddToRoleAsync(user, "Student");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtHandler.CreateToken(user, roles);


            return Redirect($"https://localhost:55063/login-success?token={token}");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Вихід успішний" });
        }

        [HttpGet("user-info")]
        public IActionResult GetUserInfo()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new
                {
                    isAuthenticated = true,
                    email = User.FindFirstValue(ClaimTypes.Email),
                    roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
                });
            }
            return Ok(new { isAuthenticated = false });
        }

        [Authorize]
        [HttpPost("toggle-bank-sync")]
        public async Task<IActionResult> ToggleBankSync([FromBody] bool isEnabled)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound("Користувача не знайдено");

            user.IsBankSyncEnabled = isEnabled;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { isBankSyncEnabled = user.IsBankSyncEnabled });
            }

            return BadRequest("Не вдалося оновити налаштування");
        }
    }
}