using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportSystem.API.Data.Models
{
    [Table("report")]
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(3500)]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.Date), Column("createDat")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [ForeignKey("Author"), Column("createdBy")]
        public int CreatedById { get; set; }

        [ForeignKey("RelatedOrder"), Column("order_id")]
        public int? OrderId { get; set; }

        [ForeignKey("ServiceRequest"), Column("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [ForeignKey("SupportRequest"), Column("support_request_id")]
        public int? SupportRequestId { get; set; }

        [InverseProperty("ReportsCreated")]
        public virtual User Author { get; set; } = null!;

        public virtual Order? RelatedOrder { get; set; }

        public virtual ServiceRequest? ServiceRequest { get; set; }

        public virtual SupportRequest? SupportRequest { get; set; }
    }
}

