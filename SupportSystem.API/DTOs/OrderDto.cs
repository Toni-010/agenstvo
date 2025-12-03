// SupportSystem.API/DTOs/OrderDto.cs
namespace SupportSystem.API.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Cost { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public string? ManagerName { get; set; }
    }
}