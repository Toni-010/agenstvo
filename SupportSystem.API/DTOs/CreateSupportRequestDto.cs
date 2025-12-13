namespace SupportSystem.API.DTOs
{
    public class CreateSupportRequestDto
    {
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? RelatedOrderId { get; set; }
    }
}
