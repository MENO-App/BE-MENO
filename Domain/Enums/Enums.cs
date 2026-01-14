using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum Role
    {
        Student = 0,
        Staff = 1,
        Kitchen = 2,
        Admin = 3
    }

    public enum MealChoiceStatus
    {
        Eating = 0,
        NotEating = 1
    }

    public enum MenuItemType
    {
        Main = 0,
        Veg = 1
    }
}
