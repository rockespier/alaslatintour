export const AVAILABLE_LANGS = ['es', 'en', 'pt'] as const;
export type Lang = (typeof AVAILABLE_LANGS)[number];

export const DEFAULT_LANG: Lang = 'es';

/** Languages that get a URL prefix (`/en/...`). Spanish lives at the root. */
export const PREFIXED_LANGS: readonly Lang[] = ['en', 'pt'];

/** BCP 47 locale used for date/number formatting. */
export const LOCALE_BY_LANG: Record<Lang, string> = {
  es: 'es',
  en: 'en-US',
  pt: 'pt-BR',
};

export function isLang(value: unknown): value is Lang {
  return typeof value === 'string' && (AVAILABLE_LANGS as readonly string[]).includes(value);
}

/** Reads the language from the first path segment (`/en/noticias` → `en`). */
export function langFromPath(path: string): Lang {
  const first = path.split(/[?#]/)[0].split('/').filter(Boolean)[0];
  return first && PREFIXED_LANGS.includes(first as Lang) ? (first as Lang) : DEFAULT_LANG;
}

/** Removes the language prefix: `/en/noticias?x=1` → `/noticias?x=1`. */
export function stripLangPrefix(url: string): string {
  const match = /^\/(en|pt)(?=\/|\?|#|$)/.exec(url);
  if (!match) return url || '/';
  const rest = url.slice(match[0].length);
  return rest.startsWith('/') ? rest : `/${rest}`;
}

/**
 * WordPress detail pages (`/noticias/:slug`, `/galerias/:slug`): each translation has its own
 * slug in WPML/Polylang, so the same slug under another language prefix may not exist.
 * Returns the language-neutral listing to send users to instead, or null for other pages.
 */
export function translatedContentFallback(url: string): string | null {
  const path = stripLangPrefix(url).split(/[?#]/)[0];
  return /^\/(noticias|galerias)\/[^/]+\/?$/.test(path) ? '/noticias' : null;
}

/**
 * Builds app paths from the per-language slugs WordPress/Polylang returns:
 * (`/noticias`, `{ es: 'recta-final', en: 'final-stretch' }`) → `{ es: '/noticias/recta-final', en: '/noticias/final-stretch' }`.
 */
export function translationPaths(base: string, slugs: Record<string, string> | null | undefined): Partial<Record<Lang, string>> {
  const paths: Partial<Record<Lang, string>> = {};
  for (const [lang, slug] of Object.entries(slugs ?? {})) {
    if (isLang(lang) && slug) paths[lang] = `${base}/${encodeURIComponent(slug)}`;
  }
  return paths;
}

/** Adds the language prefix to an app-absolute path: (`/noticias`, `en`) → `/en/noticias`. */
export function localizeUrl(url: string, lang: Lang): string {
  const base = stripLangPrefix(url.startsWith('/') ? url : `/${url}`);
  if (lang === DEFAULT_LANG) return base;
  if (base === '/') return `/${lang}`;
  if (base.startsWith('/?') || base.startsWith('/#')) return `/${lang}${base.slice(1)}`;
  return `/${lang}${base}`;
}
