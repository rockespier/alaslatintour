import { Component, inject } from '@angular/core';
import { Router, RouterLink, UrlTree } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/i18n/language.service';
import { SeoService } from '../../../core/i18n/seo.service';
import { AVAILABLE_LANGS, Lang, localizeUrl, translatedContentFallback } from '../../../core/i18n/languages';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [RouterLink, TranslocoModule],
  template: `
    <div class="flex items-center gap-1 text-xs font-semibold uppercase" role="group" [attr.aria-label]="'language.label' | transloco">
      @for (lang of langs; track lang) {
        <a [routerLink]="hrefFor(lang)"
           [attr.hreflang]="lang"
           [attr.lang]="lang"
           [attr.title]="'language.' + lang | transloco"
           [attr.aria-current]="lang === language.activeLang() ? 'true' : null"
           class="px-1.5 py-1 rounded"
           [class.text-[#0081C6]]="lang === language.activeLang()"
           [class.text-[#AAAAAA]]="lang !== language.activeLang()"
           [class.hover:text-white]="lang !== language.activeLang()">{{ lang }}</a>
      }
    </div>
  `,
})
export class LanguageSwitcherComponent {
  protected readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  protected readonly langs = AVAILABLE_LANGS;

  /**
   * Same page (params, query, fragment) in another language. On article/gallery detail pages the
   * slug is language-specific (Polylang): go to the translated post WordPress reported, or to the
   * news listing when that language has no translation.
   */
  hrefFor(lang: Lang): UrlTree {
    const url = this.router.url;
    if (lang === this.language.activeLang()) return this.router.parseUrl(url);

    const listing = translatedContentFallback(url);
    const target = listing ? this.seo.contentAlternateFor(url, lang) ?? listing : url;
    return this.router.parseUrl(localizeUrl(target, lang));
  }
}
