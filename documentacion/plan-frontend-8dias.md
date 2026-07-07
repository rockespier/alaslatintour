# Plan de Implementación Frontend — ALAS Latin Tour PWA
**Duración:** 8 días · **Stack:** Angular 19 + SSR · Tailwind CSS · NSwag client  
**Última actualización:** 2026-07-07

---

## Estado de partida

| Elemento | Estado |
|---|---|
| Proyecto Angular 19 + SSR | ✅ Scaffolded en `/frontend` |
| Cliente NSwag generado | ✅ `/src/app/core/api/alas-api-client.ts` (cubre todos los endpoints) |
| Prototipos HTML de referencia | ✅ 16 pantallas en `/specs/` |
| Tailwind CSS | ❌ Solo CDN en prototipos — hay que instalar vía npm |
| PWA (service worker) | ❌ Por instalar |
| Rutas Angular | ❌ Array vacío |
| Componentes | ❌ Solo `AppComponent` vacío |

---

## Día 1 — Fundamentos y arquitectura

**Objetivo:** El proyecto compila, se ve el shell correcto y hay servicios base funcionando.

### Setup de dependencias
```bash
# Desde /frontend
npm install -D tailwindcss @tailwindcss/forms @tailwindcss/typography postcss autoprefixer
npx tailwindcss init -p
npm install @angular/pwa --save
ng add @angular/pwa
```

### Estructura de carpetas a crear
```
src/app/
├── core/
│   ├── api/                  # alas-api-client.ts (ya existe)
│   ├── services/
│   │   ├── auth.service.ts
│   │   └── api-wrapper.service.ts  # wrapper Angular del NSwag client
│   ├── guards/
│   │   ├── auth.guard.ts           # redirige a /login si no hay token
│   │   └── admin.guard.ts          # redirige si no tiene rol admin
│   ├── interceptors/
│   │   └── auth.interceptor.ts     # inyecta Bearer token en cada request
│   └── models/
│       └── index.ts                # re-export de tipos del NSwag client
├── shared/
│   └── components/
│       ├── navbar/                 # barra de navegación pública
│       ├── footer/                 # footer con atribución SurfScores
│       ├── admin-sidebar/          # sidebar admin con badge de tokens
│       ├── loading-spinner/
│       ├── star-rating/            # 1–5 estrellas visuales
│       ├── pagination/
│       └── status-badge/           # badge de color según estado
├── layouts/
│   ├── public-layout/             # navbar + router-outlet + footer
│   └── admin-layout/              # sidebar + router-outlet
└── features/
    ├── public/
    ├── competitor/
    └── admin/
```

### Configuración de environments
```
src/environments/
├── environment.ts          # apiUrl: 'http://localhost:5132'
└── environment.prod.ts     # apiUrl: 'https://api.alasglobaltour.com'
```

### Routing skeleton completo (app.routes.ts)
Definir las 16 rutas con lazy loading desde el inicio — incluso las que aún no tienen componente real usarán un placeholder.

```
/                           → HomeComponent
/quienes-somos              → QuienesSomosComponent
/noticias                   → NoticiasComponent
/noticias/:slug             → NoticiaDetalleComponent
/eventos                    → EventosComponent
/inscripcion/:eventId       → InscripcionComponent (guard: auth)
/pago-playa/:inscriptionId  → PagoPlayaComponent (guard: auth)

/mi-panel                   → MiPanelComponent (guard: auth, tipo: competidor)
/mi-panel/inscripciones     → MisInscripcionesComponent
/mi-panel/historial         → HistorialPuntosComponent
/mi-panel/calendario        → MiCalendarioComponent
/mi-panel/datos             → DatosPersonalesComponent

/login                      → LoginComponent
/registro                   → RegistroComponent
/recuperar-password         → RecuperarPasswordComponent

/admin                      → AdminDashboardComponent (guard: admin)
/admin/usuarios             → UsuariosComponent
/admin/circuitos            → CircuitosComponent
/admin/eventos              → AdminEventosComponent
/admin/categorias           → CategoriasComponent
/admin/inscritos            → InscritosComponent
/admin/pagos                → PagosComponent
/admin/tokens               → AdminTokensComponent
/admin/configuracion        → ConfiguracionComponent
```

### AuthService — contrato
```typescript
// Almacena JWT en localStorage (key: 'alas_token')
// Expone: currentUser$: Signal<UserInfo | null>
// Métodos: login(), logout(), isAuthenticated(), hasRole()
```

### Entregable del día
- `ng serve` levanta en localhost con el shell de la app visible
- Navbar pública y sidebar admin renderizan aunque estén vacíos
- Los guards redirigen correctamente

---

## Día 2 — Portal Público: Home + Quiénes Somos

**Pantallas de referencia:** `specs/index.html`, `specs/quienes-somos.html`

### HomeComponent
Secciones a implementar en orden:

| Sección | API | Notas |
|---|---|---|
| Hero con CTAs | — | Static. Tipografía Oswald, gradiente oceánico |
| "Próximas Paradas" | `GET /v1/events?status=Inscripciones Abiertas&limit=4` | Scroll horizontal en móvil |
| "Ranking EN VIVO" | `GET /v1/rankings?categoryId={open}&year=2026&limit=8` | Requiere `categoryId` de rankings/categories. Badge pulsante verde. **Atribución "Results by SurfScores.com"** obligatoria |
| "Últimas Noticias" | `GET /v1/articles?featured=true&limit=3` | Grid 3 col desktop / 1 col móvil |
| Footer | — | SurfScores attribution + redes sociales |

> **Regla SDD:** toda pantalla que muestre puntajes debe tener visible el link "Results by SurfScores.com". Implementar como componente `<app-surfscores-credit>`.

### QuienesSomosComponent
- Contenido completamente estático (no hay API para esto)
- Timeline de 5 hitos, tarjetas Misión/Visión/Valores, contadores animados con `IntersectionObserver`
- Datos del equipo directivo hardcodeados (provienen de WP en futuro)

### SEO con Angular SSR
Para ambas vistas, inyectar `Meta` y `Title` services:
```typescript
// HomeComponent
this.title.setTitle('ALAS Latin Tour — Surf Profesional Latinoamericano');
this.meta.updateTag({ name: 'description', content: '...' });
this.meta.updateTag({ property: 'og:image', content: '...' });
```

### Entregable del día
- Home renderiza con datos reales del backend (events + articles)
- El ranking aparece (aunque la API de rankings aún no esté implementada, mostrar estado vacío elegante)
- Ambas páginas tienen meta tags correctos en el HTML server-rendered

---

## Día 3 — Portal Público: Noticias + Noticia Detalle

**Pantallas de referencia:** `specs/noticias.html`, `specs/noticia-detalle.html`

### NoticiasComponent
| Feature | Implementación |
|---|---|
| Tabs: Noticias / Resultados / Fotos | `signal<'noticias' \| 'resultados' \| 'fotos'>` + `@if` |
| Filtros | `?category=&search=&page=&limit=` → `GET /v1/articles` |
| Skeleton loaders | `@defer (on idle) { <article-card> } loading { <skeleton> }` |
| Galería (tab Fotos) | Grid masonry CSS, datos de WP |
| Paginación | Componente `<app-pagination>` compartido |

### NoticiaDetalleComponent
| Feature | Implementación |
|---|---|
| SSR meta tags | `Title` + `Meta` services: `og:title`, `og:description`, `og:image`, `canonical` |
| Cuerpo del artículo | `[innerHTML]` sanitizado con `DomSanitizer` |
| Tabla SurfScores | Componente `<app-surfscores-results>` — datos embebidos en el artículo |
| Artículos relacionados | `GET /v1/articles?category={misma}&limit=3` |

> **Nota SSR:** `NoticiaDetalleComponent` es la pantalla más crítica para SEO. Verificar que el `<head>` generado por el servidor contenga los meta tags OG antes de responder al cliente.

### Entregable del día
- Las noticias se cargan paginadas desde la API de artículos
- El skeleton loader aparece durante la carga
- El detalle de noticia tiene meta tags correctos visibles en `view-source:`

---

## Día 4 — Flujo Competidor: Eventos + Inscripción

**Pantallas de referencia:** `specs/eventos.html`, `specs/inscripcion.html`

### EventosComponent (vista competidor logueado)
| Sección | API | Notas |
|---|---|---|
| Saludo personalizado + stats | `GET /v1/competitors/{id}` | Ranking actual + puntos |
| Tabs de circuito | `GET /v1/circuits?status=Activo&year=2026` | Alpine → Angular signal |
| Tarjetas de evento | `GET /v1/events?circuitId={id}` | Estrellas, país, fechas, inscritos, capacidad |
| Expandir "Ver detalles" | `GET /v1/events/{id}/categories` | Tabla de categorías + tarifas efectivas |
| Panel "Mis Inscripciones" | `GET /v1/competitors/{id}/inscriptions` | Sidebar desktop / sección móvil |

**Lógica de UI:**
- Botón "Inscribirse" deshabilitado si `enrolledCount >= capacidadMaxima`
- Badge de cupo lleno según regla de negocio del SDD
- Eventos con estado `Completado` o `Cerrado` muestran botón deshabilitado

### InscripcionComponent (checkout 3 pasos)
```
Paso 1 — Datos del competidor
  Campos pre-llenados (read-only desde AuthService)
  Campo número de camiseta (editable)
  Checkbox reglamento: REQUERIDO para avanzar

Paso 2 — Selección de categoría
  GET /v1/events/{eventId}/categories
  Tarjetas radio con precio efectivo
  Resumen de precio al pie

Paso 3 — Método de pago
  Radio: PayPal | Pago en Playa
  Si PayPal: botón "Pagar con PayPal" → POST /v1/inscriptions → POST /v1/payments
  Si Pago en Playa: botón "Solicitar código" → navega a /pago-playa/:inscriptionId
```

**Estado del formulario:** `signal<1 | 2 | 3>` para el paso activo. Círculos numerados activo=azul, completado=verde con checkmark.

### Entregable del día
- El flujo completo de inscripción (pasos 1→2→3) funciona con validaciones
- Al elegir Pago en Playa navega a la ruta correcta con el `inscriptionId`
- Al elegir PayPal se hace el POST real (o mock si la API no está lista)

---

## Día 5 — Flujo Competidor: Pago en Playa + Panel

**Pantallas de referencia:** `specs/pago-playa.html`, `specs/mis-inscripciones.html`, etc.

### PagoPlayaComponent — Máquina de estados crítica

```typescript
type BeachPaymentState = 
  | 'request'      // 1. Botón "Solicitar código"
  | 'pending'      // 2. Esperando aprobación admin
  | 'enter_token'  // 3. Campo token + countdown
  | 'expired'      // 4. Token expirado (HTTP 400)
  | 'confirmed';   // 5. Inscripción habilitada

state = signal<BeachPaymentState>('request');
```

| Estado | API call | Transición |
|---|---|---|
| `request` → `pending` | `POST /v1/payments/beach/request` | Al hacer clic en "Solicitar Código" |
| `pending` → `enter_token` | (polling o WebSocket futuro) | Cuando el admin aprueba |
| `enter_token` → `confirmed` | `POST /v1/payments/beach/redeem` | Token válido |
| `enter_token` → `expired` | — | Countdown llega a 0 o API devuelve 400 |
| `expired` → `pending` | `POST /v1/payments/beach/request` | "Re-solicitar token" |

**Countdown timer:**
```typescript
// RxJS interval — NUNCA setInterval en Angular
countdown$ = interval(1000).pipe(
  take(24 * 60 * 60),       // 24 horas en segundos
  map(elapsed => (24 * 3600) - elapsed),
  takeUntilDestroyed()
);
// CSS: pulso rojo cuando countdown < 180 segundos (3 min)
```

### Panel del competidor (4 sub-rutas)
| Componente | API principal |
|---|---|
| `MisInscripcionesComponent` | `GET /v1/competitors/{id}/inscriptions` |
| `HistorialPuntosComponent` | `GET /v1/competitors/{id}/points-history?year=2026` |
| `MiCalendarioComponent` | `GET /v1/competitors/{id}/calendar` · `GET /v1/competitors/{id}/calendar/export` |
| `DatosPersonalesComponent` | `GET /v1/competitors/{id}` · `PUT /v1/competitors/{id}` |

### LoginComponent + RegistroComponent
Formularios que usan `POST /v1/auth/login` y `POST /v1/auth/register`.  
Password con criterios visibles (mín 8 chars, 1 mayúscula, 1 dígito).

### Entregable del día
- La máquina de estados de pago en playa funciona con transiciones visuales
- El countdown es funcional y pulsa en rojo al acercarse a 0
- Login/Registro funcionales con manejo de errores

---

## Día 6 — Admin: Dashboard + Circuitos + Eventos Admin

**Pantallas de referencia:** `specs/admin-dashboard.html`, `specs/circuitos.html`, `specs/admin-eventos.html`

### AdminLayoutComponent
- Sidebar con los 9 links de navegación
- Badge reactivo en "Tokens" → `GET /v1/payments/beach/tokens?status=pendiente` → count
- Mobile: sidebar colapsa con hamburger
- Info de usuario al pie (desde `AuthService.currentUser$`)

### AdminDashboardComponent
| Widget | API |
|---|---|
| KPI: Eventos activos | `GET /v1/events?status=Activo` → totalItems |
| KPI: Inscripciones hoy | `GET /v1/inscriptions?fromDate={hoy}` → totalItems |
| KPI: Tokens pendientes (clickeable → /admin/tokens) | `GET /v1/payments/beach/tokens?status=pendiente` → totalItems |
| KPI: Recaudación | `GET /v1/payments/kpis` |
| Tabla de eventos | `GET /v1/events?limit=6` |
| Tabla de circuitos | `GET /v1/circuits?year=2026` |
| Últimos inscritos | `GET /v1/inscriptions?limit=8` |

### CircuitosComponent
| Feature | API |
|---|---|
| Tabla de circuitos | `GET /v1/circuits` |
| Fila expandible | `GET /v1/events?circuitId={id}` + panel de sync |
| Modal crear/editar | `POST /v1/circuits` · `PUT /v1/circuits/{id}` |
| Eliminar | `DELETE /v1/circuits/{id}` + confirmación |
| Botón "Sincronizar SurfScores" | `POST /v1/surfscores/sync/{circuitId}` |

### AdminEventosComponent
| Feature | API |
|---|---|
| Tabla con barra de progreso de capacidad | `GET /v1/events` |
| Filtros | circuito, estado, país, búsqueda |
| Modal — Tab "Datos" | `POST /v1/events` · `PUT /v1/events/{id}` |
| Modal — Tab "Categorías" | `PUT /v1/events/{id}/categories` |
| Modal — Tab "Tarifas" | `PUT /v1/events/{id}/categories` (con tarifas custom) |

### Entregable del día
- CRUD completo de circuitos y eventos funciona end-to-end
- El badge de tokens en el sidebar es reactivo
- La barra de progreso de capacidad (ej: 47/80) renderiza correctamente

---

## Día 7 — Admin: Categorías + Inscritos + Pagos

**Pantallas de referencia:** `specs/categorias.html`, `specs/inscritos.html`, `specs/pagos.html`

### CategoriasComponent — Panel dual
```
Panel izquierdo: lista de categorías
  GET /v1/categories
  Click selecciona → signal<string | null> (categoryId activo)
  Botón "Nueva Categoría" → modal

Panel derecho: tarifas de la categoría seleccionada
  GET /v1/categories/{id}/tariffs (5 filas: 1★ → 5★)
  Edición inline: click en celda → input editable
  PUT /v1/categories/{id}/tariffs/{starLevel} por fila
```

**Formulario de categoría:** incluye `successorCategoryId` (selector de categoría sucesiva — regla de negocio del SDD).

### InscritosComponent — 3 tabs
| Tab | Feature | API |
|---|---|---|
| **Inscritos** | Tabla con filtros, estados de pago, export CSV | `GET /v1/events/{id}/inscriptions` |
| Fila expandible | Datos completos, confirmar pago en playa | `PUT /v1/inscriptions/{id}` |
| **Puntajes de Premios** | Matriz 8×5 editable (puestos × estrellas) | — (configuración local o API futura) |
| **Puestos por Evento** | Selector evento+categoría, pódium CSS, tabla completa | `GET /v1/events/{id}/results` |

### PagosComponent — 3 tabs
| Tab | Feature | API |
|---|---|---|
| **Resumen** | 4 KPIs + gráfico de barras 6 meses + últimas 10 transacciones | `GET /v1/payments/kpis` · `GET /v1/payments` |
| **Inscripciones** | Filtros, totales por método, modal "Validar Pago" | `GET /v1/payments` · `PUT /v1/payments/{id}` |
| **Membresías** | Tabla de clubes, badge "Vence pronto", botón Renovar | `GET /v1/memberships` · `PUT /v1/memberships/{id}` |

**Gráfico de barras:** Implementar en CSS puro (como el prototipo) — no añadir Chart.js a menos que el usuario lo pida.

### Entregable del día
- La edición inline de tarifas (categorías × estrellas) funciona y persiste
- Los 3 tabs de inscritos funcionan
- El módulo de pagos muestra datos reales

---

## Día 8 — Admin: Tokens + Configuración + PWA + SSR

**Pantallas de referencia:** `specs/admin-tokens.html`, `specs/configuracion.html`

### AdminTokensComponent — Flujo reactivo
```
Tokens pendientes: GET /v1/payments/beach/tokens?status=pendiente

Modal de Aprobación:
  POST /v1/payments/beach/tokens/{id}/approve
  Toast verde "Token enviado a {email}"
  Fila desaparece del listado
  Badge en sidebar se decrementa

Modal de Rechazo:
  POST /v1/payments/beach/tokens/{id}/reject { reason: "..." }
  Toast rojo
  Estado vacío cuando no quedan pendientes

Historial: GET /v1/payments/beach/tokens (todos)
```

### ConfiguracionComponent — 5 tabs
| Tab | Highlights de implementación |
|---|---|
| **General** | Formulario organización 2 cols + redes sociales |
| **Ranking** | Matriz 8×5 editable de puntos de liga + parámetros |
| **Integraciones** | Badge "Conectado" SurfScores. **Advertencia prominente** sobre prohibición Live Heatboard. `PUT /v1/circuits` para actualizar credenciales. Botón [Probar conexión]. |
| **Notificaciones** | Validez del token (hardcodeado: 24h según SDD). Emails admin. Toggle notificaciones. |
| **En Vivo** | YouTube embed con preview reactivo. SurfScores Live: slider intervalo (mín 5 min con warning). "Sitio público" bloqueado con tachado. |

### PWA final
```bash
# Verificar que ngsw-config.json esté correctamente configurado
# Añadir al manifest.webmanifest:
#   name: "ALAS Latin Tour"
#   short_name: "ALAS"
#   theme_color: "#002359"
#   background_color: "#002359"
#   display: "standalone"
```

### SSR — checklist de verificación
Para cada ruta pública, hacer `curl http://localhost:4000/[ruta]` y verificar:
- [ ] `<title>` correcto
- [ ] `<meta name="description">` presente
- [ ] `<meta property="og:*">` presentes
- [ ] `<link rel="canonical">` correcto
- [ ] La página no carga en blanco (sin JS habilitado)

### Build de producción
```bash
ng build --configuration production
# Verificar bundle size < 500KB initial
# Verificar que el server.ts compile sin errores
```

### Entregable del día
- Todos los módulos admin funcionan
- PWA instalable en móvil (lighthouse PWA score > 90)
- SSR verificado en todas las rutas públicas
- Build de producción sin errores

---

## Resumen de dependencias npm a instalar

```bash
# CSS
npm install -D tailwindcss @tailwindcss/forms @tailwindcss/typography postcss autoprefixer

# PWA
ng add @angular/pwa

# Opcional (solo si el usuario aprueba)
# npm install chart.js   → solo si la implementación CSS del gráfico no es suficiente
# npm install @angular/material → NO — el diseño usa Tailwind custom
```

> **No instalar** `@angular/material`, `ngx-charts`, ni otras librerías de UI — el diseño del prototipo usa Tailwind custom que hay que replicar exactamente.

---

## Convenciones de código a seguir

```typescript
// ✅ Usar Angular Signals (no BehaviorSubject para UI state)
items = signal<CircuitResponse[]>([]);
isLoading = signal(false);

// ✅ Usar nueva sintaxis de control flow
@if (isLoading()) { <app-loading-spinner /> }
@for (item of items(); track item.id) { ... }
@defer (on idle) { <heavy-component /> } loading { <skeleton /> }

// ✅ Input required moderno (Angular 17+)
eventId = input.required<string>();

// ✅ Inject function (no constructor injection)
private apiService = inject(ApiWrapperService);
private router = inject(Router);
```

---

## Referencia rápida: spec → ruta → componente

| Archivo spec | Ruta Angular | Componente |
|---|---|---|
| `index.html` | `/` | `HomeComponent` |
| `quienes-somos.html` | `/quienes-somos` | `QuienesSomosComponent` |
| `noticias.html` | `/noticias` | `NoticiasComponent` |
| `noticia-detalle.html` | `/noticias/:slug` | `NoticiaDetalleComponent` |
| `eventos.html` | `/eventos` | `EventosComponent` |
| `inscripcion.html` | `/inscripcion/:eventId` | `InscripcionComponent` |
| `pago-playa.html` | `/pago-playa/:inscriptionId` | `PagoPlayaComponent` |
| `admin-dashboard.html` | `/admin` | `AdminDashboardComponent` |
| `usuarios.html` | `/admin/usuarios` | `UsuariosComponent` |
| `circuitos.html` | `/admin/circuitos` | `CircuitosComponent` |
| `admin-eventos.html` | `/admin/eventos` | `AdminEventosComponent` |
| `categorias.html` | `/admin/categorias` | `CategoriasComponent` |
| `inscritos.html` | `/admin/inscritos` | `InscritosComponent` |
| `pagos.html` | `/admin/pagos` | `PagosComponent` |
| `admin-tokens.html` | `/admin/tokens` | `AdminTokensComponent` |
| `configuracion.html` | `/admin/configuracion` | `ConfiguracionComponent` |

---

*Proyecto: ALAS Latin Tour PWA — Frontend Angular 19*
