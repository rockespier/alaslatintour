# Product

## Register

product

Note: this register applies as the **site-wide default**, but the project explicitly splits by surface:
- **Brand surfaces** (design IS the product, more expressive): `index.html`, `quienes-somos.html`, `eventos.html`, `noticias.html`, `noticia-detalle.html`.
- **Product surfaces** (design SERVES the workflow, more restrained/dense): `admin-dashboard.html`, `admin-tokens.html`, `admin-eventos.html`, `categorias.html`, `circuitos.html`, `configuracion.html`, `inscripcion.html`, `inscritos.html`, `pagos.html`, `pago-playa.html`, `usuarios.html`.

When working a specific page, apply the rules of its own surface, not the site-wide default.

## Users

Three profiles, per the SDD (see root `CLAUDE.md`):

- **Espectador (público)**: browses without logging in — inicio, quiénes somos, eventos, ranking en vivo, noticias, fotos, contacto. Consumes results and wave stats passively.
- **Competidor**: registers, gets a personal panel, enrolls in competitions via a 3-step flow, pays online (PayPal) or requests a beach-cash token, exports calendar/point history. Often on mobile, possibly at the venue with patchy connectivity.
- **Organizador / Administrador**: two consoles — WordPress for editorial content (Noticias, Fotos) and the .NET CMS for circuits, ranking parameters, events, categories, star-based fee tiers, viewing registrants, validating physical payments, and the financial dashboard. Power users running this daily; density and speed matter more than first-impression polish.

## Product Purpose

Replaces an obsolete ASP-classic platform for the ALAS Latin Tour (Asociación Latinoamericana de Surfistas Profesionales) with a modern PWA that unifies editorial content, event/competition management, registration + payment flows, and live ranking sync (SurfScores Refresh API). Success = friction-free registration/payment for athletes, zero double-entry of results for organizers, and a fast, SEO-indexable public presence for the tour.

## Brand Personality

Athletic, professional, energetic. A serious professional sports circuit — not casual beach/surf-culture, not a generic SaaS tool. The existing direction (deep navy `#002359`→`#004F8E` gradients, condensed Oswald/Barlow headings, live-pulse status dots, cyan `#0081C6` accents) should be read as "competitive tour broadcast" energy: confident, high-performance, internationally credible.

## Anti-references

- **Generic SaaS dashboard clichés** — especially on the admin console: cream/card-grid overload, hero-metric templates (big number + small label + gradient accent), tiny uppercase tracked eyebrows above every section, nested cards, side-stripe accent borders.
- **Dated federation/bureaucracy sites** — the cluttered, low-density, ASP-era look this project explicitly replaces (CLAUDE.md §1). No dense unstyled tables, no inconsistent ad-hoc spacing, no legacy form sprawl.

## Design Principles

1. **One brand, two postures.** Public pages sell the tour (expressive, motion-forward, broadcast energy); admin/competitor pages run the tour (dense, fast, predictable). Don't blur the two — don't bring marketing flourish into the admin console, don't bring admin terseness into the public hero.
2. **Results are sacred and attributed.** Any UI showing scores/rankings must keep "Results by SurfScores.com" visible and linked (legal mandate, CLAUDE.md §8), and must never imply live/real-time scoring (rate-limit + caching constraint). **Known deviation:** the credit was removed from `index.html` and `ranking.html` at the user's explicit request (2026-07-01) while the site is still a visual prototype with no live SurfScores integration. Re-add it before any real SurfScores API connection ships — the mandate itself hasn't changed.
3. **Never block on payment friction.** The beach-cash token flow, PayPal checkout, and capacity-limit messaging are core trust moments for competitors — these states (pending, expired, full, rejected) need to be as carefully designed as the happy path, not bolted on.
4. **Same nouns everywhere.** Circuito → Evento → Categoría → Inscripción → Token/Pago terminology must stay identical across WordPress-fed content, the public site, and the admin CMS — competitors and organizers move between these constantly.
5. **Mobile-first for competitors, density-first for organizers.** Competitors register and check status from a phone, often at an event. Organizers triage long lists (inscritos, pagos, tokens) at a desk. Don't optimize one posture at the expense of the other.

## Accessibility & Inclusion

WCAG AA baseline (4.5:1 text contrast, full keyboard navigation, visible focus states, `prefers-reduced-motion` support) across the whole site. Extra care on ranking/live-status UI: never encode meaning (rank up/down, payment pending/approved, token valid/expired) by color alone — pair with icon, label, or shape, since color-blind users must be able to read standings and payment status without relying on red/green.
