// SupportSystem.API.Tests/TestBase.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportSystem.API.Data; 
using SupportSystem.API.Data.Models; 
using SupportSystem.API.Data.Enums; 

namespace SupportSystem.API.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected ApplicationDbContext Context { get; private set; }
        protected readonly string TestDbName;
        protected IConfiguration Configuration { get; private set; }

        protected TestBase()
        {
            TestDbName = $"TestDb_{Guid.NewGuid()}";
            InitializeConfiguration();
            InitializeDbContext();
        }

        protected virtual void InitializeConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "your-256-bit-secret-key-for-testing-purposes-only",
                    ["Jwt:Issuer"] = "SupportSystemTests",
                    ["Jwt:Audience"] = "SupportSystemTestClients",
                    ["Jwt:ExpireDays"] = "7"
                })
                .Build();
        }

        protected virtual void InitializeDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: TestDbName)
                .Options;

            Context = new ApplicationDbContext(options);
        }

        protected async Task<User> CreateTestUser(
            int id = 1,
            UserRole role = UserRole.User, 
            string name = "Test User",
            string email = "test@test.com",
            string phone = null)
        {
            var user = new User
            {
                Id = id,
                Name = name,
                Email = email,
                Phone = phone,
                Password = "password123",
                Role = role, 
                RegDate = DateTime.Now
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        protected async Task<Order> CreateTestOrder(
            int clientId = 1,
            string orderName = "Test Order",
            OrderStatus status = OrderStatus.New, 
            Priority priority = Priority.Medium, 
            decimal? cost = null,
            int? assignedToId = null)
        {
            var order = new Order
            {
                OrderName = orderName,
                Description = $"Description for {orderName}",
                ClientId = clientId,
                Status = status,
                Priority = priority,
                Cost = cost,
                AssignedToId = assignedToId,
                CreateDate = DateTime.Now,
                CompleteDate = status == OrderStatus.Completed ? DateTime.Now : null
            };

            Context.Orders.Add(order);
            await Context.SaveChangesAsync();
            return order;
        }

        protected async Task<SupportRequest> CreateTestSupportRequest(
            int clientId = 1,
            string topic = "Test Support Topic",
            string message = "Test support message",
            RequestStatus status = RequestStatus.New, 
            int? relatedOrderId = null,
            int? assignedToId = null)
        {
            var request = new SupportRequest
            {
                Topic = topic,
                Message = message,
                ClientId = clientId,
                Status = status,
                CreateDate = DateTime.Now,
                RelatedOrderId = relatedOrderId,
                AssignedToId = assignedToId
            };

            Context.SupportRequests.Add(request);
            await Context.SaveChangesAsync();
            return request;
        }

        protected async Task<ServiceRequest> CreateTestServiceRequest(
            int clientId = 1,
            string serviceType = "Repair",
            string description = "Test service description",
            RequestStatus status = RequestStatus.New, 
            decimal? cost = null,
            int? orderId = null,
            int? assignedToId = null)
        {
            var request = new ServiceRequest
            {
                ServiceType = serviceType,
                Description = description,
                ClientId = clientId,
                Status = status,
                Cost = cost,
                OrderId = orderId,
                AssignedToId = assignedToId,
                CreateDate = DateTime.Now
            };

            Context.ServiceRequests.Add(request);
            await Context.SaveChangesAsync();
            return request;
        }

        protected async Task<Report> CreateTestReport(
            int createdById,
            string title = "Test Report",
            string content = "Test report content",
            int? orderId = null,
            int? serviceRequestId = null,
            int? supportRequestId = null)
        {
            var report = new Report
            {
                Title = title,
                Content = content,
                CreatedById = createdById,
                OrderId = orderId,
                ServiceRequestId = serviceRequestId,
                SupportRequestId = supportRequestId,
                CreateDate = DateTime.Now
            };

            Context.Reports.Add(report);
            await Context.SaveChangesAsync();
            return report;
        }

        protected async Task ClearDatabaseAsync()
        {
            Context.Reports.RemoveRange(Context.Reports);
            Context.SupportRequests.RemoveRange(Context.SupportRequests);
            Context.ServiceRequests.RemoveRange(Context.ServiceRequests);
            Context.Orders.RemoveRange(Context.Orders);
            Context.Users.RemoveRange(Context.Users);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        
        protected async Task<User> CreateTestUserFromString(
            int id = 1,
            string role = "User", 
            string name = "Test User",
            string email = "test@test.com",
            string phone = null)
        {
            
            var userRole = Enum.Parse<UserRole>(role);
            return await CreateTestUser(id, userRole, name, email, phone);
        }

        protected async Task<Order> CreateTestOrderFromString(
            int clientId = 1,
            string orderName = "Test Order",
            string status = "New", 
            string priority = "Medium", 
            decimal? cost = null,
            int? assignedToId = null)
        {
            
            var orderStatus = Enum.Parse<OrderStatus>(status);
            var orderPriority = Enum.Parse<Priority>(priority);
            return await CreateTestOrder(clientId, orderName, orderStatus, orderPriority, cost, assignedToId);
        }

        protected List<Claim> CreateUserClaims(
            int userId,
            string role = "User",
            string name = "Test User",
            string email = "test@test.com")
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email)
            };
        }

        protected void SetupControllerContext(ControllerBase controller, List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        public virtual void Dispose()
        {
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}