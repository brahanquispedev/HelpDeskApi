using HelpDesk.Application.Common;
using HelpDesk.Application.Security;
using HelpDesk.Domain.Dtos.Common;
using HelpDesk.Domain.Dtos.IntegrantesEquipo;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;
using MediatR;

namespace HelpDesk.Application.UseCases.IntegrantesEquipo.Commands;

public class CrearIntegrantesEquipoCommand : IRequest<ResponseDto<IntegrantesEquipoResponseDto>>
{
    public Guid IdEquipo { get; set; }
    public Guid IdUsuario { get; set; }
    public bool EsCoordinador { get; set; }
}

internal sealed class CrearIntegrantesEquipoCommandHandler(IUnitOfWork _unitOfWork, AccessRules access)
    : IRequestHandler<CrearIntegrantesEquipoCommand, ResponseDto<IntegrantesEquipoResponseDto>>
{
    public async Task<ResponseDto<IntegrantesEquipoResponseDto>> Handle(CrearIntegrantesEquipoCommand request, CancellationToken ct)
    {
        try
        {
            Validation.Check(request);
            if (!access.Admin) access.Permission("integrantes_equipo.crear");

            var equipo = await _unitOfWork.Repository<Domain.Models.EquiposSoporte>().GetByIdAsync([request.IdEquipo]);
            Validation.Require(equipo != null, "El equipo de soporte no existe.");
            Validation.Require(equipo!.Activo, "El equipo de soporte está inactivo.");

            var usuario = await _unitOfWork.Repository<Domain.Models.Usuarios>().GetByIdAsync([request.IdUsuario]);
            Validation.Require(usuario != null, "El usuario no existe.");
            Validation.Require(usuario!.Activo, "El usuario está inactivo.");

            var repo = _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>();
            var existente = await repo.GetByIdAsync([request.IdEquipo, request.IdUsuario]);
            Validation.Require(existente == null, "El usuario ya forma parte de este equipo.");

            var entity = new Domain.Models.IntegrantesEquipo
            {
                IdEquipo = request.IdEquipo,
                IdUsuario = request.IdUsuario,
                EsCoordinador = request.EsCoordinador,
                Disponible = true,
                Activo = true
            };

            await repo.AddAsync(entity);
            await _unitOfWork.Complete();

            var dto = new IntegrantesEquipoResponseDto
            {
                IdEquipo = entity.IdEquipo,
                IdUsuario = entity.IdUsuario,
                EsCoordinador = entity.EsCoordinador,
                Disponible = entity.Disponible,
                Activo = entity.Activo
            };

            return new ResponseDto<IntegrantesEquipoResponseDto>(201, dto, "Integrante asignado al equipo correctamente.");
        }
        catch (BusinessException ex)
        {
            return new ResponseDto<IntegrantesEquipoResponseDto>(ex.Status, default, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new ResponseDto<IntegrantesEquipoResponseDto>(500, default, "No se pudo completar la operacion.");
        }
    }
}