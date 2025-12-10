namespace SupportSystem.API.DTOs
{
    public class CreateOrderDto
    {
        public string OrderName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Cost { get; set; }
        public string Priority { get; set; } = "Medium";
    }
}