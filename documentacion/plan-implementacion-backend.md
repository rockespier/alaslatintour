# Plan de Implementacion Backend ALAS

Ultima actualizacion: 2026-07-02 15:20:00 +02:00

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

### En curso

- Lote 3: `Category Tariffs` + `Competitors`.

### Pendiente

- Lotes funcionales restantes a partir de `Category Tariffs`.
- Migraciones EF Core formales para sustituir `EnsureCreated` cuando se estabilice el modelo.
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

- `Category Tariffs`
- `Competitors` siguiente lote recomendado

### Dia 3

- `Competitors`

### Dia 4

- `Inscriptions`

### Dia 5

- `Payments`
- `Beach Tokens`

### Dia 6

- `Rankings`
- adapter `SurfScores`

### Dia 7

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
