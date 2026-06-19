using ChineseRaffleApi.Dto;
using ChineseRaffleApi.Services.DI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChineseRaffleApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.UserName) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                _logger.LogWarning("Login attempt with missing credentials.");
                return BadRequest(new { message = "userName and password are required." });
            }
            var result = await _userService.AuthenticateAsync(loginDto.UserName, loginDto.Password);

            if (result == null)
            {
                _logger.LogWarning("Invalid login attempt for user: {UserName}", loginDto.UserName);
                return Unauthorized(new { message = "Invalid userName or password." });
            }

            SetAuthCookie(result.Token, result.ExpiresIn);
            return Ok(result);
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] AddUserDto createDto)
        {
            try
            {
                var result = await _userService.RegisterAndAuthenticateAsync(createDto);
                SetAuthCookie(result.Token, result.ExpiresIn);
                return Created(string.Empty, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch(Exception ex)
            {
                _logger.LogError("An error occurred during registration: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        private void SetAuthCookie(string token, int expiresInSeconds)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = false,
                Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds)
            };
            Response.Cookies.Append("jwt_token", token, cookieOptions);
        }
    }
}
