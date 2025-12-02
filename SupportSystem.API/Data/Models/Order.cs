using SupportSystem.API.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportSystem.API.Data.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, StringLength(250), Column("orderName")]
        public string OrderName { get; set; } = string.Empty;

        [Required, StringLength(3500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Cost { get; set; }

        [Required, Column(TypeName = "varchar(20)")]
        public OrderStatus Status { get; set; } = OrderStatus.New;

        [Required, Column(TypeName = "varchar(10)")]
        public Priority Priority { get; set; } = Priority.Medium;

        [DataType(DataType.Date), Column("createDat")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date), Column("completeDat")]
        public DateTime? CompleteDate { get; set; }

        [ForeignKey("Client"), Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey("Manager"), Column("assignedTo")]
        public int? AssignedToId { get; set; }

        [InverseProperty("OrdersAsClient")]
        public virtual User Client { get; set; } = null!;

        [InverseProperty("OrdersAsManager")]
        public virtual User? Manager { get; set; }

        [InverseProperty("Order")]
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

        [InverseProperty("RelatedOrder")]
        public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();

        [InverseProperty("RelatedOrder")]
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
