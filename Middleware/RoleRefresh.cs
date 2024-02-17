#region

using System.Security.Claims;
using AGC_Management.Utils;

#endregion

public class RoleRefreshMiddleware
{
    private readonly RequestDelegate _next;

    public RoleRefreshMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var role = await GetRoleAsync(userId);
                var claimsIdentity = context.User.Identity as ClaimsIdentity;
                claimsIdentity?.RemoveClaim(claimsIdentity.FindFirst(ClaimTypes.Role));
                claimsIdentity?.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        await _next(context);
    }

    private async Task<string> GetRoleAsync(string userId)
    {
        ulong.TryParse(userId, out ulong id);
        var r = await AuthUtils.RetrieveRole(id);
        return r;
    }
}