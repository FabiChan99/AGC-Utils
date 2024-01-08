#region

using System.Security.Claims;
using AGC_Management.Utils;
using Microsoft.AspNetCore.Components.Authorization;

#endregion

namespace AGC_Management.Providers;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var user = _httpContextAccessor.HttpContext.User;

        if (user.Identity.IsAuthenticated)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Identity.Name),
                new Claim(ClaimTypes.NameIdentifier, userIdClaim),
                new Claim(ClaimTypes.Role, AuthUtils.RetrieveRole(ulong.Parse(userIdClaim)).Result)
            };

            identity = new ClaimsIdentity(claims, "Discord");
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }


    public void MarkUserAsAuthenticated(string username, string userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("UserId", userId),
            new Claim(ClaimTypes.Role, AuthUtils.RetrieveRole(ulong.Parse(userId)).Result)
        }, "Discord");

        var user = new ClaimsPrincipal(identity);
        Console.WriteLine(user.Identity.Name);
        Console.WriteLine(user.Identity.IsAuthenticated);
        

        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));

        var g = GetAuthenticationStateAsync().Result;
        Console.WriteLine(g.User.Identity.Name);
    }

    public void MarkUserAsLoggedOut()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }
}

