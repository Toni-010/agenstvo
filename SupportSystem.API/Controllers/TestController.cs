using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportSystem.API.Data;

namespace SupportSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("db-check")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                // Пробуем выполнить простой запрос
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new
                    {
                        message = "✅ Подключение к БД успешно!",
                        database = _context.Database.GetDbConnection().Database,
                        server = _context.Database.GetDbConnection().DataSource
                    });
                }
                else
                {
                    return BadRequest(new { message = "❌ Не удалось подключиться к БД" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Ошибка подключения к БД",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }
}