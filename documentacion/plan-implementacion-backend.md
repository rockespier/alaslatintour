# Plan de Implementacion Backend ALAS

Ultima actualizacion: 2026-07-07 11:35:00 +02:00

## Objetivo

Implementar los endpoints del backend de ALAS en lotes de 5 a 10 endpoints por dia, usando:

- C#
- Clean Architecture por capas
- `AlasApp.Domain`
- `AlasApp.Application`
- `AlasApp.Infrastructure`
- `AlasApp.Api`
- Entity Framework Core con enfoque code-first

## Lineamientos tecnicos

- El contrato externo de la API parte de `specs/openapi.yaml`.
- La implementacion real no depende del controlador abstracto generado; los controladores concretos viven en `AlasApp.Api`.
- La capa `Domain` contiene entidades, enums y reglas de negocio.
- La capa `Application` usa CQRS simple por caso de uso.
- La capa `Infrastructure` implementa persistencia EF Core, repositorios, configuracion de base de datos y adapters externos.
- La capa `Api` expone endpoints `v1`, mapea request/response y centraliza el manejo de errores.
- Las integraciones externas reales se diferirán; primero se dejan adapters y contratos.

## Slice actual

### Lote 1: Circuits + Events

Endpoints objetivo:

1. `GET /v1/circuits`
2. `POST /v1/circuits`
3. `GET /v1/circuits/{circuitId}`
4. `PUT /v1/circuits/{circuitId}`
5. `DELETE /v1/circuits/{circuitId}`
6. `GET /v1/events`
7. `POST /v1/events`
8. `GET /v1/events/{eventId}`
9. `PUT /v1/events/{eventId}`
10. `DELETE /v1/events/{eventId}`

### Alcance funcional del lote 1

- Modelar `Circuit` y `Event`.
- Persistir ambos con EF Core code-first.
- Resolver listados con paginacion y filtros.
- Validar reglas basicas de negocio.
- Impedir eliminar un circuito con eventos asociados.
- Responder con contratos compatibles con la spec.

## Estado actual

### Completado

- Generacion inicial de contratos desde OpenAPI.
- Compilacion del proyecto con controladores generados y cliente TypeScript.
- Definicion de la base de `Domain` para `Circuit` y `Event`.
- Definicion de enums de dominio para circuitos y eventos.
- Creacion de excepciones de dominio.
- Base de `Application` con:
  - abstracciones de mensajeria
  - dispatcher simple
  - contratos de persistencia
  - `IUnitOfWork`
  - `IClock`
  - DTOs internos
  - queries y commands para `Circuits`
  - queries y commands para `Events`
- Implementacion de `Infrastructure` con EF Core code-first.
- Repositorios y configuraciones EF para `Circuits` y `Events`.
- Registro de dependencias en `Api`.
- Controladores concretos `CircuitsController` y `EventsController`.
- Middleware de errores y mapeo de contratos API.
- Inicializacion de base de datos en el arranque.
- Tests de integracion HTTP para CRUD de `Circuits` y `Events`.
- Regla de negocio para impedir eliminar un circuito con eventos asociados.
- Compatibilidad de filtros query-string con los valores definidos en OpenAPI.
- Modelado completo de `Categories`, `CategoryTariffs` y `EventCategories`.
- Implementacion de endpoints CRUD para `Categories`.
- Implementacion de endpoints `GET/PUT /v1/events/{eventId}/categories`.
- Persistencia EF Core para categorias, tarifas y categorias habilitadas por evento.
- Tests de integracion HTTP para `Categories` y `Event Categories`.
- Implementacion de endpoints `GET /v1/categories/{categoryId}/tariffs` y `PUT /v1/categories/{categoryId}/tariffs/{starLevel}`.
- Modelado completo de `Competitors`.
- Persistencia EF Core code-first para `Competitors` y categorias habilitadas de licencia.
- Implementacion de endpoints CRUD base para `Competitors`:
  - `GET /v1/competitors`
  - `POST /v1/competitors`
  - `GET /v1/competitors/{competitorId}`
  - `PUT /v1/competitors/{competitorId}`
  - `DELETE /v1/competitors/{competitorId}`
- Implementacion de endpoints restantes de `Competitors`:
  - `PUT /v1/competitors/{competitorId}/license`
  - `GET /v1/competitors/{competitorId}/notifications`
  - `PUT /v1/competitors/{competitorId}/notifications`
  - `GET /v1/competitors/{competitorId}/inscriptions`
  - `GET /v1/competitors/{competitorId}/points-history`
  - `GET /v1/competitors/{competitorId}/calendar`
  - `GET /v1/competitors/{competitorId}/calendar/export`
- Tests de integracion HTTP para `Category Tariffs` y CRUD base de `Competitors`.
- Tests de integracion HTTP para licencia, notificaciones, historial y calendario de `Competitors`.
- Modelado completo de `Inscriptions`.
- Persistencia EF Core code-first para `Inscriptions`.
- Implementacion de endpoints de `Inscriptions`:
  - `GET /v1/events/{eventId}/inscriptions`
  - `GET /v1/inscriptions`
  - `POST /v1/inscriptions`
  - `GET /v1/inscriptions/{inscriptionId}`
  - `PUT /v1/inscriptions/{inscriptionId}`
  - `DELETE /v1/inscriptions/{inscriptionId}`
- Integracion de `Inscriptions` con `Competitors` para:
  - `GET /v1/competitors/{competitorId}/inscriptions`
  - `GET /v1/competitors/{competitorId}/calendar`
  - `GET /v1/competitors/{competitorId}/calendar/export`
- Tests de integracion HTTP para `Inscriptions`.
- Modelado completo de `Payments`.
- Modelado completo de `Beach Tokens`.
- Persistencia EF Core code-first para `Payments` y `BeachTokens`.
- Implementacion de endpoints de `Payments`:
  - `GET /v1/payments`
  - `POST /v1/payments`
  - `GET /v1/payments/{paymentId}`
  - `PUT /v1/payments/{paymentId}`
  - `GET /v1/payments/kpis`
- Implementacion de endpoints de `Beach Tokens`:
  - `POST /v1/payments/beach/request`
  - `POST /v1/payments/beach/redeem`
  - `GET /v1/payments/beach/tokens`
  - `POST /v1/payments/beach/tokens/{tokenId}/approve`
  - `POST /v1/payments/beach/tokens/{tokenId}/reject`
- Integracion de `Beach Tokens` con `Inscriptions` y `Payments` para:
  - solicitud
  - aprobacion/rechazo
  - canje con pago pendiente
  - validacion posterior del pago en playa
- Migracion EF Core para `Payments` + `BeachTokens`.
- Tests de integracion HTTP para `Payments` y `Beach Tokens`.
- Modelado completo de `Rankings`.
- Persistencia EF Core code-first para `RankingSnapshots` y `RankingSnapshotEntries`.
- Implementacion de endpoints de `Rankings`:
  - `GET /v1/rankings`
  - `GET /v1/rankings/categories`
- Implementacion del adapter interno `SurfScores` para construir cache deterministico desde datos locales.
- Implementacion de endpoint de sincronizacion:
  - `POST /v1/surfscores/sync/{circuitId}`
- Tests de integracion HTTP para sincronizacion y consulta de rankings.
- Slice de autenticacion y sesiones base:
  - `POST /v1/auth/login`
  - `POST /v1/auth/register`
  - `POST /v1/auth/password-reset/request`
  - `POST /v1/auth/password-reset/confirm`
  - `POST /v1/auth/logout`
- Modelado code-first de `UserAccount` y `PasswordResetToken`.
- Persistencia SQL Server para autenticacion con migracion EF Core dedicada.
- Hash de contraseña con PBKDF2 y emision/validacion de JWT con invalidacion por version de token.
- Registro de competidores desde `auth/register` enlazado con `Competitors`.
- Tests de integracion HTTP para login, registro, logout y password reset.

### En curso

- Lote 7 y 8 pausados temporalmente mientras se cierra la base de autenticacion.

### Pendiente

- Continuar `Articles` y adapter `WordPress`.
- Continuar `Admin Users`, `Roles`, `Dashboard` y `Memberships`.
- Endurecer autorizacion por rol sobre endpoints administrativos una vez se implemente `Admin Users`.
- Lotes funcionales restantes a partir de `Rankings`.
- Integraciones externas reales (`SurfScores`, `WordPress`).

## Arquitectura objetivo por modulo

### Domain

- Entidades:
  - `Circuit`
  - `Event`
- Enums:
  - `CircuitStatus`
  - `CircuitRegion`
  - `CircuitModalidad`
  - `EventStatusAdmin`
  - `EventStatusPublic`
  - `EventAccessType`
- Reglas:
  - temporada valida
  - estrellas validas
  - fechas coherentes
  - strings obligatorios
  - no borrar circuito con eventos

### Application

- Mensajeria simple:
  - `IRequest<T>`
  - `IRequestHandler<TRequest, TResponse>`
  - `IRequestDispatcher`
- Persistencia:
  - `ICircuitRepository`
  - `IEventRepository`
  - `IUnitOfWork`
- Casos de uso:
  - create/get/list/update/delete de circuitos
  - create/get/list/update/delete de eventos

### Infrastructure

- `AlasAppDbContext`
- Configuraciones EF Core
- Repositorios concretos
- Unit of Work
- Inicializacion de base de datos

### Api

- Controladores versionados en `v1`
- Mapeo entre contratos generados y modelos internos
- Manejo consistente de errores:
  - `400`
  - `404`
  - `409`
  - `500`

## Orden de implementacion recomendado

### Dia 1

- `Circuits` + `Events` completado

### Dia 2

- `Categories` completado
- `Event Categories` completado

### Dia 3

- `Category Tariffs` completado
- `Competitors` completado

### Dia 4

- `Inscriptions` completado

### Dia 5

- `Payments` completado
- `Beach Tokens` completado

### Dia 6

- `Rankings` completado
- adapter `SurfScores` completado

### Dia 7

- `Auth` base completado antes de iniciar el lote
- `Articles`
- adapter `WordPress`

### Dia 8

- `Admin Users`
- `Roles`
- `Dashboard`
- `Memberships`

## Criterio de terminado por lote

- Compila la solucion completa.
- La base de datos se inicializa correctamente.
- Los endpoints del lote responden con el contrato esperado.
- Existen pruebas del flujo principal y de errores.
- El lote queda listo para continuar el siguiente bounded context sin rehacer base tecnica.

## Archivos clave involucrados

- `specs/openapi.yaml`
- `backend/src/AlasApp.Api/Program.cs`
- `backend/src/AlasApp.Api/Controllers/GeneratedControllers.cs`
- `backend/src/AlasApp.Domain`
- `backend/src/AlasApp.Application`
- `backend/src/AlasApp.Infrastructure`

## Notas

- Este documento debe mantenerse vivo durante la implementacion.
- Cada vez que se cierre un lote, se debe actualizar el estado y marcar el siguiente.
