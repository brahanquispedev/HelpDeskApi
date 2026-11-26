using System.ComponentModel.DataAnnotations;
using HelpDesk.Application.Interfaces;
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

internal sealed class CrearIntegrantesEquipoCommandHandler(IUnitOfWork _unitOfWork, IAccessRules access)
    : IRequestHandler<CrearIntegrantesEquipoCommand, ResponseDto<IntegrantesEquipoResponseDto>>
{
    public async Task<ResponseDto<IntegrantesEquipoResponseDto>> Handle(CrearIntegrantesEquipoCommand request, CancellationToken ct)
    {
        try
        {
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
            if (request.IdEquipo == Guid.Empty)
            {
                throw new ValidationException("IdEquipo debe ser un identificador valido.");
            }
            if (request.IdUsuario == Guid.Empty)
            {
                throw new ValidationException("IdUsuario debe ser un identificador valido.");
            }
            if (!access.Admin) access.Permission("organizacion.administrar");

            var equipo = await _unitOfWork.Repository<Domain.Models.EquiposSoporte>().GetByIdAsync([request.IdEquipo]);
            if (!(equipo != null))
            {
                throw new BusinessException(400, "El equipo de soporte no existe.");
            }
            if (!(equipo!.Activo))
            {
                throw new BusinessException(400, "El equipo de soporte está inactivo.");
            }

            var usuario = await _unitOfWork.Repository<Domain.Models.Usuarios>().GetByIdAsync([request.IdUsuario]);
            if (!(usuario != null))
            {
                throw new BusinessException(400, "El usuario no existe.");
            }
            if (!(usuario!.Activo))
            {
                throw new BusinessException(400, "El usuario está inactivo.");
            }

            var repo = _unitOfWork.Repository<Domain.Models.IntegrantesEquipo>();
            var existente = await repo.GetByIdAsync([request.IdEquipo, request.IdUsuario]);
            if (!(existente == null))
            {
                throw new BusinessException(400, "El usuario ya forma parte de este equipo.");
            }

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
        catch (ValidationException ex)
        {
            return new ResponseDto<IntegrantesEquipoResponseDto>(400, default, ex.Message);
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