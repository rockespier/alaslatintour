# Plan: 14 fixes/features across /eventos, /admin, /inscripcion, /mi-panel, /ranking

## Contexto

El usuario reportó 14 problemas puntuales encontrados durante pruebas manuales de la plataforma ALAS Latin Tour, agrupados por pantalla. Se investigó cada uno con 3 agentes de exploración en paralelo (solo lectura). Los hallazgos cambian el diagnóstico de varios ítems:

- Algunos ya están implementados (inscritos confirmados en /eventos).
- Uno es un bug de autorización real y grave: `/mi-panel/datos` llama a un endpoint admin-only, por lo que **todo competidor** recibe 403 (y el widget de ranking en `/eventos` falla en silencio por el mismo motivo).
- Uno no reproduce con el código actual (ShirtNumber no es requerido en backend ni falta en el UI) — se documenta como no-bug y se refuerza con manejo de errores genérico por si el mensaje viene de otra fuente.
- Varios requieren decisión de arquitectura, ya resueltas con el usuario:
  - Multi-categoría en inscripción → **un solo pago combinado** (requiere backend nuevo).
  - Exclusión de ranking por membresía → usar **`Inscription.MembershipPlan`/`MembershipFeeUsd`** (ya ligado al competidor).
  - Tarifa de categoría → **exponer la tabla `CategoryTariff` existente** en el admin (sin cambios de backend).

Este plan cubre los 14 puntos, agrupados por dependencia técnica, con el componente compartido `StarRatingComponent` primero porque desbloquea 5 pantallas distintas.

---

## Orden de ejecución

1. **StarRatingComponent** (bloquea 3 items: /eventos #3, /mi-panel/calendario #1, revisión visual general)
2. **/eventos** #1 (botón PDF programación) y #2 (ya implementado, solo verificar)
3. **/mi-panel/datos** (bug de autorización — alto impacto, afecta también `/eventos`)
4. **/admin** sidebar colapsable
5. **/admin/dashboard** (2 fixes de texto/dato)
6. **/admin/categorias** (UI de tarifas por estrella)
7. **/admin/pagos** (exportar transacciones)
8. **/inscripcion** paso 2 (filtro género/edad + multi-categoría) y paso 3 (validación botón + manejo de errores)
9. **/ranking** (paginación, circuitos pasados, fórmula configurable, exclusión por membresía)

---

## 1. StarRatingComponent — máximo 6 estrellas, "Prime" solo texto	Estado: OK

**Por qué:** el negocio define 6 niveles de estrellas + "Prime" como bono del 10% sobre el nivel 6 (no un 7º nivel de estrellas físico) — confirmado en `AdminSettingsDefaults.BuildDefaultPointsMatrix()` (columnas Star1..Star6) y en el script `update-ranking-points-matrix-90.sql`. Hoy `StarRatingComponent` dibuja hasta `max()` estrellas (default 7), sin caso especial para Prime.

**Archivo:** `frontend/src/app/shared/components/star-rating/star-rating.component.ts`

**Cambio:**
- Si `value() === 7` → renderizar únicamente el texto "Prime" (sin íconos de estrella), con el mismo estilo de acento usado en `eventos.component.ts:106` (`font-accent uppercase tracking-wider text-orange-brand` o el que ya use el badge "Evento estrella" para consistencia).
- Si `value() <= 6` → renderizar como hoy pero con `max` fijo en 6 (no configurable desde fuera, ya que ningún caller pasa `max` hoy — confirmado por grep, los 5 usos solo pasan `[value]`).

**Consumidores que heredan el fix automáticamente (no tocar):**
`eventos.component.ts:228`, `home.component.ts:251`, `inscripcion.component.ts:93`, `historial-puntos.component.ts:84`, `mi-calendario.component.ts:84`.

**No tocar en este paso** (ya hacen `s === 7 ? 'Prime' : '★'.repeat(s)` correctamente, son implementaciones inline independientes, no usan el componente compartido): `admin-eventos.component.ts:217,807`, `dashboard.component.ts:175`, `inscritos.component.ts:771-772`. Quedan fuera de alcance salvo que el usuario pida unificarlos.

**Criterio de aceptación:**
- Evento con `stars: 6` → 6 estrellas doradas, ninguna gris de más.
- Evento con `stars: 7` → texto "Prime", cero íconos de estrella, en las 5 pantallas listadas arriba.
- Evento con `stars: 1..5` → igual que hoy pero sin la 7ª estrella gris de relleno.

---

## 2. /eventos — botón de PDF de programación (issue #1)	

**Por qué:** hoy `isLiveScheduleAvailable()` exige `s.isLive === true` además de que el evento coincida y exista `schedulePdfUrl`. El backend (`GetPublicLiveStatusQueryHandler`) YA devuelve `schedulePdfUrl` aunque `isLive` sea `false` — es el frontend el que lo oculta de más. El campo sigue siendo global (vive en `Live.SchedulePdfUrl`, asociado indirectamente al evento vía `YouTube.EventId`), no hay modelo per-evento para esto ni falta crearlo: basta con dejar de exigir `isLive` para la visibilidad y usarlo solo para el texto.

**Archivo:** `frontend/src/app/features/competitor/eventos/eventos.component.ts`

**Cambios:**
1. Reemplazar `isLiveScheduleAvailable(eventId)` (líneas ~839-842) por una función que valide solo existencia: `s?.event?.id === eventId && !!s.schedulePdfUrl` (quitar `s?.isLive`).
2. Agregar un getter `scheduleButtonLabel(): string` que devuelva `'Descargar programación (PDF)'` si `this.liveStatus.isLive()` es `true`, o `'Ver programación (PDF)'` si es `false` (texto exacto a confirmar con el usuario al implementar; es una suposición razonable, no bloqueante).
3. Actualizar los dos sitios de template que usan el botón (rama `Completado`, líneas ~303-310, y el botón secundario para eventos no completados, líneas ~345-353) para usar la nueva función de visibilidad y el label dinámico. Ambos son el mismo bloque duplicado — considerar extraerlo a un `<ng-template>` local para no mantener el markup dos veces (limpieza opcional, no obligatoria).
4. La rama `Completado` sigue mostrando "Ver ganadores" vía `resultsPdfUrl(event.id)` **solo si no hay** programación disponible para ese evento (mantener el `@else` ya existente) — esto no cambia.

**Criterio de aceptación:**
- Evento con PDF de programación configurado y `isLive = false` → botón visible, texto "Ver Ganadores (PDF)".
- Mismo evento con `isLive = true` → botón visible, texto "Descargar programación (PDF)".
- Evento sin `schedulePdfUrl` configurado, o que no coincide con `youTube.EventId` → botón no aparece, sin importar `isLive`.

## /eventos — filtro de categoría en "Inscritos confirmados" (issue #2) 

**Ya implementado.** `eventos.component.ts` líneas 398-412 (`<select>`), 793-811 (`selectedInscritosCategoryId`, `selectInscritosCategory`, `filteredConfirmedInscriptions`). No requiere cambios — solo verificar manualmente en el navegador que funciona como se espera.

---

## 3. /mi-panel/datos — "No se pudieron cargar tus datos personales" (bug real de autorización) 	

**Por qué:** `GET /v1/competitors/{competitorId}` está protegido con `AdminPolicies.UsersRead` (política pensada para la pantalla admin de "Usuarios"), pero `datos-personales.component.ts` (panel del propio competidor) llama a ese mismo endpoint para leer su propio perfil. Un JWT de competidor no tiene el claim `admin_role`, así que `AdminPermissionAuthorizationHandler` nunca autoriza y el request cae en 403 → el `catch` genérico del frontend muestra el mensaje reportado. Los endpoints hermanos del mismo controller (`/notifications`, `/inscriptions`, `/points-history`, `/calendar`) **no** tienen `[Authorize]` admin, lo que confirma que `GetById` quedó mal reusado. Esto también explica por qué el widget de ranking en `/eventos` (`loadCompetitorStats()`, mismo endpoint) falla en silencio.

**Archivo backend:** `backend/src/AlasApp.Api/Controllers/CompetitorsController.cs` (método `GetById`, línea ~64-65)

**Cambio:** permitir que un competidor autenticado consulte **su propio** registro, además del acceso admin existente. Opciones (a decidir en implementación, siguiendo el patrón que ya use el proyecto para "self or admin" si existe uno — buscar antes de escribir código nuevo; si no existe patrón reusable, la opción más simple):
- Quitar `[Authorize(Policy = AdminPolicies.UsersRead)]` de `GetById` y reemplazar por `[Authorize]` simple + una comprobación en el handler/controller: si el usuario autenticado es competidor, `competitorId` debe ser igual a su propio `CompetitorId` (claim del JWT); si es admin, sin restricción. Devolver 403 si un competidor pide el perfil de otro.

**No tocar** el `PUT /competitors/{competitorId}` (guardado) en este ítem — el usuario solo reportó el error de carga; si al verificar el guardado también falla por el mismo motivo, aplicar la misma corrección ahí (mencionarlo en la revisión, no incluido en el criterio de aceptación de este punto salvo que se confirme el mismo síntoma).

**Criterio de aceptación:**
- Login como competidor → `/mi-panel/datos` carga los datos sin el mensaje de error.
- Login como competidor, intento manual de pedir `GET /competitors/{otroId}` (no el propio) → 403.
- Login como admin → `/admin/usuarios` (o donde se use `GetById`) sigue funcionando igual que hoy.
- Bonus: verificar que el widget "Ranking {{año}}" en `/eventos` para un competidor logueado ahora muestra datos reales en vez de `—`.

---

## 4. /admin — sidebar colapsable a solo íconos		Estado: OK

**Por qué:** aprovechar ancho de pantalla en vistas con tablas anchas (categorías, inscritos, pagos). Hoy no existe ningún mecanismo de colapso de escritorio — solo un toggle de apertura/cierre en móvil (`open`/`isDesktop` signals). El patrón reusable más cercano es `AdminThemeService` (signal + `localStorage`, SSR-safe con `isPlatformBrowser`).

**Archivos:**
- Nuevo: `frontend/src/app/core/services/admin-sidebar.service.ts` (clonar el patrón de `admin-theme.service.ts`: `signal<boolean>` `collapsed`, `toggle()`, persistencia en `localStorage` bajo una key como `'alas-admin-sidebar-collapsed'`, guardas `isPlatformBrowser`).
- `frontend/src/app/shared/components/admin-sidebar/admin-sidebar.component.ts`:
  - Inyectar el nuevo servicio.
  - Ancho condicional: `w-64` ↔ `w-16`/`w-20` según `collapsed()`.
  - Ocultar el `<span>` de label (línea ~46) cuando está colapsado, dejando solo el ícono (línea ~45). Los íconos son emojis — verificar alineación centrada en modo colapsado.
  - Agregar botón de toggle (ej. flecha) visible solo en desktop (`isDesktop()`).
- `frontend/src/app/layouts/admin-layout/admin-layout.component.ts`:
  - Cambiar el `lg:ml-64` estático (línea ~12) por una clase condicionada a `collapsed()` del mismo servicio inyectado aquí también (ej. `[class.lg:ml-64]="!collapsed()"` / `[class.lg:ml-20]="collapsed()"`).

**Criterio de aceptación:**
- Botón de colapsar/expandir visible en desktop (≥1024px).
- Al colapsar: sidebar se angosta a solo íconos, el contenido principal recupera el ancho (margen izquierdo se ajusta), y el estado persiste al recargar la página (localStorage).
- En móvil, el comportamiento de apertura/cierre existente no se rompe (son dos mecanismos independientes: mobile open/close vs desktop collapse).

---

## 5. /admin/dashboard — dos fixes

**Archivo:** `frontend/src/app/features/admin/dashboard/dashboard.component.ts`

### 5a. Renombrar "Circuitos de la temporada" → "Circuitos"	Estado: OK
Línea ~201 (`<h2 ...>Circuitos de la temporada</h2>`). Cambio de texto puro, sin lógica.

### 5b. "Eventos del circuito" no muestra inscritos 
**Diagnóstico:** el wiring de datos (`ae.inscritosCount` → `DashboardEventRow.inscritos` → `{{ ev.inscritos }}`, línea ~176) está correcto end-to-end (backend `InscritosCount`, JSON `inscritosCount`, frontend igual). No hay bug de nombre de campo. Causas probables reales:
1. El conteo backend (`AdminDashboardRepository.cs` línea ~33) cuenta **todas** las filas de `Inscriptions` para el evento sin filtrar por estado (incluye canceladas/rechazadas) — puede mostrar un número, pero potencialmente "raro", no ausente.
2. `activeEvents` está limitado a los 10 primeros eventos activos/próximos (`.Take(10)`) — eventos fuera de ese top-10 no aparecen en la tabla en absoluto.
3. Puede que simplemente no haya inscripciones reales en el ambiente probado.

**Acción antes de tocar código:** verificar en el navegador (Network tab) la respuesta real de `GET /v1/admin/dashboard` para confirmar si `inscritosCount` viene en `0`/`null`/ausente, o si el problema es visual (CSS/template) o de datos. Si la respuesta trae el número correcto y no se ve en pantalla, revisar clases CSS de la celda (línea ~176) por posible texto invisible/color igual al fondo. Si la API ya trae 0 para eventos con inscripciones reales, filtrar `AdminDashboardRepository.cs` línea ~33 por estado válido de inscripción (excluir rechazadas/canceladas) como fix de backend.

**Criterio de aceptación:** la columna "Inscritos" en "Eventos del circuito" muestra un número >0 para un evento con inscripciones confirmadas reales en el ambiente de prueba.

---

## 6. /admin/categorias — tarifa de inscripción por nivel de estrellas 	Estado: OK

**Por qué (decisión ya tomada):** el backend ya tiene todo: `Category.Tariffs` (`CategoryTariff` por `StarLevel` 1-7), endpoints `GET/PUT /v1/categories/{categoryId}/tariffs/{starLevel}` (`CategoryTariffsController.cs`), y la resolución completa en `EventCategoryRepository.GetByEventIdAsync`: si `Event.UseCircuitTariffs = true`, se usa `Category.Tariffs[stars].Usd` (si no hay tarifa activa para ese nivel, cae a `0`). Solo falta la pantalla de admin — sin cambios de backend.

**Archivo:** `frontend/src/app/features/admin/categorias/categorias.component.ts`

**Cambios:**
1. Al editar una categoría (o en una sección nueva dentro del form/detalle), agregar una tabla de 7 filas (niveles de estrella 1-7, igual que la constante `STARS` de `admin-eventos.component.ts:74-75`) con columnas: USD, COP, Activo (checkbox), y guardar/cargar vía:
   - `GET /categories/{categoryId}/tariffs` al abrir la categoría.
   - `PUT /categories/{categoryId}/tariffs/{starLevel}` por fila modificada (o al guardar todas, iterar y hacer un `PUT` por fila cambiada).
2. Mostrar junto al checkbox "Usar tarifas del circuito" en `admin-eventos.component.ts` (línea ~547) un texto aclaratorio que ya existe y es correcto ("Las tarifas se calculan según la categoría y el nivel de estrellas asignado") — no requiere cambio, solo confirmar que sigue siendo preciso tras el cambio.
3. Considerar (opcional, mencionar en el plan pero no obligatorio si el usuario no lo pide) mostrar una advertencia en categorías sin ninguna tarifa configurada para el nivel de estrellas de un evento activo, ya que hoy eso resuelve silenciosamente a `$0`.

**Criterio de aceptación:**
- Admin puede definir/editar tarifa USD y COP por nivel de estrella en una categoría y guardarla.
- Al crear/editar un evento con "Usar tarifas del circuito" activado, la tarifa mostrada en categorías coincide con lo configurado por nivel de estrella del evento.
- Al desactivar el checkbox, sigue funcionando el flujo actual de tarifa custom por evento-categoría (sin cambios ahí).

---

## 7. /admin/pagos — exportar transacciones recientes	Estado: OK

**Por qué:** no existe ningún export para pagos (frontend ni backend), pero el patrón ya existe completo para Inscritos (`inscritos.component.ts` botón "Exportar XLSX" → `ApiService.downloadFile()` → `InscriptionsController.Export` → `IBulkExcelService.BuildInscriptionsExport` con ClosedXML). Replicar el mismo patrón para pagos evita duplicar mecanismos.

**Archivos backend:**
- `backend/src/AlasApp.Application/Abstractions/Services/IBulkExcelService.cs`: agregar `BuildPaymentsExport(IReadOnlyCollection<...> rows)`.
- `backend/src/AlasApp.Infrastructure/Imports/ClosedXmlBulkExcelService.cs`: implementar `BuildPaymentsExport` (mismas columnas que la tabla de "Transacciones recientes": Fecha, Competidor, Evento, Categoría, Monto, Método, ID Transacción, Estado).
- `backend/src/AlasApp.Api/Controllers/PaymentsController.cs`: agregar `[HttpGet("export")]` que reutilice los mismos filtros que `GET /v1/payments` (`method`, `status`, `fromDate`, `toDate`), sin el límite de 50 de la vista (usar un tope amplio tipo `MaxExportRows` como en Inscripciones, ej. 5000), y devolver `File(...)` con el mismo content-type XLSX.

**Archivo frontend:** `frontend/src/app/features/admin/pagos/pagos.component.ts`
- Agregar botón "Exportar XLSX" junto al título "Transacciones recientes" (línea ~201), que llame `this.api.downloadFile('/payments/export?' + <mismos query params que loadTransacciones>, 'transacciones.xlsx')` — reusar `ApiService.downloadFile` (ya existe en `api.service.ts:70-90`), mismo patrón que `inscritos.component.ts` línea ~724.
- El export debe respetar los filtros actualmente aplicados en pantalla (`filterEstado`, `filterFromDate`, `filterToDate`), no solo las 50 filas cargadas en memoria.

**Criterio de aceptación:**
- Botón "Exportar XLSX" en Pagos descarga un archivo `.xlsx` válido con las transacciones que coinciden con los filtros activos en pantalla (no limitado a 50 filas si hay más en el rango filtrado).

---

## 8. /inscripcion — paso 2 (filtro + multi-categoría) y paso 3 (validación)

### 8a. Paso 2 — filtrar categorías por género y edad, permitir selección múltiple, sumar total	| Estado: OK

**Por qué:** hoy el filtro de género ya existe (backend `EventCategoriesController` + réplica client-side), pero **no hay filtro de edad** pese a que `Category` ya tiene `AgeRestriction`/`MinAge`/`MaxAge`/`SuccessorCategoryId` en el dominio (reglas de negocio de CLAUDE.md sección 9, "Categoría sucesiva") — están validados al crear categorías pero nunca se usan para filtrar elegibilidad. Tampoco existe `FechaNacimiento` del competidor en el frontend de inscripción (solo se pide `genero` via `GET /competitors/{id}`). Selección es single-select hoy; se decidió ir a **un solo pago combinado**, lo que requiere cambios de backend.

**Cambios backend:**
1. `backend/src/AlasApp.Api/Controllers/EventCategoriesController.cs`: extender el filtro de elegibilidad (función `IsGenderCompatible` ya existente, líneas 55-60) para además validar edad: obtener `Competitor.FechaNacimiento`, calcular edad a la fecha del evento (o a la fecha actual — definir consistentemente con cómo se calculó al crear la categoría), y excluir categorías donde `AgeRestriction = true` y la edad no esté en `[MinAge, MaxAge]`. Reusar `Competitor.FechaNacimiento` (`Competitor.cs:65`) y `Category.MinAge/MaxAge/AgeRestriction` (`Category.cs:56-60`), ya existentes — no crear campos nuevos.
2. Nuevo comando/endpoint para inscripción multi-categoría con un solo pago:
   - Opción recomendada por ser más simple de mapear al dominio actual (que ya crea `Inscription` por `(competitor, event, category)`): nuevo `CreateBulkInscriptionCommand` que reciba `competitorId, eventId, categoryIds: Guid[], shirtNumber, consentimientos, paymentMethod`, cree **N filas `Inscription`** (una por categoría, reusando la lógica actual de `CreateInscriptionCommandHandler` por cada categoría — validación de duplicado, cupo, tarifa) dentro de una transacción, y devuelva el total combinado + un identificador de "grupo de pago" (o simplemente la lista de `inscriptionIds` creadas) para que el flujo de pago (PayPal / token de playa) opere sobre el conjunto.
   - El flujo de pago (`CapturePayPalOrderCommandHandler`, `RedeemBeachTokenCommandHandler`) deberá poder aplicar el resultado del pago a **todas** las inscripciones del grupo, no a una sola — revisar si conviene agregar una tabla/campo "InscriptionGroupId" o simplemente iterar la lista de IDs recibida en la captura. Esto es la pieza de mayor riesgo/esfuerzo del plan completo; profundizar el diseño exacto en la fase de implementación (fuera del alcance de este documento de planificación, pero se señala explícitamente como el punto que más discusión de diseño va a requerir).
3. `EventCategoryListDto`/contrato: no requiere cambios de forma, pero el frontend necesitará el `FechaNacimiento` o la edad calculada del competidor si se quiere mostrar por qué una categoría no es elegible (opcional, no obligatorio para el criterio de aceptación).

**Cambios frontend** (`inscripcion.component.ts`):
1. Cambiar `selectedCategoryId: signal('')` por `selectedCategoryIds: signal<Set<string>>(new Set())`.
2. Cambiar el UI de selección (líneas ~166-236) de radio-círculos a checkboxes.
3. `totalAmount` computed: sumar `tarifa` de todas las categorías seleccionadas + membresía (una sola vez, no por categoría, salvo que el negocio diga lo contrario — confirmar en revisión si `membresiaPorEventoUsd` se cobra una vez por evento o una vez por categoría inscrita).
4. `confirm()`: enviar `categoryIds: Array.from(this.selectedCategoryIds())` al nuevo endpoint bulk en vez de `categoryId` único.

**Criterio de aceptación:**
- Un competidor cuya edad no cae en el rango de una categoría con restricción de edad no la ve listada en el paso 2.
- Puede marcar 2+ categorías elegibles; el total mostrado en el paso 3 es la suma de sus tarifas (+ membresía si aplica).
- Al confirmar y pagar (PayPal o playa), se crean inscripciones para todas las categorías seleccionadas y el pago/token cubre el total combinado en una sola operación.

### 8b. Paso 3 — no habilitar "Confirmar y Pagar" si falta un dato obligatorio; mostrar el error | Estado: OK

**Diagnóstico:** el botón ya está deshabilitado por `!paymentMethod() || submitting()` pero el guard de `confirm()` también exige `consentsAccepted()` sin reflejarlo en el `[disabled]` del botón — inconsistencia menor. Sobre el caso puntual reportado (`ShirtNumber` requerido con 400): **no reproduce en el código actual** — `ShirtNumber` es opcional end-to-end (`CreateInscriptionCommand.ShirtNumber` es `string?`, sin validación en `Validate()`, sin `[Required]` en el contrato generado) y el campo YA existe en el paso 1 (línea ~120, "Número de camiseta"). Es posible que el error observado viniera de una versión de backend distinta a la actual, o de otro campo con nombre parecido (ej. `NumeroCamiseta` del perfil del competidor en `registro.component.ts`, que es un campo de perfil, no de inscripción). Se documenta como no reproducible, pero se refuerza el manejo de errores para que, si vuelve a ocurrir con cualquier campo, sea visible.

**Cambios** (`inscripcion.component.ts`):
1. Agregar `consentsAccepted()` a la condición `[disabled]` del botón "Confirmar y Pagar" (línea ~360-367), para que coincida exactamente con el guard interno de `confirm()`.
2. En el `catch` de `confirm()` (líneas ~593-598), además de `err?.body?.message`, leer `err?.body?.errors` (objeto `{ campo: [mensajes] }`, tal como lo devuelve `ValidationException`/el ejemplo del usuario) y concatenar los mensajes de cada campo al `errorMessage` mostrado, en vez de descartarlos silenciosamente.

**Criterio de aceptación:**
- El botón permanece deshabilitado si falta cualquier consentimiento del paso 1, no solo el método de pago.
- Si el backend devuelve un `errors` por campo (cualquier campo, no solo ShirtNumber), el mensaje mostrado en el banner rojo incluye el detalle de cada campo, no un genérico.

---

## 9. /ranking — paginación, circuitos pasados, fórmula configurable, exclusión por membresía

### 9a. Paginación: "0 resultados" y páginas que cambian al ir a la última	| Estado: OK

**Causas confirmadas (bugs de wiring, no de datos):**
1. `ranking.component.ts` (líneas ~146-151) usa `<app-pagination [currentPage] [totalPages] (pageChange)>` **sin pasar `[totalItems]`** → `PaginationComponent` usa su default `0` → siempre dice "0 resultados" en el pie, aunque el header de la propia página (`{{ totalItems() }} competidores`, línea 78) sí esté bien.
2. `PaginationComponent` (`shared/components/pagination/pagination.component.ts:36-45`) genera una ventana deslizante `±2` alrededor de `currentPage` — al ir a la última página, la ventana se recentra y "aparecen" números nuevos porque se descartan los del inicio. Es un componente compartido: verificar otros usos antes de cambiar su comportamiento por defecto (grep `<app-pagination` en todo el frontend).

**Cambios:**
1. `ranking.component.ts`: agregar `[totalItems]="totalItems()"` a la invocación de `<app-pagination>`.
2. `pagination.component.ts`: cambiar `pages` computed para listar **todas** las páginas desde 1 hasta `totalPages()` (sin ventana), salvo que el número de páginas sea muy grande — si otros consumidores del componente (verificar con grep) dependen de la ventana ±2 por diseño (ej. tablas con cientos de páginas), evaluar una prop opcional `windowed: boolean` en vez de cambiar el comportamiento global, para no romper otras pantallas.

**Criterio de aceptación:**
- El pie de la tabla de ranking muestra el total real de competidores, no "0 resultados".
- Al hacer clic en la última página, se siguen viendo todas las páginas desde la 1, sin que aparezcan números nuevos fuera de rango.
- Ninguna otra pantalla que use `<app-pagination>` se rompe visualmente (revisar tras el cambio).

### 9b. Ver rankings de circuitos pasados| Estado: OK

**Diagnóstico:** hoy solo hay selector de **año**, no de circuito, y el backend (`GetRankingQueryHandler.cs`) siempre resuelve el circuito vía `circuitRepository.GetCurrentBySeasonAsync(seasonYear)` usando la temporada **actual configurada en Configuración**, ignorando el `year` que el usuario elige — nunca puede llegar a un `Circuit` distinto (recordar que puede haber varios circuitos por temporada, por región, según el modelo de `Circuit` con campos `temporada`+`region`). Esto es más una limitación estructural que un simple bug de un año.

**Cambios backend:**
1. `GetRankingQuery`/`RankingsController`/`GetRankingQueryHandler.cs`: agregar parámetro opcional `circuitId`. Si viene, usarlo directamente (con validación de que existe); si no viene, mantener el fallback actual (circuito actual de la temporada actual) para no romper el comportamiento por defecto.
2. `ListRankingCategoriesQueryHandler.cs`: igual, aceptar `circuitId` opcional para poblar categorías/años disponibles del circuito seleccionado, no solo del circuito actual.

**Cambios frontend** (`ranking.component.ts` + `ranking.service.ts`):
1. Agregar selector de circuito (mismo patrón que `eventos.component.ts`: cargar `/circuits?limit=100`, usar `pickCurrentCircuit()` de `current-circuit.util.ts` como default).
2. Pasar `circuitId` en la llamada a `GET /rankings`.
3. Mantener el selector de año existente, ahora filtrado/poblado según el circuito elegido.

**Criterio de aceptación:**
- Selector de circuito visible en `/ranking`, con el circuito actual (según Configuración) seleccionado por defecto — mismo criterio ya usado en `/eventos`.
- Elegir un circuito de una temporada anterior muestra su ranking histórico correctamente, sin mezclar datos del circuito actual.

### 9c. Fórmula de cálculo del ranking final (configurable on/off) | no veo este cambio

**Por qué:** la fórmula actual (`SurfScoresGateway.BuildCircuitRankingCacheAsync`, líneas 14-78) es: tomar los mejores `BestResultsCount` resultados de cada competidor y sumarlos, desempate por `Events desc → Name asc`. La nueva fórmula pedida por el usuario es distinta: descartar el 30% de las etapas válidas del calendario de esa categoría (redondeo: `<0.50` hacia abajo, `≥0.50` hacia arriba), con desempate por recálculo progresivo (contar 1 evento menos, luego 1 más allá del total, etc.) y fallback final al ranking del año anterior. Debe poder activarse/desactivarse desde Configuración sin perder la fórmula actual.

**Cambios backend:**
1. `AdminSettingsDto.RankingSettingsDto`: agregar `bool UseAdvancedFormula` (nombre a definir, ej. `UseStageDropPercentageFormula`).
2. `AdminSettingsValidator.cs`: sin nueva validación obligatoria más allá del booleano.
3. Nuevo servicio de cálculo (ej. `AdvancedRankingCalculator`), separado de `SurfScoresGateway.BuildCircuitRankingCacheAsync`, implementando:
   - Total de etapas válidas de la categoría en el circuito/temporada.
   - Etapas a descartar = `round(total * 0.30)` con la regla de redondeo indicada (`MidpointRounding` no sirve directo por el corte en 0.50 hacia arriba explícito — implementar redondeo manual: `Math.Floor(x + 0.5)` da exactamente esa regla para valores positivos).
   - Sumar los mejores `(total - descartadas)` resultados por competidor.
   - Desempate: si dos compiten empatados, recalcular contando `total - descartadas - 1` (una etapa menos) para am­bos, y si persiste, `total - descartadas - 2`, etc. hasta 1; si sigue empatado, ir hacia arriba: `total - descartadas + 1`, `+2`, etc. Si persiste tras agotar el rango razonable, usar el ranking final de la temporada anterior de esos competidores como último criterio de desempate (requiere poder leer el `RankingSnapshot` de `year - 1` para el mismo circuito/categoría).
4. `SurfScoresGateway.BuildCircuitRankingCacheAsync` (o quien orqueste el recálculo tras `EventResultsWriter.UpsertAsync`): si `settings.Ranking.UseAdvancedFormula` es `true`, delegar al nuevo calculador; si es `false`, mantener la lógica actual sin tocarla.

**Cambios frontend** (`configuracion.component.ts`, tab Ranking): agregar un toggle "Usar fórmula de descuento por etapas (30%)" junto a los campos `bestResultsCount`/`dnsPercent`/`dsqPenalty` ya existentes.

**Nota de limpieza relacionada (mencionar, no obligatoria):** `DnsScorePercentage` y `DsqPenaltyPoints` están confirmados como configuración "muerta" (no se usan en ningún cálculo real hoy) — si se toca esta área, vale la pena decidir si se implementan ahora o se documentan como pendiente aparte; no se incluye en el criterio de aceptación de este ítem salvo que el usuario lo pida.

**Criterio de aceptación:**
- Con el toggle desactivado, el ranking se calcula exactamente igual que hoy (regresión cero).
- Con el toggle activado, un circuito con calendario conocido (ej. 10 etapas) descarta exactamente 3 resultados por competidor, y el empate se resuelve según el algoritmo de recuento progresivo descrito, cayendo al ranking del año anterior si el empate persiste.

### 9d. Excluir del ranking a competidores sin membresía pagada (configurable on/off)| no veo este cambio

**Por qué (decisión ya tomada):** usar `Inscription.MembershipPlan`/`MembershipFeeUsd`, ya ligado al competidor vía sus inscripciones de la temporada — no la entidad `Membership` de club/federación (que no tiene FK a competidor individual).

**Cambios backend:**
1. `AdminSettingsDto.RankingSettingsDto`: agregar `bool ExcludeCompetitorsWithoutMembership`.
2. En el cálculo de ranking (`SurfScoresGateway.BuildCircuitRankingCacheAsync`, líneas ~44-66, tanto en la fórmula actual como en la nueva del punto 9c): si el flag está activo, antes de agrupar por competidor, excluir a quienes no tengan **al menos una** `Inscription` en la temporada/circuito con `MembershipPlan` pagado (definir "pagado" como `EstadoAdmin = Pagado` sobre esa inscripción, reusando el mismo campo que ya determina "Pago confirmado" en el resto de la app).

**Cambios frontend** (`configuracion.component.ts`, tab Ranking): agregar el toggle correspondiente junto a los demás de esta sección.

**Criterio de aceptación:**
- Con el toggle desactivado, el ranking incluye a todos los competidores como hoy.
- Con el toggle activado, un competidor con resultados pero sin ninguna inscripción de membresía pagada en la temporada no aparece en el ranking publicado.

---

## Verificación end-to-end sugerida (por bloque, al implementar)

- **Frontend:** `npx tsc --noEmit` + `npx ng build --configuration development` tras cada bloque (ya es el patrón usado en esta sesión).
- **Backend:** `docker start alas-sql` (o el contenedor que corresponda) + `dotnet test AlasApp.slnx`, apuntando siempre a `AlasAppTests`, nunca al VPS (regla ya documentada en CLAUDE.md del proyecto).
- **Manual (browser):** para los ítems de UI (estrellas, sidebar, paginación, botones), usar el navegador para confirmar visualmente cada criterio de aceptación — especialmente el bug de `/mi-panel/datos` (login real como competidor) y el flujo completo de multi-categoría en `/inscripcion` (crear 2+ inscripciones y pagar una vez).
