param(
    [string]$Owner = "@me",
    [string]$ProjectTitle = "Desarrollo de producto Alas"
)

$ErrorActionPreference = "Stop"

function Invoke-Gh {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & gh @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Error ejecutando: gh $($Arguments -join ' ')"
    }
}

Write-Host "Verificando autenticación..." -ForegroundColor Cyan
Invoke-Gh -Arguments @("auth", "status")

Write-Host "Creando Project: $ProjectTitle" -ForegroundColor Cyan

$projectJson = & gh project create `
    --owner $Owner `
    --title $ProjectTitle `
    --format json

if ($LASTEXITCODE -ne 0) {
    throw "No se pudo crear el Project."
}

$project = $projectJson | ConvertFrom-Json
$projectNumber = $project.number
$projectUrl = $project.url

if (-not $projectNumber) {
    throw "GitHub no devolvió el número del Project."
}

Write-Host "Project creado: #$projectNumber" -ForegroundColor Green
Write-Host $projectUrl -ForegroundColor DarkGray

# Priority
Invoke-Gh -Arguments @(
    "project", "field-create", "$projectNumber",
    "--owner", $Owner,
    "--name", "Priority",
    "--data-type", "SINGLE_SELECT",
    "--single-select-options", "Critical,High,Medium,Low"
)

# Area
Invoke-Gh -Arguments @(
    "project", "field-create", "$projectNumber",
    "--owner", $Owner,
    "--name", "Area",
    "--data-type", "SINGLE_SELECT",
    "--single-select-options",
    "Backend .NET,Frontend Angular,Database,API,Authentication,Security,DevOps,Documentation"
)

# Estimate
Invoke-Gh -Arguments @(
    "project", "field-create", "$projectNumber",
    "--owner", $Owner,
    "--name", "Estimate",
    "--data-type", "SINGLE_SELECT",
    "--single-select-options", "XS,S,M,L,XL"
)

# Assigned agent
Invoke-Gh -Arguments @(
    "project", "field-create", "$projectNumber",
    "--owner", $Owner,
    "--name", "Assigned agent",
    "--data-type", "SINGLE_SELECT",
    "--single-select-options",
    "Unassigned,Architect .NET,Backend .NET,Angular,DBA - EF Core,DevOps,Codex,Claude Code,Human"
)

# Target version
Invoke-Gh -Arguments @(
    "project", "field-create", "$projectNumber",
    "--owner", $Owner,
    "--name", "Target version",
    "--data-type", "TEXT"
)

Write-Host ""
Write-Host "Campos creados correctamente." -ForegroundColor Green

Write-Host ""
Write-Host "Campos actuales:" -ForegroundColor Cyan

Invoke-Gh -Arguments @(
    "project", "field-list", "$projectNumber",
    "--owner", $Owner
)

Write-Host ""
Write-Host "Faltan dos configuraciones manuales:" -ForegroundColor Yellow
Write-Host "1. Configurar las opciones del campo Status."
Write-Host "2. Crear el campo Iteration."
Write-Host ""
Write-Host "Project: $projectUrl" -ForegroundColor Green