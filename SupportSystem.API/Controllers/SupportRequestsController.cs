using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SupportSystem.API.Data;
using SupportSystem.API.Data.Models;
using SupportSystem.API.Data.Enums;
using SupportSystem.API.DTOs;
using Microsoft.Extensions.Logging;

namespace SupportSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupportRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupportRequestsController> _logger;

        public SupportRequestsController(ApplicationDbContext context, ILogger<SupportRequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/SupportRequests/my (получить мои запросы в поддержку)
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<SupportRequestDto>>> GetMySupportRequests()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                var supportRequests = await _context.SupportRequests
                    .Where(s => s.ClientId == userId)
                    .Include(s => s.Client)
                    .Include(s => s.Manager)
                    .Include(s => s.RelatedOrder)
                    .OrderByDescending(s => s.CreateDate)
                    .Select(s => new SupportRequestDto
                    {
                        Id = s.Id,
                        Topic = s.Topic,
                        Message = s.Message,
                        Status = s.Status.ToString(),
                        CreateDate = s.CreateDate,
                        ClientId = s.ClientId,
                        ClientName = s.Client.Name,
                        AssignedToId = s.AssignedToId,
                        ManagerName = s.Manager != null ? s.Manager.Name : "Не назначен",
                        RelatedOrderId = s.RelatedOrderId,
                        OrderName = s.RelatedOrder != null ? s.RelatedOrder.OrderName : null
                    })
                    .ToListAsync();

                return Ok(supportRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении запросов в поддержку");
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при получении запросов",
                    error = ex.Message
                });
            }
        }

        // GET: api/SupportRequests/my-orders (получить мои заказы для выпадающего списка)
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderDropdownDto>>> GetMyOrdersForDropdown()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                var orders = await _context.Orders
                    .Where(o => o.ClientId == userId)
                    .OrderByDescending(o => o.CreateDate)
                    .Select(o => new OrderDropdownDto
                    {
                        Id = o.Id,
                        OrderName = o.OrderName,
                        Status = o.Status.ToString(),
                        CreateDate = o.CreateDate
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказов для выпадающего списка");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении заказов",
                    error = ex.Message
                });
            }
        }

        // GET: api/SupportRequests/5 (получить конкретный запрос)
        [HttpGet("{id}")]
        public async Task<ActionResult<SupportRequestDto>> GetSupportRequest(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                var supportRequest = await _context.SupportRequests
                    .Include(s => s.Client)
                    .Include(s => s.Manager)
                    .Include(s => s.RelatedOrder)
                    .FirstOrDefaultAsync(s => s.Id == id && s.ClientId == userId);

                if (supportRequest == null)
                {
                    return NotFound(new { message = "Запрос в поддержку не найден" });
                }

                var supportRequestDto = new SupportRequestDto
                {
                    Id = supportRequest.Id,
                    Topic = supportRequest.Topic,
                    Message = supportRequest.Message,
                    Status = supportRequest.Status.ToString(),
                    CreateDate = supportRequest.CreateDate,
                    ClientId = supportRequest.ClientId,
                    ClientName = supportRequest.Client.Name,
                    AssignedToId = supportRequest.AssignedToId,
                    ManagerName = supportRequest.Manager != null ? supportRequest.Manager.Name : "Не назначен",
                    RelatedOrderId = supportRequest.RelatedOrderId,
                    OrderName = supportRequest.RelatedOrder != null ? supportRequest.RelatedOrder.OrderName : null
                };

                return Ok(supportRequestDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении запроса в поддержку с ID {Id}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении запроса",
                    error = ex.Message
                });
            }
        }

        // POST: api/SupportRequests/create (создать новый запрос в поддержку)
        [HttpPost("create")]
        public async Task<ActionResult<object>> CreateSupportRequest([FromBody] CreateSupportRequestDto supportRequestDto)
        {
            try
            {
                // Получаем ID текущего пользователя из токена
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                if (!int.TryParse(userIdClaim, out int userId) || userId == 0)
                {
                    return Unauthorized(new { message = "Неверный идентификатор пользователя" });
                }

                // Проверяем существование пользователя
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return BadRequest(new { message = "Пользователь не найден" });
                }

                // Валидация входных данных
                if (string.IsNullOrWhiteSpace(supportRequestDto.Topic))
                {
                    return BadRequest(new { message = "Тема запроса обязательна" });
                }

                if (string.IsNullOrWhiteSpace(supportRequestDto.Message))
                {
                    return BadRequest(new { message = "Сообщение обязательно" });
                }

                if (supportRequestDto.Topic.Length > 250)
                {
                    return BadRequest(new { message = "Тема не должна превышать 250 символов" });
                }

                if (supportRequestDto.Message.Length > 3500)
                {
                    return BadRequest(new { message = "Сообщение не должно превышать 3500 символов" });
                }

                // Если указан заказ, проверяем его существование и принадлежность пользователю
                if (supportRequestDto.RelatedOrderId.HasValue && supportRequestDto.RelatedOrderId.Value > 0)
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == supportRequestDto.RelatedOrderId.Value && o.ClientId == userId);

                    if (order == null)
                    {
                        return BadRequest(new { message = "Указанный заказ не найден или не принадлежит вам" });
                    }
                }

                // Создаем новый запрос в поддержку
                var supportRequest = new SupportRequest
                {
                    Topic = supportRequestDto.Topic.Trim(),
                    Message = supportRequestDto.Message.Trim(),
                    Status = RequestStatus.New,
                    CreateDate = DateTime.Now,
                    ClientId = userId,
                    AssignedToId = null, // Пока не назначен менеджер
                    RelatedOrderId = supportRequestDto.RelatedOrderId
                };

                _context.SupportRequests.Add(supportRequest);
                await _context.SaveChangesAsync();

                // Получаем данные для ответа
                var client = await _context.Users.FindAsync(userId);
                string orderName = null;
                if (supportRequest.RelatedOrderId.HasValue)
                {
                    var relatedOrder = await _context.Orders.FindAsync(supportRequest.RelatedOrderId.Value);
                    orderName = relatedOrder?.OrderName;
                }

                return Ok(new
                {
                    success = true,
                    supportRequestId = supportRequest.Id,
                    data = new
                    {
                        Id = supportRequest.Id,
                        Topic = supportRequest.Topic,
                        Message = supportRequest.Message,
                        Status = supportRequest.Status.ToString(),
                        CreateDate = supportRequest.CreateDate,
                        ClientId = supportRequest.ClientId,
                        ClientName = client?.Name ?? "Неизвестный клиент",
                        AssignedToId = supportRequest.AssignedToId,
                        RelatedOrderId = supportRequest.RelatedOrderId,
                        OrderName = orderName
                    },
                    message = "Запрос в поддержку успешно создан! Скоро с вами свяжется менеджер."
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при создании запроса в поддержку");
                return StatusCode(500, new
                {
                    message = "Ошибка сохранения запроса в базу данных",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании запроса в поддержку");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании запроса в поддержку",
                    error = ex.Message
                });
            }
        }

        // GET: api/SupportRequests (для менеджеров и админов)
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<SupportRequestDto>>> GetSupportRequests()
        {
            try
            {
                var supportRequests = await _context.SupportRequests
                    .Include(s => s.Client)
                    .Include(s => s.Manager)
                    .Include(s => s.RelatedOrder)
                    .OrderByDescending(s => s.CreateDate)
                    .Select(s => new SupportRequestDto
                    {
                        Id = s.Id,
                        Topic = s.Topic,
                        Message = s.Message,
                        Status = s.Status.ToString(),
                        CreateDate = s.CreateDate,
                        ClientId = s.ClientId,
                        ClientName = s.Client.Name,
                        AssignedToId = s.AssignedToId,
                        ManagerName = s.Manager != null ? s.Manager.Name : "Не назначен",
                        RelatedOrderId = s.RelatedOrderId,
                        OrderName = s.RelatedOrder != null ? s.RelatedOrder.OrderName : null
                    })
                    .ToListAsync();

                return Ok(supportRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка запросов в поддержку");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении запросов",
                    error = ex.Message
                });
            }
        }
    }
}