using Invoicely.Core.DTOs;
using Invoicely.Core.Entities;
using Invoicely.Core.Enums;
using Invoicely.Core.Interfaces;
using Invoicely.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicely.Infrastructure.Services;

public class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email, ct))
            throw new InvalidOperationException("Email already registered.");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            role = UserRole.Employee;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokenService.GenerateToken(user), ToDto(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponse(tokenService.GenerateToken(user), ToDto(user));
    }

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return ToDto(user);
    }

    private static UserDto ToDto(User u) =>
        new(u.Id, u.Name, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
}
