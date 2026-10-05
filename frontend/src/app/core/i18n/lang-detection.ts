import { DEFAULT_LANG, Lang, isLang } from './languages';

/** Cookie holding the language the visitor picked in the switcher; it always wins over detection. */
export const LANG_COOKIE = 'alas_lang';

/** Countries whose main language is Spanish (ISO 3166-1 alpha-2). */
const SPANISH_COUNTRIES = new Set([
  'AR', 'BO', 'CL', 'CO', 'CR', 'CU', 'DO', 'EC', 'ES', 'GQ', 'GT', 'HN',
  'MX', 'NI', 'PA', 'PE', 'PR', 'PY', 'SV', 'UY', 'VE',
]);

/** Countries whose main language is Portuguese. */
const PORTUGUESE_COUNTRIES = new Set(['AO', 'BR', 'CV', 'GW', 'MZ', 'PT', 'ST', 'TL']);

/** Brazil → pt, Spanish-speaking countries → es, everything else → en. */
export function langForCountry(country: string): Lang {
  const code = country.trim().toUpperCase();
  if (SPANISH_COUNTRIES.has(code)) return 'es';
  if (PORTUGUESE_COUNTRIES.has(code)) return 'pt';
  return 'en';
}

/**
 * Country from the browser's `Accept-Language` (`pt-BR,pt;q=0.9` → `BR`), or the language itself
 * when no region is given (`pt` → pt, `de` → en). Returns null when the header is empty.
 */
export function langFromAcceptLanguage(header: string | null | undefined): Lang | null {
  const first = header
    ?.split(',')
    .map(part => part.split(';')[0].trim())
    .find(tag => tag && tag !== '*');
  if (!first) return null;

  const [language, region] = first.split('-');
  if (region && /^[a-z]{2}$/i.test(region)) return langForCountry(region);
  const base = language.toLowerCase();
  return isLang(base) ? base : 'en';
}

export function readLangCookie(cookieHeader: string | null | undefined): Lang | null {
  const match = cookieHeader?.match(new RegExp(`(?:^|;\\s*)${LANG_COOKIE}=([^;]+)`));
  return match && isLang(match[1]) ? match[1] : null;
}

const BOT_UA = /bot|crawl|spider|slurp|facebookexternalhit|embedly|preview|lighthouse/i;

export interface LangDetectionInput {
  cookie?: string | null;
  /** Country from a CDN/proxy geo header (`CF-IPCountry`, `X-Country-Code`). */
  country?: string | null;
  acceptLanguage?: string | null;
  userAgent?: string | null;
}

/**
 * Language to redirect a visitor of an unprefixed (Spanish) URL to, or null to stay in Spanish.
 * Crawlers are never redirected so every language version stays indexable.
 */
export function detectPreferredLang(input: LangDetectionInput): Lang | null {
  if (BOT_UA.test(input.userAgent ?? '')) return null;

  const chosen = readLangCookie(input.cookie);
  if (chosen) return chosen;

  const country = input.country?.trim();
  const lang = country && /^[a-z]{2}$/i.test(country) && country.toUpperCase() !== 'XX'
    ? langForCountry(country)
    : langFromAcceptLanguage(input.acceptLanguage);

  return lang ?? DEFAULT_LANG;
}
