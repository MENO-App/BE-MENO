using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdentityUserIdAsync(string identityUserId);
    Task SaveChangesAsync();

    Task AddAsync(Domain.Entities.User user);

}
