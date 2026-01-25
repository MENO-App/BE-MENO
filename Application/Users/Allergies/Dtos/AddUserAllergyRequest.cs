namespace Application.Users.Allergies.Dtos;

public sealed class AddUserAllergyRequest
{
    public Guid AllergyId { get; set; }

    // Used for "Annan" (t.ex. "Kiwi"). Can be empty.
    public string? Notes { get; set; }
}
