using SupportSystem.API.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportSystem.API.Data.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(35)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]  
        public string Password { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(12)]
        public string? Phone { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public UserRole Role { get; set; } = UserRole.User;

        [DataType(DataType.Date)]
        [Column("regDat")]
        public DateTime RegDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        [InverseProperty("Client")]
        public virtual ICollection<Order> OrdersAsClient { get; set; } = new List<Order>();

        [InverseProperty("Manager")]
        public virtual ICollection<Order> OrdersAsManager { get; set; } = new List<Order>();

        [InverseProperty("Client")]
        public virtual ICollection<ServiceRequest> ServiceRequestsAsClient { get; set; } = new List<ServiceRequest>();

        [InverseProperty("Manager")]
        public virtual ICollection<ServiceRequest> ServiceRequestsAsManager { get; set; } = new List<ServiceRequest>();

        [InverseProperty("Client")]
        public virtual ICollection<SupportRequest> SupportRequestsAsClient { get; set; } = new List<SupportRequest>();

        [InverseProperty("Manager")]
        public virtual ICollection<SupportRequest> SupportRequestsAsManager { get; set; } = new List<SupportRequest>();

        [InverseProperty("Author")]
        public virtual ICollection<Report> ReportsCreated { get; set; } = new List<Report>();
    }
}