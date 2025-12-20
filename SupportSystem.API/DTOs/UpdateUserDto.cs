namespace SupportSystem.API.DTOs
{
    public class UpdateUserDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
    }

    public class UpdateUserRoleDto
    {
        public string Role { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}