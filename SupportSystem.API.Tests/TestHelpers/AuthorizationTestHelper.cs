// SupportSystem.API.Tests/TestHelpers/AuthorizationTestHelper.cs
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SupportSystem.API.Tests.TestHelpers
{
    public static class AuthorizationTestHelper
    {
        public static void SetupUserWithRole(ControllerBase controller, int userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, $"User {userId}"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        public static string GetExpectedAccessMessage(string role, bool shouldHaveAccess)
        {
            var access = shouldHaveAccess ? "РАЗРЕШЕН" : "ЗАПРЕЩЕН";
            return $"Пользователь с ролью '{role}' → ДОСТУП {access}";
        }
    }
}