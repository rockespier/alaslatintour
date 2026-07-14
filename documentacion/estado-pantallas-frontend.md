# Estado de las pantallas del frontend

Este documento es la referencia viva de qué pantallas ya están conectadas al backend real
(.NET BFF) y cuáles todavía son solo maquetas visuales fieles al diseño de `docs/*.html`,
con datos locales de ejemplo y sin llamadas HTTP reales. Actualizar esta tabla cada vez que
una pantalla pase de "mock" a "conectada".

## Portal público

| Ruta | Componente | Estado | Notas |
|---|---|---|---|
| `/` | `features/public/home` | ✅ Backend real | Eventos, artículos y ranking vía API |
| `/quienes-somos` | `features/public/quienes-somos` | ⚪ Estático | Contenido institucional fijo, no requiere backend |
| `/noticias` | `features/public/noticias` | ✅ Backend real | Artículos y galerías vía WordPress (proxy BFF) |
| `/noticias/:slug` | `features/public/noticia-detalle` | ✅ Backend real | |
| `/galerias/:slug` | `features/public/galeria-detalle` | ✅ Backend real | |
| `/ranking` | `features/public/ranking` | ✅ Backend real | Vía `RankingService` (cache SurfScores) |
| `/eventos` | `features/competitor/eventos` | ✅ Backend real | Vista personalizada del competidor logueado ("Hola, X") |
| `/calendario` | `features/public/calendario` | ✅ Backend real | Vista pública anónima del calendario, distinta de `/eventos`. Conectada a `GET /v1/events` + `GET /v1/circuits` (tabs de circuito dinámicos) + `GET /v1/events/{id}/categories` (tarifas al expandir) |
| `/inscripcion/:eventId` | `features/competitor/inscripcion` | ✅ Backend real | |
| `/pago-playa/:inscriptionId` | `features/competitor/pago-playa` | ✅ Backend real | |
| `/mi-panel/inscripciones` | `features/competitor/mi-panel/mis-inscripciones` | ✅ Backend real | |
| `/mi-panel/historial` | `features/competitor/mi-panel/historial-puntos` | ✅ Backend real | |
| `/mi-panel/calendario` | `features/competitor/mi-panel/mi-calendario` | ✅ Backend real | |
| `/mi-panel/datos` | `features/competitor/mi-panel/datos-personales` | ✅ Backend real | |

## Autenticación

| Ruta | Componente | Estado |
|---|---|---|
| `/login` | `features/auth/login` | ✅ Backend real |
| `/registro` | `features/auth/registro` | ✅ Backend real |
| `/recuperar-password` | `features/auth/recuperar-password` | ✅ Backend real |

## Panel de administración

| Ruta | Componente | Estado | Notas |
|---|---|---|---|
| `/admin` (Dashboard) | `features/admin/dashboard` | ✅ Backend real | `GET /v1/admin/dashboard` (kpis, activeEvents, recentInscriptions) enriquecido con `GET /v1/events` (país/fechas/estrellas/capacidad) y `GET /v1/circuits` (tarjetas de circuitos) |
| `/admin/usuarios` | `features/admin/usuarios` | ✅ Backend real | `AdminUsersController` (list/create/update rol+estado/desactivar) + `AdminRolesController` para la matriz de permisos real (tabs "Roles y Perfiles" y "Permisos") |
| `/admin/circuitos` | `features/admin/circuitos` | ✅ Backend real | |
| `/admin/eventos` | `features/admin/eventos` | ✅ Backend real | Incluye categorías habilitadas + estrellas por categoría + tarifas |
| `/admin/categorias` | `features/admin/categorias` | ✅ Backend real | CRUD de categorías conectado. Falta UI de matriz de tarifas por categoría×estrellas (`CategoryTariffsController` ya existe en backend, sin UI) |
| `/admin/inscritos` | `features/admin/inscritos` | ✅ Backend real | Tab "Inscritos" vía `GET/PUT /v1/inscriptions`. Tab "Puntajes de Premios" vía `GET /v1/events/{id}/prize-distribution` (solo lectura, por evento). Tab "Puestos por Evento" vía `GET /v1/events/{id}/results` (solo lectura, con atribución obligatoria a SurfScores.com) |
| `/admin/pagos` | `features/admin/pagos` | ✅ Backend real | `PaymentsController` (kpis + listado + validar pago) y `MembershipsController` (CRUD completo). Se removió el gráfico mensual fabricado: no existe endpoint de serie histórica |
| `/admin/tokens` | `features/admin/tokens` | ✅ Backend real | `BeachTokensController`: listado, aprobar y rechazar tokens de pago en playa |
| `/admin/configuracion` | `features/admin/configuracion` | ✅ Backend real | `AdminSettingsController` (`GET/PUT /v1/admin/settings`, `POST /v1/admin/settings/integrations/{provider}/test`). Los 5 tabs (General, Ranking, Integraciones, Notificaciones, En Vivo) leen y guardan del mismo objeto de configuración |

## Notas de implementación relevantes

### `/admin/inscritos` — Puntajes de Premios
Este tab **no** es una matriz editable de montos por puesto×estrellas (eso vive en Configuración → Ranking, como porcentajes). Es una vista de solo lectura **por evento**: se elige un evento y el backend calcula el monto en USD de cada puesto usando `event.prizeAmountUsd` × el porcentaje configurado para el nivel de estrellas de ese evento.

### `/admin/inscritos` — Puestos por Evento
Solo lectura, sin edición manual de resultados desde esta pantalla (aunque el backend expone `POST /v1/events/{id}/results` para cargar/actualizar resultados, probablemente pensado para un flujo de sincronización con SurfScores, no para captura manual desde este tab). Incluye la leyenda "Results by SurfScores.com" enlazada, obligatoria por los términos de la API externa (ver `CLAUDE.md` §8).

### `/admin/pagos` — Resumen
El prototipo original mostraba una gráfica de barras con recaudación mensual de los últimos 6 meses. No existe ningún endpoint que exponga una serie histórica de recaudación, así que esa sección se eliminó en vez de fabricar datos.

### `/admin/usuarios` — límites del PUT de usuarios
`PUT /v1/admin/users/{id}` solo permite cambiar `rol` y `status`; no se puede editar nombre/apellido/email de un usuario ya creado. El modal de edición refleja esto (esos campos quedan de solo lectura al editar).

### Autorización de endpoints (hallazgo informativo, no bloqueante)
`PaymentsController`, `MembershipsController`, `BeachTokensController`, `InscriptionsController` y `EventInscriptionsController` no tienen actualmente el atributo `[Authorize]` a nivel de clase ni de acción, a diferencia de `AdminUsersController`, `AdminRolesController`, `DashboardController` y `AdminSettingsController`, que sí exigen políticas (`UsersRead`, `ConfigurationRead`, etc.). No hay una política de fallback global en `Program.cs`. Esto significa que esos endpoints "admin" son alcanzables hoy sin token válido. Vale la pena revisarlo antes de producción.

## Convención de este documento

- ✅ **Backend real**: el componente llama a `ApiService`/servicios reales y los datos vienen de la base de datos o de integraciones externas (WordPress, SurfScores).
- 🟡 **Mock local**: el componente usa arrays/señales locales con datos de ejemplo fieles al diseño del prototipo. Los botones de guardar muestran un toast de confirmación pero no persisten nada.
- ⚪ **Estático**: página de contenido fijo que no necesita backend (institucional).
