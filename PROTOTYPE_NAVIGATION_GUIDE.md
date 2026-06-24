# ALAS Latin Tour — Guía de Navegación del Prototipo
**Versión:** 1.0 · **Fecha:** 24 jun 2026  
**Stack del prototipo:** HTML puro + Tailwind CSS (CDN) + Alpine.js (CDN) — sin npm, sin build step.

---

## Cómo abrir el prototipo

### Opción A — Doble clic directo (más rápido)
1. Abre el Explorador de Windows y navega a `C:\repo\rtres-net\Alas\prototypes\`
2. Haz doble clic en `index.html`
3. Se abrirá en tu navegador por defecto (Chrome/Edge recomendados)

### Opción B — Servidor local (recomendado para Alpine.js + x-cloak)
```powershell
# Desde PowerShell, en la raíz del repo:
cd C:\repo\rtres-net\Alas\prototypes

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

FLUJO COMPETIDOR (requiere "Iniciar sesión")
├── eventos.html              → Listado de eventos + selección de categoría
│   └── → inscripcion.html   (botón "Inscribirse" en evento abierto)
├── inscripcion.html          → Checkout 3 pasos
│   └── → pago-playa.html    (si elige "Pago en Playa" en Paso 3)
└── pago-playa.html           → Máquina de estados del token

CONSOLA DE ADMINISTRACIÓN
├── admin-dashboard.html      → Dashboard principal con sidebar
│   └── → admin-tokens.html  (tarjeta "Tokens Pendientes" / sidebar)
└── admin-tokens.html         → Autorización de tokens + modales
```

---

## Guía de revisión pantalla por pantalla

### 1. `index.html` — Home Público
| Sección | Qué revisar |
|---|---|
| Hero | Tipografía Oswald masiva, gradiente oceánico, 2 CTAs |
| Próximas Paradas | Scroll horizontal en móvil, estrellas doradas, badges de estado |
| Ranking EN VIVO | Punto verde pulsante, tabla top 8, atribución "Results by SurfScores.com" |
| Últimas Noticias | Grid 3 col (desktop) / 1 col (móvil), tarjetas completas |
| Footer | Atribución SurfScores obligatoria, redes sociales |

**Interactividad:** Menú hamburguesa en móvil (< 1024px).

---

### 2. `quienes-somos.html` — Quiénes Somos
| Sección | Qué revisar |
|---|---|
| Historia | Layout 2 columnas, texto realista en español |
| Timeline | 5 hitos (2008–2024), hito 2024 destacado en naranja |
| Misión/Visión/Valores | 3 tarjetas con iconos SVG inline |
| Estadísticas | 4 contadores: 12 países, 340+ competidores, etc. |
| Equipo Directivo | Avatares de iniciales con gradiente |

---

### 3. `noticias.html` — Noticias y Fotos
| Sección | Qué revisar |
|---|---|
| Filtros por pestaña | Click en "Noticias" / "Resultados" / "Fotos" — pestaña activa con subrayado cian |
| Skeleton loaders | Click en **"Simular carga"** para ver la animación de esqueleto → contenido real |
| Galería | Grid tipo masonry, hover muestra caption con overlay |

**Interactividad:** Botón "Simular carga" alterna entre skeleton y contenido real (Alpine.js).

---

### 4. `noticia-detalle.html` — Detalle de Noticia
| Sección | Qué revisar |
|---|---|
| **Indicador SSR** (arriba del todo) | Caja azul de código con `<title>`, `<meta og:*>`, `<link rel="canonical">` — dismissible con ✕ |
| Cuerpo del artículo | Pull quote con borde naranja, foto con caption |
| **Caja SurfScores** | Tabla de resultados del heat dentro del artículo (obligatorio por contrato API) |
| Artículos relacionados | Grid de 3 tarjetas al final |

---

### 5. `eventos.html` — Selección de Eventos (Competidor)
| Sección | Qué revisar |
|---|---|
| Saludo personalizado | "Hola, Gabriel 👋" con ranking y puntos actuales |
| Tabs de circuito | ALAS Open / Junior / Masters — Alpine.js |
| Tarjetas de eventos | Estrellas, país, fechas, inscritos, botón de inscripción |
| **"Ver detalles"** | Click expande tabla de categorías y tarifas (Alpine.js x-show + x-collapse) |
| Eventos cerrados/completados | Botón deshabilitado visualmente |
| Panel "Mis Inscripciones" | Sidebar derecho (desktop) / sección colapsable (móvil) |

---

### 6. `inscripcion.html` — Checkout 3 Pasos
Flujo lineal. Usa los botones "Siguiente →" y "← Anterior".

| Paso | Qué revisar |
|---|---|
| **Paso 1 — Datos** | Campos pre-llenados (solo lectura), campo de camiseta editable, checkbox de reglamento obligatorio para avanzar |
| **Paso 2 — Categoría** | Tarjetas radio con borde cian al seleccionar, resumen de precio al pie |
| **Paso 3 — Pago** | 2 métodos (PayPal / Pago en Playa), advertencia naranja al elegir Playa |
| Redirección | "Confirmar y Pagar" con Pago en Playa → abre `pago-playa.html` |

**Indicador visual:** Círculos numerados (1–2–3) en la parte superior: activo=cian, completado=verde con ✓.

---

### 7. `pago-playa.html` — Flujo de Pago en Playa ⭐ PANTALLA CRÍTICA
Esta es la pantalla más interactiva. Tiene una **barra de estados** naranja en la parte superior para saltar entre estados durante la revisión.

| Estado | Cómo acceder | Qué revisar |
|---|---|---|
| **1. Solicitar código** | Botón "1. Request" en la barra | Explicación del proceso en 3 pasos, botón naranja grande |
| **2. Pendiente aprobación** | Botón "2. Pending" o hacer clic en "Solicitar Código" | Ícono pulsante, email del competidor, indicador de espera |
| **3. Ingresar token** | Botón "3. Token" o "[Simular aprobación]" en estado 2 | **CRONÓMETRO 20:00 en tiempo real**, campo monospace, pulso rojo < 3 min |
| **4. Token expirado** | Botón "4. Expired" o dejar que el cronómetro llegue a 0 | Ícono de error rojo, callout HTTP 400 (educativo para devs), botón Re-solicitar |
| **5. Inscripción confirmada** | Botón "5. Success" o confirmar un token en estado 3 | Badge amarillo "Estado financiero: Pago Pendiente ⚠️" (intencional — el pago es pendiente) |

> **Nota de negocio:** En el estado Success, la insignia "Pago Pendiente ⚠️" es **intencional y correcta** según el SDD. La inscripción se habilita, pero el estado financiero permanece pendiente hasta que el admin valide el efectivo en el evento.

---

### 8. `admin-dashboard.html` — Dashboard de Administración
Layout de sidebar. Acceso directo: abre el archivo en el navegador.

| Sección | Qué revisar |
|---|---|
| Sidebar | Logo ALAS Admin, links de navegación, badge naranja "3" en Tokens, info de usuario al pie |
| Tarjetas de estadísticas | 4 KPIs: Eventos activos, Inscripciones hoy, **Tokens Pendientes** (clickeable → tokens page), Recaudación |
| Tabla de Eventos | 6 eventos con estado, estrellas, botones Editar/Ver/Eliminar |
| Tabla de Circuitos | 3 circuitos 2026 |
| Últimos Inscritos | 8 filas con mix de estados de pago |

**Interactividad:** Sidebar colapsa en móvil con botón hamburguesa.

---

### 9. `admin-tokens.html` — Autorización de Tokens ⭐ PANTALLA CRÍTICA ADMIN
Accesible desde el Dashboard (tarjeta "Tokens Pendientes" o sidebar).

| Elemento | Qué revisar |
|---|---|
| Badge reactivo | El número en el sidebar (`3`) se reduce cada vez que se aprueba o rechaza |
| 3 solicitudes pendientes | Gabriel Villani 🇧🇷, Camila Restrepo 🇨🇴, Diego Núñez 🇲🇽 |
| **Modal de Aprobación** | Click en "Aprobar y Generar Token" → modal con token `A3K9-Z2MX` en monospace, botón "Confirmar y Enviar" |
| **Toast de confirmación** | Al confirmar: toast verde "✓ Token enviado a gabriel.villani@surf.com", fila desaparece de la tabla |
| **Modal de Rechazo** | Click en "Rechazar" → modal con textarea para motivo, toast rojo al confirmar |
| Estado vacío | Al aprobar/rechazar las 3 solicitudes, aparece estado vacío con checkmark verde |
| Tabla historial | 15 filas con tokens históricos: Usado ✓ / Expirado / Rechazado / Pendiente |

---

## Revisión en modo móvil

Para simular la vista PWA de un competidor en la playa:

1. Abre Chrome DevTools (`F12`)
2. Clic en el ícono de dispositivo móvil (toggle device toolbar) o `Ctrl+Shift+M`
3. Selecciona **"iPhone 14 Pro"** o **"Galaxy S23"** (375–390px ancho)
4. Las pantallas más importantes para probar en móvil: `pago-playa.html`, `inscripcion.html`, `eventos.html`

---

## Archivos del prototipo

```
C:\repo\rtres-net\Alas\prototypes\
├── index.html               Portal público — Home
├── quienes-somos.html       Portal público — Historia / Equipo
├── noticias.html            Portal público — Noticias y Fotos
├── noticia-detalle.html     Portal público — Artículo individual + SSR indicator
├── eventos.html             Competidor — Selección de evento y categoría
├── inscripcion.html         Competidor — Checkout 3 pasos
├── pago-playa.html          Competidor — Flujo token pago en playa (5 estados)
├── admin-dashboard.html     Admin CMS — Dashboard principal
└── admin-tokens.html        Admin CMS — Autorización de tokens
```

**Total:** 9 pantallas · ~4,400 líneas de HTML · 0 dependencias npm

---

## Notas técnicas para el equipo de desarrollo

| Elemento del prototipo | Equivalente en producción (Angular + .NET) |
|---|---|
| Alpine.js `x-data` components | Angular Components con `@Component` |
| `paymentFlow()` state machine | Angular service con BehaviorSubject + states enum |
| `tokensConsole()` reactive array | Angular component + SignalR hub para actualizaciones en tiempo real |
| Countdown timer `setInterval` | Angular `interval()` de RxJS + `takeUntil` |
| Tailwind CSS (CDN) | Tailwind instalado vía npm con purge en producción |
| Mock data inline | HTTP calls al BFF .NET → endpoints REST |
| Skeleton loaders | `@defer` blocks de Angular 17+ con `loading` template |
| Nav `[x-cloak]` | Angular structural directives con `*ngIf` o `@if` |
| SSR indicator box | `<head>` gestionado por Angular `Meta` y `Title` services + transferState |

---

*Prototipo generado para ALAS Latin Tour — Fase de Diseño UX/UI — Jun 2026*
