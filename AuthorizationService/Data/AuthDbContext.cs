using Microsoft.EntityFrameworkCore;
using AuthorizationService.Models;

namespace AuthorizationService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<AuthUser> AuthUsers { get; set; } = null!;
        public DbSet<AuthUserPermission> AuthUserPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthUser>(b =>
            {
                b.ToTable("auth_users");
                b.HasKey(u => u.Id);
                b.Property(u => u.Id).HasColumnName("id");
                b.Property(u => u.CustomerId).HasColumnName("customer_id");
                b.Property(u => u.Email).HasColumnName("email").IsRequired();
                b.Property(u => u.PasswordHash).HasColumnName("password_hash");
                b.HasIndex(u => u.Email).IsUnique();
                b.Property(u => u.DisplayName).HasColumnName("display_name");
                b.Property(u => u.CreatedAt).HasColumnName("created_at");
                b.HasMany(u => u.Permissions)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuthUserPermission>(b =>
            {
                b.ToTable("auth_user_permissions");
                b.HasKey(p => p.Id);
                b.Property(p => p.Id).HasColumnName("id");
                b.Property(p => p.UserId).HasColumnName("user_id");
                b.Property(p => p.Permission).HasColumnName("permission").IsRequired();
                b.Property(p => p.GrantedAt).HasColumnName("granted_at");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
