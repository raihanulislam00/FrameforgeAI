using Microsoft.EntityFrameworkCore;
using VideoMaker.Application.Common.Interfaces;
using VideoMaker.Contracts.Auth;
using VideoMaker.Contracts.Common;
using VideoMaker.Domain.Entities;

namespace VideoMaker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, ct);

        if (existingUser)
        {
            return ApiResponse<AuthResponse>.ErrorResult("Email is already registered");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            }
        }, "Registration successful");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.ErrorResult("Invalid email or password");
        }

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(ct);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            }
        }, "Login successful");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse<AuthResponse>.ErrorResult("Refresh token is required");
        }

        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (existingToken == null || !existingToken.IsActive || existingToken.User == null)
        {
            return ApiResponse<AuthResponse>.ErrorResult("Invalid or expired refresh token");
        }

        // Revoke current token
        existingToken.RevokedAt = DateTime.UtcNow;

        // Generate new pair
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(existingToken.UserId);
        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync(ct);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(existingToken.User);

        return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            User = new UserDto
            {
                Id = existingToken.User.Id,
                Name = existingToken.User.Name,
                Email = existingToken.User.Email,
                CreatedAt = existingToken.User.CreatedAt
            }
        }, "Token refreshed successfully");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse<bool>.SuccessResult(true, "Logged out successfully");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            return ApiResponse<UserDto>.ErrorResult("User not found");
        }

        return ApiResponse<UserDto>.SuccessResult(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        });
    }
}
