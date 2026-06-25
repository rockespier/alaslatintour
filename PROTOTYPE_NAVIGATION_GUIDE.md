# ALAS Latin Tour — Guía de Navegación del Prototipo
**Versión:** 2.0 · **Fecha:** 25 jun 2026  
**Stack del prototipo:** HTML puro + Tailwind CSS (CDN) + Alpine.js (CDN) — sin npm, sin build step.

---

## Cómo abrir el prototipo

### Opción A — Doble clic directo (más rápido)
1. Abre el Explorador de Windows y navega a `C:\repo\rtres-net\Alas\docs\`
2. Haz doble clic en `index.html`
3. Se abrirá en tu navegador por defecto (Chrome/Edge recomendados)

### Opción B — Servidor local (recomendado para Alpine.js + x-cloak)
```powershell
# Desde PowerShell, en la raíz del repo:
cd C:\repo\rtres-net\Alas\docs

# Si tienes Python 3:
python -m http.server 5500

# Si tienes Node.js (npx sin instalar):
npx serve .

# Si tienes VS Code: usa la extensión "Live Server" y click derecho > "Open with Live Server" en index.html
```
Luego abre: **http://localhost:5500/index.html**

> **Nota:** Con servidor local, las transiciones de Alpine.js y `x-cloak` funcionan perfectamente. Con doble clic también funciona, pero puede haber un destello de contenido sin clase `[x-cloak]`.

---

## Mapa de pantallas y rutas de navegación

```
PORTAL PÚBLICO
├── index.html                → Home (punto de entrada)
│   ├── → quienes-somos.html (nav link "Quiénes Somos")
│   ├── → noticias.html      (nav link "Noticias" + sección Últimas Noticias)
│   └── → eventos.html       (botón "Ver Calendario" / nav "Eventos")
│
├── quienes-somos.html        → Historia, Timeline, Equipo
├── noticias.html             → Grilla de noticias + Galería
│   └── → noticia-detalle.html (click en cualquier artículo)
└── noticia-detalle.html      → Artículo individual con indicador SSR

FLUJO COMPETIDOR (requiere hacer clic en "Iniciar sesión")
├── eventos.html              → Listado de eventos + selección de categoría
│   └── → inscripcion.html   (botón "Inscribirse" en evento abierto)
├── inscripcion.html          → Checkout 3 pasos
│   └── → pago-playa.html    (si elige "Pago en Playa" en Paso 3)
└── pago-playa.html           → Máquina de estados del token (5 estados)

CONSOLA DE ADMINISTRACIÓN (sidebar persistente en todas las páginas)
├── admin-dashboard.html      → Dashboard principal: KPIs, eventos, inscritos recientes
├── usuarios.html             → Usuarios · Roles y Perfiles · Permisos
├── circuitos.html            → Gestión de circuitos + sincronización SurfScores
├── admin-eventos.html        → Gestión de eventos (admin) + categorías y tarifas por evento
├── categorias.html           → Categorías + matriz de tarifas USD/COP por estrellas
├── inscritos.html            → Inscritos · Puntajes de Premios · Puestos por Evento
├── pagos.html                → Recaudación: resumen · inscripciones · membresías
├── admin-tokens.html         → Autorización de tokens de pago en playa
└── configuracion.html        → General · Ranking · Integraciones API · Notificaciones · En Vivo
```

---

## Guía de revisión pantalla por pantalla

### 1. `index.html` — Home Público
| Sección | Qué revisar |
|---|---|
| Hero | Tipografía Oswald masiva, gradiente oceánico, 2 CTAs |
| Próximas Paradas | Scroll horizontal en móvil, estrellas, badges de estado |
| Ranking EN VIVO | Punto verde pulsante, tabla top 8, atribución "Results by SurfScores.com" |
| Últimas Noticias | Grid 3 col (desktop) / 1 col (móvil), tarjetas completas |
| Footer | Atribución SurfScores obligatoria, redes sociales |

**Interactividad:** Menú hamburguesa en móvil (< 1024px).

---

### 2. `quienes-somos.html` — Quiénes Somos
| Sección | Qué revisar |
|---|---|
| Historia | Layout 2 columnas, texto realista en español |
| Timeline | 5 hitos (2008–2024), hito 2024 destacado |
| Misión/Visión/Valores | 3 tarjetas con iconos SVG inline |
| Estadísticas | 4 contadores: 12 países, 340+ competidores, etc. |
| Equipo Directivo | Avatares de iniciales con gradiente |

---

### 3. `noticias.html` — Noticias y Fotos
| Sección | Qué revisar |
|---|---|
| Filtros por pestaña | Click en "Noticias" / "Resultados" / "Fotos" — pestaña activa con subrayado |
| Skeleton loaders | Click en **"Simular carga"** para ver la animación de esqueleto → contenido real |
| Galería | Grid tipo masonry, hover muestra caption con overlay |

---

### 4. `noticia-detalle.html` — Detalle de Noticia
| Sección | Qué revisar |
|---|---|
| **Indicador SSR** (arriba del todo) | Caja de código con `<title>`, `<meta og:*>`, `<link rel="canonical">` — dismissible con ✕ |
| Cuerpo del artículo | Pull quote, foto con caption |
| **Caja SurfScores** | Tabla de resultados del heat dentro del artículo (obligatorio por contrato API) |
| Artículos relacionados | Grid de 3 tarjetas al final |

---

### 5. `eventos.html` — Selección de Eventos (Competidor)
| Sección | Qué revisar |
|---|---|
| Saludo personalizado | "Hola, Gabriel 👋" con ranking y puntos actuales |
| Tabs de circuito | ALAS Open / Junior / Masters — Alpine.js |
| Tarjetas de eventos | Estrellas, país, fechas, inscritos, botón de inscripción |
| **"Ver detalles"** | Click expande tabla de categorías y tarifas (Alpine.js x-show) |
| Eventos cerrados/completados | Botón deshabilitado visualmente |
| Panel "Mis Inscripciones" | Sidebar derecho (desktop) / sección colapsable (móvil) |

---

### 6. `inscripcion.html` — Checkout 3 Pasos
Flujo lineal. Usa los botones "Siguiente →" y "← Anterior".

| Paso | Qué revisar |
|---|---|
| **Paso 1 — Datos** | Campos pre-llenados (solo lectura), campo de camiseta editable, checkbox de reglamento obligatorio para avanzar |
| **Paso 2 — Categoría** | Tarjetas radio con borde activo al seleccionar, resumen de precio al pie |
| **Paso 3 — Pago** | 2 métodos (PayPal / Pago en Playa), advertencia al elegir Playa |
| Redirección | "Confirmar y Pagar" con Pago en Playa → abre `pago-playa.html` |

**Indicador visual:** Círculos numerados (1–2–3): activo=azul, completado=verde con ✓.

---

### 7. `pago-playa.html` — Flujo de Pago en Playa ⭐ PANTALLA CRÍTICA
Tiene una **barra de estados** en la parte superior para saltar entre estados durante la revisión.

| Estado | Cómo acceder | Qué revisar |
|---|---|---|
| **1. Solicitar código** | Botón "1. Request" en la barra | Explicación del proceso en 3 pasos, botón principal |
| **2. Pendiente aprobación** | Botón "2. Pending" o clic en "Solicitar Código" | Ícono pulsante, email del competidor, indicador de espera |
| **3. Ingresar token** | Botón "3. Token" o "[Simular aprobación]" en estado 2 | **CRONÓMETRO 20:00 en tiempo real**, campo monospace, pulso rojo < 3 min |
| **4. Token expirado** | Botón "4. Expired" o dejar que el cronómetro llegue a 0 | Ícono de error, callout HTTP 400 (educativo para devs), botón Re-solicitar |
| **5. Inscripción confirmada** | Botón "5. Success" o confirmar un token en estado 3 | Badge "Estado financiero: Pago Pendiente ⚠️" (intencional) |

> **Nota de negocio:** En el estado Success, "Pago Pendiente ⚠️" es **intencional y correcto** según el SDD. La inscripción se habilita, pero el estado financiero permanece pendiente hasta que el admin valide el efectivo en el evento.

---

### 8. `admin-dashboard.html` — Dashboard de Administración
| Sección | Qué revisar |
|---|---|
| Sidebar | Logo ALAS Admin, 9 links de navegación, badge "3" en Tokens, info de usuario al pie |
| Tarjetas de estadísticas | 4 KPIs: Eventos activos, Inscripciones hoy, **Tokens Pendientes** (clickeable), Recaudación |
| Tabla de Eventos | 6 eventos con estado, estrellas, botones Editar/Ver/Eliminar |
| Tabla de Circuitos | 3 circuitos 2026 |
| Últimos Inscritos | 8 filas con mix de estados de pago |

**Interactividad:** Sidebar colapsa en móvil con botón hamburguesa.

---

### 9. `usuarios.html` — Usuarios, Roles y Permisos
| Tab | Qué revisar |
|---|---|
| **Usuarios** | Tabla de 8 usuarios con avatares de iniciales, roles coloreados, última sesión, estado activo/inactivo. Botones [Editar] y [Activar/Desactivar]. Modal de creación con campo de invitación por email. |
| **Roles y Perfiles** | 4 tarjetas: Super Admin (1 usuario), Admin (3), Árbitro (3), Revisor (2). Descripción de alcance y botón [Editar Rol]. |
| **Permisos** | Matriz 4×9: roles en filas, módulos en columnas. ✓ acceso total, ◑ solo lectura, — sin acceso. No editable en prototipo (referencia visual). |

---

### 10. `circuitos.html` — Gestión de Circuitos
| Sección | Qué revisar |
|---|---|
| Stats (3 cards) | Circuitos activos, total eventos, total competidores |
| Tabla de circuitos | 4 filas: 3 activos 2026 + 1 archivado 2025, con código SurfScores visible |
| **Fila expandible** | Click en [Ver] → panel inline con sub-tabla de eventos del circuito + panel de sincronización API SurfScores con timestamp de última sync |
| **Modal Nuevo/Editar** | Campos: Nombre, Temporada, Descripción, Región, Modalidad, Código SurfScores. Incluye advertencia sobre sensibilidad del código SurfScores. |

---

### 11. `admin-eventos.html` — Gestión de Eventos (Admin)
> Nota: esta es la vista de administración de eventos. `eventos.html` es la vista pública del competidor.

| Sección | Qué revisar |
|---|---|
| Stats (4 cards) | Activos, Próximos, Completados, Inscripciones abiertas |
| Barra de filtros | Búsqueda + filtro por circuito, estado y país |
| Tabla de eventos | 6 filas con barras de progreso de capacidad (47/80), flags de país, estrellas |
| Botón [Ver Inscritos] | Navega a `inscritos.html` |
| **Modal Nuevo/Editar** — Tab "Datos" | Nombre, país, circuito, fechas, capacidad, estrellas, código SurfScores del evento |
| **Modal** — Tab "Categorías" | Checkboxes para habilitar/deshabilitar categorías por evento |
| **Modal** — Tab "Tarifas" | Toggle "Usar tarifas del circuito" o definir precios custom por categoría para este evento |

---

### 12. `categorias.html` — Categorías y Tarifas
| Sección | Qué revisar |
|---|---|
| **Panel izquierdo** | Lista de 6 categorías con punto de color, ícono de género, rango de edad. Click en una categoría la selecciona. |
| **Panel derecho** | Título dinámico "Tarifas para: [Categoría seleccionada]". Tabla 5 filas (1★ a 5★) × 2 columnas (USD / COP). |
| **Edición inline** | Click en un valor de tarifa → se convierte en `<input>` editable. Botones Guardar/Cancelar por fila. |
| **Modal Nueva Categoría** | Nombre, descripción, género (radio), restricción de edad (toggle + campos min/max), estado |

---

### 13. `inscritos.html` — Inscritos, Premios y Puestos ⭐ PANTALLA CRÍTICA ADMIN
| Tab | Qué revisar |
|---|---|
| **Inscritos** | Barra de filtros (evento, categoría, estado de pago, búsqueda). Tabla 15 filas con mix de estados: PayPal ✓, Efectivo ✓, Efectivo ⏳ Pendiente. [Exportar CSV] button. |
| **Fila expandible** | Click [Ver detalle] → panel con datos completos, ID transacción o código token, campo de notas editable, botón "Confirmar Pago en Playa" para pendientes. |
| **Puntajes de Premios** | Matriz editable 7×5 (puestos × estrellas). Click en celda → input editable. Botón "Guardar Distribución". |
| **Puestos por Evento** | Selector Evento + Categoría. Pódium CSS (bloques oro/plata/bronce) con nombre y score. Tabla completa de posiciones debajo. Datos de "Las Palmas Open" pre-cargados. |

---

### 14. `pagos.html` — Recaudación
| Tab | Qué revisar |
|---|---|
| **Resumen** | 4 KPI cards con variación (↑12% vs mes anterior). Gráfico de barras CSS con 6 meses (Ene–Jun), barra Jun destacada. Tabla de últimas 10 transacciones. |
| **Inscripciones** | Filtros por evento/método/estado/fecha. Totales por método de pago. Modal "Validar Pago" para filas pendientes. |
| **Membresías** | Tabla de 3 clubes/federaciones. Fila "Federación Surf Chile" con badge "Vence pronto ⚠️". Botón [Renovar] que abre modal de confirmación. |

---

### 15. `admin-tokens.html` — Autorización de Tokens ⭐ PANTALLA CRÍTICA ADMIN
| Elemento | Qué revisar |
|---|---|
| Badge reactivo | El número en el sidebar (`3`) se reduce cada vez que se aprueba o rechaza |
| 3 solicitudes pendientes | Gabriel Villani 🇧🇷, Camila Restrepo 🇨🇴, Diego Núñez 🇲🇽 |
| **Modal de Aprobación** | Click en "Aprobar y Generar Token" → modal con token `A3K9-Z2MX` en monospace, botón "Confirmar y Enviar" |
| **Toast de confirmación** | Toast verde "✓ Token enviado a gabriel.villani@surf.com", fila desaparece de la tabla |
| **Modal de Rechazo** | Textarea para motivo, toast rojo al confirmar |
| Estado vacío | Al procesar las 3 solicitudes, aparece estado vacío con checkmark verde |
| Tabla historial | 15 filas: Usado ✓ / Expirado / Rechazado / Pendiente |

---

### 16. `configuracion.html` — Configuración del Sistema
| Tab | Qué revisar |
|---|---|
| **General** | Formulario de datos de la organización (2 columnas), redes sociales con iconos, selector de temporada activa y fechas. Botón "Guardar cambios". |
| **Ranking** | Matriz editable 8×5 (puestos × estrellas) de puntos de liga. Parámetros: N mejores resultados a contar (valor: 5), penalización DNS/DSQ. |
| **Integraciones** | **SurfScores:** badge "Conectado", credenciales enmascaradas, caché (5 min), checkbox de aceptación de política, botones [Probar conexión] / [Sincronizar ahora]. **⚠️ Advertencia:** "La API SurfScores prohíbe el uso como marcador en tiempo real (Live Heatboard). Polling excesivo puede causar bloqueo de IP." **WordPress:** endpoint headless, API key enmascarada. |
| **Notificaciones** | Validez del token (20 min — marcado como crítico), emails de admin, plantilla editable del correo al competidor, toggles de notificaciones. |
| **En Vivo** | **YouTube Streaming:** selector de evento, campo de Video ID/URL, radio de privacidad (Público / No listado), dimensiones personalizables, textarea con código iframe reactivo (se actualiza al escribir), preview visual con simulador de reproductor y badge "EN VIVO" animado, toggle de activación con punto rojo pulsante. **SurfScores Live Scores:** advertencia prominente sobre prohibición de uso como Live Heatboard público, campo de URL de embed, slider de intervalo de actualización (mín. 5 min — con advertencia si se baja de ese umbral), checkboxes de uso donde "Sitio público" aparece bloqueado con tachado, textarea con código iframe reactivo + comentario de atribución obligatoria `Results by SurfScores.com`, preview de marcador de calor simulado con atribución visible. |

---

## Revisión en modo móvil

Para simular la vista PWA de un competidor en la playa:

1. Abre Chrome DevTools (`F12`)
2. Clic en el ícono de dispositivo móvil o `Ctrl+Shift+M`
3. Selecciona **"iPhone 14 Pro"** o **"Galaxy S23"** (375–390px ancho)
4. Pantallas prioritarias en móvil: `pago-playa.html`, `inscripcion.html`, `eventos.html`

Para las páginas de admin en móvil:
- El sidebar se colapsa → se abre con el botón ☰ en la esquina superior izquierda
- Las tablas tienen scroll horizontal (`overflow-x-auto`)
- En `categorias.html` el panel doble pasa a layout vertical

---

## Inventario completo de archivos

```
C:\repo\rtres-net\Alas\docs\
│
│  PORTAL PÚBLICO (4 pantallas)
├── index.html               Home: hero, ranking EN VIVO, próximas paradas, noticias
├── quienes-somos.html       Historia, timeline, misión/visión, equipo directivo
├── noticias.html            Grilla noticias + galería + skeleton loaders
├── noticia-detalle.html     Artículo + indicador SSR + tabla resultados SurfScores
│
│  FLUJO COMPETIDOR (3 pantallas)
├── eventos.html             Selección de evento y categoría (vista competidor logueado)
├── inscripcion.html         Checkout 3 pasos (Datos → Categoría → Pago)
├── pago-playa.html          Token de pago en playa — máquina de 5 estados
│
│  CONSOLA DE ADMINISTRACIÓN (9 pantallas)
├── admin-dashboard.html     Dashboard principal con KPIs y tablas resumen
├── usuarios.html            Usuarios · Roles · Matriz de permisos
├── circuitos.html           Gestión de circuitos + integración SurfScores
├── admin-eventos.html       Gestión de eventos (admin) + categorías + tarifas
├── categorias.html          Categorías + tarifas USD/COP editables por estrellas
├── inscritos.html           Inscritos · Puntajes de premios · Puestos por evento
├── pagos.html               Recaudación: inscripciones y membresías
├── admin-tokens.html        Autorización de tokens de pago en playa
└── configuracion.html       General · Ranking · APIs · Notificaciones · En Vivo
│
│  ASSETS
└── assets/
    ├── logos/
    │   ├── logo-pro-tour-white-2x.png   (nav y sidebar — fondo oscuro)
    │   └── new-logo-blue-4x.png         (uso en fondos claros)
    └── css/
        └── alaslatintour-com-color-palette-hex.css
```

**Total:** 16 pantallas · ~9,100 líneas de HTML · 0 dependencias npm

---

## Paleta de colores corporativa aplicada

| Token | Hex | Uso |
|---|---|---|
| `navy-deepest` | `#002359` | Fondo de página, sidebar, scrollbar track |
| `navy-dark` | `#003873` | Fondo de tarjetas y paneles |
| `navy-mid` | `#004F8E` | Bordes, hover estructural |
| `cyan-brand` | `#0081C6` | Acento primario, links activos, sidebar activo |
| `cyan-dark` | `#007AFF` | Hover sobre acento |
| `orange-brand` | `#007AFF` | Botones CTA principales |
| `orange-light` | `#0D6EFD` | Hover sobre CTA |
| `text-light` | `#EEEEEE` | Texto principal |
| `text-muted` | `#AAAAAA` | Texto secundario, labels |
| `success-brand` | `#22C55E` | Badges de éxito, estados pagados |
| `warning-brand` | `#FBBF24` | Alertas de negocio (pago pendiente) |
| `error-brand` | `#EF4444` | Errores, token expirado, rechazos |

---

## Notas técnicas para el equipo de desarrollo

| Elemento del prototipo | Equivalente en producción (Angular + .NET) |
|---|---|
| Alpine.js `x-data` components | Angular Components con `@Component` + Signals |
| `paymentFlow()` state machine | Angular service con `signal<PaymentState>` o BehaviorSubject |
| `tokensConsole()` reactive array | Angular component + SignalR hub para actualizaciones en tiempo real |
| Countdown timer `setInterval` | Angular `interval()` de RxJS + `takeUntil(destroy$)` |
| Inline editable cells (`editing` flag) | Angular `[(ngModel)]` con `contenteditable` o `mat-cell-edit` |
| Permission matrix (visual) | Angular `PermissionsGuard` + role-based `*ngIf` / `@if` |
| Tariff matrix editable | Angular Reactive Form con `FormArray` de `FormGroup` por fila |
| CSS bar chart en `pagos.html` | Angular + ngx-charts o Chart.js con datos del BFF |
| Tailwind CSS (CDN) | Tailwind instalado vía npm con `content` purge en producción |
| Mock data inline | HTTP calls al BFF .NET → endpoints REST de la Clean Architecture |
| Skeleton loaders | `@defer` blocks de Angular 17+ con `loading` template |
| SSR indicator box | `<head>` gestionado por Angular `Meta` y `Title` services + `TransferState` |
| SurfScores cache slider | Configuración persistida en .NET `appsettings.json` + endpoint admin |
| Token validity (20 min) | Constante en `TokenService.cs` del BFF, configurable desde la base de datos |

---

*Prototipo generado para ALAS Latin Tour — Fase de Diseño UX/UI — Jun 2026*
