using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Property> Properties { get; set;  }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Payment> Payments { get; set;}
        public DbSet<ContactMessage> Contacts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(e => e.Email).IsUnique();
                e.HasIndex(e => e.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
                e.Property(e => e.Email).IsRequired().HasMaxLength(255);
                e.Property(e => e.Username).HasMaxLength(50);
                e.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                e.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                e.Property(e => e.PasswordHash).IsRequired();
                e.Property(e => e.PhoneNumber).HasMaxLength(20);
                e.Property(e => e.Role).HasDefaultValue(Domain.Enums.UserRole.User);
                e.Property(e => e.IsLocked).HasDefaultValue(false);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.Property(e => e.Token).IsRequired().HasMaxLength(500);
                e.HasIndex(e => e.Token).IsUnique();
                e.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Permission>(e =>
            {
                e.Property(e => e.Name).IsRequired().HasMaxLength(150);
                e.HasIndex(e => e.Name).IsUnique();
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Role>(e =>
            {
                e.Property(e => e.Name).IsRequired().HasMaxLength(100);
                e.HasIndex(e => e.Name).IsUnique();
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<RolePermission>(e =>
            {
                e.HasKey(e => new { e.RoleId, e.PermissionId });
                e.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserRoleAssignment>(e =>
            {
                e.HasKey(e => new { e.UserId, e.RoleId });
                e.HasOne(e => e.User).WithMany(u => u.UserRoleAssignments).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(e => e.Role).WithMany(r => r.UserRoleAssignments).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.Property(e => e.Name).IsRequired().HasMaxLength(100);
                e.Property(e => e.Slug).IsRequired().HasMaxLength(120);
                e.Property(e => e.Description).HasMaxLength(1000);
                e.HasIndex(e => e.Slug).IsUnique();
                e.HasOne(e => e.ParentCategory).WithMany(c => c.SubCategories).HasForeignKey(e => e.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Booking>(e =>
            {
                e.Property(e => e.Notes).HasMaxLength(1000);
                e.Property(e => e.ContactPhone).HasMaxLength(20);
                e.Property(e => e.ContactEmail).HasMaxLength(255);
                e.Property(e => e.CancellationReason).HasMaxLength(500);
                e.HasOne(e => e.Property).WithMany(p => p.Bookings).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(e => e.User).WithMany(u => u.Bookings).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(e => e.Status);
                e.HasIndex(e => e.RequestedDateTime);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Review>(e =>
            {
                e.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
                e.HasOne(e => e.ReviewerUser).WithMany(u => u.ReviewsWritten).HasForeignKey(e => e.ReviewerUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(e => e.RevieweeUser).WithMany(u => u.ReviewsReceived).HasForeignKey(e => e.RevieweeUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(e => e.Property).WithMany(p => p.Reviews).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(e => e.PropertyId);
                e.HasIndex(e => e.RevieweeUserId);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Property>(e =>
            {
                e.Property(e => e.Title).IsRequired().HasMaxLength(200);
                e.Property(e => e.Description).IsRequired().HasMaxLength(5000);
                e.Property(e => e.Price).HasPrecision(18, 2);
                e.Property(e => e.Area).HasPrecision(10, 2);
                e.Property(e => e.Latitude).HasPrecision(10, 7);
                e.Property(e => e.Longitude).HasPrecision(10, 7);
                e.Property(e => e.City).IsRequired().HasMaxLength(100);
                e.Property(e => e.District).IsRequired().HasMaxLength(100);
                e.Property(e => e.Address).IsRequired().HasMaxLength(500);
                e.HasOne(e => e.User).WithMany(e => e.Properties).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(e => e.Category).WithMany(c => c.Properties).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(e => e.Type);
                e.HasIndex(e => e.CategoryId);
                e.HasIndex(e => e.Status);
                e.HasIndex(e => e.City);
                e.HasIndex(e => e.Price);
                e.HasIndex(e => e.IsFeatured);
                e.HasIndex(e => e.IsPublished);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<PropertyImage>(e =>
            {
                e.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
                e.HasOne(e => e.Property).WithMany(e => e.PropertyImages).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(e => e.DisplayOrder);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Favorite>(e =>
            {
                e.HasIndex(e => new { e.UserId, e.PropertyId }).IsUnique();
                e.HasOne(e => e.User).WithMany(e => e.Favorites).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(e => e.Property).WithMany(e => e.Favorites).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.Cascade);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(e => e.Amount).HasPrecision(18, 2);
                e.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                e.Property(e => e.Currency).HasDefaultValue("TRY").HasMaxLength(3);
                e.HasOne(e => e.Property).WithMany(e => e.Payments).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(e => e.Status);
                e.HasIndex(e => e.TransactionId);
                e.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<ContactMessage>(e =>
            {
                e.Property(e => e.Name).IsRequired().HasMaxLength(100);
                e.Property(e => e.Email).IsRequired().HasMaxLength(255);
                e.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                e.Property(e => e.Message).IsRequired().HasMaxLength(2000);

                e.HasOne(e => e.User).WithMany(e => e.ContactMessages).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(e => e.Property).WithMany(e => e.Contacts).HasForeignKey(e => e.PropertyId).OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(e => e.IsRead);
                e.HasIndex(e => e.IsReplied);
                e.HasQueryFilter(e => !e.IsDeleted);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach(var entry in entries)
            {
                if(entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                if(entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
                if(entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
