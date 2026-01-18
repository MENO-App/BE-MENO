using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public sealed class School
    {
        public Guid SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<MenuWeek> MenuWeeks { get; set; } = new List<MenuWeek>();
    }

    public sealed class User
    {
        public Guid UserId { get; set; }
        public Guid SchoolId { get; set; }
        public Role Role { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public string ClassGroup { get; set; } = string.Empty;
        public bool DefaultVegetarian { get; set; }
        public DateTime CreatedAt { get; set; }

        public School? School { get; set; }
        public ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();
        public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    }

    public sealed class Allergy
    {
        public Guid AllergyId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<UserAllergy> UserAllergies { get; set; } = new List<UserAllergy>();
    }

    public sealed class UserAllergy
    {
        public Guid UserId { get; set; }
        public Guid AllergyId { get; set; }
        public string Notes { get; set; } = string.Empty;

        public User? User { get; set; }
        public Allergy? Allergy { get; set; }
    }

    public sealed class MenuWeek
    {
        public Guid MenuWeekId { get; set; }
        public Guid SchoolId { get; set; }

        public int Year { get; set; }
        public int WeekNumber { get; set; }
        public DateTime? PublishedAt { get; set; }

        public School? School { get; set; }
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }

    public sealed class MenuItem
    {
        public Guid MenuItemId { get; set; }
        public Guid MenuWeekId { get; set; }

        // 1 = Monday, 7 = Sunday (ISO-8601)
        public int DayOfWeek { get; set; }

        public MenuItemType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public MenuWeek? MenuWeek { get; set; }
        public ICollection<MenuItemAllergen> Allergens { get; set; } = new List<MenuItemAllergen>();
    }

    public sealed class MenuItemAllergen
    {
        public Guid MenuItemId { get; set; }
        public string AllergenCode { get; set; } = string.Empty;

        public MenuItem? MenuItem { get; set; }
    }

    public sealed class MealPlan
    {
        public Guid UserId { get; set; }
        public DateOnly Date { get; set; }

        public MealChoiceStatus Status { get; set; }
        public bool WantsVegetarian { get; set; }

        public User? User { get; set; }
    }
}
