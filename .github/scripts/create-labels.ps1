$ErrorActionPreference = "Stop"

$labels = @(
    # Tipo
    @{
        Name = "type:bug"
        Color = "D73A4A"
        Description = "Error o comportamiento incorrecto"
    },
    @{
        Name = "type:feature"
        Color = "A2EEEF"
        Description = "Nueva funcionalidad o mejora"
    },
    @{
        Name = "type:technical-debt"
        Color = "FBCA04"
        Description = "Refactorización o deuda técnica"
    },
    @{
        Name = "type:documentation"
        Color = "0075CA"
        Description = "Cambios relacionados con documentación"
    },
    @{
        Name = "type:dependencies"
        Color = "0366D6"
        Description = "Actualización de dependencias"
    },

    # Área
    @{
        Name = "area:backend"
        Color = "5319E7"
        Description = "Cambios en backend .NET"
    },
    @{
        Name = "area:frontend"
        Color = "1D76DB"
        Description = "Cambios en frontend Angular"
    },
    @{
        Name = "area:database"
        Color = "0052CC"
        Description = "Base de datos, SQL o Entity Framework Core"
    },
    @{
        Name = "area:api"
        Color = "006B75"
        Description = "Endpoints, contratos y modelos API"
    },
    @{
        Name = "area:authentication"
        Color = "B60205"
        Description = "Autenticación, autorización o Microsoft Entra ID"
    },
    @{
        Name = "area:security"
        Color = "D93F0B"
        Description = "Seguridad o vulnerabilidades"
    },
    @{
        Name = "area:devops"
        Color = "0E8A16"
        Description = "GitHub Actions, CI/CD o infraestructura"
    },

    # Estado
    @{
        Name = "status:triage"
        Color = "EDEDED"
        Description = "Pendiente de revisión y clasificación"
    },
    @{
        Name = "status:needs-analysis"
        Color = "D4C5F9"
        Description = "Necesita análisis técnico o funcional"
    },
    @{
        Name = "status:ready"
        Color = "0E8A16"
        Description = "Issue completo y listo para implementar"
    },
    @{
        Name = "status:in-progress"
        Color = "FBCA04"
        Description = "Trabajo actualmente en desarrollo"
    },
    @{
        Name = "status:review"
        Color = "1D76DB"
        Description = "Implementación pendiente de revisión"
    },
    @{
        Name = "status:blocked"
        Color = "B60205"
        Description = "Bloqueado por una dependencia o decisión"
    },
    @{
        Name = "status:done"
        Color = "6F42C1"
        Description = "Trabajo completado"
    },

    # Prioridad
    @{
        Name = "priority:critical"
        Color = "B60205"
        Description = "Debe atenderse inmediatamente"
    },
    @{
        Name = "priority:high"
        Color = "D93F0B"
        Description = "Alta prioridad"
    },
    @{
        Name = "priority:medium"
        Color = "FBCA04"
        Description = "Prioridad normal"
    },
    @{
        Name = "priority:low"
        Color = "C2E0C6"
        Description = "Puede atenderse posteriormente"
    },

    # Agente o especialidad
    @{
        Name = "agent:architect"
        Color = "7057FF"
        Description = "Requiere análisis de arquitectura"
    },
    @{
        Name = "agent:dotnet"
        Color = "512BD4"
        Description = "Trabajo recomendado para agente .NET"
    },
    @{
        Name = "agent:angular"
        Color = "DD0031"
        Description = "Trabajo recomendado para agente Angular"
    },
    @{
        Name = "agent:dba"
        Color = "336791"
        Description = "Trabajo recomendado para DBA o EF Core"
    },
    @{
        Name = "agent:devops"
        Color = "2088FF"
        Description = "Trabajo recomendado para DevOps"
    },

    # Otras
    @{
        Name = "needs-tests"
        Color = "F9D0C4"
        Description = "Necesita pruebas automatizadas"
    },
    @{
        Name = "breaking-change"
        Color = "B60205"
        Description = "Introduce un cambio incompatible"
    },
    @{
        Name = "good-first-issue"
        Color = "7057FF"
        Description = "Issue adecuado para comenzar a contribuir"
    }
)

Write-Host "Creando etiquetas en el repositorio actual..." -ForegroundColor Cyan

foreach ($label in $labels) {
    Write-Host "Procesando: $($label.Name)"

    gh label create $label.Name `
        --color $label.Color `
        --description $label.Description `
        --force

    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo crear o actualizar la etiqueta: $($label.Name)"
    }
}

Write-Host ""
Write-Host "Etiquetas creadas o actualizadas correctamente." -ForegroundColor Green