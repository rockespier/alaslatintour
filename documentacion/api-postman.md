# AlasApp API — Guía de Postman

## Configuración base

| Variable | Valor |
|----------|-------|
| `{{base_url}}` | `http://localhost:5132` |
| `{{version}}` | `v1` |

URL completa de ejemplo: `http://localhost:5132/v1/circuits`

> **Autenticación:** `login`, `register`, `password-reset/request` y `password-reset/confirm` son públicos. `logout` requiere `Authorization: Bearer {{access_token}}`. Los endpoints de `/v1/admin/*` sí tienen enforcement de autenticación y permisos (`401` / `403`).

---

## Convenciones generales

- **IDs:** GUIDs en formato string — `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`
- **Fechas:** Solo fecha, formato `yyyy-MM-dd` — `"2025-07-01"` (sin hora)
- **Enums:** Strings en español con tildes — `"Latinoamérica"`, `"Próximamente"`, etc.
- **Paginación:** Todos los listados devuelven `{ "data": [...], "pagination": { "currentPage", "itemsPerPage", "totalItems", "totalPages" } }`

---

## Entrega para frontend Angular

Los endpoints nuevos o modificados que el equipo frontend debe considerar en esta fase son:

| Módulo | Método | Ruta | Estado |
|--------|--------|------|--------|
| Auth | `POST` | `/v1/auth/login` | nuevo |
| Auth | `POST` | `/v1/auth/register` | nuevo |
| Auth | `POST` | `/v1/auth/password-reset/request` | nuevo |
| Auth | `POST` | `/v1/auth/password-reset/confirm` | nuevo |
| Auth | `POST` | `/v1/auth/logout` | nuevo |
| Articles | `GET` | `/v1/articles` | nuevo |
| Articles | `GET` | `/v1/articles/{slug}` | nuevo |
| Articles | `POST` | `/v1/articles` | nuevo |
| Articles | `PUT` | `/v1/articles/{slug}` | nuevo |
| Articles | `DELETE` | `/v1/articles/{slug}` | nuevo |
| Galleries | `GET` | `/v1/galleries` | nuevo |
| Galleries | `GET` | `/v1/galleries/{slug}` | modificado |
| Uploads | `POST` | `/v1/uploads/event-poster` | nuevo |
| Categories | `GET` | `/v1/categories` | modificado |
| Categories | `GET` | `/v1/categories/{categoryId}` | modificado |
| Categories | `POST` | `/v1/categories` | modificado |
| Categories | `PUT` | `/v1/categories/{categoryId}` | modificado |
| Rankings | `GET` | `/v1/rankings` | nuevo |
| Rankings | `GET` | `/v1/rankings/categories` | nuevo |
| SurfScores | `POST` | `/v1/surfscores/sync/{circuitId}` | nuevo |
| Memberships | `GET` | `/v1/memberships` | nuevo |
| Memberships | `GET` | `/v1/memberships/{membershipId}` | nuevo |
| Memberships | `POST` | `/v1/memberships` | nuevo |
| Memberships | `PUT` | `/v1/memberships/{membershipId}` | nuevo |
| Memberships | `DELETE` | `/v1/memberships/{membershipId}` | nuevo |
| Admin | `GET` | `/v1/admin/users` | nuevo |
| Admin | `GET` | `/v1/admin/users/{userId}` | nuevo |
| Admin | `POST` | `/v1/admin/users` | nuevo |
| Admin | `PUT` | `/v1/admin/users/{userId}` | nuevo |
| Admin | `DELETE` | `/v1/admin/users/{userId}` | nuevo |
| Admin | `GET` | `/v1/admin/roles` | nuevo |
| Admin | `GET` | `/v1/admin/dashboard` | nuevo |

### Cambios importantes de contrato

- `GET /v1/articles/{slug}` devuelve campos adicionales para frontend: `id`, `content`, `showRankingWidget` y `author`.
- `GET /v1/articles` ya resuelve `imagenUrl` y `tags`.
- `GET /v1/galleries` devuelve una card liviana con una sola portada por galería.
- `GET /v1/galleries/{slug}` ya no devuelve `photos` plano. Ahora devuelve `galleryDays[]` con `assets[]` tipados.
- `POST /v1/uploads/event-poster` sube la imagen a WordPress y devuelve la URL final que luego debe enviarse en `POST/PUT /v1/events`.
- `GET/PUT /v1/admin/settings` expone `general.administrativeFeeUsd` para parametrizar la cuota administrativa global.
- `POST /v1/events` y `PUT /v1/events/{id}` aceptan `imagenUrl` para el afiche del evento.
- `POST /v1/events` y `PUT /v1/events/{id}` aceptan además `auspiciador` y `eventType`.
- `GET /v1/events` y `GET /v1/events/{id}` ahora devuelven `auspiciador` y `eventType`.
- `POST /v1/events` y `PUT /v1/events/{id}` permiten `stars` de `1` a `7`.
- `GET/PUT /v1/events/{eventId}/categories` devuelve `gender` y `stars` por categoría habilitada.
- `GET/PUT /v1/events/{eventId}/categories` ya no maneja tarifa COP; el override por evento quedó solo en USD.
- `GET/POST/PUT /v1/categories` ahora expone `membresiaAnualUsd` y `membresiaPorEventoUsd`.
- `GET /v1/inscriptions`, `GET /v1/inscriptions/{id}` y `GET /v1/competitors/{id}/inscriptions` ahora devuelven `baseAmountUsd` y, solo si aplica, `administrativeFeeUsd`.
- `GET /v1/categories/{categoryId}/tariffs` y `PUT /v1/categories/{categoryId}/tariffs/{starLevel}` soportan `starLevel` de `1` a `7`.
- En ranking, los eventos `Prime` aplican bono `+10%` y `SuperPrime` `+50%` sobre `ligaPoints` al construir la caché.
- La matriz de puntos y la distribución de premios ya contemplan eventos de 6 y 7 estrellas.
- `/v1/admin/*` requiere JWT y aplica permisos por rol. Angular debe manejar `401 Unauthorized` y `403 Forbidden`.

---

## 1. Circuits — `/v1/circuits`

## Auth — `/v1/auth`

### POST /v1/auth/login — Iniciar sesión

```http
POST {{base_url}}/v1/auth/login
Content-Type: application/json
```

```json
{
  "email": "competidor@ejemplo.com",
  "password": "Password1",
  "rememberMe": false
}
```

**Response:** `200 OK`

**Ejemplo de response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "competidor@ejemplo.com",
    "fullName": "Juan Perez",
    "tipo": "competidor"
  }
}
```

**Errores comunes:**
- `401 Unauthorized` si el email o password no coinciden.
- `400 Bad Request` si faltan campos obligatorios.

---

### POST /v1/auth/register — Registrar usuario

```http
POST {{base_url}}/v1/auth/register
Content-Type: application/json
```

### Registro de espectador

```json
{
  "email": "viewer@ejemplo.com",
  "password": "Password1",
  "nombre": "Ana",
  "apellido": "Ruiz",
  "tipo": "espectador",
  "pais": "",
  "idiomaPreferido": "English",
  "newsletter": true,
  "terminos": true,
  "reglamento": false,
  "fechaNacimiento": "0001-01-01",
  "genero": "Ambos",
  "telefono": "",
  "club": "",
  "postura": "Regular",
  "tallaCamiseta": "M",
  "federacion": "",
  "patrocinadores": ""
}
```

### Registro de competidor

```json
{
  "email": "competidor@ejemplo.com",
  "password": "Password1",
  "nombre": "Juan",
  "apellido": "Perez",
  "tipo": "competidor",
  "pais": "Perú",
  "idiomaPreferido": "Español",
  "newsletter": false,
  "terminos": true,
  "reglamento": true,
  "fechaNacimiento": "1998-03-20",
  "genero": "Masculino",
  "telefono": "+51 999 111 222",
  "club": "Club Local",
  "postura": "Regular",
  "tallaCamiseta": "M",
  "federacion": "FENTA",
  "patrocinadores": "Marca X"
}
```

**Reglas relevantes de implementación actual:**
- `password` debe tener al menos 8 caracteres, 1 mayúscula y 1 dígito.
- Para `tipo = competidor`, `reglamento`, `fechaNacimiento`, `genero`, `postura`, `tallaCamiseta` y `pais` son requeridos.
- Si el correo ya existe, responde `409 Conflict`.

**Ejemplo de response para competidor:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "competidor@ejemplo.com",
  "tipo": "competidor",
  "licenseStatus": "Pendiente de validación",
  "message": "Registro exitoso. Tu licencia será validada en las próximas 48 horas."
}
```

**Ejemplo de response para espectador:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "viewer@ejemplo.com",
  "tipo": "espectador",
  "message": "Registro exitoso."
}
```

---

### POST /v1/auth/password-reset/request — Solicitar recuperación

```http
POST {{base_url}}/v1/auth/password-reset/request
Content-Type: application/json
```

```json
{
  "email": "competidor@ejemplo.com"
}
```

**Response:** `200 OK`

**Ejemplo de response:**
```json
{
  "message": "Si el correo existe, recibirás un enlace de recuperación."
}
```

> La respuesta es uniforme aunque el correo no exista.

---

### POST /v1/auth/password-reset/confirm — Confirmar nueva contraseña

```http
POST {{base_url}}/v1/auth/password-reset/confirm
Content-Type: application/json
```

```json
{
  "token": "token-recibido-por-correo",
  "newPassword": "Password2"
}
```

**Response:** `200 OK`

**Ejemplo de response:**
```json
{
  "message": "Contraseña actualizada correctamente."
}
```

**Errores comunes:**
- `400 Bad Request` si el token es inválido o expiró.
- `400 Bad Request` si `newPassword` no cumple la política.

---

### POST /v1/auth/logout — Cerrar sesión

```http
POST {{base_url}}/v1/auth/logout
Authorization: Bearer {{access_token}}
```

**Body:** vacío

**Response:** `200 OK`

```json
{
  "message": "Sesión cerrada."
}
```

> El `logout` invalida la versión activa del token. Si intentas reutilizar el mismo JWT, la API debe responder `401 Unauthorized`.

---

### GET /v1/circuits — Listar circuitos

```
GET {{base_url}}/v1/circuits
```

**Query params (todos opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número (default: 1) |
| `limit` | número (default: 20) |
| `status` | `Activo` · `Borrador` · `Archivado` · `Proximo` |
| `year` | año, ej: `2025` |
| `modalidad` | `Shortboard` · `Longboard` · `Mixed` |

**Ejemplo con filtros:**
```
GET {{base_url}}/v1/circuits?status=Activo&year=2025&page=1&limit=10
```

---

### GET /v1/circuits/{id} — Obtener circuito

```
GET {{base_url}}/v1/circuits/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Responses:** `200 OK` · `404 Not Found`

---

### POST /v1/circuits — Crear circuito

```
POST {{base_url}}/v1/circuits
Content-Type: application/json
```

```json
{
  "nombre": "ALAS Latin Tour 2025",
  "temporada": 2025,
  "descripcion": "Circuito principal de surf latinoamericano",
  "region": "Latinoamérica",
  "modalidad": "Shortboard",
  "estado": "Borrador",
  "surfScoresCode": "ALT2025"
}
```

**Enums válidos:**
- `region`: `Latinoamérica` · `América del Sur` · `América Central` · `América del Norte`
- `modalidad`: `Shortboard` · `Longboard` · `Mixed`
- `estado`: `Activo` · `Borrador` · `Archivado` · `Próximo`

**Response:** `201 Created` con header `Location: /v1/circuits/{newId}`

**Ejemplo de response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nombre": "ALAS Latin Tour 2025",
  "temporada": 2025,
  "descripcion": "Circuito principal de surf latinoamericano",
  "region": "Latinoamérica",
  "modalidad": "Shortboard",
  "estado": "Borrador",
  "surfScoresCode": "ALT2025",
  "eventsCount": 0,
  "competidoresCount": 0,
  "totalPrizeUsd": 0.0,
  "lastSyncAt": null,
  "createdAt": "2025-07-01T00:00:00Z",
  "updatedAt": "2025-07-01T00:00:00Z"
}
```

---

### PUT /v1/circuits/{id} — Actualizar circuito

```
PUT {{base_url}}/v1/circuits/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

```json
{
  "nombre": "ALAS Latin Tour 2025",
  "temporada": 2025,
  "descripcion": "Descripción actualizada",
  "region": "América del Sur",
  "modalidad": "Shortboard",
  "estado": "Activo",
  "surfScoresCode": "ALT2025"
}
```

**Response:** `200 OK`

---

### DELETE /v1/circuits/{id} — Eliminar circuito

```
DELETE {{base_url}}/v1/circuits/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `204 No Content`

---

## 2. Events — `/v1/events`

### GET /v1/events — Listar eventos

```
GET {{base_url}}/v1/events
```

**Query params (todos opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número (default: 1) |
| `limit` | número (default: 20) |
| `circuitId` | GUID |
| `status` | `Inscripciones Abiertas` · `Proximamente` · `Completado` · `Cerrado` |
| `country` | código de país, ej: `PE` |
| `year` | año, ej: `2025` |
| `stars` | `1` · `2` · `3` · `4` · `5` · `6` · `7` |

**Ejemplo:**
```
GET {{base_url}}/v1/events?circuitId=3fa85f64-5717-4562-b3fc-2c963f66afa6&stars=3
```

---

### GET /v1/events/{id} — Obtener evento

```
GET {{base_url}}/v1/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Responses:** `200 OK` · `404 Not Found`

---

### POST /v1/events — Crear evento

```
POST {{base_url}}/v1/events
Content-Type: application/json
```

```json
{
  "nombre": "Máncora Pro 2025",
  "circuitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fechaInicio": "2025-07-15",
  "fechaFin": "2025-07-18",
  "pais": "PE",
  "ciudad": "Máncora",
  "playa": "Playa de Máncora",
  "auspiciador": "Monster Energy",
  "imagenUrl": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2025.png",
  "stars": 6,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "surfScoresCode": "MAN2025",
  "eventType": "Prime",
  "accessType": "Abierto",
  "estado": "Borrador"
}
```

**Enums válidos:**
- `eventType`: `Regular` · `Prime` · `SuperPrime`
- `accessType`: `Abierto` · `Restringido` · `Solo invitación`
- `estado`: `Activo` · `Próximamente` · `Completado` · `Cancelado` · `Borrador`

**Response:** `201 Created`

**Ejemplo de response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nombre": "Máncora Pro 2025",
  "circuitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fechaInicio": "2025-07-15",
  "fechaFin": "2025-07-18",
  "pais": "PE",
  "ciudad": "Máncora",
  "playa": "Playa de Máncora",
  "auspiciador": "Monster Energy",
  "imagenUrl": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2025.png",
  "stars": 6,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "enrolledCount": 0,
  "eventType": "Prime",
  "statusPublic": "Próximamente",
  "lugar": "Máncora, PE",
  "createdAt": "2025-07-01T00:00:00Z",
  "updatedAt": "2025-07-01T00:00:00Z"
}
```

---

### PUT /v1/events/{id} — Actualizar evento

```
PUT {{base_url}}/v1/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

```json
{
  "nombre": "Máncora Pro 2025",
  "circuitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fechaInicio": "2025-07-15",
  "fechaFin": "2025-07-18",
  "pais": "PE",
  "ciudad": "Máncora",
  "playa": "Playa de Máncora",
  "auspiciador": "Monster Energy",
  "imagenUrl": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2025-v2.png",
  "stars": 7,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "eventType": "SuperPrime",
  "accessType": "Abierto",
  "estado": "Activo"
}
```

**Response:** `200 OK`

**Notas de frontend:**
- `auspiciador` es opcional y puede enviarse `null`.
- `eventType` es obligatorio y debe enviarse siempre; si no hay selección explícita usar `Regular`.
- La UI de ranking no recibe el multiplicador por separado; el valor final ya llega aplicado en `points`.

---

## 11. Rankings — `/v1/rankings`

### GET /v1/rankings — Ranking cacheado por categoría

```
GET {{base_url}}/v1/rankings?categoryId={{category_id}}&year=2026&page=1&limit=20
```

**Query params:**

| Param | Requerido | Descripción |
|-------|-----------|-------------|
| `categoryId` | sí | GUID de la categoría |
| `year` | no | temporada; default: año actual UTC |
| `page` | no | página; default: 1 |
| `limit` | no | tamaño de página; default: 20 |

**Ejemplo de response:**
```json
{
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "categoryName": "Open Mujeres",
  "year": 2026,
  "cachedAt": "2026-07-06T10:15:00Z",
  "attribution": "Results by SurfScores.com",
  "data": [
    {
      "pos": 1,
      "name": "Sofia Perez",
      "country": "Peru",
      "points": 400,
      "events": 1,
      "variation": 0
    }
  ],
  "pagination": {
    "currentPage": 1,
    "itemsPerPage": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

**Responses:** `200 OK` · `404 Not Found`

---

### GET /v1/rankings/categories — Categorías disponibles en rankings

```
GET {{base_url}}/v1/rankings/categories
```

**Ejemplo de response:**
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "nombre": "Open Mujeres",
      "availableYears": [2026]
    }
  ]
}
```

**Response:** `200 OK`

---

## 12. SurfScores Sync — `/v1/surfscores`

### POST /v1/surfscores/sync/{circuitId} — Sincronizar cache de ranking

```
POST {{base_url}}/v1/surfscores/sync/{{circuit_id}}
```

**Body:** vacío

**Ejemplo de response:**
```json
{
  "syncedAt": "2026-07-06T10:15:00Z",
  "recordsUpdated": 12,
  "circuitCode": "ALAS-RANK-26"
}
```

**Responses:** `200 OK` · `404 Not Found`

---

## 13. Articles — `/v1/articles`

### GET /v1/articles — Listar noticias

```http
GET {{base_url}}/v1/articles?page=1&limit=10&category=Resultados&featured=true&search=alas
```

**Query params (opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número (default: 1) |
| `limit` | número (default: 20) |
| `category` | `Resultados` · `Circuito` · `Entrevista` · `Reglamento` · `Tecnología` |
| `featured` | `true` · `false` |
| `search` | texto libre |

**Ejemplo de response:**
```json
{
  "data": [
    {
      "autor": "Equipo ALAS",
      "autorTitulo": "Periodista ALAS",
      "categoria": "Resultados",
      "featured": true,
      "fechaPublicacion": "2026-07-08T00:00:00Z",
      "id": "article-1",
      "imagenUrl": "https://cdn.test/nota.jpg",
      "relatedEventId": null,
      "resumen": "Resumen destacado",
      "slug": "nota-destacada",
      "tags": ["tour", "ranking"],
      "tiempoLecturaMin": 3,
      "titulo": "Nota destacada"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "itemsPerPage": 10,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

---

### GET /v1/articles/{slug} — Detalle de noticia

```http
GET {{base_url}}/v1/articles/nota-destacada
```

**Ejemplo de response:**
```json
{
  "autor": "Equipo ALAS",
  "autorTitulo": "Periodista ALAS",
  "author": {
    "name": "Equipo ALAS",
    "role": "Periodista ALAS"
  },
  "categoria": "Resultados",
  "content": "<p>Contenido destacado</p>",
  "featured": true,
  "fechaPublicacion": "2026-07-08T00:00:00Z",
  "id": "article-1",
  "imagenUrl": "https://cdn.test/nota.jpg",
  "relatedEventId": null,
  "resumen": "Resumen destacado",
  "showRankingWidget": true,
  "slug": "nota-destacada",
  "tags": ["tour", "ranking"],
  "tiempoLecturaMin": 3,
  "titulo": "Nota destacada"
}
```

> `content` contiene HTML renderizado desde WordPress.

---

### POST /v1/articles — Crear noticia

```http
POST {{base_url}}/v1/articles
Content-Type: application/json
```

```json
{
  "titulo": "Nueva nota",
  "resumen": "Resumen de la nota",
  "categoria": "Circuito",
  "autor": "Equipo ALAS",
  "autorTitulo": "Redacción",
  "imagenUrl": "https://cdn.test/image.jpg",
  "tags": ["tour", "olas"],
  "featured": false,
  "relatedEventId": null,
  "content": "<p>Contenido HTML</p>",
  "showRankingWidget": false
}
```

**Response:** `201 Created`

---

### PUT /v1/articles/{slug} — Actualizar noticia

Usa el mismo body que `POST /v1/articles`.

**Response:** `200 OK`

---

### DELETE /v1/articles/{slug} — Eliminar noticia

```http
DELETE {{base_url}}/v1/articles/nota-destacada
```

**Response:** `204 No Content`

---

## 14. Galleries — `/v1/galleries`

## Uploads — `/v1/uploads`

### POST /v1/uploads/event-poster — Subir afiche de evento a WordPress

```http
POST {{base_url}}/v1/uploads/event-poster
Content-Type: multipart/form-data
```

**Form-data:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `file` | archivo | sí | Imagen JPG, PNG o WEBP |

**Response:** `201 Created`

**Ejemplo de response:**
```json
{
  "mediaId": "501",
  "url": "https://alasglobaltour.rtres.net/wp-content/uploads/2026/07/mancora-pro-2025.png",
  "fileName": "mancora-pro-2025.png",
  "contentType": "image/png",
  "sizeBytes": 248193
}
```

**Errores comunes:**
- `400 Bad Request` si no se adjunta archivo.
- `400 Bad Request` si el `contentType` no es `image/jpeg`, `image/png` o `image/webp`.

**Flujo esperado para Angular:**
1. Subir el archivo al endpoint `/v1/uploads/event-poster`.
2. Tomar `response.url`.
3. Enviar esa URL en `imagenUrl` al crear o editar el evento con `/v1/events`.

> El backend actúa como proxy hacia WordPress. El frontend no debe llamar directamente la API de media de WordPress.

### GET /v1/galleries — Cards de galerías

```http
GET {{base_url}}/v1/galleries
```

**Ejemplo de response:**
```json
{
  "data": [
    {
      "id": "gallery-1",
      "slug": "roca-bruja-classic",
      "title": "Roca Bruja Classic",
      "eventDate": "2026-07-08T00:00:00Z",
      "coverImageUrl": "https://cdn.test/gallery-1-cover.jpg",
      "photoCount": 3
    }
  ]
}
```

> Este listado es liviano: una sola imagen de portada por galería.

---

### GET /v1/galleries/{slug} — Detalle de galería

```http
GET {{base_url}}/v1/galleries/roca-bruja-classic
```

**Ejemplo de response:**
```json
{
  "id": "gallery-1",
  "slug": "roca-bruja-classic",
  "title": "Roca Bruja Classic",
  "eventDate": "2026-07-08T00:00:00Z",
  "pressDownloadLink": "https://drive.test/roca-bruja",
  "coverImageUrl": "https://cdn.test/gallery-1-cover.jpg",
  "photoCount": 3,
  "galleryDays": [
    {
      "dayName": "Day 1",
      "assets": [
        {
          "id": "photo-1",
          "type": "photo",
          "url": "https://cdn.test/gallery-1-cover.jpg",
          "width": 700,
          "height": 467
        }
      ]
    },
    {
      "dayName": "Day 2",
      "assets": [
        {
          "id": "photo-3",
          "type": "photo",
          "url": "https://cdn.test/gallery-1-3.jpg",
          "width": 700,
          "height": 467
        }
      ]
    }
  ]
}
```

**Cambio relevante para Angular:**
- El detalle usa `galleryDays[].assets[]`.
- El campo `type` puede ser `photo` o `video`.
- El contrato anterior con `photos` plano ya no debe usarse.

---

## 15. Memberships — `/v1/memberships`

### GET /v1/memberships — Listar membresías

```http
GET {{base_url}}/v1/memberships?page=1&limit=20&status=Activo
```

**Query params (opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número (default: 1) |
| `limit` | número (default: 20) |
| `status` | `Activo` · `Vence pronto` |

**Ejemplo de response:**
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "clubFederacion": "Federacion Peruana de Surf",
      "pais": "Perú",
      "plan": "Mensual",
      "estado": "Activo",
      "inicioVigencia": "2026-07-01",
      "vencimiento": "2026-10-15",
      "emailContacto": "membresias@alas.test",
      "competidoresAfiliados": 2,
      "createdAt": "2026-07-09T00:00:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "itemsPerPage": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

---

### GET /v1/memberships/{membershipId}

```http
GET {{base_url}}/v1/memberships/{{membership_id}}
```

**Responses:** `200 OK` · `404 Not Found`

---

### POST /v1/memberships

```http
POST {{base_url}}/v1/memberships
Content-Type: application/json
```

```json
{
  "clubFederacion": "Federacion Peruana de Surf",
  "pais": "Perú",
  "plan": "Mensual",
  "inicioVigencia": "2026-07-01",
  "vencimiento": "2026-10-15",
  "emailContacto": "membresias@alas.test"
}
```

**Planes válidos:** `Mensual` · `Por evento`

---

### PUT /v1/memberships/{membershipId}

Usa el mismo body que `POST /v1/memberships`.

**Response:** `200 OK`

---

### DELETE /v1/memberships/{membershipId}

```http
DELETE {{base_url}}/v1/memberships/{{membership_id}}
```

**Response:** `204 No Content`

---

## 16. Admin — `/v1/admin`

### Requisitos de acceso

- Todos los endpoints requieren `Authorization: Bearer {{access_token}}`.
- `GET /v1/admin/dashboard` requiere permiso de lectura sobre `Dashboard`.
- `GET /v1/admin/users` y `GET /v1/admin/users/{userId}` requieren permiso `UsersRead`.
- `POST`, `PUT` y `DELETE` sobre `/v1/admin/users` requieren permiso `UsersWrite`.
- `GET /v1/admin/roles` requiere permiso `ConfigurationRead`.

### GET /v1/admin/users

```http
GET {{base_url}}/v1/admin/users
Authorization: Bearer {{access_token}}
```

**Ejemplo de response:**
```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "admin@test.com",
      "fullName": "Gabriel Villani",
      "initials": "GV",
      "role": "Admin",
      "status": "Activo",
      "createdAt": "2026-07-09T00:00:00Z",
      "lastSession": null
    }
  ]
}
```

---

### GET /v1/admin/users/{userId}

```http
GET {{base_url}}/v1/admin/users/{{user_id}}
Authorization: Bearer {{access_token}}
```

---

### POST /v1/admin/users

```http
POST {{base_url}}/v1/admin/users
Authorization: Bearer {{access_token}}
Content-Type: application/json
```

```json
{
  "nombre": "Gabriel",
  "apellido": "Villani",
  "email": "admin@test.com",
  "rol": "Admin",
  "sendInvitationEmail": true
}
```

**Roles válidos:** `Super Admin` · `Admin` · `Árbitro` · `Revisor`

---

### PUT /v1/admin/users/{userId}

```http
PUT {{base_url}}/v1/admin/users/{{user_id}}
Authorization: Bearer {{access_token}}
Content-Type: application/json
```

```json
{
  "rol": "Revisor",
  "status": "Inactivo"
}
```

**Status válidos:** `Activo` · `Inactivo`

---

### DELETE /v1/admin/users/{userId}

```http
DELETE {{base_url}}/v1/admin/users/{{user_id}}
Authorization: Bearer {{access_token}}
```

**Responses:** `204 No Content` · `404 Not Found` · `409 Conflict`

> `409 Conflict` si el usuario intenta eliminarse a sí mismo.

---

### GET /v1/admin/roles

```http
GET {{base_url}}/v1/admin/roles
Authorization: Bearer {{access_token}}
```

**Ejemplo de response:**
```json
{
  "data": [
    {
      "name": "Super Admin",
      "permissions": [
        {
          "module": "Dashboard",
          "level": "Full"
        }
      ]
    }
  ]
}
```

---

### GET /v1/admin/dashboard

```http
GET {{base_url}}/v1/admin/dashboard
Authorization: Bearer {{access_token}}
```

**Ejemplo de response:**
```json
{
  "kpis": {
    "totalCompetidores": 2,
    "totalEventosActivos": 1,
    "totalInscripciones": 2,
    "recaudacionMesUsd": 95.0,
    "tokensPendientes": 1
  },
  "activeEvents": [
    {
      "id": "guid-event",
      "nombre": "Evento Dashboard",
      "estado": "Activo",
      "fechaInicio": "2026-10-02T00:00:00Z",
      "inscritosCount": 2
    }
  ],
  "recentInscriptions": [
    {
      "competitorName": "Laura Mendez",
      "evento": "Evento Dashboard",
      "categoria": "Open",
      "inscripcionAt": "2026-07-09T00:00:00Z"
    }
  ]
}
```

---

### DELETE /v1/events/{id} — Eliminar evento

```
DELETE {{base_url}}/v1/events/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `204 No Content`

---

## 3. Categories — `/v1/categories`

### GET /v1/categories — Listar categorías

```
GET {{base_url}}/v1/categories
```

**Query params:**

| Param | Valores posibles |
|-------|-----------------|
| `status` | `Activo` · `Inactivo` |

---

### GET /v1/categories/{id} — Obtener categoría

```
GET {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Responses:** `200 OK` · `404 Not Found`

---

### POST /v1/categories — Crear categoría

```
POST {{base_url}}/v1/categories
Content-Type: application/json
```

```json
{
  "nombre": "Sub-16 Masculino",
  "descripcion": "Categoría masculina para menores de 16 años",
  "gender": "Masculino",
  "ageRestriction": true,
  "minAge": 12,
  "maxAge": 15,
  "membresiaAnualUsd": 35.0,
  "membresiaPorEventoUsd": 12.0,
  "successorCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Activo"
}
```

**Enums válidos:**
- `gender`: `Masculino` · `Femenino` · `Ambos`
- `status`: `Activo` · `Inactivo`

> **Nota:** `successorCategoryId` es la categoría a la que pasa el competidor al superar el límite de edad (ej: Sub-14 → Sub-16 → Sub-18 → Open).

**Response:** `201 Created`

**Ejemplo de response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nombre": "Sub-16 Masculino",
  "gender": "Masculino",
  "ageRestriction": true,
  "minAge": 12,
  "maxAge": 15,
  "membresiaAnualUsd": 35.0,
  "membresiaPorEventoUsd": 12.0,
  "successorCategory": {
    "id": "another-guid",
    "nombre": "Sub-18 Masculino"
  },
  "status": "Activo",
  "createdAt": "2025-07-01T00:00:00Z"
}
```

---

### PUT /v1/categories/{id} — Actualizar categoría

```
PUT {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

```json
{
  "nombre": "Sub-16 Masculino",
  "descripcion": "Descripción actualizada",
  "gender": "Masculino",
  "ageRestriction": true,
  "minAge": 12,
  "maxAge": 15,
  "membresiaAnualUsd": 40.0,
  "membresiaPorEventoUsd": 15.0,
  "successorCategoryId": null,
  "status": "Activo"
}
```

**Response:** `200 OK`

**Notas de frontend:**
- Ambos importes deben enviarse siempre; si la UI todavía no tiene captura, enviar `0`.
- La API rechaza valores negativos.

---

### DELETE /v1/categories/{id} — Eliminar categoría

```
DELETE {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `204 No Content`

---

## 4. Event Categories — `/v1/events/{eventId}/categories`

### GET /v1/events/{eventId}/categories — Ver categorías del evento

```
GET {{base_url}}/v1/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/categories
```

**Responses:** `200 OK` · `404 Not Found`

**Ejemplo de response:**
```json
{
  "data": [
    {
      "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "categoryName": "Sub-16 Masculino",
      "gender": "Masculino",
      "stars": 6,
      "customTariffUsd": null,
      "effectiveTariffUsd": 150.0,
      "capacidad": 30,
      "enrolledCount": 12
    }
  ],
  "useCircuitTariffs": true
}
```

> Si `customTariffUsd` es `null`, la tarifa efectiva se hereda del circuito según el nivel de estrellas del evento.
> `stars` permite sobrescribir el nivel competitivo por categoría; si llega `null`, la categoría usa las estrellas del evento.
> En este endpoint ya no existe `customTariffCop` ni `effectiveTariffCop`.

---

### PUT /v1/events/{eventId}/categories — Configurar categorías del evento

```
PUT {{base_url}}/v1/events/3fa85f64-5717-4562-b3fc-2c963f66afa6/categories
Content-Type: application/json
```

```json
{
  "useCircuitTariffs": true,
  "categories": [
    {
      "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "stars": null,
      "customTariffUsd": null,
      "capacidad": 30
    },
    {
      "categoryId": "another-guid-here",
      "stars": 7,
      "customTariffUsd": 200.0,
      "capacidad": 20
    }
  ]
}
```

> Enviar `customTariffUsd: null` hace que ese evento herede la tarifa del circuito para esa categoría.
> `stars` acepta `null` o un valor entre `1` y `7`.
> La personalización de tarifa por evento quedó únicamente en USD.

**Response:** `200 OK`

---

## 5. Category Tariffs — `/v1/categories/{categoryId}/tariffs`

### GET /v1/categories/{categoryId}/tariffs — Ver tarifas

```
GET {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6/tariffs
```

**Response:** `200 OK`

**Ejemplo de response:**
```json
{
  "data": [
    {
      "starLevel": 1,
      "usd": 75.0,
      "cop": 300000.0,
      "active": true
    },
    {
      "starLevel": 7,
      "usd": 95.0,
      "cop": 380000.0,
      "active": true
    }
  ]
}
```

> La API devuelve 7 filas, una por cada `starLevel` entre `1` y `7`.

---

### PUT /v1/categories/{categoryId}/tariffs/{starLevel} — Actualizar tarifa

```
PUT {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6/tariffs/7
Content-Type: application/json
```

```json
{
  "usd": 150.0,
  "cop": 600000.0,
  "active": true
}
```

**Response:** `200 OK`

> `starLevel` válido: `1` a `7`.

---

## 6. Competitors — `/v1/competitors`

### GET /v1/competitors — Listar competidores

```
GET {{base_url}}/v1/competitors?page=1&limit=20&country=Perú&licenseStatus=Activa
```

**Query params (opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número (default: 1) |
| `limit` | número (default: 20) |
| `country` | texto |
| `categoryId` | GUID |
| `licenseStatus` | `Activa` · `Pendiente de validación` |
| `search` | texto libre |

---

### POST /v1/competitors — Crear competidor

```http
POST {{base_url}}/v1/competitors
Content-Type: application/json
```

```json
{
  "nombre": "Juan",
  "apellido": "Perez",
  "email": "juan.perez@example.com",
  "fechaNacimiento": "1998-03-20",
  "genero": "Masculino",
  "pais": "Perú",
  "telefono": "+51 999 111 222",
  "club": "Club Local",
  "postura": "Regular",
  "tallaCamiseta": "M",
  "numeroCamiseta": "#12",
  "patrocinadores": "Marca X",
  "federacion": "FENTA"
}
```

**Response:** `201 Created`

---

### GET /v1/competitors/{id} — Obtener competidor

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Responses:** `200 OK` · `404 Not Found`

---

### PUT /v1/competitors/{id} — Actualizar competidor

```http
PUT {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

Usa el mismo body que `POST /v1/competitors`.

**Response:** `200 OK`

---

### DELETE /v1/competitors/{id} — Eliminar competidor

```
DELETE {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `204 No Content`

---

### PUT /v1/competitors/{id}/license — Actualizar licencia

```http
PUT {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/license
Content-Type: application/json
```

```json
{
  "status": "Activa",
  "licenseNumber": "ALAS-2026-001",
  "expirationDate": "2026-12-31",
  "enabledCategories": [
    "open-hombres",
    "open-mixto"
  ]
}
```

**Response:** `200 OK`

---

### GET /v1/competitors/{id}/notifications

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/notifications
```

### PUT /v1/competitors/{id}/notifications

```http
PUT {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/notifications
Content-Type: application/json
```

```json
{
  "email": true,
  "push": false,
  "resultados": true,
  "inscripciones": true
}
```

---

### GET /v1/competitors/{id}/inscriptions

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/inscriptions?status=pendiente
```

### GET /v1/competitors/{id}/points-history

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/points-history?year=2026&categoryId=open-mixto
```

### GET /v1/competitors/{id}/calendar

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/calendar
```

### GET /v1/competitors/{id}/calendar/export

```
GET {{base_url}}/v1/competitors/3fa85f64-5717-4562-b3fc-2c963f66afa6/calendar/export
```

Devuelve archivo `text/calendar`.

---

## 7. Inscriptions — `/v1/inscriptions`

### GET /v1/inscriptions — Listar inscripciones admin

```
GET {{base_url}}/v1/inscriptions?page=1&limit=20&eventId={eventId}&categoryId={categoryId}&status=Pendiente
```

---

### POST /v1/inscriptions — Crear inscripción

```http
POST {{base_url}}/v1/inscriptions
Content-Type: application/json
```

```json
{
  "competitorId": "guid",
  "eventId": "guid",
  "categoryId": "guid",
  "shirtNumber": "7",
  "paymentMethod": "beach",
  "reglamento": true
}
```

**Response:** `201 Created`

**Ejemplo de response:**
```json
{
  "id": "guid",
  "competitor": {
    "id": "guid",
    "fullName": "Carlos Diaz",
    "country": "Perú"
  },
  "event": {
    "id": "guid",
    "nombre": "Evento Inscriptions",
    "lugar": "Lima, Perú"
  },
  "category": {
    "id": "guid",
    "nombre": "Open Mixto"
  },
  "circuit": {
    "id": "guid",
    "nombre": "Circuito Inscriptions"
  },
  "shirtNumber": "#99",
  "paymentMethod": "Paypal",
  "baseAmountUsd": 80.0,
  "administrativeFeeUsd": 15.0,
  "montoUsd": 95.0,
  "estadoAdmin": "Pendiente",
  "estadoCompetidor": "Pendiente",
  "resultado": null,
  "transaccionId": null,
  "inscripcionAt": "2026-07-16T00:00:00Z"
}
```

**Reglas de frontend:**
- `montoUsd` es el total final a cobrar y el único valor que debe usarse para iniciar PayPal o registrar pagos.
- `baseAmountUsd` representa la inscripción sin recargo administrativo.
- `administrativeFeeUsd` puede no venir en el payload cuando la cuota configurada es `0`; en ese caso la UI debe ocultar esa línea del resumen.

---

### GET /v1/inscriptions/{id}

```
GET {{base_url}}/v1/inscriptions/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Devuelve el mismo desglose monetario de `POST /v1/inscriptions`.

### PUT /v1/inscriptions/{id}

```http
PUT {{base_url}}/v1/inscriptions/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

```json
{
  "shirtNumber": "#100",
  "estadoAdmin": "Pagado",
  "notes": "Pago confirmado"
}
```

### DELETE /v1/inscriptions/{id}

```
DELETE {{base_url}}/v1/inscriptions/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Regla:** solo el competidor autenticado dueño de la inscripción puede eliminarla, y únicamente si sigue incompleta (`Pendiente`, sin pago confirmado ni transacción).

---

### GET /v1/events/{eventId}/inscriptions — Inscritos del evento

```
GET {{base_url}}/v1/events/{eventId}/inscriptions?page=1&limit=20&categoryId={categoryId}&status=Pendiente
```

## 15. Admin Settings — `/v1/admin/settings`

### GET /v1/admin/settings

```http
GET {{base_url}}/v1/admin/settings
Authorization: Bearer {{access_token}}
```

**Campo nuevo relevante para frontend/backoffice:**
- `general.administrativeFeeUsd`: cuota administrativa global en USD.

**Ejemplo parcial de response:**
```json
{
  "general": {
    "organizationName": "ALAS Latin Tour",
    "shortName": "ALAS",
    "contactEmail": "info@alasglobaltour.com",
    "phone": "+57 310 000 0000",
    "website": "www.alaslatintour.com",
    "headquartersCountry": "Colombia",
    "administrativeFeeUsd": 15.0,
    "socialLinks": {
      "instagram": "@alasglobaltour",
      "facebook": "facebook.com/alaslatintour",
      "x": "@alasglobaltour",
      "youTube": "youtube.com/@alasglobaltour"
    },
    "season": {
      "currentYear": 2026,
      "startDate": "2026-01-01",
      "endDate": "2026-12-31"
    }
  }
}
```

### PUT /v1/admin/settings

```http
PUT {{base_url}}/v1/admin/settings
Authorization: Bearer {{access_token}}
Content-Type: application/json
```

**Reglas del campo `general.administrativeFeeUsd`:**
- debe enviarse como número decimal en USD,
- no admite valores negativos,
- si se guarda en `0`, las nuevas respuestas de inscripción omiten `administrativeFeeUsd`.

---

## 8. Payments — `/v1/payments`

### GET /v1/payments — Listar pagos

```
GET {{base_url}}/v1/payments?page=1&limit=20&method=Paypal&status=Confirmado
```

**Query params (opcionales):**

| Param | Valores posibles |
|-------|-----------------|
| `page` | número |
| `limit` | número |
| `method` | `Paypal` · `Beach` |
| `status` | `Confirmado` · `Pendiente` |
| `fromDate` | `yyyy-MM-dd` |
| `toDate` | `yyyy-MM-dd` |

---

### POST /v1/payments — Registrar pago

```http
POST {{base_url}}/v1/payments
Content-Type: application/json
```

```json
{
  "inscriptionId": "guid",
  "method": "Paypal",
  "amountUsd": 95.0,
  "transactionId": "PP-9X8C7B2A"
}
```

**Response:** `201 Created`

---

## 8.1 PayPal Checkout — `/v1/paypal`

### POST /v1/paypal/orders — Iniciar checkout

```http
POST {{base_url}}/v1/paypal/orders
Content-Type: application/json
```

```json
{
  "inscriptionId": "guid",
  "returnUrl": "http://localhost:4200/paypal/retorno?inscriptionId=guid",
  "cancelUrl": "http://localhost:4200/paypal/cancelado?inscriptionId=guid"
}
```

**Response:** `200 OK`

```json
{
  "orderId": "5O190127TN364715T",
  "approvalUrl": "https://www.sandbox.paypal.com/checkoutnow?token=5O190127TN364715T",
  "amountUsd": 95.0
}
```

### POST /v1/paypal/orders/{orderId}/capture — Confirmar pago

```http
POST {{base_url}}/v1/paypal/orders/5O190127TN364715T/capture
Content-Type: application/json
```

```json
{
  "inscriptionId": "guid"
}
```

**Uso frontend:**

1. Crear la inscripción con `paymentMethod = "Paypal"`.
2. Llamar `POST /v1/paypal/orders`.
3. Redirigir el navegador a `approvalUrl`.
4. PayPal volverá a `returnUrl` con `?token={orderId}`.
5. El frontend debe llamar `POST /v1/paypal/orders/{orderId}/capture`.

---

### GET /v1/payments/{id}

```
GET {{base_url}}/v1/payments/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### PUT /v1/payments/{id}

```http
PUT {{base_url}}/v1/payments/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

```json
{
  "status": "Confirmado",
  "notes": "Validado en playa"
}
```

### GET /v1/payments/kpis

```
GET {{base_url}}/v1/payments/kpis
```

---

## 9. Beach Tokens — `/v1/payments/beach`

### POST /v1/payments/beach/request

```http
POST {{base_url}}/v1/payments/beach/request
Content-Type: application/json
```

```json
{
  "inscriptionId": "guid"
}
```

**Response:** `201 Created`

---

### POST /v1/payments/beach/redeem

```http
POST {{base_url}}/v1/payments/beach/redeem
Content-Type: application/json
```

```json
{
  "inscriptionId": "guid",
  "tokenCode": "ABCD-1234"
}
```

> El token tiene formato `XXXX-XXXX` y TTL de 24 horas. Si expira o es inválido, la API devuelve `400`.

---

### GET /v1/payments/beach/tokens

```
GET {{base_url}}/v1/payments/beach/tokens?page=1&limit=20&status=Pendiente
```

**Status válidos:** `Pendiente` · `Usado` · `Expirado` · `Rechazado`

---

### POST /v1/payments/beach/tokens/{id}/approve

```
POST {{base_url}}/v1/payments/beach/tokens/{id}/approve
```

### POST /v1/payments/beach/tokens/{id}/reject

```http
POST {{base_url}}/v1/payments/beach/tokens/{id}/reject
Content-Type: application/json
```

```json
{
  "reason": "Documentacion incompleta"
}
```

---

## 17. Pendientes funcionales

No hay endpoints nuevos pendientes de documentar para Angular en los lotes cerrados.

Pendiente técnico fuera del contrato HTTP:

- validación funcional del adapter real contra WordPress
- validación funcional del adapter real contra SurfScores

---

## 18. Bootstrap Super Admin

El primer usuario `Super Admin` no se crea desde un endpoint público.

La API ahora soporta bootstrap automático al arranque usando configuración.

### Configuración en `appsettings`

En `backend/src/AlasApp.Api/appsettings.json` o `appsettings.Development.json`:

```json
"BootstrapAdmin": {
  "Enabled": true,
  "Email": "superadmin@alas.local",
  "Password": "Password1!",
  "Nombre": "Super",
  "Apellido": "Admin"
}
```

### Configuración por variables de entorno

```powershell
$env:BootstrapAdmin__Enabled="true"
$env:BootstrapAdmin__Email="superadmin@alas.local"
$env:BootstrapAdmin__Password="Password1!"
$env:BootstrapAdmin__Nombre="Super"
$env:BootstrapAdmin__Apellido="Admin"
```

### Comportamiento

- Si `Enabled = false`, no hace nada.
- Si ya existe un usuario con rol `Super Admin`, no crea otro.
- Si la configuración está incompleta, no crea nada.
- Si el email configurado ya existe pero no tiene rol `Super Admin`, no eleva esa cuenta automáticamente.

### Flujo recomendado

1. Configurar `BootstrapAdmin` con `Enabled = true`.
2. Levantar la API.
3. Hacer `POST /v1/auth/login` con ese email y password.
4. Verificar acceso a `/v1/admin/dashboard` o `/v1/admin/users`.
5. Volver a dejar `Enabled = false` para evitar depender del bootstrap en arranques posteriores.

### Ejemplo de login del bootstrap admin

```http
POST {{base_url}}/v1/auth/login
Content-Type: application/json
```

```json
{
  "email": "superadmin@alas.local",
  "password": "Password1!",
  "rememberMe": false
}
```

---

## Flujo de prueba recomendado

Para probar el ciclo completo de los lotes implementados:

```
1. POST /v1/categories
2. PUT  /v1/categories/{id}/tariffs/{starLevel}
3. POST /v1/circuits
4. POST /v1/events
5. PUT  /v1/events/{id}/categories
6. POST /v1/competitors
7. POST /v1/inscriptions
8. POST /v1/payments/beach/request
9. POST /v1/payments/beach/tokens/{id}/approve
10. POST /v1/payments/beach/redeem
11. PUT /v1/payments/{id}
12. GET /v1/payments/kpis
```
