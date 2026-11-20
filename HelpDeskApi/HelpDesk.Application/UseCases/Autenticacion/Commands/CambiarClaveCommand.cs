using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Application.UseCases.Autenticacion.Commands;

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
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

            if (!_actor.IsAuthenticated)
                throw new BusinessException(401, "Inicie sesion.");

            var usuario = await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Query()
                .FirstOrDefaultAsync(u => u.IdUsuario == _actor.Id, ct);

            if (!(usuario is not null))
            {
                throw new BusinessException(404, "Usuario no encontrado.");
            }
            if (!(_passwordService.Verify(usuario!.ClaveHash, request.ClaveActual)))
            {
                throw new BusinessException(401, "La clave actual es incorrecta.");
            }
            if (!(request.ClaveActual != request.ClaveNueva))
            {
                throw new BusinessException(400, "La nueva clave debe ser diferente a la actual.");
            }

            usuario.ClaveHash = _passwordService.Hash(request.ClaveNueva);
            _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().Update(usuario);
            await _unitOfWork.Complete();

            return new ResponseDto<OperacionDto>(200, new OperacionDto(true));
        }
        catch (ValidationException ex)
        {
            return new ResponseDto<OperacionDto>(400, default, ex.Message);
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
