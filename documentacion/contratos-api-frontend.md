# Contratos API — Frontend ALAS Latin Tour PWA
**Generado:** 2026-07-09  
**Consumidor:** Angular 19 SSR (`/frontend`)  
**Base URL:** `{apiUrl}/v1` — dev: `http://localhost:5132/v1`

> **Convención de respuestas exitosas**
> ```json
> { "data": <payload>, "pagination": { "totalPages": 3, "totalItems": 30, "page": 1 } }
> ```
> `pagination` solo en listados paginados.
>
> **Convención de errores**
> ```json
> { "message": "Descripción del error" }
> ```
> Con el HTTP status correspondiente (400, 401, 403, 404, 409…).

---

## 1. Auth

### POST `/v1/auth/login`
```json
// Request
{ "email": "string", "password": "string", "rememberMe": false }

// Response 200
{
  "accessToken": "string (JWT)",
  "user": {
    "id": "string",
    "email": "string",
    "fullName": "string",
    "tipo": "espectador | competidor",
    "adminRole": false
  }
}
```

### POST `/v1/auth/register`
```json
// Request
{
  "tipo": "espectador | competidor",
  "nombre": "string",
  "apellido": "string",
  "email": "string",
  "password": "string",
  "pais": "string",
  "telefono": "string (opcional)",
  "fechaNacimiento": "ISO date (opcional, competidor)",
  "genero": "string (opcional, competidor)",
  "postura": "Regular | Goofy (opcional, competidor)",
  "tallaCamiseta": "XS | S | M | L | XL | XXL (opcional)",
  "club": "string (opcional)"
}

// Response 201
{ "message": "Registro exitoso" }
```

### POST `/v1/auth/forgot-password`
```json
// Request
{ "email": "string" }

// Response 200 (siempre, no revelar si el correo existe)
{ "message": "Si el correo está registrado recibirás un enlace." }
```

---

## 2. Events

### GET `/v1/events`
| Param | Tipo | Notas |
|-------|------|-------|
| `limit` | number | default 10 |
| `page` | number | default 1 |
| `includeCategories` | boolean | si `true`, incluye array `categorias` en cada evento |
| `status` | string | filtra por `statusPublic` |
| `circuitId` | string | filtra por circuito |

```json
// Response 200
{
  "data": [
    {
      "id": "string",
      "nombre": "string",
      "ciudad": "string",
      "pais": "string (código ISO-2: PE, BR, CL…)",
      "stars": 1,
      "statusPublic": "Inscripciones Abiertas | Próximamente | Completado | Cerrado",
      "fechaInicio": "ISO date",
      "fechaFin": "ISO date",
      "circuito": "string (nombre del circuito)",
      "circuitoId": "string",
      "capacidadTotal": 80,
      "inscritosTotal": 47,
      "premioUSD": 5000,
      "inscripcionCierre": "ISO date (opcional)",
      "isInvitational": false,
      "waveSize": "string (opcional, ej: '4-6 ft')",
      "ganador": "string (opcional, si Completado)",
      "categorias": [
        {
          "id": "string",
          "nombre": "string",
          "tipo": "Principal | Junior | Masters",
          "tarifa": 75,
          "inscritos": 12,
          "capacidad": 20,
          "descripcion": "string (opcional)"
        }
      ]
    }
  ],
  "pagination": { "totalPages": 5, "totalItems": 48, "page": 1 }
}
```

### GET `/v1/events/{id}`
```json
// Response 200
{ "data": { /* mismo shape que EventItem */ } }
```

### GET `/v1/event-categories`
| Param | Tipo |
|-------|------|
| `eventId` | string (requerido) |
| `limit` | number |

```json
// Response 200
{
  "data": [
    {
      "id": "string",
      "nombre": "string",
      "tipo": "Principal | Junior | Masters",
      "tarifa": 75,
      "inscritos": 12,
      "capacidad": 20,
      "descripcion": "string (opcional)"
    }
  ]
}
```

---

## 3. Circuits

### GET `/v1/circuits`
| Param | Tipo |
|-------|------|
| `status` | `Activo` |
| `year` | number |
| `limit` | number |

```json
// Response 200
{
  "data": [
    { "id": "string", "nombre": "string", "estado": "Activo" }
  ]
}
```

---

## 4. Articles (WordPress → BFF)

El BFF consume WordPress y re-expone los artículos en formato normalizado.

### GET `/v1/articles`
| Param | Tipo | Notas |
|-------|------|-------|
| `limit` | number | |
| `page` | number | |
| `featured` | boolean | filtra artículos destacados |
| `category` | string | filtra por categoría |
| `slug` | string | devuelve un artículo por slug |

```json
// Response 200
{
  "data": [
    {
      "id": "string",
      "slug": "string",
      "title": "string",
      "excerpt": "string",
      "content": "string (HTML, solo cuando se pide por slug)",
      "imageUrl": "string (URL absoluta)",
      "category": "Resultados | Circuito | Entrevista | Reglamento",
      "publishedAt": "ISO date",
      "readingTime": 4,
      "featured": false
    }
  ],
  "pagination": { "totalPages": 3, "totalItems": 18, "page": 1 }
}
```

---

## 5. Galleries (WordPress → BFF)

### GET `/v1/galleries`

> ⚠️ **Prioritario:** este endpoint estaba sin respuesta en producción, causando que la sección de galería en `/noticias` se quedara cargando indefinidamente. El frontend tiene un timeout de 6 s; si el endpoint no existe aún, debe devolver `{ "data": [] }` con 200.

```json
// Response 200
{
  "data": [
    {
      "id": "string",
      "slug": "string",
      "title": "string",
      "eventDate": "ISO date | null",
      "pressDownloadLink": "string (URL) | null",
      "photos": [
        {
          "id": "string",
          "url": "string (URL absoluta)",
          "width": 1920,
          "height": 1280
        }
      ]
    }
  ]
}
```

---

## 6. Rankings (SurfScores → BFF con caché)

> El BFF debe implementar caché de al menos 5 minutos. Nunca exponer credenciales de SurfScores al frontend.

### GET `/v1/rankings/categories`
```json
// Response 200
{
  "data": [
    {
      "id": "string (id interno del circuito/categoría)",
      "nombre": "Open Hombres",
      "availableYears": [2024, 2025, 2026]
    }
  ]
}
```

### GET `/v1/rankings`
| Param | Tipo |
|-------|------|
| `categoryId` | string (requerido) |
| `year` | number |
| `page` | number |
| `limit` | number |

```json
// Response 200
{
  "data": {
    "rows": [
      {
        "pos": 1,
        "name": "string",
        "country": "string (código ISO-2)",
        "points": 4850,
        "events": 8,
        "variation": 2
      }
    ],
    "categoryName": "Open Hombres",
    "totalItems": 120,
    "totalPages": 12,
    "currentPage": 1,
    "cachedAt": "ISO datetime"
  }
}
```

---

## 7. Competitors

### GET `/v1/competitors/{id}`
```json
// Response 200
{
  "data": {
    "id": "string",
    "nombre": "string",
    "apellido": "string",
    "email": "string",
    "telefono": "string",
    "pais": "string",
    "postura": "Regular | Goofy",
    "tallaCamiseta": "S | M | L | XL | XXL",
    "club": "string",
    "rankingActual": 14,
    "puntosActual": 3200,
    "rankingAnterior": 18
  }
}
```

### PUT `/v1/competitors/{id}`
```json
// Request (solo campos editables)
{
  "nombre": "string",
  "apellido": "string",
  "telefono": "string",
  "pais": "string",
  "postura": "Regular | Goofy",
  "tallaCamiseta": "string",
  "club": "string"
}

// Response 200
{ "data": { /* perfil actualizado */ } }
```

### GET `/v1/competitors/{id}/inscriptions`
| Param | Tipo |
|-------|------|
| `limit` | number |
| `status` | `activa | completada | cancelada` (opcional) |

```json
// Response 200
{
  "data": [
    {
      "id": "string (= inscriptionId)",
      "eventoNombre": "string",
      "eventoPais": "string (emoji de bandera o código ISO-2)",
      "eventoFechaInicio": "ISO date",
      "eventoFechaFin": "ISO date",
      "categoria": "string",
      "status": "activa | completada | cancelada",
      "statusPago": "confirmado | pendiente | rechazado",
      "monto": 80,
      "metodoPago": "paypal | beach"
    }
  ]
}
```

### GET `/v1/competitors/{id}/points-history`
| Param | Tipo |
|-------|------|
| `year` | number |
| `limit` | number |

```json
// Response 200
{
  "data": [
    {
      "eventoNombre": "string",
      "eventoPais": "string (código ISO-2)",
      "eventStars": 3,
      "categoria": "string",
      "posicion": 4,
      "puntos": 850,
      "fecha": "ISO date"
    }
  ]
}
```

### GET `/v1/competitors/{id}/calendar`
```json
// Response 200
{
  "data": [
    {
      "id": "string (eventoId)",
      "inscriptionId": "string",
      "eventoNombre": "string",
      "eventoPais": "string (código ISO-2)",
      "ciudad": "string",
      "stars": 4,
      "fechaInicio": "ISO date",
      "fechaFin": "ISO date",
      "categoria": "string",
      "statusPago": "confirmado | pendiente"
    }
  ]
}
```

### GET `/v1/competitors/{id}/calendar/export`
```
// Response 200
Content-Type: text/calendar
Content-Disposition: attachment; filename="alas-calendario.ics"

BEGIN:VCALENDAR
...
END:VCALENDAR
```

---

## 8. Inscriptions

### POST `/v1/inscriptions`
```json
// Request
{
  "eventId": "string",
  "categoryId": "string",
  "paymentMethod": "paypal | beach",
  "shirtNumber": 23
}

// Response 201
{ "data": { "id": "string" } }
// El campo "id" es el inscriptionId usado en los siguientes pasos de pago.
```

### GET `/v1/inscriptions/my`
| Param | Tipo |
|-------|------|
| `limit` | number |
| `status` | `activa` (opcional) |

```json
// Response 200 — mismo shape que /competitors/{id}/inscriptions
```

### GET `/v1/inscriptions/{id}`
```json
// Response 200
{
  "data": {
    "eventoNombre": "string",
    "eventoPais": "string (emoji o ISO-2)",
    "categoriaNombre": "string"
  }
}
```

### DELETE `/v1/inscriptions/{id}`
El competidor autenticado puede eliminar solo sus propias inscripciones incompletas.

Condiciones:
- `estadoAdmin = Pendiente`
- `estadoCompetidor = Pendiente`
- sin pago registrado
- sin `transaccionId`
- sin resultado final

```json
// Response 204
// Sin body

// Response 409
{ "message": "Solo se pueden eliminar inscripciones incompletas y pendientes." }
```

---

## 9. PayPal

### POST `/v1/paypal/orders`
Inicia el checkout PayPal para una inscripción ya creada con `paymentMethod = "paypal"`.

```json
// Request
{
  "inscriptionId": "string",
  "returnUrl": "https://app.alas.com/paypal/retorno?inscriptionId=...",
  "cancelUrl": "https://app.alas.com/paypal/cancelado?inscriptionId=..."
}

// Response 200
{
  "orderId": "5O190127TN364715T",
  "approvalUrl": "https://www.sandbox.paypal.com/checkoutnow?token=5O190127TN364715T",
  "amountUsd": 95.0
}
```

### POST `/v1/paypal/orders/{orderId}/capture`
Se invoca cuando PayPal redirige al frontend a `returnUrl` con el query param `token={orderId}`.

```json
// Request
{ "inscriptionId": "string" }

// Response 200
{
  "data": {
    "id": "string",
    "inscriptionId": "string",
    "method": "paypal",
    "amountUsd": 95.0,
    "transactionId": "string",
    "status": "confirmado"
  }
}
```

## 10. Payments

### POST `/v1/payments/beach/request`
Solicita al administrador que genere un token de pago en playa.

```json
// Request
{ "inscriptionId": "string" }

// Response 200
{ "message": "Solicitud enviada al administrador." }
```

### GET `/v1/payments/beach/status/{inscriptionId}`
El frontend hace polling cada 15 s para detectar la aprobación del admin.

```json
// Response 200
{
  "data": {
    "status": "pending | token_sent | confirmed",
    "secondsRemaining": 82400
  }
}
// "secondsRemaining" solo presente cuando status = "token_sent"
```

### POST `/v1/payments/beach/redeem`
El competidor ingresa el token recibido por correo.

```json
// Request
{ "inscriptionId": "string", "token": "ABC123" }

// Response 200 — token válido
{ "message": "Inscripción confirmada." }

// Response 400 — token inválido o expirado
{ "message": "El token es inválido o ha expirado." }
```

---

## 11. Contact

### POST `/v1/contact`
```json
// Request
{
  "nombre": "string",
  "email": "string",
  "asunto": "string",
  "mensaje": "string"
}

// Response 200
{ "message": "Mensaje recibido." }
```

---

## Resumen de prioridades

| Prioridad | Endpoints | Bloquea |
|-----------|-----------|---------|
| 🔴 Alta | Auth login/register, Events, Inscriptions, Payments | Flujo principal competidor |
| 🟠 Media | Competitors (perfil, historial, calendario), Circuits | Panel del competidor |
| 🟡 Normal | Articles, Rankings | Secciones públicas |
| 🟢 Baja | Galleries, Contact, forgot-password | Secciones secundarias |

> **Nota sobre Galleries:** aunque es baja prioridad, el endpoint **debe responder** (aunque sea `{ "data": [] }`) para no bloquear el renderizado de `/noticias`. El frontend tiene timeout de 6 s pero es mejor que el endpoint exista.
