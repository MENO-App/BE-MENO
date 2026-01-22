using System;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DomainUser = Domain.Entities.User;


namespace Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hämtar domän-user baserat på IdentityUserId (AspNetUsers.Id) som kommer från JWT.
    /// OBS: Vi använder Set<DomainUser>() för att undvika att _context.Users (Identity) används.
    /// </summary>
    public async Task<DomainUser?> GetByIdentityUserIdAsync(string identityUserId)
    {
        return await _context.Set<DomainUser>()
            .Include(u => u.UserAllergies)
                .ThenInclude(ua => ua.Allergy)
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(DomainUser user)
    {
        await _context.DomainUsers.AddAsync(user);
    }
}