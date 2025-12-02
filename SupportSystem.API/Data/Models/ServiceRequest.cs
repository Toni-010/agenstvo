using SupportSystem.API.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportSystem.API.Data.Models
{
    [Table("serviceRequest")]
    public class ServiceRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(250), Column("ServiceType")]
        public string ServiceType { get; set; } = string.Empty;

        [Required, StringLength(3500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Cost { get; set; }

        [Required, Column(TypeName = "varchar(20)")]
        public RequestStatus Status { get; set; } = RequestStatus.New;

        [DataType(DataType.Date), Column("createDat")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [ForeignKey("Client"), Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey("Manager"), Column("assignedTo")]
        public int? AssignedToId { get; set; }

        [ForeignKey("Order"), Column("order_id")]
        public int? OrderId { get; set; }

        [InverseProperty("ServiceRequestsAsClient")]
        public virtual User Client { get; set; } = null!;

        [InverseProperty("ServiceRequestsAsManager")]
        public virtual User? Manager { get; set; }

        public virtual Order? Order { get; set; }

        [InverseProperty("ServiceRequest")]
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}