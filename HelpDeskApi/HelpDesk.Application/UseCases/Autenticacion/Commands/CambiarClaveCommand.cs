using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Common;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Autenticacion;

public sealed class CambiarClaveCommand : IRequest<ResponseDto<OperacionDto>>
{
    [Required, StringLength(128, MinimumLength = 10)]
    public string ClaveActual { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string ClaveNueva { get; set; } = string.Empty;
}

internal sealed class CambiarClaveCommandHandler(
    IUnitOfWork _unitOfWork,
    IPasswordService _passwordService,
    ICurrentUser _actor)
    : IRequestHandler<CambiarClaveCommand, ResponseDto<OperacionDto>>
{
    public async Task<ResponseDto<OperacionDto>> Handle(CambiarClaveCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);

            if (!_actor.IsAuthenticated)
                throw new BusinessException(401, "Inicie sesion.");

            var usuario = await _unitOfWork.Repository<Usuarios>().Query()
                .FirstOrDefaultAsync(u => u.IdUsuario == _actor.Id, ct);

            Validation.Require(usuario is not null, "Usuario no encontrado.", 404);
            Validation.Require(_passwordService.Verify(usuario!.ClaveHash, request.ClaveActual), "La clave actual es incorrecta.", 401);
            Validation.Require(request.ClaveActual != request.ClaveNueva, "La nueva clave debe ser diferente a la actual.", 400);

            usuario.ClaveHash = _passwordService.Hash(request.ClaveNueva);
            _unitOfWork.Repository<Usuarios>().Update(usuario);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true));
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<OperacionDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<OperacionDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}
