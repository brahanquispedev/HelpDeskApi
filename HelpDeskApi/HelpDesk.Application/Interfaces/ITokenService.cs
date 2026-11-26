using HelpDesk.Domain.Ports;

namespace HelpDesk.Application.Interfaces;

public interface ITokenService
{
    AccessToken Create(Guid userId, string username, IEnumerable<string> roles, string credentialStamp);
}
