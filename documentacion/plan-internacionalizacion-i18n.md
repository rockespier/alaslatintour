# Plan: Sitio ALAS Latin Tour en Español / Inglés / Portugués

> **Estado**: plan aprobado para documentar, pendiente de ejecución. No implementar hasta indicación explícita.

## Contexto

El frontend Angular (19.2, standalone, SSR obligatorio por CLAUDE.md) está hoy 100% en español: todo el texto vive inline en los `template:` de cada `@Component` (no hay `.html` separados), no existe ninguna librería de i18n, no hay `Intl`/`DatePipe`/`CurrencyPipe`, y las etiquetas de enums (género, estados, etc.) llegan del backend como strings literales en español con tildes/espacios (ej. `"Pendiente de validación"`, `"América del Sur"`) horneados en el contrato NSwag — no se pueden cambiar sin romper la API. El deploy es un único proceso Express (`server.mjs`) detrás de IIS (`deploy/web.config`, sin Docker), lo que descarta el enfoque nativo `@angular/localize` (exige un build/dist separado por idioma).

Decisiones ya confirmadas con el usuario:
- **Alcance**: sitio público + panel del competidor. El CMS/admin (`features/admin/**`) queda en español únicamente, sin tocar.
- **URLs**: prefijo de ruta (`/en/...`, `/pt/...`); español sin prefijo en la raíz (preserva URLs indexadas hoy, `hreflang="x-default"` = español).
- **Mensajes de error/validación del backend** (341 puntos en `AlasApp.Application`/`Domain` que lanzan excepciones en español, mostrados tal cual por `ExceptionHandlingMiddleware.cs`): **fuera de este plan**, se documentan como limitación conocida para una fase futura.
- **Contenido traducido**: se redactan traducciones borrador (calidad IA) a EN/PT como parte de esta implementación, marcadas para revisión de un hablante nativo antes de publicar — especialmente el texto histórico de "Quiénes Somos" (~2000 palabras, migrado recientemente desde `nosotros.asp`).
- **Emails transaccionales**: sí se traducen, usando el campo `IdiomaPreferido` (`PreferredLanguage`, ya capturado en el registro pero sin uso hoy) — pero **solo los 3 templates que van al competidor**, no los que van a staff interno de ALAS (ver corrección de alcance abajo).

## Corrección de alcance verificada (importante)

De los "6 templates de email" identificados inicialmente, solo **3 van al competidor** (se traducen); los otros 3 van a staff interno de ALAS o son admin-only (quedan en español, sin tocar):

| Handler | Destinatario | ¿Traducir? |
|---|---|---|
| `Auth/Commands/RequestPasswordReset/RequestPasswordResetCommandHandler.cs` | el `UserAccount` que pide el reset | ✅ sí |
| `Payments/Commands/ApproveBeachToken/ApproveBeachTokenCommandHandler.cs` → `SendApprovalEmailAsync` | `token.CompetitorEmail` | ✅ sí |
| `Inscriptions/Commands/UpdateInscription/UpdateInscriptionCommandHandler.cs` → `NotifyPaymentConfirmedAsync` | `competitor.Email` | ✅ sí |
| `Payments/Commands/RequestBeachToken/RequestBeachTokenCommandHandler.cs` → `NotifyAdminsAsync` | `userAccountRepository.ListAdminEmailsByPermissionAsync(...)` — staff interno (verificado en código) | ❌ no |
| `Inscriptions/Commands/CreateInscription/CreateInscriptionCommandHandler.cs` → `NotifyAdminAsync` | `settings.Notifications.AdminEmail` — staff interno | ❌ no |
| `AdminUsers/Commands/CreateAdminUser/CreateAdminUserCommandHandler.cs` | invitación de admin | ❌ no (admin, fuera de alcance) |

`IdiomaPreferido` vive en `UserAccount.IdiomaPreferido` (`backend/src/AlasApp.Domain/Entities/UserAccount.cs:62`), no en `Competitor`. `IUserAccountRepository` ya expone `GetByEmailAsync` y `GetByCompetitorIdAsync` (verificado en `backend/src/AlasApp.Application/Abstractions/Persistence/IUserAccountRepository.cs`) — suficiente para resolver el idioma en los 3 handlers; `ApproveBeachTokenCommandHandler` y `UpdateInscriptionCommandHandler` no inyectan hoy `IUserAccountRepository` y necesitan agregarlo al constructor (rompe tests existentes que instancian el handler directamente — corregirlos es parte del trabajo).

## 1. Librería: `@jsverse/transloco`, no `@angular/localize`

`@angular/localize` compila un bundle/dist separado por idioma — inviable de forma económica contra el único proceso Express detrás de IIS que existe hoy (multiplicaría procesos/memoria/artefactos de build). `@jsverse/transloco` carga JSON en runtime (un solo build), tiene soporte oficial para Angular 19 standalone (`provideTransloco(...)` compone con `mergeApplicationConfig` igual que `app.config.server.ts` hoy) y SSR.

**Riesgo a verificar temprano (spike, no asumir resuelto)**: evitar el doble-fetch de traducciones (SSR las carga para renderizar, el cliente no debe re-pedirlas al hidratar). La app ya usa `provideHttpClient(withFetch())` (confirmado en `app.config.ts:19`, comentario existente explica que es justamente para que SSR espere las requests) + `provideClientHydration(withEventReplay())` (línea 16) — el **HTTP TransferState cache nativo de Angular** debería interceptar el `HttpClient.get()` que hace el loader de Transloco sin configuración adicional de Transloco, siempre que las URLs de SSR y cliente coincidan exactamente. Esto se verifica con curl + Network tab en la Fase 0 (spike) antes de tocar los 28 componentes.

## 2. Ruteo: prefijo `:lang` sin duplicar rutas

`frontend/src/app/app.routes.ts` hoy es un solo array (`routes`). Se reestructura así, **sin duplicar** los `children` existentes:

```ts
const appChildren: Routes = [ /* todo el contenido actual de routes.ts, MENOS el bloque 'admin' */ ];

function langPrefixMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length > 0 && ['en', 'pt'].includes(segments[0].path)) {
    return { consumed: [segments[0]], posParams: { lang: segments[0] } };
  }
  return null;
}

export const routes: Routes = [
  { matcher: langPrefixMatcher, canActivate: [langGuard], children: appChildren },
  { path: '', canActivate: [langGuard], children: appChildren }, // default 'es'
  { path: 'admin', canActivate: [adminGuard], /* ...bloque admin actual, tal cual, sin prefijo de idioma... */ },
  { path: '**', redirectTo: '' },
];
```

- `admin` se saca del árbol compartido y queda como rama top-level propia (sin matcher de idioma) — así `/en/admin` simplemente no existe (correcto: admin es español-only).
- `langGuard` (nuevo, `core/guards/lang.guard.ts`) lee `route.params['lang']` (undefined → `'es'`), valida contra `['es','en','pt']` y llama a `LanguageService.setActiveLang(lang)` antes de activar los hijos. Un matcher inválido (`/xx/...`) no matchea ninguna rama y cae al fallback `**` → redirect a home en español (comportamiento aceptable, sin lógica especial).
- `frontend/src/app/app.routes.server.ts` **no cambia** — sigue siendo el catch-all `RenderMode.Server`, funciona igual sin importar cuántos segmentos matchee el router del lado cliente.
- **Selector de idioma** (`shared/components/language-switcher/`, nuevo): usa `router.parseUrl(router.url)`, empalma el primer segmento (quita/pone `en`/`pt`), navega con `router.navigateByUrl(tree)` — preserva la ruta actual completa (params dinámicos como `:slug`, query params, fragment) en vez de rebotar a home. Se agrega a `navbar.component.ts` y `footer.component.ts`.

## 3. SSR: `<html lang>` y sin lógica nueva en `server.ts`

Como el idioma es 100% estado de ruta (prefijo de URL, no negociación por `Accept-Language`), `src/server.ts` no necesita parseo manual — `AngularNodeAppEngine.handle()` ya resuelve la ruta (y por tanto el idioma) igual que en el cliente. Lo único que falta es el `<html lang>` (Angular `Meta`/`Title` no tocan el tag `<html>`): un `HtmlLangService` (`core/services/`) con un `effect(() => document.documentElement.lang = languageService.activeLang())`, inyectado una vez en el componente raíz — corre igual en SSR (DOM de `@angular/platform-server`) y en cliente.

`app.config.ts` gana `provideTransloco({ config: { availableLangs: ['es','en','pt'], defaultLang: 'es', fallbackLang: 'es', reRenderOnLangChange: true }, loader: TranslocoHttpLoader })`; `app.config.server.ts` no necesita nada específico de Transloco (el loader usa `HttpClient`, ya SSR-safe).

## 4. Archivos de traducción y tabla de enums

`frontend/src/assets/i18n/{es,en,pt}/{scope}.json`, alineado a los límites de rutas lazy ya existentes:
- `shared.json` — navbar, footer, language-switcher, status-badge (chrome no-enum), paginación.
- `public.json` — home, noticias, noticia-detalle, ranking, calendario, galeria-detalle.
- `quienes-somos.json` — scope propio (aislado porque es, por lejos, la página más pesada en texto; no debe inflar el bundle de las demás páginas públicas).
- `auth.json` — login, registro (incluye mensajes de validación hoy hardcodeados: "Requerido", "Mínimo 8 caracteres"), recuperar/restablecer password.
- `competitor.json` — eventos, inscripcion, pago-playa, paypal-return, mi-panel/*.
- `enums.json` — tabla `{ tabla: { valorEsDelWire: { es, en, pt } } }`.

**La clave de `enums.json` es el valor literal que manda el backend por el wire** (con espacios/tildes tal cual, ej. `"América del Sur"`, no `América_del_Sur`) — verificado leyendo `backend/src/AlasApp.Api/Controllers/GeneratedControllers.cs` (atributos `[EnumMember(Value = @"...")]`). El inventario confirmado incluye, entre otros: `CircuitStatus`, `CircuitRegion`, `EventStatusPublic`, `LicenseStatus`, `CategoryGender` (`Masculino`/`Femenino`/`Ambos`), `PaymentMethod` (`paypal`/`beach`, minúsculas), `InscriptionStatus` (`confirmado`/`pendiente`/`completado`, minúsculas), `TokenHistoryStatus`, `AdminRole` (`Super Admin`/`Admin`/`Árbitro`/`Revisor` — admin, no se traduce si esas pantallas quedan fuera de alcance), `MembershipType`. **La lista completa hay que auditarla contra el archivo generado antes de redactar el JSON** — hay más enums de los mapeados en la exploración inicial y hay que filtrar cuáles son realmente visibles en pantallas públicas/competidor (no en admin).

Pipe nuevo `shared/pipes/enum-label.pipe.ts` (`impure: true` para reaccionar al cambio de idioma): `{{ circuit.status | enumLabel:'circuitStatus' }}`, con fallback al valor crudo si la clave no existe en la tabla (evita romper si el backend agrega un valor nuevo sin que alguien actualice el JSON). `status-badge.component.ts` gana un `input()` de nombre de tabla, ya que hoy es genérico y reutilizado para distintos tipos de estado.

**Riesgo de mantenimiento a documentar explícitamente**: no hay ningún vínculo automático entre un enum nuevo en el backend y `enums.json` — cada vez que se agregue un valor de enum habrá que actualizarlo a mano en los 3 idiomas. Vale la pena, como fast-follow (no en este plan), un test backend que falle si `enums.es.json` no cubre todos los `EnumMember` de los enums en-alcance.

## 5. Patrón de conversión de componentes

Un solo patrón, aplicado uniformemente — no se enumeran los ~24 componentes en-alcance uno por uno (7 públicos + 8 competidor + 4 auth + 9 shared, admin excluido). Ejemplo representativo con `quienes-somos.component.ts` (ya migrado con texto real en la tarea anterior):

- Cada string del template pasa a `{{ 'quienesSomos.hero.intro' | transloco }}`. Párrafos largos (como el texto histórico) **no se fragmentan por oración** — cada `<p>` es una clave con el párrafo completo, para no romper la fluidez de la traducción.
- `imports: [TranslocoModule]`, `providers: [provideTranslocoScope('quienes-somos')]` para carga lazy del scope junto con el componente.
- El patrón hoy repetido de `inject(Meta)`/`inject(Title)` + `ngOnInit` (visto en `quienes-somos.component.ts`) se centraliza en un `SeoService` nuevo (`core/services/seo.service.ts`): `setPageMeta({ titleKey, descriptionKey, routePath })` resuelve las claves vía `TranslocoService.translate()`, llama `title.setTitle()`/`meta.updateTag(...)` como hoy, y además agrega los 4 `<link rel="alternate" hreflang="...">` (es, en, pt, x-default=es) manipulando `document.head` directamente (Angular `Meta` no gestiona `<link>`).

Archivos representativos por scope (patrón se repite, no exhaustivo): `shared/components/navbar/navbar.component.ts`, `footer/footer.component.ts`, `status-badge/status-badge.component.ts`; `features/public/home/home.component.ts`, `noticias/noticias.component.ts`, `ranking/ranking.component.ts`; `features/auth/login/login.component.ts`, `registro/registro.component.ts`; `features/competitor/inscripcion/inscripcion.component.ts`, `pago-playa/pago-playa.component.ts`, `mi-panel/datos-personales/datos-personales.component.ts`.

## 6. Fechas y moneda

Reemplazar los `toLocaleDateString('es', ...)` / `toLocaleString('en-US')` hardcodeados en `pago-playa.component.ts`, `inscripcion.component.ts`, `eventos.component.ts` (los equivalentes en admin quedan igual). En vez de depender de `LOCALE_ID` (que Angular resuelve una sola vez por injector y no reacciona bien a un cambio de idioma en runtime sin recargar), un `LocaleFormatService` (`core/services/locale-format.service.ts`) envuelve `formatDate()`/`formatCurrency()` de `@angular/common`, leyendo el idioma activo en cada llamada. Portugués: registrar `pt-BR` (no `pt-PT`) vía `registerLocaleData(localePtBR, 'pt-BR')` — es el mercado relevante para un tour de LatAm. La moneda sigue siendo USD en los 3 idiomas (la organización opera en USD); solo cambia el formato de separadores y las etiquetas de texto alrededor ("Monto"/"Amount"/"Valor").

## 7. Backend: WordPress con idioma (Noticias/Fotos)

`backend/src/AlasApp.Infrastructure/WordPress/WordPressService.cs` y `WordPressMediaService.cs` no pasan ningún parámetro de idioma hoy. Cambio aditivo (no rompe nada existente):
- `ArticleListFilter` (y el filtro equivalente de galerías) gana un `string? Lang` opcional; cuando viene, se agrega `?lang=xx` a la query hacia WordPress (convención estándar de WPML/Polylang, confirmado que están activos en el WP del cliente).
- Los componentes Angular `noticias`/`noticia-detalle`/`galeria-detalle` pasan el idioma activo de Transloco al llamar al endpoint.
- Sin lógica de fallback nueva en C#: WPML/Polylang ya resuelven en WordPress qué hacer si falta la traducción (mostrar el contenido en el idioma default o 404) — confirmar con el administrador de WP qué modo está configurado antes de publicar, pero es una tarea operativa, no de código.

## 8. Backend: localización de los 3 emails al competidor

`backend/src/AlasApp.Application/Emails/TransactionalEmailTemplate.cs` gana un parámetro `lang` (`"es"|"en"|"pt"`) que determina el `<html lang="...">` del template y el texto fijo de chrome ("Notificacion oficial" → "Official notification" → "Notificação oficial"). Todos los call sites existentes (incluidos los 3 que quedan en español) pasan `lang` explícito.

En los 3 handlers en-alcance (`RequestPasswordResetCommandHandler`, `ApproveBeachTokenCommandHandler`, `UpdateInscriptionCommandHandler`):
- Agregar `IUserAccountRepository` al constructor donde falte (`ApproveBeachToken`, `UpdateInscription`).
- Resolver la cuenta (`GetByEmailAsync`/`GetByCompetitorIdAsync`) y mapear `IdiomaPreferido` → `"es"/"en"/"pt"` con un único helper compartido (`PreferredLanguageMapper`, evita duplicar el switch en 3 lugares).
- Un `Copy` estático por handler con el texto fijo en los 3 idiomas (proporcional a 3 templates — **no** se construye infraestructura genérica de `.resx`/`IStringLocalizer`, sería desproporcionado para este alcance).

**Limitación a documentar para el product owner**: en `ApproveBeachTokenCommandHandler`, el cuerpo real de instrucciones del token (`settings.Notifications.CompetitorTokenEmailTemplate`) es texto libre editable por el admin en `AdminSettings` — **queda en español** salvo que el CMS admin gane un campo de plantilla por idioma (fuera de este plan). Solo se traduce el chrome fijo alrededor (asunto, labels del `Render()`).

## 9. Fases (orden de dependencias)

0. **Spike** (medio día): `provideTransloco` + un scope (`shared.json`, solo `es`) + `<html lang>` + build/run SSR real, verificar con curl que el texto está en el HTML crudo y que no hay doble-fetch post-hidratación. Bloquea todo lo demás — valida el mecanismo antes de tocar 24 componentes.
1. **Infra**: ruteo con `:lang`, `langGuard`, `LanguageService`, `SeoService`, `LocaleFormatService`, `EnumLabelPipe` + `enums.json` (es baseline + en/pt borrador). Sin cambios visibles todavía.
2. **Shared**: navbar, footer, status-badge, language-switcher wireado.
3. **Público**: home, noticias, noticia-detalle, ranking, calendario, galeria-detalle (`public.json`).
4. **Quiénes Somos** (puede ir en paralelo a la fase 3 una vez lista la fase 1): scope propio, traducción borrador del texto histórico.
5. **Auth**: login, registro (+ mensajes de validación), recuperar/restablecer password (paralelo a fase 3/4).
6. **Panel del competidor**: eventos, inscripcion, pago-playa, paypal-return, mi-panel/* — depende de `LocaleFormatService` y `EnumLabelPipe` (mucha superficie de labels de enum acá). Va al final del frontend por ser la más grande.
7. **Backend — WordPress `lang`**: después de la fase 3 (necesita el componente de noticias ya traducido para tener sentido consumirlo).
8. **Backend — emails**: independiente, puede ir en paralelo desde la fase 1 en adelante.

## 10. Verificación

- **SSR/SEO (el requisito duro)**: build (`npm run build`) + correr `node dist/alas-app.web/server/server.mjs`, luego `curl` crudo (no browser) contra `/`, `/en/quienes-somos`, `/pt/quienes-somos`: verificar `<html lang="...">` correcto, las 4 etiquetas `hreflang`, el `<title>` traducido, y que el texto traducido está **en el HTML servido por el servidor** (grep de una frase en inglés/portugués conocida) — no solo tras hidratación en el navegador. Probar también un prefijo inválido (`/xx/...`) y confirmar el fallback a home en español.
- **Sin doble-fetch**: Network tab (o `mcp__claude-in-chrome`) contra una ruta en `/en/...`, confirmar que no hay un segundo `GET /assets/i18n/en/...json` después de hidratar.
- **Selector de idioma preserva ruta**: navegar a `/noticias/algun-slug-real`, cambiar a inglés, confirmar que la URL es `/en/noticias/algun-slug-real` (no `/en`), y viceversa.
- **CI frontend existente** (`.github/workflows/frontend-ci.yml`: `npm ci && npm run build && npm test`) debe seguir pasando; agregar tests unitarios para `langGuard`, `LanguageService`, `EnumLabelPipe` (fallback a valor crudo si falta la clave), `SeoService` (genera hreflang), y el cálculo de URL del selector de idioma (preserva params/query).
- **Backend**: seguir la convención del proyecto — `docker start alas-sql` (macOS) o SQL local (Windows) + `dotnet test AlasApp.slnx` contra `AlasAppTests`. Agregar tests para: WordPress pasando `lang=en` en la query saliente cuando `ArticleListFilter.Lang` viene seteado (y sin el parámetro cuando es `null`, para no romper llamadas existentes); los 3 handlers de email en-alcance devolviendo el asunto/HTML en el idioma esperado según `IdiomaPreferido` de la cuenta mockeada; y **corregir los tests existentes que instancian `ApproveBeachTokenCommandHandler`/`UpdateInscriptionCommandHandler` directamente**, ya que el cambio de constructor (agregar `IUserAccountRepository`) los rompe en compilación — esto es un fix obligatorio, no opcional, de la fase 8.
- Confirmar explícitamente que `RequestBeachTokenCommandHandler` y `CreateInscriptionCommandHandler` (notificaciones a staff interno) **no cambian** — sus tests existentes deben seguir pasando sin modificación, como chequeo de que la fase 8 no tocó handlers fuera de alcance por error.

## Archivos críticos

- `frontend/src/app/app.routes.ts` — reestructura de ruteo con prefijo de idioma (núcleo de la estrategia de URLs).
- `frontend/src/app/app.config.ts` / `app.config.server.ts` — `provideTransloco`, wiring de idioma activo.
- `frontend/src/app/features/public/quienes-somos/quienes-somos.component.ts` — caso representativo más grande (patrón de conversión + scope propio).
- `frontend/src/app/shared/components/status-badge/status-badge.component.ts` — patrón de `EnumLabelPipe`.
- `backend/src/AlasApp.Application/Emails/TransactionalEmailTemplate.cs` — punto de entrada de localización de emails.
- `backend/src/AlasApp.Application/Payments/Commands/ApproveBeachToken/ApproveBeachTokenCommandHandler.cs` y `backend/src/AlasApp.Application/Inscriptions/Commands/UpdateInscription/UpdateInscriptionCommandHandler.cs` — agregar `IUserAccountRepository`.
- `backend/src/AlasApp.Infrastructure/WordPress/WordPressService.cs` — passthrough de `lang`.
- `backend/src/AlasApp.Api/Controllers/GeneratedControllers.cs` — fuente de verdad para auditar el inventario completo de enums antes de redactar `enums.json`.
