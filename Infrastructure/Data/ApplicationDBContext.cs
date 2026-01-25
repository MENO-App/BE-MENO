using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Data
{
    public sealed class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDBContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Domain user table (not used for authentication)
        public DbSet<Domain.Entities.User> DomainUsers { get; set; } = default!;

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

            //  Loads all IEntityTypeConfiguration<T> from this assembly (e.g., AllergyConfiguration with HasData)
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Composite keys (viktiga!)
            modelBuilder.Entity<UserAllergy>()
                .HasKey(x => new { x.UserId, x.AllergyId });

            modelBuilder.Entity<MealPlan>()
                .HasKey(x => new { x.UserId, x.Date });

            modelBuilder.Entity<MenuItemAllergen>()
                .HasKey(x => new { x.MenuItemId, x.AllergenCode });

            modelBuilder.Entity<Domain.Entities.User>(entity =>
            {
                entity.ToTable("User");
                entity.Ignore(e => e.UserAllergies);
                entity.Ignore(e => e.MealPlans);
            });

            // UserAllergy.UserId references AspNetUsers.Id directly (no FK to domain User)
            modelBuilder.Entity<UserAllergy>()
                .Ignore(x => x.User);

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

            // Seed Allergies
            //modelBuilder.Entity<Allergy>().HasData(
            //    new Allergy { AllergyId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Gluten" },
            //    new Allergy { AllergyId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Laktos" },
            //    new Allergy { AllergyId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Mjölkprotein" },
            //    new Allergy { AllergyId = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Ägg" },
            //    new Allergy { AllergyId = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Fisk" },
            //    new Allergy { AllergyId = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Skaldjur" },
            //    new Allergy { AllergyId = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Jordnötter" },
            //    new Allergy { AllergyId = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Nötter" },
            //    new Allergy { AllergyId = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "Soja" },
            //    new Allergy { AllergyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Sesam" },
            //    new Allergy { AllergyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Selleri" },
            //    new Allergy { AllergyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "Senap" }
            //);
            // NOTE:
            // Domain.Entities.User is no longer used for authentication.
            // ASP.NET Identity (ApplicationUser) is now the single source of truth for users.
            // Therefore, all EF Core relations referencing Domain User are intentionally disabled.
            // User ownership is handled via UserId (Guid) only, not navigation properties.

            // Disabled relations (intentionally):
            // - School 1..* Domain Users
            // modelBuilder.Entity<Domain.Entities.User>()
            //     .HasOne(x => x.School)
            //     .WithMany(x => x.Users)
            //     .HasForeignKey(x => x.SchoolId);

            // - Domain User 1..* MealPlans
            // modelBuilder.Entity<MealPlan>()
            //     .HasOne(x => x.User)
            //     .WithMany(x => x.MealPlans)
            //     .HasForeignKey(x => x.UserId);

            // - Domain User 1..* UserAllergies
            // modelBuilder.Entity<UserAllergy>()
            //     .HasOne(x => x.User)
            //     .WithMany(x => x.UserAllergies)
            //     .HasForeignKey(x => x.UserId);
        }
    }
}
