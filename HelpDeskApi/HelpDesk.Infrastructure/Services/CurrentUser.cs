using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using HelpDesk.Application.Interfaces;
namespace HelpDesk.Infrastructure.Services;

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
