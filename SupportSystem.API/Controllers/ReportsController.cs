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
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/Reports/create - создать новый отчет (ответ)
        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto reportDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                // Валидация
                if (string.IsNullOrWhiteSpace(reportDto.Title))
                {
                    return BadRequest(new { message = "Заголовок отчета обязателен" });
                }

                if (string.IsNullOrWhiteSpace(reportDto.Content))
                {
                    return BadRequest(new { message = "Содержание отчета обязательно" });
                }

                if (reportDto.Title.Length > 250)
                {
                    return BadRequest(new { message = "Заголовок не должен превышать 250 символов" });
                }

                if (reportDto.Content.Length > 3500)
                {
                    return BadRequest(new { message = "Содержание не должно превышать 3500 символов" });
                }

                // Проверяем, что хотя бы одна из сущностей указана
                if (!reportDto.OrderId.HasValue && !reportDto.ServiceRequestId.HasValue && !reportDto.SupportRequestId.HasValue)
                {
                    return BadRequest(new { message = "Укажите связанный заказ, сервисный запрос или запрос поддержки" });
                }

                // Если указан запрос поддержки, проверяем права
                if (reportDto.SupportRequestId.HasValue)
                {
                    var supportRequest = await _context.SupportRequests
                        .FirstOrDefaultAsync(sr => sr.Id == reportDto.SupportRequestId.Value);

                    if (supportRequest == null)
                    {
                        return NotFound(new { message = "Запрос поддержки не найден" });
                    }

                    // Проверяем, что менеджер назначен на этот запрос или является админом
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    if (supportRequest.AssignedToId != userId && userRole != "Admin")
                    {
                        return Forbid();
                    }
                }

                var report = new Report
                {
                    Title = reportDto.Title.Trim(),
                    Content = reportDto.Content.Trim(),
                    CreateDate = DateTime.Now,
                    CreatedById = userId,
                    OrderId = reportDto.OrderId,
                    ServiceRequestId = reportDto.ServiceRequestId,
                    SupportRequestId = reportDto.SupportRequestId
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Если это ответ на запрос поддержки и отмечен флаг завершения
                if (reportDto.SupportRequestId.HasValue && reportDto.CompleteRequest)
                {
                    var supportRequest = await _context.SupportRequests
                        .FirstOrDefaultAsync(sr => sr.Id == reportDto.SupportRequestId.Value);

                    if (supportRequest != null)
                    {
                        supportRequest.Status = Data.Enums.RequestStatus.Completed;
                        await _context.SaveChangesAsync();
                    }
                }

                // Получаем автора
                var author = await _context.Users.FindAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "Отчет успешно создан",
                    report = new
                    {
                        Id = report.Id,
                        Title = report.Title,
                        Content = report.Content,
                        CreateDate = report.CreateDate,
                        CreatedById = report.CreatedById,
                        AuthorName = author?.Name ?? "Неизвестный",
                        AuthorEmail = author?.Email,
                        OrderId = report.OrderId,
                        ServiceRequestId = report.ServiceRequestId,
                        SupportRequestId = report.SupportRequestId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании отчета");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании отчета",
                    error = ex.Message
                });
            }
        }

        // GET: api/Reports/support-request/5 - получить отчеты по запросу поддержки
        [HttpGet("support-request/{supportRequestId}")]
        public async Task<IActionResult> GetReportsBySupportRequest(int supportRequestId)
        {
            try
            {
                var reports = await _context.Reports
                    .Where(r => r.SupportRequestId == supportRequestId)
                    .Include(r => r.Author)
                    .OrderByDescending(r => r.CreateDate)
                    .Select(r => new ReportDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Content = r.Content,
                        CreateDate = r.CreateDate,
                        CreatedById = r.CreatedById,
                        AuthorName = r.Author.Name,
                        AuthorEmail = r.Author.Email,
                        SupportRequestId = r.SupportRequestId
                    })
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении отчетов для запроса поддержки {Id}", supportRequestId);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении отчетов",
                    error = ex.Message
                });
            }
        }
    }
}