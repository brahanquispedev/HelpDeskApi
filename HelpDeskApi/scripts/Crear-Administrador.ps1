param(
    [Parameter(Mandatory = $true)][string]$NombreUsuario,
    [Parameter(Mandatory = $true)][string]$Correo,
    [string]$Nombres = 'Administrador',
    [string]$Apellidos = 'Inicial'
)
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\HelpDeskApi\HelpDeskApi.csproj'
$password = Read-Host 'Clave del administrador (minimo 10 caracteres)' -AsSecureString
$values = @{
    Bootstrap__NombreUsuario = $NombreUsuario
    Bootstrap__Correo = $Correo
    Bootstrap__Nombres = $Nombres
    Bootstrap__Apellidos = $Apellidos
    Bootstrap__Clave = [System.Net.NetworkCredential]::new('', $password).Password
    DOTNET_ENVIRONMENT = 'Development'
    ASPNETCORE_ENVIRONMENT = 'Development'
}
$previousValues = @{}
try {
    foreach ($name in $values.Keys) {
        $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $values[$name], 'Process')
    }
    & dotnet run --project $projectPath --no-launch-profile -- --crear-admin
    if ($LASTEXITCODE -ne 0) { throw 'La inicializacion no se completo. Revise el mensaje anterior.' }
}
finally {
    foreach ($name in $previousValues.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousValues[$name], 'Process')
    }
    $values.Clear()
    $password.Dispose()
}
