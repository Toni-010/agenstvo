using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupportSystem.API.Data;
using SupportSystem.API.Data.Enums;
using SupportSystem.API.Data.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SupportSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return BadRequest(new { message = "Email и пароль обязательны" });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || user.Password != loginDto.Password)
                {
                    _logger.LogWarning("Неудачная попытка входа для email: {Email}", loginDto.Email);
                    return Unauthorized(new { message = "Неверный email или пароль" });
                }

                var token = GenerateJwtToken(user);

                _logger.LogInformation("Успешный вход пользователя: {UserId} ({Email})", user.Id, user.Email);

                return Ok(new
                {
                    success = true,
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        role = user.Role.ToString()
                    },
                    message = "Вход выполнен успешно"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка при входе в систему",
                    error = ex.Message
                });
            }
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(registerDto.Name))
                {
                    return BadRequest(new { message = "Имя обязательно" });
                }

                if (string.IsNullOrWhiteSpace(registerDto.Email))
                {
                    return BadRequest(new { message = "Email обязателен" });
                }

                if (string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    return BadRequest(new { message = "Пароль обязателен" });
                }

                if (registerDto.Password.Length < 6)
                {
                    return BadRequest(new { message = "Пароль должен содержать минимум 6 символов" });
                }

                
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(registerDto.Email))
                {
                    return BadRequest(new { message = "Неверный формат email" });
                }

                // Проверка на существующего пользователя
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "Пользователь с таким email уже существует"
                    });
                }

                
                if (!Enum.TryParse<UserRole>(registerDto.Role, true, out var userRole))
                {
                    userRole = UserRole.User; 
                }

                
                var user = new User
                {
                    Name = registerDto.Name.Trim(),
                    Email = registerDto.Email.Trim().ToLower(),
                    Phone = registerDto.Phone?.Trim(),
                    Password = registerDto.Password,
                    Role = userRole,
                    RegDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Генерируем токен для автоматического входа после регистрации
                var token = GenerateJwtToken(user);

                _logger.LogInformation("Зарегистрирован новый пользователь: {UserId} ({Email})", user.Id, user.Email);

                return Ok(new
                {
                    success = true,
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        phone = user.Phone,
                        role = user.Role.ToString()
                    },
                    message = "Регистрация успешна!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка при регистрации",
                    error = ex.Message
                });
            }
        }

        // POST: api/Auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                // Валидация
                if (string.IsNullOrWhiteSpace(changePasswordDto.OldPassword))
                {
                    return BadRequest(new { message = "Старый пароль обязателен" });
                }

                if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
                {
                    return BadRequest(new { message = "Новый пароль обязателен" });
                }

                if (changePasswordDto.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Новый пароль должен содержать минимум 6 символов" });
                }

                
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                if (user.Password != changePasswordDto.OldPassword)
                {
                    return BadRequest(new { message = "Неверный старый пароль" });
                }

                
                if (user.Password == changePasswordDto.NewPassword)
                {
                    return BadRequest(new { message = "Новый пароль должен отличаться от старого" });
                }

                
                user.Password = changePasswordDto.NewPassword;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Пароль изменен для пользователя: {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Пароль успешно изменен"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене пароля");
                return StatusCode(500, new
                {
                    message = "Ошибка при смене пароля",
                    error = ex.Message
                });
            }
        }

        // GET: api/Auth/check - проверка токена
        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Токен недействителен или отсутствует" });
                }

                return Ok(new
                {
                    success = true,
                    userId,
                    userName,
                    userRole,
                    message = "Токен действителен"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке токена");
                return StatusCode(500, new
                {
                    message = "Ошибка при проверке авторизации",
                    error = ex.Message
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "your-super-secret-key-minimum-32-characters-long-for-security"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "SupportSystem",
                audience: _configuration["Jwt:Audience"] ?? "SupportSystemClients",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "User";
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}