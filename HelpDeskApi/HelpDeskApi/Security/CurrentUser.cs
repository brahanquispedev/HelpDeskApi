using System.Security.Claims;
using HelpDesk.Domain.Ports;
namespace HelpDesk.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor _httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid Id =>
        Guid.TryParse(Principal?.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public bool HasPermission(string permission) =>
        Principal?.HasClaim("permiso", permission) == true;
}
