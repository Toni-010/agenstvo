using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SupportSystem.API.Data;
using SupportSystem.API.Data.Models;
using SupportSystem.API.DTOs;
using Microsoft.Extensions.Logging;

namespace SupportSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Phone = u.Phone,
                        Role = u.Role.ToString(),
                        RegDate = u.RegDate
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении пользователей",
                    error = ex.Message
                });
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Пользователь может получить только свои данные, админ и менеджер - любые
                if (currentUserId != id && currentUserRole != "Admin" && currentUserRole != "Manager")
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    RegDate = user.RegDate
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя с ID {UserId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении пользователя",
                    error = ex.Message
                });
            }
        }

        // GET: api/Users/me - получение данных текущего пользователя
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    RegDate = user.RegDate
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении данных текущего пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении данных пользователя",
                    error = ex.Message
                });
            }
        }

        // POST: api/Users
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> PostUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                // Проверка на существующего пользователя
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == createUserDto.Email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "Пользователь с таким email уже существует"
                    });
                }

                // Проверка валидности роли
                if (!Enum.TryParse<Data.Enums.UserRole>(createUserDto.Role, out var userRole))
                {
                    userRole = Data.Enums.UserRole.User;
                }

                var user = new User
                {
                    Name = createUserDto.Name,
                    Email = createUserDto.Email,
                    Phone = createUserDto.Phone,
                    Password = createUserDto.Password, // В реальном приложении нужно хэшировать!
                    Role = userRole,
                    RegDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Возвращаем DTO без пароля
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.ToString(),
                    RegDate = user.RegDate
                };

                return CreatedAtAction("GetUser", new { id = user.Id }, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании пользователя",
                    error = ex.Message
                });
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                // Проверяем авторизацию - пользователь может редактировать только свой профиль
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Разрешаем редактировать:
                // 1. Самому пользователю (currentUserId == id)
                // 2. Администратору (Admin)
                // 3. Менеджеру (Manager) ← ДОБАВЛЯЕМ ЭТО!
                if (currentUserId != id && currentUserRole != "Admin" && currentUserRole != "Manager") // ИЗМЕНЕНИЕ ТУТ!
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Валидация
                if (string.IsNullOrWhiteSpace(updateUserDto.Name))
                {
                    return BadRequest(new { message = "Имя не может быть пустым" });
                }

                // Проверяем уникальность email, кроме текущего пользователя
                if (!string.IsNullOrEmpty(updateUserDto.Email))
                {
                    var existingUserWithEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == updateUserDto.Email && u.Id != id);

                    if (existingUserWithEmail != null)
                    {
                        return BadRequest(new { message = "Пользователь с таким email уже существует" });
                    }
                }

                // Обновляем только разрешенные поля
                user.Name = updateUserDto.Name.Trim();

                if (!string.IsNullOrEmpty(updateUserDto.Email))
                {
                    user.Email = updateUserDto.Email.Trim();
                }

                user.Phone = updateUserDto.Phone?.Trim();

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Данные успешно обновлены",
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        phone = user.Phone,
                        role = user.Role.ToString(),
                        regDate = user.RegDate
                    }
                });
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                _logger.LogError(dbEx, "Ошибка конкурентного доступа при обновлении пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка при обновлении данных. Попробуйте еще раз.",
                    error = dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя с ID {UserId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при обновлении данных",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                // Проверяем, нет ли связанных данных
                var hasOrders = await _context.Orders.AnyAsync(o => o.ClientId == id || o.AssignedToId == id);
                var hasServiceRequests = await _context.ServiceRequests.AnyAsync(s => s.ClientId == id || s.AssignedToId == id);
                var hasSupportRequests = await _context.SupportRequests.AnyAsync(s => s.ClientId == id || s.AssignedToId == id);
                var hasReports = await _context.Reports.AnyAsync(r => r.CreatedById == id);

                if (hasOrders || hasServiceRequests || hasSupportRequests || hasReports)
                {
                    return BadRequest(new
                    {
                        message = "Невозможно удалить пользователя, так как с ним связаны заказы, запросы или отчеты"
                    });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Пользователь успешно удален"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя с ID {UserId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при удалении пользователя",
                    error = ex.Message
                });
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}