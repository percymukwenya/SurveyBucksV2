using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Set the schema for all tables
            builder.HasDefaultSchema("SurveyBucks");

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users");
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Roles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable(name: "UserRoles");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable(name: "UserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable(name: "UserLogins");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable(name: "RoleClaims");
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable(name: "UserTokens");
            });

            // Configure Soft Delete
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Skip Identity tables for soft delete
                if (entityType.ClrType.Namespace != "Microsoft.AspNetCore.Identity.EntityFrameworkCore"
                    && entityType.ClrType.Name != "IdentityUserRole`1"
                    && entityType.ClrType.Name != "IdentityRoleClaim`1"
                    && entityType.ClrType.Name != "IdentityUserToken`1")
                {
                    // Check if the entity has IsDeleted property
                    if (entityType.FindProperty("IsDeleted") != null)
                    {
                        // Configure query filter
                        var parameter = Expression.Parameter(entityType.ClrType, "e");
                        var property = Expression.Property(parameter, "IsDeleted");
                        var falseConstant = Expression.Constant(false);
                        var condition = Expression.Equal(property, falseConstant);
                        var lambda = Expression.Lambda(condition, parameter);

                        builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            UpdateSoftDeleteAndAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateSoftDeleteAndAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateSoftDeleteAndAuditFields()
        {
            var now = DateTimeOffset.UtcNow;
            var user = "system"; // In a real app, get this from the current user

            foreach (var entry in ChangeTracker.Entries())
            {
                // Skip entities without IsDeleted property
                if (entry.Metadata.FindProperty("IsDeleted") == null)
                    continue;

                // Handle soft delete
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Property("IsDeleted").CurrentValue = true;

                    if (entry.Metadata.FindProperty("ModifiedDate") != null)
                        entry.Property("ModifiedDate").CurrentValue = now;

                    if (entry.Metadata.FindProperty("ModifiedBy") != null)
                        entry.Property("ModifiedBy").CurrentValue = user;
                }
                // Handle audit fields
                else if (entry.State == EntityState.Added)
                {
                    entry.Property("IsDeleted").CurrentValue = false;

                    if (entry.Metadata.FindProperty("CreatedDate") != null)
                        entry.Property("CreatedDate").CurrentValue = now;

                    if (entry.Metadata.FindProperty("CreatedBy") != null)
                        entry.Property("CreatedBy").CurrentValue = user;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Don't let IsDeleted be modified back to false
                    if ((bool)entry.Property("IsDeleted").OriginalValue)
                        entry.Property("IsDeleted").CurrentValue = true;

                    if (entry.Metadata.FindProperty("ModifiedDate") != null)
                        entry.Property("ModifiedDate").CurrentValue = now;

                    if (entry.Metadata.FindProperty("ModifiedBy") != null)
                        entry.Property("ModifiedBy").CurrentValue = user;
                }
            }
        }
    }
}
