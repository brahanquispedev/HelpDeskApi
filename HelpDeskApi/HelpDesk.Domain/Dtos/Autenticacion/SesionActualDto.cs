namespace HelpDesk.Domain.Dtos.Autenticacion;

public sealed record SesionActualDto(string[] Roles, string[] Permisos);
