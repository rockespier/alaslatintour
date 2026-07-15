# Plan de Resolución de Issues

Base: [Issues.md](C:\repo\rtres-net\Alas\documentacion\Issues.md)

## Objetivo

Resolver los issues uno por uno, priorizando:

1. Reglas de negocio que afectan datos y contrato API.
2. Cambios transversales de seguridad y permisos.
3. Funcionalidades administrativas de alto impacto.
4. Mejoras masivas de carga/importación.

## Criterios de ejecución

- No mezclar en un mismo slice cambios de dominio no relacionados.
- Cerrar cada issue con:
  - cambios en `Domain`, `Application`, `Infrastructure` y `Api` si aplica,
  - tests backend,
  - documentación en `api-postman.md` o documento funcional correspondiente,
  - validación manual mínima en frontend si el flujo lo requiere.
- Cuando un issue cambie estructura de datos, hacerlo antes que UI dependiente.

## Orden recomendado

## Fase 1. Reglas base de eventos, tarifas y ranking

### Issue 2
Agregar en eventos:
- `auspiciador`
- tipo de evento: `Regular`, `Prime`, `SuperPrime`
- regla de bono de puntos:
  - `Prime`: +10%
  - `SuperPrime`: +50%

**Razón:** afecta modelo de eventos y luego impacta ranking, categorías habilitadas y frontend admin.

**Estado 2026-07-15:** implementado en backend.

**Checklist técnico del cierre:**
- migración EF para `Events`,
- contratos `POST/PUT/GET /v1/events`,
- formulario admin de eventos,
- aplicación del bono en ranking cacheado,
- tests de CRUD de eventos.

**Nota de validación:** la compilación quedó en verde y la migración fue generada. La prueba de integración HTTP quedó bloqueada en este entorno por la conexión local a SQL Server con cifrado requerido.

### Issue 4
Actualizar eventos/categorías habilitadas:
- soportar hasta 7 estrellas,
- considerar eventos `Prime` y `SuperPrime` como parte del modelo competitivo.

**Dependencia:** issue 2.

**Estado 2026-07-15:** implementado en backend.

**Checklist técnico del cierre:**
- validaciones de `Event`, `EventCategory` y `CategoryTariff` ampliadas a `1..7`,
- contratos OpenAPI y NSwag regenerados para `stars` y `starLevel`,
- respuesta de `GET/PUT /v1/events/{eventId}/categories` enriquecida con `gender` y `stars`,
- configuración de ranking y distribución de premios extendida a estrellas 6 y 7,
- formulario admin de eventos actualizado para permitir 6 y 7 estrellas,
- tests y compilación backend en verde.

**Nota:** no requirió migración EF porque el cambio fue de reglas, contratos y configuración serializada; no cambió el esquema físico.

### Issue 3
Eliminar `tarifa COP` de eventos/categorías habilitadas y contratos asociados.

**Razón:** simplifica modelo antes de seguir agregando tarifas y carga masiva.

**Estado 2026-07-15:** implementado en backend.

**Checklist técnico del cierre:**
- eliminación de `CustomTariffCop` del agregado `EventCategory`,
- contratos OpenAPI y NSwag regenerados sin `customTariffCop` ni `effectiveTariffCop`,
- `GET/PUT /v1/events/{eventId}/categories` quedan solo con tarifa USD,
- configuración EF y migración para remover la columna `CustomTariffCop` de `EventCategories`,
- tests de integración ajustados al nuevo contrato.

**Nota de alcance:** el retiro de COP se aplicó a tarifas configuradas por evento. La matriz general de `CategoryTariffs` permanece con `usd` y `cop` hasta una decisión funcional distinta.

### Issue 6
Agregar en categorías:
- `membresiaAnualUsd`
- `membresiaPorEventoUsd`

**Razón:** completa la estructura económica de categorías antes de configuración avanzada.

### Issue 14
Hacer configurable la cuota administrativa:
- valor en configuración,
- si `0`, no mostrarla en resumen de inscripción.

**Dependencia:** issue 3 recomendado antes para evitar tocar dos veces la lógica de montos.

### Issue 7
Configurar ranking:
- cantidad de mejores resultados por categoría,
- solo para circuito actual.

**Dependencia:** issues 2 y 4.

## Fase 2. Seguridad, acceso y notificaciones

### Issue 11
Limitar login a máximo 3 intentos fallidos.

**Incluye:**
- contador por usuario,
- bloqueo temporal o estado de bloqueo,
- reset al login correcto,
- tests de autenticación.

**Razón:** es una regla de seguridad transversal y conviene resolverla antes de ampliar flujos de usuarios.

### Issue 12
Permisos por rol en panel admin:
- ocultar opciones sin acceso,
- si el rol tiene solo lectura, permitir ver pero no editar.

**Razón:** ordena el comportamiento del sistema antes de agregar más módulos administrativos.

### Issue 1
Agregar notificación al administrador para revisar tokens de pago en playa.

**Canales sugeridos:**
- notificación interna/dashboard,
- correo opcional si ya existe infraestructura.

**Dependencia:** issue 12 recomendado para asegurar visibilidad correcta según rol.

## Fase 3. Usuarios y competidores

### Issue 8
Completar CRUD de competidores desde administración.

**Alcance esperado:**
- listar,
- ver detalle,
- crear,
- editar,
- eliminar o desactivar según reglas.

### Issue 9
Permitir acceso al perfil personal del usuario:
- admin puede entrar a su perfil y cambiar contraseña,
- desde la ficha de usuario/competidor/espectador debe poder cambiarse la contraseña de forma controlada.

**Dependencia:** issue 8 recomendado.

### Issue 13
Filtrar categorías disponibles en inscripción según sexo del competidor.

**Estado:** probablemente ya implementado en frontend; validar backend y contratos para no depender solo del cliente.

**Acción:** auditar primero y cerrar solo si backend también impone la regla.

## Fase 4. Penalidades y operaciones

### Issue 10
Registrar multas monetarias a competidores por infracciones o penalidades.

**Decisiones de diseño previas:**
- entidad propia `CompetitorFine` o equivalente,
- impacto contable,
- si bloquea inscripción o solo informa deuda,
- trazabilidad del administrador que la registró.

## Fase 5. Importación/exportación masiva

### Issue 5
Descarga de plantilla Excel e importación masiva para:
- eventos,
- circuitos,
- categorías.

**Razón:** conviene hacerlo después de estabilizar el modelo de datos de eventos y categorías.

## Slices sugeridos

### Slice A
- Issue 2
- Issue 4
- Issue 3

### Slice B
- Issue 6
- Issue 14
- Issue 7

### Slice C
- Issue 11
- Issue 12
- Issue 1

### Slice D
- Issue 8
- Issue 9
- Issue 13

### Slice E
- Issue 10

### Slice F
- Issue 5

## Primer issue a ejecutar

Recomiendo comenzar por **Issue 2**, porque define la base del modelo de eventos y evita retrabajo posterior en:

- estrellas,
- ranking,
- tarifas,
- categorías habilitadas,
- frontend admin de eventos.

## Checklist de cierre por issue

- Migración EF si cambia persistencia.
- Ajustes de dominio y reglas.
- Casos de aplicación y repositorios.
- Endpoint/documentación.
- Tests automatizados.
- Validación manual del flujo afectado.
