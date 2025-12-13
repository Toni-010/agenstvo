namespace SupportSystem.API.DTOs
{
    public class SupportRequestDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public string? ManagerName { get; set; }
        public int? RelatedOrderId { get; set; }
        public string? OrderName { get; set; }
    }

    public class OrderDropdownDto
    {
        public int Id { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
    }

    // Добавим в SupportRequestDto.cs
    public class SupportRequestDetailDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerEmail { get; set; }
        public string? ManagerPhone { get; set; }
        public int? RelatedOrderId { get; set; }
        public string? OrderName { get; set; }
        public int ReportCount { get; set; }
        public DateTime? LastReportDate { get; set; }
    }

    public class UpdateSupportRequestStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class CreateReportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool CompleteRequest { get; set; } = false;
    }
}