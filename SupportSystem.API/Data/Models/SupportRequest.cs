using SupportSystem.API.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportSystem.API.Data.Models
{
    [Table("supportRequest")]
    public class SupportRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Topic { get; set; } = string.Empty;

        [Required, StringLength(3500)]
        public string Message { get; set; } = string.Empty;

        [Required, Column(TypeName = "varchar(20)")]
        public RequestStatus Status { get; set; } = RequestStatus.New;

        [DataType(DataType.Date), Column("createDat")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [ForeignKey("Client"), Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey("Manager"), Column("assignedTo")]
        public int? AssignedToId { get; set; }

        [ForeignKey("RelatedOrder"), Column("related_order_id")]
        public int? RelatedOrderId { get; set; }

        [InverseProperty("SupportRequestsAsClient")]
        public virtual User Client { get; set; } = null!;

        [InverseProperty("SupportRequestsAsManager")]
        public virtual User? Manager { get; set; }

        [InverseProperty("SupportRequests")]
        public virtual Order? RelatedOrder { get; set; }

        [InverseProperty("SupportRequest")]
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
