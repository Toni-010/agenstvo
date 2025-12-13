namespace SupportSystem.API.DTOs
{
    public class CreateReportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public int? ServiceRequestId { get; set; }
        public int? SupportRequestId { get; set; }
        public bool CompleteRequest { get; set; } = false;
    }

    public class ReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public int CreatedById { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public int? OrderId { get; set; }
        public int? ServiceRequestId { get; set; }
        public int? SupportRequestId { get; set; }
    }
}