using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public sealed class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDBContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ✅ Domain tables (NOT Identity users)
        public DbSet<User> DomainUsers => Set<User>();

        // ✅ Required by IApplicationDBContext
        public DbSet<User> Users => Set<User>();

        public DbSet<School> Schools => Set<School>();
        public DbSet<Allergy> Allergies => Set<Allergy>();
        public DbSet<UserAllergy> UserAllergies => Set<UserAllergy>();

        public DbSet<MenuWeek> MenuWeeks => Set<MenuWeek>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<MenuItemAllergen> MenuItemAllergens => Set<MenuItemAllergen>();

        public DbSet<MealPlan> MealPlans => Set<MealPlan>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Apply IEntityTypeConfiguration<T> (ex AllergyConfiguration med HasData)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ---------------------------
            // Table mapping (Domain User)
            // ---------------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User"); // keep as "User" (matches your DB)
                entity.HasKey(x => x.UserId);

                entity.Property(x => x.IdentityUserId).IsRequired();

                entity.Property(x => x.DisplayName).IsRequired();
                entity.Property(x => x.ClassGroup).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                // School 1..* Users
                entity.HasOne(x => x.School)
                      .WithMany(s => s.Users)
                      .HasForeignKey(x => x.SchoolId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------------------
            // Composite keys
            // ---------------------------
            modelBuilder.Entity<UserAllergy>()
                .HasKey(x => new { x.UserId, x.AllergyId });

            modelBuilder.Entity<MealPlan>()
                .HasKey(x => new { x.UserId, x.Date });

            modelBuilder.Entity<MenuItemAllergen>()
                .HasKey(x => new { x.MenuItemId, x.AllergenCode });

            // ---------------------------
            // Relationships
            // ---------------------------

            // User 1..* UserAllergies (FK -> Domain User)
            modelBuilder.Entity<UserAllergy>()
                .HasOne(x => x.User)
                .WithMany(u => u.UserAllergies)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Allergy 1..* UserAllergies
            modelBuilder.Entity<UserAllergy>()
                .HasOne(x => x.Allergy)
                .WithMany(a => a.UserAllergies)
                .HasForeignKey(x => x.AllergyId)
                .OnDelete(DeleteBehavior.Cascade);

            // School 1..* MenuWeeks
            modelBuilder.Entity<MenuWeek>()
                .HasOne(x => x.School)
                .WithMany(s => s.MenuWeeks)
                .HasForeignKey(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            // MenuWeek 1..* MenuItems
            modelBuilder.Entity<MenuItem>()
                .HasOne(x => x.MenuWeek)
                .WithMany(w => w.MenuItems)
                .HasForeignKey(x => x.MenuWeekId)
                .OnDelete(DeleteBehavior.Cascade);

            // MenuItem 1..* MenuItemAllergens
            modelBuilder.Entity<MenuItemAllergen>()
                .HasOne(x => x.MenuItem)
                .WithMany(mi => mi.Allergens)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // User 1..* MealPlans (FK -> Domain User)
            modelBuilder.Entity<MealPlan>()
                .HasOne<User>()
                .WithMany(u => u.MealPlans)
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
