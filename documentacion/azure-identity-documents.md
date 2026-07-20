# Azure Blob Storage para documentos de identidad

Esta implementación guarda el documento de identidad adjunto durante el registro de usuarios competidores en un contenedor privado de Azure Blob Storage.

## App settings requeridos

Agregar la sección `IdentityDocuments` al `appsettings.{Environment}.json` o variables de entorno equivalentes:

```json
{
  "IdentityDocuments": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<storage-account>;AccountKey=<key>;EndpointSuffix=core.windows.net",
    "ContainerName": "competitor-identity-documents"
  }
}
```

Variables de entorno equivalentes:

- `IdentityDocuments__ConnectionString`
- `IdentityDocuments__ContainerName`

## Configuración recomendada en Azure

1. Crear o reutilizar una Storage Account privada.
2. Crear el contenedor `competitor-identity-documents` con acceso público deshabilitado.
3. Guardar el connection string en Azure App Service Configuration o Key Vault; no debe versionarse en el repositorio.
4. Restringir permisos del secret a la aplicación backend.
5. Activar soft delete y logs de acceso en la Storage Account si están disponibles en el entorno.

## Formatos aceptados

El backend acepta documentos `image/jpeg`, `image/png`, `image/webp` y `application/pdf`.

## Flujo funcional

- El frontend envía `multipart/form-data` a `POST /v1/auth/register`.
- Para usuarios `competidor`, el campo `identityDocument` es obligatorio.
- El backend crea el perfil de competidor, sube el archivo al blob privado y persiste el nombre del blob en `Competitors.IdentityDocumentBlobName`.
- El estado de licencia queda `PendienteDeValidacion` hasta que el administrador lo cambie a `Activa`.
- Las inscripciones rechazan competidores que no estén con licencia `Activa`.
