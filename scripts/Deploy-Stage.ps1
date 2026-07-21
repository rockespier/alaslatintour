[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$StageComputerName,

    [Parameter(Mandatory = $true)]
    [string]$ApiDestination,

    [Parameter(Mandatory = $true)]
    [string]$FrontendDestination,

    [string]$ApiAppPool,
    [string]$FrontendAppPool,
    [string]$FrontendService,
    [pscredential]$Credential,
    [switch]$UseSsl,
    [switch]$SkipNpmCi,
    [switch]$PackageOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Checked {
    param([scriptblock]$Command, [string]$Description)

    Write-Host "`n==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description fallo (codigo de salida: $LASTEXITCODE)."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repositoryRoot 'backend\src\AlasApp.Api\AlasApp.Api.csproj'
$frontendRoot = Join-Path $repositoryRoot 'frontend'
$frontendDist = Join-Path $frontendRoot 'dist\alas-app.web'
$frontendWebConfig = Join-Path $frontendRoot 'deploy\web.config'
$publishRoot = Join-Path $repositoryRoot 'publish'
$apiPackage = Join-Path $publishRoot 'backend'
$frontendPackage = Join-Path $publishRoot 'frontend'

if (-not (Test-Path -LiteralPath $apiProject)) { throw "No se encontro $apiProject" }
if (-not (Test-Path -LiteralPath $frontendWebConfig)) { throw "No se encontro $frontendWebConfig" }

Write-Host "Commit local: $(git -C $repositoryRoot rev-parse --short HEAD)"
git -C $repositoryRoot status --short

# Siempre se parte de artefactos nuevos; nunca se reutiliza publish/ anterior.
if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $apiPackage, $frontendPackage -Force | Out-Null

Invoke-Checked { dotnet publish $apiProject --configuration Release --output $apiPackage } 'Publicando backend .NET (Release)'

# La configuracion de Stage debe vivir en variables de entorno/IIS, no en el paquete.
Get-ChildItem -LiteralPath $apiPackage -Filter 'appsettings*.json' -File | Remove-Item -Force

Push-Location $frontendRoot
try {
    if (-not $SkipNpmCi) {
        Invoke-Checked { npm ci } 'Instalando dependencias bloqueadas del frontend'
    }
    # angular.json define "production" como defaultConfiguration del target build.
    # No se pasan argumentos a npm: algunas versiones los reenvian como nombre de proyecto.
    Invoke-Checked { npm run build } 'Compilando frontend Angular SSR (production)'
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath (Join-Path $frontendDist 'browser'))) { throw 'El build no genero la carpeta browser.' }
if (-not (Test-Path -LiteralPath (Join-Path $frontendDist 'server'))) { throw 'El build no genero la carpeta server.' }

# -LiteralPath no expande '*'; se copian ambas carpetas SSR explicitamente.
Copy-Item -LiteralPath (Join-Path $frontendDist 'browser') -Destination $frontendPackage -Recurse -Force
Copy-Item -LiteralPath (Join-Path $frontendDist 'server') -Destination $frontendPackage -Recurse -Force

# Requisito de IIS: el web.config de deploy se ubica dentro de la carpeta server.
$serverPackage = Join-Path $frontendPackage 'server'
Copy-Item -LiteralPath $frontendWebConfig -Destination (Join-Path $serverPackage 'web.config') -Force

Compress-Archive -Path (Join-Path $apiPackage '*') -DestinationPath (Join-Path $publishRoot 'backend-stage.zip') -Force
Compress-Archive -Path (Join-Path $frontendPackage '*') -DestinationPath (Join-Path $publishRoot 'frontend-stage.zip') -Force

Write-Host "`nArtefactos creados:" -ForegroundColor Green
Write-Host "  Backend:  $apiPackage"
Write-Host "  Frontend: $frontendPackage"
Write-Host "  Frontend web.config: $(Join-Path $serverPackage 'web.config')"

if ($PackageOnly) {
    Write-Host '`nPackageOnly indicado: no se modifico el servidor Stage.' -ForegroundColor Yellow
    return
}

if (-not $Credential) {
    $Credential = Get-Credential -Message "Credenciales de PowerShell Remoting para $StageComputerName"
}

$session = New-PSSession -ComputerName $StageComputerName -Credential $Credential -UseSSL:$UseSsl
try {
    $remoteStaging = Invoke-Command -Session $session -ScriptBlock {
        $path = Join-Path $env:TEMP ("alas-stage-" + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        $path
    }

    Copy-Item -ToSession $session -LiteralPath $apiPackage -Destination $remoteStaging -Recurse -Force
    Copy-Item -ToSession $session -LiteralPath $frontendPackage -Destination $remoteStaging -Recurse -Force

    Invoke-Command -Session $session -ArgumentList $remoteStaging, $ApiDestination, $FrontendDestination, $ApiAppPool, $FrontendAppPool, $FrontendService -ScriptBlock {
        param($staging, $apiDestination, $frontendDestination, $apiAppPool, $frontendAppPool, $frontendService)
        $ErrorActionPreference = 'Stop'
        Import-Module WebAdministration

        foreach ($pool in @($apiAppPool, $frontendAppPool) | Where-Object { $_ }) {
            if ((Get-WebAppPoolState -Name $pool).Value -eq 'Started') { Stop-WebAppPool -Name $pool }
        }
        if ($frontendService -and (Get-Service -Name $frontendService -ErrorAction SilentlyContinue)) {
            Stop-Service -Name $frontendService -Force
        }

        try {
            foreach ($deployment in @(
                @{ Source = (Join-Path $staging 'backend'); Destination = $apiDestination },
                @{ Source = (Join-Path $staging 'frontend'); Destination = $frontendDestination }
            )) {
                New-Item -ItemType Directory -Path $deployment.Destination -Force | Out-Null
                robocopy $deployment.Source $deployment.Destination /E /COPY:DAT /R:2 /W:2 /NFL /NDL /NP
                if ($LASTEXITCODE -gt 7) { throw "robocopy fallo al copiar a $($deployment.Destination) (codigo $LASTEXITCODE)." }
            }
        }
        finally {
            if ($frontendService -and (Get-Service -Name $frontendService -ErrorAction SilentlyContinue)) {
                Start-Service -Name $frontendService
            }
            foreach ($pool in @($apiAppPool, $frontendAppPool) | Where-Object { $_ }) {
                Start-WebAppPool -Name $pool
            }
            Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
finally {
    if ($session) { Remove-PSSession $session }
}

Write-Host '`nDespliegue Stage completado.' -ForegroundColor Green
