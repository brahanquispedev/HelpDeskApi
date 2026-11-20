using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Dtos.Autenticacion;
using Microsoft.EntityFrameworkCore;
using HelpDesk.Domain.Models;
using HelpDesk.Domain.Exceptions;
using HelpDesk.Domain.Ports;

namespace HelpDesk.Application.Services;

public sealed class AuthService(IUnitOfWork _unitOfWork, IPasswordService passwords)
{
    public async Task Bootstrap(RegistroDto input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        // Restriccion de una sola inicializacion. Se ejecuta por consola, nunca por HTTP.
        var adminRole = (await (_unitOfWork.Repository<Roles>().Query().Where(x => x.Nombre == "administrador")).ToListAsync()).SingleOrDefault()
                        ?? throw new BusinessException(409, "Falta el rol administrador.");
        if (!(await(_unitOfWork.Repository<UsuariosRoles>().Query().Where(x => x.IdRol == adminRole.IdRol)).CountAsync() == 0))
        {
            throw new BusinessException(409, "Ya existe un administrador; use la API para altas posteriores.");
        }
        var user = MakeUser(input);
        await _unitOfWork.Repository<global::HelpDesk.Domain.Models.Usuarios>().AddAsync(user);
        await _unitOfWork.Repository<UsuariosRoles>().AddAsync(new UsuariosRoles
        {
            IdUsuario = user.IdUsuario,
            IdRol = adminRole.IdRol,
            FechaAsignacion = DateTime.UtcNow
        });
        await _unitOfWork.Complete();
    }
    private Usuarios MakeUser(RegistroDto x) => new()
    {
        IdUsuario = Guid.NewGuid(),
        Nombres = x.Nombres.Trim(),
        Apellidos = x.Apellidos.Trim(),
        NombreUsuario = x.NombreUsuario.Trim(),
        Correo = x.Correo.Trim().ToLowerInvariant(),
        ClaveHash = passwords.Hash(x.Clave),
        Activo = true,
        FechaRegistro = DateTime.UtcNow
    };
}