using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;

namespace Infrastructure.Data
{
    public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDBContext

    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<School> Schools => Set<School>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Allergy> Allergies => Set<Allergy>();
        public DbSet<UserAllergy> UserAllergies => Set<UserAllergy>();

        public DbSet<MenuWeek> MenuWeeks => Set<MenuWeek>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<MenuItemAllergen> MenuItemAllergens => Set<MenuItemAllergen>();

        public DbSet<MealPlan> MealPlans => Set<MealPlan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite keys (viktiga!)
            modelBuilder.Entity<UserAllergy>()
                .HasKey(x => new { x.UserId, x.AllergyId });

            modelBuilder.Entity<MealPlan>()
                .HasKey(x => new { x.UserId, x.Date });

            modelBuilder.Entity<MenuItemAllergen>()
                .HasKey(x => new { x.MenuItemId, x.AllergenCode });

            // Relation: School 1..* Users
           // modelBuilder.Entity<User>()
             //   .HasOne(x => x.School)
             //   .WithMany(x => x.Users)
               // .HasForeignKey(x => x.SchoolId);

            // Relation: School 1..* MenuWeeks
            modelBuilder.Entity<MenuWeek>()
                .HasOne(x => x.School)
                .WithMany(x => x.MenuWeeks)
                .HasForeignKey(x => x.SchoolId);

            // Relation: MenuWeek 1..* MenuItems
            modelBuilder.Entity<MenuItem>()
                .HasOne(x => x.MenuWeek)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.MenuWeekId);

            // Relation: User 1..* MealPlans
           // modelBuilder.Entity<MealPlan>()
             //   .HasOne(x => x.User)
            //    .WithMany(x => x.MealPlans)
             //   .HasForeignKey(x => x.UserId);

            // Relation: UserAllergy join
            //modelBuilder.Entity<UserAllergy>()
              //  .HasOne(x => x.User)
               // .WithMany(x => x.UserAllergies)
              //  .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<UserAllergy>()
                .HasOne(x => x.Allergy)
                .WithMany(x => x.UserAllergies)
                .HasForeignKey(x => x.AllergyId);

            // Relation: MenuItemAllergen -> MenuItem
            modelBuilder.Entity<MenuItemAllergen>()
                .HasOne(x => x.MenuItem)
                .WithMany(x => x.Allergens)
                .HasForeignKey(x => x.MenuItemId);

            // NOTE:
            // Domain.Entities.User is no longer used for authentication.
            // ASP.NET Identity (ApplicationUser) is now the single source of truth for users.
            // Therefore, all EF Core relations referencing Domain User are intentionally disabled.
            // User ownership is handled via UserId (Guid) only, not navigation properties.

        }
    }
}
