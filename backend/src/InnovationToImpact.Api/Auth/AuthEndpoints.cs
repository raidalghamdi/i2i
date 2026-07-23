using System.Security.Claims;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Api.Auth;

public record LoginInput(string Email, string Password);
public record RefreshInput(string RefreshToken);
public record ChangePasswordInput(string CurrentPassword, string NewPassword);

/// <summary>
/// JWT login/refresh/logout for the Staging (cloud, pre-AD-integration) deployment. This is
/// additive to the existing Negotiate (Production) and DevAuth (local Development) paths -- neither
/// is touched, so switching ASPNETCORE_ENVIRONMENT back to Production once real AD is wired up needs
/// no further changes here.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginInput input, HttpContext context, InnovationDbContext db, IJwtTokenService tokens) =>
        {
            var user = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Email == input.Email);

            if (user is null || user.PasswordHash is null || !user.IsActive || !PasswordHasher.Verify(input.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var roleCodes = user.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList();
            var issued = await tokens.IssueAsync(user, roleCodes, context.Connection.RemoteIpAddress?.ToString());

            return Results.Ok(new
            {
                accessToken = issued.AccessToken,
                refreshToken = issued.RefreshToken,
                expiresAt = issued.AccessTokenExpiresAt,
                user = new { id = user.Id, email = user.Email, fullNameEn = user.FullNameEn, roles = roleCodes },
            });
        }).RequireRateLimiting("login");

        app.MapPost("/api/auth/refresh", async (RefreshInput input, HttpContext context, IJwtTokenService tokens) =>
        {
            var issued = await tokens.RotateAsync(input.RefreshToken, context.Connection.RemoteIpAddress?.ToString());
            if (issued is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                accessToken = issued.AccessToken,
                refreshToken = issued.RefreshToken,
                expiresAt = issued.AccessTokenExpiresAt,
            });
        }).RequireRateLimiting("login");

        app.MapPost("/api/auth/logout", async (RefreshInput input, IJwtTokenService tokens) =>
        {
            await tokens.RevokeAsync(input.RefreshToken);
            return Results.Ok(new { ok = true });
        });

        app.MapPost("/api/auth/change-password", async (ChangePasswordInput input, ClaimsPrincipal principal, InnovationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(input.NewPassword) || input.NewPassword.Length < 10)
            {
                return Results.BadRequest(new { error = "New password must be at least 10 characters." });
            }

            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await db.Users.SingleAsync(u => u.Id == userId);

            if (user.PasswordHash is null || !PasswordHasher.Verify(input.CurrentPassword, user.PasswordHash))
            {
                return Results.BadRequest(new { error = "Current password is incorrect." });
            }

            user.PasswordHash = PasswordHasher.Hash(input.NewPassword);
            await db.SaveChangesAsync();
            return Results.Ok(new { ok = true });
        }).RequireAuthorization();

        return app;
    }
}
