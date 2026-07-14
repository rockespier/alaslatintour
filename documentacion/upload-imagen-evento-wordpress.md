# Endpoint para subir imagen de evento a WordPress

Este documento describe el endpoint del backend que sube el afiche o imagen principal de un evento a WordPress y devuelve la URL final para guardarla luego en el evento.

## Resumen

| Campo | Valor |
|-------|-------|
| Endpoint | `POST /v1/uploads/event-poster` |
| Controller | `UploadsController` |
| Content-Type | `multipart/form-data` |
| Campo del archivo | `file` |
| Formatos permitidos | `image/jpeg`, `image/png`, `image/webp` |
| Respuesta exitosa | `201 Created` |
| Servicio externo | WordPress REST API media |

El backend funciona como proxy hacia WordPress. El frontend no debe subir archivos directamente a WordPress.

## Flujo esperado

1. El frontend selecciona la imagen del evento.
2. El frontend envia el archivo a `POST /v1/uploads/event-poster` usando `multipart/form-data`.
3. El backend valida que exista un archivo y que su `Content-Type` sea permitido.
4. El backend sube el archivo a WordPress usando las credenciales configuradas.
5. WordPress devuelve la metadata del media creado.
6. El backend responde con `mediaId`, `url`, `fileName`, `contentType` y `sizeBytes`.
7. El frontend toma `response.url`.
8. El frontend envia esa URL como `imagenUrl` al crear o actualizar el evento con `POST /v1/events` o `PUT /v1/events/{eventId}`.

Importante: este endpoint no crea ni actualiza el evento. Solo sube la imagen y devuelve la URL publica.

## Request

```http
POST {{base_url}}/v1/uploads/event-poster
Content-Type: multipart/form-data
```

### Form-data

| Campo | Tipo | Requerido | Descripcion |
|-------|------|-----------|-------------|
| `file` | archivo | si | Imagen del evento en JPG, PNG o WEBP |

## Ejemplo con cURL

```bash
curl -X POST "{{base_url}}/v1/uploads/event-poster" \
  -F "file=@./mancora-pro-2026.png;type=image/png"
```

## Ejemplo con Angular

```ts
const formData = new FormData();
formData.append('file', file);

this.http
  .post<UploadedMediaResponse>('/v1/uploads/event-poster', formData)
  .subscribe((uploaded) => {
    const imagenUrl = uploaded.url;
    // Usar imagenUrl en POST /v1/events o PUT /v1/events/{eventId}
  });
```

```ts
export interface UploadedMediaResponse {
  mediaId: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}
```

## Response exitoso

Status: `201 Created`

```json
{
  "mediaId": "501",
  "url": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2026.png",
  "fileName": "mancora-pro-2026.png",
  "contentType": "image/png",
  "sizeBytes": 248193
}
```

### Campos de respuesta

| Campo | Tipo | Descripcion |
|-------|------|-------------|
| `mediaId` | string | ID del media creado en WordPress |
| `url` | string | URL publica del archivo subido a WordPress |
| `fileName` | string | Nombre del archivo recibido por el backend |
| `contentType` | string | MIME type del archivo |
| `sizeBytes` | number | Peso del archivo en bytes, si el stream permite calcularlo |

## Errores

### Archivo faltante

Status: `400 Bad Request`

```json
{
  "message": "Debes adjuntar un archivo."
}
```

### Tipo de archivo no permitido

Status: `400 Bad Request`

```json
{
  "message": "Solo se permiten archivos JPG, PNG o WEBP."
}
```

### Error de WordPress

Si WordPress responde con error, el adapter lanza una excepcion con el status y detalle devuelto por WordPress. El middleware global del backend transforma esa excepcion en la respuesta de error correspondiente.

## Configuracion requerida

El backend usa la seccion `WordPressConfig`:

```json
{
  "WordPressConfig": {
    "BaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/posts",
    "PostsBaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/posts",
    "GalleriesBaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/gallery",
    "MediaBaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/media",
    "Username": "usuario-wordpress",
    "AppPassword": "application-password-wordpress"
  }
}
```

Para este endpoint se usa `MediaBaseUrl`. Si no esta configurado, el backend intenta resolver la URL de media a partir de `BaseUrl`.

La autenticacion contra WordPress se hace con Basic Auth usando `Username` y `AppPassword`.

## Uso junto al CRUD de eventos

Despues de subir la imagen, usar `url` como `imagenUrl`:

```json
{
  "circuitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nombre": "Mancora Pro 2026",
  "pais": "PE",
  "ciudad": "Mancora",
  "fechaInicio": "2026-07-20",
  "fechaFin": "2026-07-24",
  "stars": 3,
  "capacidad": 160,
  "estado": "Borrador",
  "imagenUrl": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2026.png"
}
```

Endpoints relacionados:

| Accion | Endpoint |
|--------|----------|
| Crear evento con imagen | `POST /v1/events` |
| Actualizar imagen del evento | `PUT /v1/events/{eventId}` |
| Consultar evento | `GET /v1/events/{eventId}` |

## Referencias en codigo

- `backend/src/AlasApp.Api/Controllers/UploadsController.cs`
- `backend/src/AlasApp.Api/Models/UploadContracts.cs`
- `backend/src/AlasApp.Infrastructure/WordPress/WordPressMediaService.cs`
- `backend/src/AlasApp.Infrastructure/WordPress/WordPressConfig.cs`
- `backend/tests/AlasApp.Api.Tests/UploadsEndpointsTests.cs`
