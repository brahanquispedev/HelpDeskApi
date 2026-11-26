using HelpDesk.Domain.Dtos.Autenticacion;

namespace HelpDesk.Application.Interfaces;

public interface IAuthService
{
    Task Bootstrap(RegistroDto input);
}