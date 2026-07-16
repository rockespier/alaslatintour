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
- `auspiciador` **OK**
- tipo de evento: `Regular`, `Prime`, `SuperPrime` **OK**
- regla de bono de puntos: **falta validar en el ranking**
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
- soportar hasta 7 estrellas, **OK**
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

**Estado frontend 2026-07-16:** implementado. Cambios: `star-rating.component.ts` (max 5→7), `calendario.component.ts` (loop de estrellas 5→7), `configuracion.component.ts` (tablas de puntos por posición y distribución de premios extendidas con columnas `s6`/`s7` y `p6`/`p7`, mapeadas a `star6`/`star7` y `star6Percent`/`star7Percent`). El formulario de eventos (`admin-eventos.component.ts`) ya soportaba 1-7 estrellas.

### Issue 3
Eliminar `tarifa COP` de eventos/categorías habilitadas y contratos asociados. **OK**

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
Agregar en categorías: **OK**
- `membresiaAnualUsd`
- `membresiaPorEventoUsd`

**Razón:** completa la estructura económica de categorías antes de configuración avanzada.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- agregado de `MembresiaAnualUsd` y `MembresiaPorEventoUsd` al agregado `Category`,
- validación de no negativos en dominio y capa de aplicación,
- contratos OpenAPI y NSwag regenerados para `CategoryRequest` y `CategoryResponse`,
- persistencia EF con columnas SQL Server `decimal(18,2)` en `Categories`,
- migración `AddCategoryMembershipFees`,
- tests de integración del slice de categorías en verde.

**Estado frontend 2026-07-16:** implementado. `categorias.component.ts` agrega los campos `membresiaAnualUsd` y `membresiaPorEventoUsd` al formulario (crear/editar) y al payload de `POST/PUT /categories`.

### Issue 14
Hacer configurable la cuota administrativa: **OK**
- valor en configuración,
- si `0`, no mostrarla en resumen de inscripción.

**Dependencia:** issue 3 recomendado antes para evitar tocar dos veces la lógica de montos.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- `general.administrativeFeeUsd` agregado a la configuración administrativa,
- validación para impedir cuotas negativas,
- desglose persistido en `Inscriptions` con `BaseAmountUsd` y `AdministrativeFeeUsd`,
- respuesta de inscripción enriquecida con `baseAmountUsd` y `administrativeFeeUsd`,
- ocultamiento del campo `administrativeFeeUsd` cuando la cuota configurada es `0`,
- migración `AddInscriptionAdministrativeFee`,
- tests de integración de settings e inscripciones en verde.

**Estado frontend 2026-07-16:** implementado.
- `configuracion.component.ts` (tab General): campo `administrativeFeeUsd` agregado al formulario y al payload de `PUT /admin/settings`.
- `inscripcion.component.ts` (resumen previo al pago, paso 3): se eliminó el valor fijo de `$5` que se inventaba en el cliente para "Cuota administrativa" (el competidor no tiene acceso a `/admin/settings` para conocerlo de antemano); el resumen ahora solo muestra la tarifa de categoría con una nota de que la cuota, si aplica, se reflejará en la confirmación.
- `pago-playa.component.ts`: como esta pantalla ya consulta `GET /inscriptions/{id}` (que sí devuelve `baseAmountUsd`/`administrativeFeeUsd`/`montoUsd` reales), se agregó el desglose ahí, ocultando la línea de cuota administrativa cuando el campo no viene en la respuesta (cuota configurada en `0`).

### Issue 7
Configurar ranking:
- cantidad de mejores resultados por categoría,
- solo para circuito actual.

**Dependencia:** issues 2 y 4.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- `Categories.BestResultsCount` agregado al modelo y persistencia SQL Server,
- contratos de categoría actualizados para `GET/POST/PUT /v1/categories`,
- caché de SurfScores recalculada usando el valor por categoría,
- lectura pública de `/v1/rankings` y `/v1/rankings/categories` limitada al circuito actual de la temporada vigente,
- migración `AddCategoryBestResultsCount`,
- tests de categorías y rankings en verde.

**Estado frontend 2026-07-16:** implementado. `categorias.component.ts` agrega el campo `bestResultsCount` (1-10, default 5) al formulario y al payload de `POST/PUT /categories`. `ranking.component.ts`/`ranking.service.ts` no requirieron cambios: nunca expusieron selector de circuito, así que la restricción de backend a "circuito actual de la temporada vigente" es transparente para la UI pública.

## Fase 2. Seguridad, acceso y notificaciones

### Issue 11
Limitar login a máximo 3 intentos fallidos.

**Incluye:**
- contador por usuario,
- bloqueo temporal o estado de bloqueo,
- reset al login correcto,
- tests de autenticación.

**Razón:** es una regla de seguridad transversal y conviene resolverla antes de ampliar flujos de usuarios.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- contador de intentos fallidos por usuario,
- bloqueo temporal por 15 minutos después de 3 intentos inválidos,
- reset automático del contador al login exitoso,
- nuevas columnas SQL Server en `UserAccounts` para `FailedLoginAttempts` y `LockedUntilUtc`,
- migración `AddUserLoginLockout`,
- tests de autenticación en verde.

**Estado frontend 2026-07-16:** implementado.
- `login.component.ts` ya mostraba `err.body?.message` con fallback genérico, por lo que el mensaje de bloqueo (`401` con texto distinto al de credenciales inválidas) se muestra sin cambios adicionales, ya que backend usa el mismo status code `401` para ambos casos y solo cambia el texto.
- Corregí un bug en `api.service.ts`: cualquier `401` disparaba `AuthService.logout()` (que navega a `/login` y limpia sesión), incluyendo el del propio `POST /auth/login`. Con el nuevo bloqueo de 3 intentos esto se volvía más frecuente y podía pisar `returnUrl` en la URL de login. Se excluyó `/auth/*` de ese comportamiento.

### Issue 12
Permisos por rol en panel admin:
- ocultar opciones sin acceso,
- si el rol tiene solo lectura, permitir ver pero no editar.

**Razón:** ordena el comportamiento del sistema antes de agregar más módulos administrativos.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- políticas por módulo registradas en `Program.cs` (`Circuitos`, `Eventos`, `Categorias`, `Inscritos`, `Pagos`, `Tokens`),
- enforcement `401/403` agregado a endpoints administrativos y de backoffice sensibles,
- `Revisor` con lectura en dashboard, usuarios, circuitos, eventos, categorías, inscripciones, pagos y tokens,
- `Arbitro` con acceso operativo a eventos e inscripciones y sin acceso a pagos/usuarios,
- suites de integración adaptadas para ejecutar endpoints administrativos con autenticación `SuperAdmin`,
- tests de permisos del panel admin en verde.

**Estado frontend 2026-07-16:** implementado.
- Nuevo `core/services/permissions.service.ts`: matriz de permisos por rol/módulo hardcodeada en el cliente (igual a la "Matriz funcional para frontend admin" de `api-postman.md`), con `canView(module)`/`canEdit(module)` derivados de `user.adminRole`. Se hardcodeó en vez de consumir `GET /v1/admin/roles` porque ese endpoint exige `ConfigurationRead`, permiso que `Árbitro` y `Revisor` no tienen — pedirlo rompería la carga de permisos para esos roles.
- Nuevo `core/guards/module.guard.ts` + `data: { module }` en cada ruta hija de `/admin` (`app.routes.ts`): si el rol no tiene ningún acceso al módulo, redirige a `/admin`.
- `admin-sidebar.component.ts`: los ítems del menú ahora se filtran con `canView(module)` — el rol ya no ve enlaces a secciones sin acceso.
- En `circuitos`, `categorias`, `admin-eventos`, `inscritos`, `pagos`, `admin-tokens`, `usuarios` y `configuracion` (los 8 módulos con acciones de escritura): se agregó `canEdit` y se ocultaron/deshabilitaron botones de crear/editar/eliminar/aprobar/rechazar/validar/guardar/toggles cuando el rol solo tiene lectura. El backend sigue siendo la fuente de verdad (401/403); esto es solo UX.
- No se tocó el modo de solo-lectura del formulario de `configuracion` campo por campo (los inputs no se deshabilitan individualmente); alcanzó con ocultar/deshabilitar los botones de "Guardar" y toggles, ya que sin esos nada persiste.

### Issue 1
Agregar notificación al administrador para revisar tokens de pago en playa.

**Canales sugeridos:**
- notificación interna/dashboard,
- correo opcional si ya existe infraestructura.

**Dependencia:** issue 12 recomendado para asegurar visibilidad correcta según rol.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- `GET /v1/admin/dashboard` ahora incluye `alerts[]` cuando existen tokens pendientes,
- alerta interna categorizada para módulo `tokens` con nivel `warning`,
- al crear una solicitud en `POST /v1/payments/beach/request` se intenta notificar por correo a usuarios admin con permiso de lectura sobre `Tokens`,
- el correo quedó en modo best-effort: si SMTP no está configurado, la solicitud no falla,
- tests de dashboard/notificaciones en verde.

**Estado frontend 2026-07-16:** implementado. `dashboard.component.ts` renderiza genéricamente `alerts[]` (no solo el caso de tokens) como banners clicables sobre las stat cards, con estilo según `level` (`warning`/`error`/`info`) y navegación a `/admin/{module}` al hacer click. El KPI "Tokens pendientes" que ya existía en el dashboard queda como complemento, no se reemplazó.

## Fase 3. Usuarios y competidores

### Issue 8
Completar CRUD de competidores desde administración.

**Alcance esperado:**
- listar,
- ver detalle,
- crear,
- editar,
- eliminar o desactivar según reglas.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- el CRUD de `Competitors` quedó formalizado como backend de administración,
- `GET /v1/competitors` y `GET /v1/competitors/{id}` requieren `Usuarios: Read`,
- `POST/PUT/DELETE /v1/competitors` y `PUT /v1/competitors/{id}/license` requieren `Usuarios: Full`,
- tests de integración de competidores actualizados al nuevo enforcement y en verde.

**Estado frontend 2026-07-16:** implementado. No existía ninguna pantalla admin para competidores (solo para usuarios staff). Se creó `features/admin/competidores/competidores.component.ts` desde cero: listado paginado server-side con filtros (búsqueda, país, estado de licencia), modal crear/editar (mismo body que `POST/PUT /competitors`), modal de licencia (`PUT /competitors/{id}/license` con estado, número, vencimiento y categorías habilitadas vía checkboxes desde `GET /categories`), y eliminar con confirmación. Nueva ruta `/admin/competidores` y entrada en el sidebar, ambas bajo el módulo de permisos `Usuarios` (igual que en la matriz de backend: lecturas requieren `Usuarios: Read`, escrituras `Usuarios: Full`). Acciones de escritura ocultas si el rol no tiene `Usuarios: Full`.

**Nota:** no se pudo verificar en navegador contra un backend real (bloqueado en este entorno por la conexión a SQL Server, ver nota del Issue 2); la respuesta exacta de `GET /competitors` tampoco tiene un ejemplo de JSON en `api-postman.md` — se infirió el shape a partir del body de `POST/PUT /competitors` y de `PUT /competitors/{id}/license`, que sí están documentados. Validar contra el backend real antes de dar por cerrado.

### Issue 9
Permitir acceso al perfil personal del usuario:
- admin puede entrar a su perfil y cambiar contraseña,
- desde la ficha de usuario/competidor/espectador debe poder cambiarse la contraseña de forma controlada.

**Dependencia:** issue 8 recomendado.

**Estado 2026-07-16:** implementado parcialmente en backend.

**Alcance implementado en este slice:**
- cambio de contraseña del admin autenticado o de otro admin vía `POST /v1/admin/users/{userId}/password`,
- soporte de alias `me` para perfil/personal del admin,
- cambio de contraseña del competidor vinculado vía `POST /v1/competitors/{competitorId}/password`,
- invalidación automática de sesiones activas por incremento de `TokenVersion`,
- tests de login con la nueva contraseña en verde.

**Pendiente funcional conocido:**
- no se agregó aún una ficha administrativa genérica para cuentas espectador sin `CompetitorId`.

**Estado frontend 2026-07-16:** implementado, ahora que `api-postman.md` documenta `POST /v1/competitors/{id}/password` con ejemplo completo (`{ "newPassword": "..." }` → `{ "success": true }`).
- `usuarios.component.ts`: botón "Contraseña" por fila (solo si `canEdit`) + modal, llama `POST /admin/users/{userId}/password`.
- `competidores.component.ts`: botón "Contraseña" por fila (solo si `canEdit`) + modal, llama `POST /competitors/{id}/password`.
- Nueva página `features/admin/perfil/perfil.component.ts` en `/admin/perfil` (sin `moduleGuard`, accesible a cualquier admin autenticado): cambio de la propia contraseña vía `POST /admin/users/me/password`. Enlazada desde el footer del sidebar (antes solo mostraba nombre/rol como texto).
- **Nota:** el body/response de `POST /admin/users/{userId}/password` y `.../me/password` no tiene ejemplo propio en `api-postman.md` (solo aparece en la tabla de entrega y en las notas de la Issue 9); se infirió `{ newPassword }` → `{ success: true }` por ser exactamente el mismo patrón que el endpoint hermano de competidores, que sí está documentado con ejemplo. Validar contra backend real.
- Queda pendiente (fuera de alcance, señalado por el propio backend): ficha de cambio de contraseña para cuentas espectador sin `CompetitorId`.

### Issue 13
Filtrar categorías disponibles en inscripción según sexo del competidor.

**Estado:** probablemente ya implementado en frontend; validar backend y contratos para no depender solo del cliente.

**Acción:** auditar primero y cerrar solo si backend también impone la regla.

**Estado 2026-07-16:** implementado en backend.

**Checklist técnico del cierre:**
- la validación de inscripción por género ya existía en `CreateInscriptionCommandHandler`,
- `GET /v1/events/{eventId}/categories?competitorId={guid}` ahora filtra categorías incompatibles antes de mostrar opciones en frontend,
- tests de filtrado por género en verde.

**Estado frontend 2026-07-16:** implementado. `inscripcion.component.ts` ahora envía `?competitorId={id}` en `GET /events/{eventId}/categories` para que el filtrado por género sea el que impone el backend; se mantiene el filtro client-side (`isCategoryGenderCompatible`) como red de seguridad redundante, no como única fuente de verdad.

## Fase 4. Penalidades y operaciones

### Issue 10
Registrar multas monetarias a competidores por infracciones o penalidades.

**Decisiones de diseño previas:**
- entidad propia `CompetitorFine` o equivalente,
- impacto contable,
- si bloquea inscripción o solo informa deuda,
- trazabilidad del administrador que la registró.

**Estado 2026-07-16:** implementado en backend.

**Alcance implementado:**
- nueva entidad `CompetitorFine`,
- estados `Pendiente`, `Pagada`, `Anulada`,
- trazabilidad mínima con `CreatedByUserId`,
- endpoints:
  - `GET /v1/competitors/{competitorId}/fines`
  - `POST /v1/competitors/{competitorId}/fines`
  - `PUT /v1/competitors/{competitorId}/fines/{fineId}`
- permisos sobre módulo `Pagos` (`Read` para consulta, `Full` para alta/edición),
- migración `AddCompetitorFinesAndManagedPasswords`,
- tests de integración en verde.

**Nota de alcance:**
- por ahora la multa es informativa/operativa; no bloquea automáticamente nuevas inscripciones.

**Estado frontend 2026-07-16:** implementado. Se agregó un tab "Multas" en `pagos.component.ts` (el permiso de fines es sobre el módulo `Pagos`, no `Usuarios`): buscador de competidor (`GET /competitors?search=`), listado de multas del competidor seleccionado (`GET /competitors/{id}/fines`), alta de multa (`POST .../fines`) y marcar pagada/anular (`PUT .../fines/{fineId}`).

**Nota:** a diferencia de los demás endpoints nuevos, `api-postman.md` sigue sin publicar un ejemplo de request/response para los 3 endpoints de `fines` (solo aparecen en la tabla de entrega y en la matriz de permisos). Se infirieron los nombres de campo (`motivo`, `montoUsd`, `estado` con valores `Pendiente`/`Pagada`/`Anulada`) a partir de las convenciones de nomenclatura usadas en el resto de la API (español, `montoUsd` en pagos/inscripciones, `estado`/`estadoAdmin` en inscripciones y pagos) y de los estados que el propio backend documentó en su checklist de cierre. Esto es una suposición razonable pero no confirmada por ejemplo real — validar contra el backend antes de dar por cerrado.

## Fase 5. Importación/exportación masiva

### Issue 5
Descarga de plantilla Excel e importación masiva para:
- eventos,
- circuitos,
- categorías.

**Razón:** conviene hacerlo después de estabilizar el modelo de datos de eventos y categorías.

**Estado 2026-07-16:** implementado en backend usando `XLSX`.

**Checklist técnico del cierre:**
- nuevas rutas administrativas `GET /template` y `POST /import` para `circuits`, `events` y `categories`,
- servicio de infraestructura con `ClosedXML` para generar y leer archivos `.xlsx`,
- importación masiva con upsert por `Id` o `SurfScoresCode`,
- en `events`, referencia al circuito por `CircuitId` o `CircuitSurfScoresCode`,
- en `categories`, soporte de sucesora por `SuccessorCategoryId` o `SuccessorSurfScoresCode`,
- tests de integración de plantillas e importación en verde.

**Estado frontend 2026-07-16:** implementado.
- Nuevo método `ApiService.downloadFile(path, filename)` (usa `responseType: 'blob'` + descarga vía anchor temporal) — no existía forma de bajar binarios antes.
- Nuevo componente compartido `shared/components/import-excel-modal/import-excel-modal.component.ts`: sube el `.xlsx`, muestra el resumen (`processedRows`/`createdCount`/`updatedCount`/`errors[]`), reutilizado en los 3 lugares en vez de triplicar el modal.
- `circuitos.component.ts`, `categorias.component.ts` y `admin-eventos.component.ts`: botones "Descargar plantilla" e "Importar Excel" (visibles solo si `canEdit`), que recargan el listado al terminar. En eventos, el nuevo "Importar Excel" queda como flujo separado del ya existente "Importar de SurfScores" (son fuentes de datos distintas).

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
