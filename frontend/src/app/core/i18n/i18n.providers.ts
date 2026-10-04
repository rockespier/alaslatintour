import { PlatformLocation, registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import localePtBR from '@angular/common/locales/pt';
import { EnvironmentProviders, Provider, inject, isDevMode, provideAppInitializer } from '@angular/core';
import { NavigationEnd, Router, TitleStrategy } from '@angular/router';
import { TranslocoService, provideTransloco } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { filter } from 'rxjs/operators';
import { I18nTitleStrategy } from './i18n-title.strategy';
import { LanguageService } from './language.service';
import { AVAILABLE_LANGS, DEFAULT_LANG, langFromPath } from './languages';
import { SeoService } from './seo.service';
import { TranslocoBundleLoader } from './transloco-loader';

registerLocaleData(localeEs, 'es');
registerLocaleData(localePtBR, 'pt-BR');

export function provideI18n(): (Provider | EnvironmentProviders)[] {
  return [
    provideTransloco({
      config: {
        availableLangs: [...AVAILABLE_LANGS],
        defaultLang: DEFAULT_LANG,
        fallbackLang: DEFAULT_LANG,
        // No runtime fallback: it would also load (and serialize into the SSR
        // TransferState) the Spanish files on every /en and /pt page. Key parity
        // across languages is enforced by translations.spec.ts instead.
        missingHandler: { useFallbackTranslation: false },
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoBundleLoader,
    }),
    { provide: TitleStrategy, useClass: I18nTitleStrategy },
    // Load the root translation for the URL's language before the first render,
    // so neither SSR nor hydration ever paints raw keys.
    provideAppInitializer(() => {
      const lang = langFromPath(inject(PlatformLocation).pathname);
      const transloco = inject(TranslocoService);
      const seo = inject(SeoService);
      inject(LanguageService).setActiveLang(lang);
      inject(Router)
        .events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
        .subscribe(e => seo.updateAlternateLinks(e.urlAfterRedirects));
      return firstValueFrom(transloco.load(lang)).then(() => undefined);
    }),
  ];
}
