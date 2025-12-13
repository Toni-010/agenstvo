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

        #region Методы для клиентов

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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                if (!int.TryParse(userIdClaim, out int userId) || userId == 0)
                {
                    return Unauthorized(new { message = "Неверный идентификатор пользователя" });
                }

                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return BadRequest(new { message = "Пользователь не найден" });
                }

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

                if (supportRequestDto.RelatedOrderId.HasValue && supportRequestDto.RelatedOrderId.Value > 0)
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == supportRequestDto.RelatedOrderId.Value && o.ClientId == userId);

                    if (order == null)
                    {
                        return BadRequest(new { message = "Указанный заказ не найден или не принадлежит вам" });
                    }
                }

                var supportRequest = new SupportRequest
                {
                    Topic = supportRequestDto.Topic.Trim(),
                    Message = supportRequestDto.Message.Trim(),
                    Status = RequestStatus.New,
                    CreateDate = DateTime.Now,
                    ClientId = userId,
                    AssignedToId = null,
                    RelatedOrderId = supportRequestDto.RelatedOrderId
                };

                _context.SupportRequests.Add(supportRequest);
                await _context.SaveChangesAsync();

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

        #endregion

        #region Методы для менеджеров и админов

        // GET: api/SupportRequests (получить все запросы поддержки)
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

        // GET: api/SupportRequests/manager - получить все запросы поддержки с полными данными
        [HttpGet("manager")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<SupportRequestDetailDto>>> GetSupportRequestsForManager()
        {
            try
            {
                var supportRequests = await _context.SupportRequests
                    .Include(s => s.Client)
                    .Include(s => s.Manager)
                    .Include(s => s.RelatedOrder)
                    .Include(s => s.Reports)
                    .OrderByDescending(s => s.CreateDate)
                    .Select(s => new SupportRequestDetailDto
                    {
                        Id = s.Id,
                        Topic = s.Topic,
                        Message = s.Message,
                        Status = s.Status.ToString(),
                        CreateDate = s.CreateDate,
                        ClientId = s.ClientId,
                        ClientName = s.Client.Name,
                        ClientEmail = s.Client.Email,
                        ClientPhone = s.Client.Phone,
                        AssignedToId = s.AssignedToId,
                        ManagerName = s.Manager != null ? s.Manager.Name : "Не назначен",
                        ManagerEmail = s.Manager != null ? s.Manager.Email : null,
                        ManagerPhone = s.Manager != null ? s.Manager.Phone : null,
                        RelatedOrderId = s.RelatedOrderId,
                        OrderName = s.RelatedOrder != null ? s.RelatedOrder.OrderName : null,
                        ReportCount = s.Reports.Count,
                        LastReportDate = s.Reports.Any() ? s.Reports.Max(r => r.CreateDate) : null
                    })
                    .ToListAsync();

                return Ok(supportRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка запросов в поддержку для менеджера");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении запросов",
                    error = ex.Message
                });
            }
        }

        // GET: api/SupportRequests/manager/detail/5 - получить детальную информацию о запросе поддержки
        [HttpGet("manager/detail/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetSupportRequestDetailForManager(int id)
        {
            try
            {
                var supportRequest = await _context.SupportRequests
                    .Include(s => s.Client)
                    .Include(s => s.Manager)
                    .Include(s => s.RelatedOrder)
                    .Include(s => s.Reports)
                        .ThenInclude(r => r.Author)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supportRequest == null)
                {
                    return NotFound(new { message = "Запрос поддержки не найден" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (supportRequest.AssignedToId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                var supportRequestDetail = new
                {
                    Id = supportRequest.Id,
                    Topic = supportRequest.Topic,
                    Message = supportRequest.Message,
                    Status = supportRequest.Status.ToString(),
                    CreateDate = supportRequest.CreateDate,

                    Client = new
                    {
                        Id = supportRequest.Client.Id,
                        Name = supportRequest.Client.Name,
                        Email = supportRequest.Client.Email ?? "Не указан",
                        Phone = supportRequest.Client.Phone ?? "Не указан"
                    },

                    Manager = supportRequest.Manager != null ? new
                    {
                        Id = supportRequest.Manager.Id,
                        Name = supportRequest.Manager.Name,
                        Email = supportRequest.Manager.Email ?? "Не указан",
                        Phone = supportRequest.Manager.Phone ?? "Не указан"
                    } : null,

                    RelatedOrder = supportRequest.RelatedOrder != null ? new
                    {
                        Id = supportRequest.RelatedOrder.Id,
                        OrderName = supportRequest.RelatedOrder.OrderName,
                        Status = supportRequest.RelatedOrder.Status.ToString()
                    } : null,

                    Reports = supportRequest.Reports.Select(r => new
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Content = r.Content,
                        CreateDate = r.CreateDate,
                        Author = new
                        {
                            Id = r.Author.Id,
                            Name = r.Author.Name,
                            Email = r.Author.Email ?? "Не указан"
                        }
                    }).OrderByDescending(r => r.CreateDate).ToList(),

                    Statistics = new
                    {
                        ReportCount = supportRequest.Reports.Count,
                        LastResponseDate = supportRequest.Reports.Any() ?
                            supportRequest.Reports.Max(r => r.CreateDate) : (DateTime?)null,
                        DaysSinceCreation = (DateTime.Now - supportRequest.CreateDate).TotalDays
                    }
                };

                return Ok(new
                {
                    success = true,
                    supportRequest = supportRequestDetail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении детальной информации о запросе поддержки {Id}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении информации о запросе",
                    error = ex.Message
                });
            }
        }

        // PUT: api/SupportRequests/manager/assign/5 - назначить себя на запрос поддержки
        [HttpPut("manager/assign/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignSupportRequestToSelf(int id)
        {
            try
            {
                var supportRequest = await _context.SupportRequests.FindAsync(id);
                if (supportRequest == null)
                {
                    return NotFound(new { message = "Запрос поддержки не найден" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                supportRequest.AssignedToId = userId;

                if (supportRequest.Status == RequestStatus.New)
                {
                    supportRequest.Status = RequestStatus.Processing;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Запрос поддержки успешно назначен на вас",
                    supportRequestId = supportRequest.Id,
                    assignedToId = supportRequest.AssignedToId,
                    status = supportRequest.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при назначении запроса поддержки {Id} на менеджера", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при назначении запроса",
                    error = ex.Message
                });
            }
        }

        // POST: api/SupportRequests/manager/respond/5 - создать ответ на запрос поддержки
        [HttpPost("manager/respond/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RespondToSupportRequest(int id, [FromBody] CreateReportDto reportDto)
        {
            try
            {
                var supportRequest = await _context.SupportRequests.FindAsync(id);
                if (supportRequest == null)
                {
                    return NotFound(new { message = "Запрос поддержки не найден" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (supportRequest.AssignedToId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                if (string.IsNullOrWhiteSpace(reportDto.Title))
                {
                    return BadRequest(new { message = "Заголовок ответа обязателен" });
                }

                if (string.IsNullOrWhiteSpace(reportDto.Content))
                {
                    return BadRequest(new { message = "Содержание ответа обязательно" });
                }

                if (reportDto.Title.Length > 250)
                {
                    return BadRequest(new { message = "Заголовок не должен превышать 250 символов" });
                }

                if (reportDto.Content.Length > 3500)
                {
                    return BadRequest(new { message = "Содержание не должно превышать 3500 символов" });
                }

                var report = new Report
                {
                    Title = reportDto.Title.Trim(),
                    Content = reportDto.Content.Trim(),
                    CreateDate = DateTime.Now,
                    CreatedById = userId,
                    SupportRequestId = id,
                    ServiceRequestId = null,
                    OrderId = supportRequest.RelatedOrderId
                };

                _context.Reports.Add(report);

                if (reportDto.CompleteRequest)
                {
                    supportRequest.Status = RequestStatus.Completed;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = reportDto.CompleteRequest ?
                        "Ответ отправлен и запрос завершен" : "Ответ успешно отправлен",
                    reportId = report.Id,
                    supportRequestId = supportRequest.Id,
                    newStatus = supportRequest.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании ответа на запрос поддержки {Id}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при отправке ответа",
                    error = ex.Message
                });
            }
        }

        // PUT: api/SupportRequests/manager/status/5 - изменить статус запроса поддержки
        [HttpPut("manager/status/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateSupportRequestStatus(int id, [FromBody] UpdateSupportRequestStatusDto statusDto)
        {
            try
            {
                var supportRequest = await _context.SupportRequests.FindAsync(id);
                if (supportRequest == null)
                {
                    return NotFound(new { message = "Запрос поддержки не найден" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (supportRequest.AssignedToId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                if (!Enum.TryParse<RequestStatus>(statusDto.Status, true, out var newStatus))
                {
                    return BadRequest(new { message = "Некорректный статус" });
                }

                supportRequest.Status = newStatus;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Статус запроса успешно обновлен",
                    supportRequestId = supportRequest.Id,
                    newStatus = supportRequest.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении статуса запроса поддержки {Id}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при изменении статуса",
                    error = ex.Message
                });
            }
        }

        // GET: api/SupportRequests/manager/stats - статистика по запросам поддержки
        [HttpGet("manager/stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetSupportRequestStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var today = DateTime.Today;

                var stats = new
                {
                    TotalRequests = await _context.SupportRequests.CountAsync(),
                    NewRequests = await _context.SupportRequests.CountAsync(s => s.Status == RequestStatus.New),
                    ProcessingRequests = await _context.SupportRequests.CountAsync(s => s.Status == RequestStatus.Processing),
                    CompletedRequests = await _context.SupportRequests.CountAsync(s => s.Status == RequestStatus.Completed),
                    CancelledRequests = await _context.SupportRequests.CountAsync(s => s.Status == RequestStatus.Cancelled),
                    MyRequests = await _context.SupportRequests.CountAsync(s => s.AssignedToId == userId),
                    TodayRequests = await _context.SupportRequests.CountAsync(s => s.CreateDate.Date == today),
                    WithoutResponse = await _context.SupportRequests.CountAsync(s =>
                        s.Status == RequestStatus.New || s.Status == RequestStatus.Processing)
                };

                return Ok(new
                {
                    success = true,
                    stats = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики запросов поддержки");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении статистики",
                    error = ex.Message
                });
            }
        }

        #endregion
    }

    #region DTO классы

    public class UpdateSupportRequestStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateReportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool CompleteRequest { get; set; } = false;
    }

    public class SupportRequestDetailDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public string? ManagerPhone { get; set; }
        public int? RelatedOrderId { get; set; }
        public string? OrderName { get; set; }
        public int ReportCount { get; set; }
        public DateTime? LastReportDate { get; set; }
    }

    #endregion
}