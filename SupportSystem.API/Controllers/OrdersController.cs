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

        #region Публичные методы (для всех пользователей)

        // GET: api/Orders
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            try
            {
                var orders = await _context.Orders
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
                        ClientEmail = o.Client.Email ?? "Не указан",
                        ClientPhone = o.Client.Phone ?? "Не указан",
                        AssignedToId = o.AssignedToId,
                        ManagerName = o.Manager != null ? o.Manager.Name : "Не назначен",
                        ManagerEmail = o.Manager != null ? o.Manager.Email ?? "Не указан" : null,
                        ManagerPhone = o.Manager != null ? o.Manager.Phone ?? "Не указан" : null,
                        ManagerRole = o.Manager != null ? o.Manager.Role.ToString() : null
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка заказов");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении заказов",
                    error = ex.Message
                });
            }
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Manager)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (order.ClientId != userId && userRole != "Admin" && userRole != "Manager")
                {
                    return Forbid();
                }

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    OrderName = order.OrderName,
                    Description = order.Description,
                    Cost = order.Cost,
                    Status = order.Status.ToString(),
                    Priority = order.Priority.ToString(),
                    CreateDate = order.CreateDate,
                    CompleteDate = order.CompleteDate,
                    ClientId = order.ClientId,
                    ClientName = order.Client.Name,
                    AssignedToId = order.AssignedToId,
                    ManagerName = order.Manager != null ? order.Manager.Name : "Не назначен",
                    ClientEmail = order.Client.Email ?? "Не указан",
                    ClientPhone = order.Client.Phone ?? "Не указан",
                    ManagerEmail = order.Manager != null ? order.Manager.Email ?? "Не указан" : null,
                    ManagerPhone = order.Manager != null ? order.Manager.Phone ?? "Не указан" : null,
                    ManagerRole = order.Manager != null ? order.Manager.Role.ToString() : null
                };

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказа с ID {OrderId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении заказа",
                    error = ex.Message
                });
            }
        }

        // POST: api/Orders/create
        [HttpPost("create")]
        public async Task<ActionResult<object>> CreateOrder([FromBody] CreateOrderDto orderDto)
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

                if (!Enum.TryParse<Priority>(orderDto.Priority, true, out var priority))
                {
                    priority = Priority.Medium;
                }

                var order = new Order
                {
                    OrderName = orderDto.OrderName.Trim(),
                    Description = orderDto.Description.Trim(),
                    Cost = orderDto.Cost,
                    Priority = priority,
                    Status = OrderStatus.New,
                    CreateDate = DateTime.Now,
                    ClientId = userId,
                    AssignedToId = null,
                    CompleteDate = null
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

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
                        ClientEmail = client?.Email ?? "Не указан",      // ← добавьте
                        ClientPhone = client?.Phone ?? "Не указан",
                        AssignedToId = order.AssignedToId,
                        CompleteDate = order.CompleteDate,
                        ManagerName = string.Empty,
                        ManagerEmail = string.Empty,
                        ManagerPhone = string.Empty,
                        ManagerRole = string.Empty
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

        #endregion

        #region Методы для клиентов

        // GET: api/Orders/my
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
                        ClientEmail = o.Client.Email ?? "Не указан",
                        ClientPhone = o.Client.Phone ?? "Не указан",
                        AssignedToId = o.AssignedToId,
                        ManagerName = o.Manager != null ? o.Manager.Name : "Не назначен",
                        ManagerEmail = o.Manager != null ? o.Manager.Email ?? "Не указан" : null,
                        ManagerPhone = o.Manager != null ? o.Manager.Phone ?? "Не указан" : null,
                        ManagerRole = o.Manager != null ? o.Manager.Role.ToString() : null
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

        #endregion

        #region Методы для менеджеров и админов

        // GET: api/Orders/manager
        [HttpGet("manager")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetManagerOrders()
        {
            try
            {
                var orders = await _context.Orders
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
                        ClientEmail = o.Client.Email ?? "Не указан",
                        ClientPhone = o.Client.Phone ?? "Не указан",
                        AssignedToId = o.AssignedToId,
                        ManagerName = o.Manager != null ? o.Manager.Name : "Не назначен",
                        ManagerEmail = o.Manager != null ? o.Manager.Email ?? "Не указан" : null,
                        ManagerPhone = o.Manager != null ? o.Manager.Phone ?? "Не указан" : null,
                        ManagerRole = o.Manager != null ? o.Manager.Role.ToString() : null
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказов для менеджера");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении заказов",
                    error = ex.Message
                });
            }
        }

        // В контроллер OrdersController.cs добавим этот метод:

        // Обновим метод GetOrderDetailForManager в OrdersController.cs

        [HttpGet("manager/detail/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetOrderDetailForManager(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Manager)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                // Получаем только сервисные запросы, связанные с этим заказом
                var serviceRequests = await _context.ServiceRequests
                    .Where(sr => sr.OrderId == id)
                    .Include(sr => sr.Client)
                    .Select(sr => new
                    {
                        Id = sr.Id,
                        ServiceType = sr.ServiceType,
                        Description = sr.Description,
                        Status = sr.Status.ToString(),
                        CreateDate = sr.CreateDate,
                        Cost = sr.Cost,
                        ClientName = sr.Client.Name
                    })
                    .ToListAsync();

                // Получаем только запросы поддержки, связанные с этим заказом
                var supportRequests = await _context.SupportRequests
                    .Where(sr => sr.RelatedOrderId == id)
                    .Include(sr => sr.Client)
                    .Select(sr => new
                    {
                        Id = sr.Id,
                        Topic = sr.Topic,
                        Message = sr.Message,
                        Status = sr.Status.ToString(),
                        CreateDate = sr.CreateDate,
                        ClientName = sr.Client.Name,
                        ClientEmail = sr.Client.Email,
                        ClientPhone = sr.Client.Phone
                    })
                    .ToListAsync();

                // Получаем отчеты, связанные с этим заказом
                var reports = await _context.Reports
                    .Where(r => r.OrderId == id)
                    .Include(r => r.Author)
                    .Select(r => new
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Content = r.Content,
                        CreateDate = r.CreateDate,
                        AuthorName = r.Author.Name,
                        AuthorEmail = r.Author.Email
                    })
                    .ToListAsync();

                var orderDetail = new
                {
                    Id = order.Id,
                    OrderName = order.OrderName,
                    Description = order.Description,
                    Cost = order.Cost,
                    Status = order.Status.ToString(),
                    Priority = order.Priority.ToString(),
                    CreateDate = order.CreateDate,
                    CompleteDate = order.CompleteDate,

                    // Полные данные о клиенте
                    Client = new
                    {
                        Id = order.Client.Id,
                        Name = order.Client.Name,
                        Email = order.Client.Email ?? "Не указан",
                        Phone = order.Client.Phone ?? "Не указан",
                        RegistrationDate = order.Client.RegDate
                    },

                    // Полные данные о менеджере (если назначен)
                    Manager = order.Manager != null ? new
                    {
                        Id = order.Manager.Id,
                        Name = order.Manager.Name,
                        Email = order.Manager.Email ?? "Не указан",
                        Phone = order.Manager.Phone ?? "Не указан",
                        Role = order.Manager.Role.ToString()
                    } : null,

                    ServiceRequests = serviceRequests,
                    SupportRequests = supportRequests,
                    Reports = reports,

                    Statistics = new
                    {
                        ServiceRequestCount = serviceRequests.Count,
                        SupportRequestCount = supportRequests.Count,
                        ReportCount = reports.Count,
                        TotalTimeInDays = order.CompleteDate.HasValue ?
                            (order.CompleteDate.Value - order.CreateDate).TotalDays : 0,

                        // Дополнительная статистика
                        HighPriorityServiceRequests = serviceRequests.Count(sr => sr.Status == "New" || sr.Status == "Processing"),
                        ActiveSupportRequests = supportRequests.Count(sr => sr.Status == "New" || sr.Status == "Processing"),
                        TotalCost = serviceRequests.Where(sr => sr.Cost.HasValue).Sum(sr => sr.Cost.Value)
                    }
                };

                return Ok(new
                {
                    success = true,
                    order = orderDetail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении детальной информации о заказе {OrderId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при получении информации о заказе",
                    error = ex.Message
                });
            }
        }

        // PUT: api/Orders/manager/5
        [HttpPut("manager/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateOrderAsManager(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Manager)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return Unauthorized(new { message = "Менеджер не авторизован" });
                }

                var managerExists = await _context.Users.AnyAsync(u => u.Id == managerId);
                if (!managerExists)
                {
                    return BadRequest(new { message = "Менеджер не найден" });
                }

                if (updateOrderDto.OrderName != null)
                {
                    if (string.IsNullOrWhiteSpace(updateOrderDto.OrderName))
                    {
                        return BadRequest(new { message = "Название заказа не может быть пустым" });
                    }
                    if (updateOrderDto.OrderName.Length > 250)
                    {
                        return BadRequest(new { message = "Название заказа не должно превышать 250 символов" });
                    }
                    order.OrderName = updateOrderDto.OrderName.Trim();
                }

                if (updateOrderDto.Description != null)
                {
                    if (string.IsNullOrWhiteSpace(updateOrderDto.Description))
                    {
                        return BadRequest(new { message = "Описание не может быть пустым" });
                    }
                    if (updateOrderDto.Description.Length > 3500)
                    {
                        return BadRequest(new { message = "Описание не должно превышать 3500 символов" });
                    }
                    order.Description = updateOrderDto.Description.Trim();
                }

                if (updateOrderDto.Cost.HasValue)
                {
                    order.Cost = updateOrderDto.Cost;
                }

                if (!string.IsNullOrEmpty(updateOrderDto.Status))
                {
                    if (Enum.TryParse<OrderStatus>(updateOrderDto.Status, true, out var newStatus))
                    {
                        order.Status = newStatus;

                        if (newStatus == OrderStatus.Completed && !order.CompleteDate.HasValue)
                        {
                            order.CompleteDate = DateTime.Now;
                        }
                        else if (newStatus != OrderStatus.Completed)
                        {
                            order.CompleteDate = null;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(updateOrderDto.Priority))
                {
                    if (Enum.TryParse<Priority>(updateOrderDto.Priority, true, out var newPriority))
                    {
                        order.Priority = newPriority;
                    }
                }

                if (updateOrderDto.AssignedToId.HasValue)
                {
                    if (updateOrderDto.AssignedToId == 0)
                    {
                        order.AssignedToId = null;
                    }
                    else
                    {
                        var assignedManager = await _context.Users.FindAsync(updateOrderDto.AssignedToId.Value);
                        if (assignedManager == null)
                        {
                            return BadRequest(new { message = "Назначенный менеджер не найден" });
                        }
                        order.AssignedToId = updateOrderDto.AssignedToId.Value;
                    }
                }

                _context.Entry(order).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                var updatedOrder = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Manager)
                    .FirstOrDefaultAsync(o => o.Id == id);

                return Ok(new
                {
                    success = true,
                    message = "Заказ успешно обновлен",
                    order = new OrderDto
                    {
                        Id = updatedOrder.Id,
                        OrderName = updatedOrder.OrderName,
                        Description = updatedOrder.Description,
                        Cost = updatedOrder.Cost,
                        Status = updatedOrder.Status.ToString(),
                        Priority = updatedOrder.Priority.ToString(),
                        CreateDate = updatedOrder.CreateDate,
                        CompleteDate = updatedOrder.CompleteDate,
                        ClientId = updatedOrder.ClientId,
                        ClientName = updatedOrder.Client.Name,
                        ClientEmail = updatedOrder.Client.Email ?? "Не указан",      // ← добавьте
                        ClientPhone = updatedOrder.Client.Phone ?? "Не указан",      // ← добавьте
                        AssignedToId = updatedOrder.AssignedToId,
                        ManagerName = updatedOrder.Manager != null ? updatedOrder.Manager.Name : "Не назначен",
                        ManagerEmail = updatedOrder.Manager != null ? updatedOrder.Manager.Email ?? "Не указан" : null,
                        ManagerPhone = updatedOrder.Manager != null ? updatedOrder.Manager.Phone ?? "Не указан" : null,
                        ManagerRole = updatedOrder.Manager != null ? updatedOrder.Manager.Role.ToString() : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении заказа {OrderId} менеджером", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при обновлении заказа",
                    error = ex.Message
                });
            }
        }

        // PUT: api/Orders/manager/assign/5
        [HttpPut("manager/assign/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignOrderToSelf(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return Unauthorized(new { message = "Менеджер не авторизован" });
                }

                order.AssignedToId = managerId;

                if (order.Status == OrderStatus.New)
                {
                    order.Status = OrderStatus.Processing;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Заказ успешно назначен на вас",
                    orderId = order.Id,
                    assignedToId = order.AssignedToId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при назначении заказа {OrderId} на менеджера", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при назначении заказа",
                    error = ex.Message
                });
            }
        }

        // GET: api/Orders/manager/stats
        [HttpGet("manager/stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetOrderStats()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var today = DateTime.Today;

                var stats = new
                {
                    TotalOrders = await _context.Orders.CountAsync(),
                    NewOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.New),
                    ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing),
                    CompletedOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),
                    CancelledOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                    MyOrders = await _context.Orders.CountAsync(o => o.AssignedToId == userId),
                    HighPriorityOrders = await _context.Orders.CountAsync(o => o.Priority == Priority.High),
                    TodayOrders = await _context.Orders.CountAsync(o => o.CreateDate.Date == today)
                };

                return Ok(new
                {
                    success = true,
                    stats = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статистики заказов");
                return StatusCode(500, new
                {
                    message = "Ошибка при получении статистики",
                    error = ex.Message
                });
            }
        }

        #endregion

        #region Методы администратора

        // POST: api/Orders
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            try
            {
                if (order == null)
                {
                    return BadRequest(new { message = "Заказ не может быть пустым" });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetOrder", new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа администратором");
                return StatusCode(500, new
                {
                    message = "Ошибка при создании заказа",
                    error = ex.Message
                });
            }
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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


        // PUT: api/Orders/manager/unassign/5 - отказаться от заказа (отвязать менеджера)
        [HttpPut("manager/unassign/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UnassignOrderToSelf(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int managerId))
                {
                    return Unauthorized(new { message = "Менеджер не авторизован" });
                }

                // Проверяем, что заказ назначен именно на текущего менеджера
                if (order.AssignedToId != managerId)
                {
                    return BadRequest(new { message = "Этот заказ не назначен на вас" });
                }

                // Проверяем, что статус позволяет отказаться
                if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
                {
                    return BadRequest(new { message = "Нельзя отказаться от завершенного или отмененного заказа" });
                }

                // Отвязываем менеджера и возвращаем статус в "Новый"
                order.AssignedToId = null;
                order.Status = OrderStatus.New;
                order.CompleteDate = null; // Сбрасываем дату завершения

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Вы успешно отказались от заказа. Заказ возвращен в статус 'Новый'.",
                    orderId = order.Id,
                    assignedToId = order.AssignedToId,
                    status = order.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отказе от заказа {OrderId} менеджером", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при отказе от заказа",
                    error = ex.Message
                });
            }
        }


        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Заказ успешно удален"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа {OrderId}", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при удалении заказа",
                    error = ex.Message
                });
            }
        }
        // PUT: api/Orders/admin/unassign/5 - снять менеджера с заказа (для администратора)
        [HttpPut("admin/unassign/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignManager(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Заказ не найден" });
                }

                // Сохраняем информацию о старом менеджере для логов
                var oldManagerId = order.AssignedToId;
                var oldManager = oldManagerId.HasValue ?
                    await _context.Users.FindAsync(oldManagerId.Value) : null;

                // Админ может снять любого менеджера
                order.AssignedToId = null;

                // Возвращаем статус в "Новый" только если заказ был в обработке
                if (order.Status == OrderStatus.Processing)
                {
                    order.Status = OrderStatus.New;
                }

                order.CompleteDate = null; // Сбрасываем дату завершения

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Менеджер успешно снят с заказа",
                    orderId = order.Id,
                    assignedToId = order.AssignedToId,
                    status = order.Status.ToString(),
                    oldManager = oldManager != null ? new
                    {
                        id = oldManager.Id,
                        name = oldManager.Name,
                        email = oldManager.Email
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при снятии менеджера с заказа {OrderId} администратором", id);
                return StatusCode(500, new
                {
                    message = "Ошибка при снятии менеджера",
                    error = ex.Message
                });
            }
        }

        #endregion

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }


    }

}