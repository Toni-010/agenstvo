using Microsoft.EntityFrameworkCore;
using SupportSystem.API.Data.Models;
using SupportSystem.API.Data.Enums;

namespace SupportSystem.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Конвертация enum в строки
            modelBuilder.Entity<User>()
                .Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            modelBuilder.Entity<Order>()
                .Property(o => o.Priority).HasConversion<string>().HasMaxLength(10);

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            modelBuilder.Entity<SupportRequest>()
                .Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            // Индексы
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Phone).IsUnique();

            // Связи Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany(u => u.OrdersAsClient)
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Manager)
                .WithMany(u => u.OrdersAsManager)
                .HasForeignKey(o => o.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // Связи ServiceRequest
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(s => s.Client)
                .WithMany(u => u.ServiceRequestsAsClient)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(s => s.Manager)
                .WithMany(u => u.ServiceRequestsAsManager)
                .HasForeignKey(s => s.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(s => s.Order)
                .WithMany(o => o.ServiceRequests)
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Связи SupportRequest
            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.Client)
                .WithMany(u => u.SupportRequestsAsClient)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.Manager)
                .WithMany(u => u.SupportRequestsAsManager)
                .HasForeignKey(s => s.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SupportRequest>()
                .HasOne(s => s.RelatedOrder)
                .WithMany(o => o.SupportRequests)
                .HasForeignKey(s => s.RelatedOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Связи Report
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Author)
                .WithMany(u => u.ReportsCreated)
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.RelatedOrder)
                .WithMany(o => o.Reports)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ServiceRequest)
                .WithMany(s => s.Reports)
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.SupportRequest)
                .WithMany(s => s.Reports)
                .HasForeignKey(r => r.SupportRequestId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}