import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlMatchResult, UrlSegment } from '@angular/router';
import { DEFAULT_LANG, PREFIXED_LANGS, isLang } from './languages';
import { LanguageService } from './language.service';

/** Matches a leading `/en` or `/pt` segment and exposes it as the `lang` param. */
export function langPrefixMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  const first = segments[0];
  if (first && (PREFIXED_LANGS as readonly string[]).includes(first.path)) {
    return { consumed: [first], posParams: { lang: first } };
  }
  return null;
}

export const langGuard: CanActivateFn = route => {
  const lang = route.params['lang'] ?? DEFAULT_LANG;
  if (!isLang(lang)) return inject(Router).createUrlTree(['/']);
  inject(LanguageService).setActiveLang(lang);
  return true;
};
