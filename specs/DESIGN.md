---
name: ALAS Global Tour
description: Broadcast-grade navy/azure design system for the official ALAS Global Tour competitive surf platform
colors:
  navy-deepest: "#002359"
  navy-dark: "#003873"
  navy-mid: "#004F8E"
  sky-cyan: "#0081C6"
  azure-cta: "#007AFF"
  azure-cta-hover: "#0D6EFD"
  text-light: "#EEEEEE"
  text-muted: "#AAAAAA"
  success: "#22C55E"
  warning: "#FBBF24"
  error: "#EF4444"
typography:
  display:
    fontFamily: "Oswald, sans-serif"
    fontSize: "clamp(2.5rem, 6vw, 6rem)"
    fontWeight: 700
    lineHeight: 0.9
    letterSpacing: "0.02em"
  headline:
    fontFamily: "Oswald, sans-serif"
    fontSize: "clamp(1.75rem, 3vw, 2.75rem)"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "0.02em"
  body:
    fontFamily: "Inter, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.6
    letterSpacing: "normal"
  label:
    fontFamily: "Barlow Condensed, sans-serif"
    fontSize: "13px"
    fontWeight: 500
    lineHeight: 1.2
    letterSpacing: "0.08em"
  mono:
    fontFamily: "JetBrains Mono, ui-monospace, monospace"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.4
    letterSpacing: "0.15em"
rounded:
  badge: "9999px"
  control: "6px"
  card: "12px"
  compact: "4px"
  soft: "8px"
spacing:
  xs: "8px"
  sm: "12px"
  md: "20px"
  lg: "32px"
components:
  button-primary:
    backgroundColor: "{colors.azure-cta}"
    textColor: "#FFFFFF"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "16px 32px"
  button-primary-hover:
    backgroundColor: "{colors.azure-cta-hover}"
  button-secondary:
    backgroundColor: "transparent"
    textColor: "{colors.sky-cyan}"
    typography: "{typography.label}"
    rounded: "{rounded.control}"
    padding: "16px 32px"
  badge-status:
    backgroundColor: "{colors.sky-cyan}"
    textColor: "{colors.sky-cyan}"
    typography: "{typography.label}"
    rounded: "{rounded.badge}"
    padding: "4px 10px"
  input-field:
    backgroundColor: "{colors.navy-dark}"
    textColor: "{colors.text-light}"
    rounded: "{rounded.control}"
    padding: "10px 12px"
---

# Design System: ALAS Latin Tour

## 1. Overview

**Creative North Star: "The Tour Broadcast"**

This system reads like the on-screen graphics package for a live international surf broadcast, not a generic web app. The base is a stacked navy field — `navy-deepest` → `navy-dark` → `navy-mid` — that holds the screen the way a broadcast lower-third holds the bottom of a frame: dark, stable, never competing for attention. Condensed, uppercase Barlow Condensed labels and Oswald display headlines carry the authority of a results board; the `sky-cyan` and `azure-cta` accents do the work that broadcast graphics use a single hot color for — live indicators, the one action that matters, the number that just changed.

The system explicitly rejects the cream/card-grid SaaS dashboard look and the cluttered, low-density federation-site look this platform replaces (see PRODUCT.md anti-references). It is built for two postures from one palette: the public surfaces broadcast the tour outward (bigger type, motion, gradient hero treatment), while the admin/competitor console runs the tour internally (denser, border-led, fewer flourishes) — but both pull from the exact same navy/azure/cyan tokens, never a second palette.

**Key Characteristics:**
- Navy-on-navy layering (3 steps of the same hue) instead of light/dark contrast for surface hierarchy
- One hot accent (`azure-cta`, #007AFF) reserved for the single primary action per screen
- Condensed, uppercase, tracked labels (`font-accent` / Barlow Condensed) for anything chip-, nav-, or status-shaped
- Flat surfaces, 1px borders for separation — shadows are rare and reserved for accent glows, not structure
- Status meaning (success/warning/error/live) always carried by a named color token, never decoration alone

## 2. Colors

The palette is a single committed hue family (navy → azure → cyan) plus a small, strict status set. There is no secondary or tertiary brand hue — resist introducing one.

### Primary
- **Azure CTA** (`#007AFF` / `{colors.azure-cta}`): the one primary-action color. Reserved for the single most important button or link on a given screen (hero CTA, "Inscribirse", "Guardar"). Hover state lightens to **Azure CTA Hover** (`#0D6EFD`).
- **Sky Cyan** (`#0081C6` / `{colors.sky-cyan}`): the secondary accent — outlined buttons, active nav state, focus rings, links, icon accents, badge text. Used far more often than Azure CTA precisely because it's quieter.

### Neutral
- **Navy Deepest** (`#002359`): the base body background for public pages and the deepest card surface in admin views.
- **Navy Dark** (`#003873`): admin body background, form-field fill, second layering step.
- **Navy Mid** (`#004F8E`): borders, dividers, hover-fill on dark rows, third layering step. This is the workhorse neutral — when in doubt about a border or a subtle fill, this is it.
- **Text Light** (`#EEEEEE`): primary text on all navy surfaces.
- **Text Muted** (`#AAAAAA`): secondary/meta text. Never drop below this lightness for body copy on navy — anything darker fails contrast on `navy-deepest`.

### Status
- **Success** (`#22C55E`): payment confirmed, registration approved, live-active indicator.
- **Warning** (`#FBBF24`): pending payment, token awaiting approval, capacity nearing limit.
- **Error** (`#EF4444`): rejected, expired token, validation failure, capacity full.

### Named Rules
**The Honest Naming Rule.** The live Tailwind config still names these tokens `orange-brand` (→ resolves to `#007AFF`, actually azure) and `gold-brand` (→ duplicates `cyan-brand` exactly, `#0081C6`). This is leftover naming from an earlier orange/gold accent that was swapped to blue/cyan and never renamed. DESIGN.md documents the *true* rendered colors under honest names (`azure-cta`, `sky-cyan`); treat the Tailwind config rename (`orange-brand` → `azure-cta`, drop the duplicate `gold-brand`) as an outstanding polish item, not a documentation choice to perpetuate.

**The One Hot Color Rule.** `azure-cta` appears on exactly one element class of action per screen (the primary CTA / primary save action). If a screen has two buttons fighting for `azure-cta`, one of them is wrong — demote it to `sky-cyan` outline or ghost.

**The Status-Never-Alone Rule.** Success/warning/error meaning is never carried by color alone (per PRODUCT.md's accessibility note on ranking/payment status) — always pair with an icon, label, or shape so color-blind users read state correctly.

## 3. Typography

**Display/Headline Font:** Oswald (with sans-serif fallback)
**Body Font:** Inter (with sans-serif fallback)
**Label/Accent Font:** Barlow Condensed (with sans-serif fallback)
**Mono Font:** JetBrains Mono (with ui-monospace fallback)

**Character:** A three-way pairing doing three distinct jobs — Oswald's condensed weight carries headline authority, Inter stays fully neutral and readable for body copy and data, Barlow Condensed (uppercase, tracked +0.08em) marks anything that behaves like a label, chip, or nav item. JetBrains Mono is a deliberate fourth voice, scoped narrowly to token/code entry (admin-tokens, pago-playa) where fixed-width characters matter for scanability. The pairing reads as broadcast/sports-editorial, not corporate SaaS.

### Hierarchy
- **Display** (700, `clamp(2.5rem, 6vw, 6rem)`, line-height 0.9): Hero headlines on public pages only.
- **Headline** (600, `clamp(1.75rem, 3vw, 2.75rem)`, line-height 1.1): Section titles, page titles in the admin console.
- **Body** (400, 16px, line-height 1.6): Paragraph copy, table cell content, form helper text. Cap line length at 65–75ch on public long-form pages (noticias, quienes-somos).
- **Label** (500, 13px, letter-spacing 0.08em, uppercase): Nav links, buttons, badges, table headers, stat-card eyebrows. This is the `font-accent` utility already in use site-wide.
- **Mono** (400, 14px, letter-spacing 0.15em): Token codes, masked credential fields, and JSON/embed-code snippets only. Never for headings, labels, or body copy — reach for it only when the content is literally code or a fixed-width credential.

### Named Rules
**The One Voice Per Element Rule.** Headings are always Oswald, body is always Inter, anything uppercase-and-tracked is always Barlow Condensed. Never mix — e.g. don't set a table header in Inter when every other label on the page is Barlow Condensed.

## 4. Elevation

This system is flat by default. Depth is conveyed entirely through the three-step navy layering (`navy-deepest` → `navy-dark` → `navy-mid`) and 1px `navy-mid` borders, not box-shadows. The one exception is a deliberate accent glow on primary CTAs and hero cards (`shadow-lg shadow-[accent]/20`), which functions as a hot-color halo, not a structural elevation cue.

### Shadow Vocabulary
- **CTA Glow** (`box-shadow: 0 10px 15px -3px rgb(0 122 255 / 0.2)`, i.e. `shadow-lg shadow-azure-cta/20`): primary CTA buttons and the hero card only. Signals "this is the hot element," not "this is raised."
- **Sticky Nav Scrim** (`backdrop-blur` + `bg-navy-deepest/85`): the only blur usage in the system — sticky nav bars over scrolling content. Not used decoratively elsewhere.

### Named Rules
**The Flat-By-Default Rule.** Surfaces sit flush against their navy layer. Reach for a border before reaching for a shadow. Reserve shadow/glow for the single hottest element on screen.

## 5. Components

### Buttons
- **Shape:** 6px radius (`rounded-md` / `{rounded.control}`) on all buttons — never `rounded-full` (that's reserved for badges/pills).
- **Primary:** `azure-cta` background, white text, Barlow Condensed label styling (uppercase, tracked), `16px 32px` padding. Hover shifts to `azure-cta-hover` (#0D6EFD). One per screen.
- **Secondary / Outline:** transparent background, `sky-cyan` 2px border and text; hover fills to `sky-cyan` background with `navy-deepest` text (full color-swap, not a tint).
- **Ghost (admin row actions):** icon-only, `text-{status-color}` on transparent, `hover:bg-navy-mid` fill, `p-1.5 rounded` (4px) — smaller radius than full buttons since these sit inside dense table rows.

The radius scale has two more members beyond badge/control/card, both reserved for dense admin contexts: **Compact** (`{rounded.compact}`, 4px) for inline-edit table cells and tight numeric inputs (e.g. ranking-position fields edited in place); **Soft** (`{rounded.soft}`, 8px) for sidebar nav items and scrollbar thumbs — slightly rounder than a control but not as rounded as a card. Don't reach for an arbitrary radius outside these five values.

### Badges / Status Pills
- **Style:** `rounded-full`, `px-2.5 py-1`, Barlow Condensed uppercase label, background at the status color's 20% opacity, text at full status color, border at the status color's 30% opacity (e.g. `bg-warning/20 text-warning border border-warning/30`).
- **State:** color is the only variant axis — success/warning/error/info(sky-cyan) — shape and sizing never change.

### Cards / Containers
- **Corner Style:** 12px radius (`rounded-xl`) for content cards on both public and admin surfaces.
- **Background:** `navy-deepest` on an admin page whose body is `navy-dark` (one layering step up); `navy-mid`-tinted gradients (`linear-gradient(160deg, navy-mid → navy-dark → navy-deepest)`) for public event/feature cards.
- **Shadow Strategy:** none at rest (see Elevation). Hover lifts with `translateY(-4px)` and a border-color shift to the relevant accent — motion communicates interactivity, not a shadow.
- **Border:** 1px `navy-mid`, brightening to the contextual accent (`cyan-brand` or `orange-brand`/40→full) on hover.
- **Internal Padding:** `p-5` (20px) standard for stat/list cards.

### Inputs / Fields
- **Style:** `navy-dark` background, 1px `navy-mid` border, 6px radius, `10px 12px` padding, 14px Inter text.
- **Focus:** border color shifts to `sky-cyan`, no glow/ring — consistent with the flat, border-led elevation philosophy.
- **Masked (token fields):** monospace, `letter-spacing: 0.15em` for token/code entry.

### Navigation
- Sticky, `navy-deepest/85` with `backdrop-blur`, bottom 1px `navy-mid/60` border. Links use Barlow Condensed uppercase with a `sky-cyan` underline that scales in from the left on hover/active (`scaleX(0)` → `scaleX(1)`, 0.25s). Active link text turns `sky-cyan`.

### Live Status Indicator (signature component)
A 10px `success` dot with a looping `box-shadow` pulse (1.6s, expanding-then-fading ring) marks "ranking live/recently updated" callouts. Per PRODUCT.md, this must never imply real-time live scoring (SurfScores rate-limit constraint) — pair it with cached-as-of copy, not a literal "LIVE" claim, and keep the required "Results by SurfScores.com" attribution link visible wherever it appears.

## 6. Do's and Don'ts

### Do:
- **Do** keep the navy three-step layering (`navy-deepest`/`navy-dark`/`navy-mid`) as the only depth mechanism on a given screen — pick the right starting layer for the page type and step up/down consistently.
- **Do** reserve `azure-cta` (#007AFF) for exactly one primary action per screen; everything else is `sky-cyan` or ghost.
- **Do** set anything chip-, nav-, or label-shaped in Barlow Condensed, uppercase, `0.08em` tracking.
- **Do** pair every status color (success/warning/error) with an icon or label, never color alone (WCAG AA + color-blind safety from PRODUCT.md).
- **Do** keep "Results by SurfScores.com" visible and linked on every screen showing scores or rankings (legal mandate).
- **Do** cap display headings at `clamp(2.5rem, 6vw, 6rem)`.
- **Do** rename `orange-brand`/`orange-light` to reflect their true blue value (azure, not orange) when that cleanup pass happens — deferred for now, tracked here. (`gold-brand` has already been consolidated into `cyan-brand`.)
- **Do** use "ALAS Latin Tour" consistently across every page.

### Don't:
- **Don't** introduce a second brand hue family (no warm accent, no purple, no green-as-brand) — status colors are the only departure from navy/azure/cyan, and they're reserved for status meaning only.
- **Don't** use `rounded-full` on anything but badges/pills/avatars/dots.
- **Don't** add box-shadows for structural elevation — borders and navy-layer steps do that job; shadows are reserved for the single hottest accent element per screen.
- **Don't** use cream/card-grid SaaS dashboard patterns, hero-metric templates, tiny uppercase eyebrows above every section, nested cards, or side-stripe colored borders (PRODUCT.md anti-references).
- **Don't** ship the cluttered, dense, unstyled-table look of the legacy ASP-era federation site this platform replaces.
- **Don't** mix font families across the same element type — headings stay Oswald, body stays Inter, labels stay Barlow Condensed, site-wide.
- **Don't** imply real-time live scoring anywhere; ranking displays must read as cached/periodically-updated, per the SurfScores rate-limit constraint.
