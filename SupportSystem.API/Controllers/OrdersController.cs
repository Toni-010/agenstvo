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
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Manager)
                .ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Manager)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // POST: api/Orders/create (для пользователя)
        [HttpPost("create")]
        public async Task<ActionResult<object>> CreateOrder([FromBody] CreateOrderDto orderDto)
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
                if (string.IsNullOrWhiteSpace(orderDto.OrderName))
                {
                    return BadRequest(new { message = "Название заказа обязательно" });
                }

                if (string.IsNullOrWhiteSpace(orderDto.Description))
                {
                    return BadRequest(new { message = "Описание обязательно" });
                }

                if (orderDto.OrderName.Length > 250)
                {
                    return BadRequest(new { message = "Название заказа не должно превышать 250 символов" });
                }

                if (orderDto.Description.Length > 3500)
                {
                    return BadRequest(new { message = "Описание не должно превышать 3500 символов" });
                }

                // Парсим приоритет
                if (!Enum.TryParse<Priority>(orderDto.Priority, true, out var priority))
                {
                    priority = Priority.Medium;
                }

                // Создаем новый заказ
                var order = new Order
                {
                    OrderName = orderDto.OrderName.Trim(),
                    Description = orderDto.Description.Trim(),
                    Cost = orderDto.Cost,
                    Priority = priority,
                    Status = OrderStatus.New,
                    CreateDate = DateTime.Now,
                    ClientId = userId,
                    AssignedToId = null, // Пока не назначен менеджер
                    CompleteDate = null // Заполнится при завершении
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Получаем данные клиента для ответа
                var client = await _context.Users.FindAsync(userId);

                return Ok(new
                {
                    success = true,
                    orderId = order.Id,
                    data = new
                    {
                        Id = order.Id,
                        OrderName = order.OrderName,
                        Description = order.Description,
                        Cost = order.Cost,
                        Status = order.Status.ToString(),
                        Priority = order.Priority.ToString(),
                        CreateDate = order.CreateDate,
                        ClientId = order.ClientId,
                        ClientName = client?.Name ?? "Неизвестный клиент",
                        AssignedToId = order.AssignedToId,
                        CompleteDate = order.CompleteDate
                    },
                    message = "Заказ успешно создан!"
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при создании заказа");
                return StatusCode(500, new
                {
                    message = "Ошибка сохранения заказа в базу данных",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании заказа",
                    error = ex.Message
                });
            }
        }

        // GET: api/Orders/my (получить заказы текущего пользователя)
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
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
                    .Include(o => o.Client)
                    .Include(o => o.Manager)
                    .OrderByDescending(o => o.CreateDate)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        OrderName = o.OrderName,
                        Description = o.Description,
                        Cost = o.Cost,
                        Status = o.Status.ToString(),
                        Priority = o.Priority.ToString(),
                        CreateDate = o.CreateDate,
                        CompleteDate = o.CompleteDate,
                        ClientId = o.ClientId,
                        ClientName = o.Client.Name,
                        AssignedToId = o.AssignedToId,
                        ManagerName = o.Manager != null ? o.Manager.Name : "Не назначен"
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказов пользователя");
                return StatusCode(500, new
                {
                    message = "Ошибка сервера при получении заказов",
                    error = ex.Message
                });
            }
        }

        // POST: api/Orders (старый метод для админов/менеджеров)
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}