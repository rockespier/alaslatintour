# AlasApp API — Guía de Postman

## Configuración base

| Variable | Valor |
|----------|-------|
| `{{base_url}}` | `http://localhost:5132` |
| `{{version}}` | `v1` |

URL completa de ejemplo: `http://localhost:5132/v1/circuits`

> **Autenticación:** `login`, `register`, `password-reset/request` y `password-reset/confirm` son públicos. `logout` requiere header `Authorization: Bearer {{access_token}}`. El resto de endpoints sigue sin enforcement por rol en esta fase.

---

## Convenciones generales

- **IDs:** GUIDs en formato string — `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`
- **Fechas:** Solo fecha, formato `yyyy-MM-dd` — `"2025-07-01"` (sin hora)
- **Enums:** Strings en español con tildes — `"Latinoamérica"`, `"Próximamente"`, etc.
- **Paginación:** Todos los listados devuelven `{ "data": [...], "pagination": { "currentPage", "itemsPerPage", "totalItems", "totalPages" } }`

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
| `stars` | `1` · `2` · `3` · `4` · `5` |

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
  "stars": 3,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "surfScoresCode": "MAN2025",
  "accessType": "Abierto",
  "estado": "Borrador"
}
```

**Enums válidos:**
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
  "stars": 3,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "enrolledCount": 0,
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
  "stars": 3,
  "capacidadMaxima": 120,
  "prizeAmountUsd": 5000.0,
  "accessType": "Abierto",
  "estado": "Activo"
}
```

**Response:** `200 OK`

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
  "successorCategoryId": null,
  "status": "Activo"
}
```

**Response:** `200 OK`

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
      "customTariffUsd": null,
      "customTariffCop": null,
      "effectiveTariffUsd": 150.0,
      "effectiveTariffCop": 600000.0,
      "capacidad": 30,
      "enrolledCount": 12
    }
  ],
  "useCircuitTariffs": true
}
```

> Si `customTariffUsd` es `null`, la tarifa efectiva se hereda del circuito según el nivel de estrellas del evento.

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
      "customTariffUsd": null,
      "customTariffCop": null,
      "capacidad": 30
    },
    {
      "categoryId": "another-guid-here",
      "customTariffUsd": 200.0,
      "customTariffCop": 800000.0,
      "capacidad": 20
    }
  ]
}
```

> Enviar `customTariffUsd: null` hace que ese evento herede la tarifa del circuito para esa categoría.

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
      "starLevel": 2,
      "usd": 95.0,
      "cop": 380000.0,
      "active": true
    }
  ]
}
```

---

### PUT /v1/categories/{categoryId}/tariffs/{starLevel} — Actualizar tarifa

```
PUT {{base_url}}/v1/categories/3fa85f64-5717-4562-b3fc-2c963f66afa6/tariffs/3
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

---

### GET /v1/inscriptions/{id}

```
GET {{base_url}}/v1/inscriptions/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

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

---

### GET /v1/events/{eventId}/inscriptions — Inscritos del evento

```
GET {{base_url}}/v1/events/{eventId}/inscriptions?page=1&limit=20&categoryId={categoryId}&status=Pendiente
```

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

## 10. Endpoints pendientes de implementación

Los siguientes endpoints siguen definidos en el contrato OpenAPI pero aún no forman parte de los lotes implementados.

### Rankings

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/v1/rankings` | Ranking (`?categoryId` REQUERIDO, `?year&page&limit`) |
| `GET` | `/v1/rankings/categories` | Categorías con ranking disponible |

---

### Artículos (noticias)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/v1/articles` | Listar artículos (`?page&limit&category&featured&search`) |
| `POST` | `/v1/articles` | Crear artículo |
| `GET` | `/v1/articles/{slug}` | Obtener por slug |
| `PUT` | `/v1/articles/{slug}` | Actualizar artículo |
| `DELETE` | `/v1/articles/{slug}` | Eliminar artículo |

Categorías: `Resultados` · `Circuito` · `Entrevista` · `Reglamento` · `Tecnología`

---

### Admin

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/v1/admin/users` | Listar usuarios admin (`?role&status`) |
| `POST` | `/v1/admin/users` | Crear usuario admin |
| `GET` | `/v1/admin/users/{id}` | Obtener usuario admin |
| `PUT` | `/v1/admin/users/{id}` | Actualizar usuario admin |
| `DELETE` | `/v1/admin/users/{id}` | Eliminar usuario admin |
| `GET` | `/v1/admin/roles` | Listar roles disponibles |
| `GET` | `/v1/admin/dashboard` | Dashboard con métricas |

Roles: `Super Admin` · `Admin` · `Árbitro` · `Revisor`

---

### SurfScores Sync

```
POST {{base_url}}/v1/surfscores/sync/{circuitId}
```

Dispara la sincronización manual de rankings desde la API externa de SurfScores para el circuito indicado.

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
