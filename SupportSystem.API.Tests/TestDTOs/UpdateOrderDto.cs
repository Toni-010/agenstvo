// SupportSystem.API.Tests/TestDTOs/UpdateOrderDto.cs
namespace SupportSystem.API.DTOs
{
    public class UpdateOrderDto
    {
        public string? OrderName { get; set; }
        public string? Description { get; set; }
        public decimal? Cost { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? AssignedToId { get; set; }
    }
}