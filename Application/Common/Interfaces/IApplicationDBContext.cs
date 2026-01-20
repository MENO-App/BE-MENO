using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
{
    public interface IApplicationDBContext
    {
        DbSet<School> Schools { get; }
       
        DbSet<Allergy> Allergies { get; }
        DbSet<UserAllergy> UserAllergies { get; }

        DbSet<MenuWeek> MenuWeeks { get; }
        DbSet<MenuItem> MenuItems { get; }
        DbSet<MenuItemAllergen> MenuItemAllergens { get; }

        DbSet<MealPlan> MealPlans { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
