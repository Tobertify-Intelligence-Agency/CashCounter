using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Auth;

/// <summary>
/// Blazor authentication state provider backed by Firebase Auth.
/// </summary>
public class CashCountAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IAuthService _authService;

    public CashCountAuthStateProvider(IAuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += OnAuthStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await _authService.GetCurrentUserAsync();

        if (user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new("IsPremium", user.IsPremium.ToString()),
            new("AuthProvider", user.AuthProvider ?? "unknown")
        };

        if (!string.IsNullOrEmpty(user.PhotoUrl))
        {
            claims.Add(new Claim("PhotoUrl", user.PhotoUrl));
        }

        var identity = new ClaimsIdentity(claims, "Firebase");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnAuthStateChanged(object? sender, UserProfile? user)
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _authService.AuthStateChanged -= OnAuthStateChanged;
    }
}
