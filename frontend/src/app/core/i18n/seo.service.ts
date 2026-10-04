import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TranslocoService } from '@jsverse/transloco';
import { take } from 'rxjs/operators';
import { AVAILABLE_LANGS, DEFAULT_LANG, Lang, localizeUrl, stripLangPrefix, translatedContentFallback } from './languages';

export interface PageMeta {
  /** Transloco scope that holds the keys (e.g. `public`); keys are then relative to it. */
  scope?: string;
  titleKey?: string;
  descriptionKey?: string;
  /** Extra params for the translation keys. */
  params?: Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly transloco = inject(TranslocoService);

  /**
   * Translates and applies title/description once the scope is loaded
   * (selectTranslate waits for it, so this is safe to call from ngOnInit, also during SSR).
   */
  setPageMeta({ scope, titleKey, descriptionKey, params }: PageMeta): void {
    const select = (key: string) =>
      this.transloco.selectTranslate<string>(key, params, scope ? { scope } : undefined).pipe(take(1));

    if (titleKey) {
      select(titleKey).subscribe(title => {
        this.title.setTitle(title);
        this.meta.updateTag({ property: 'og:title', content: title });
      });
    }
    if (descriptionKey) {
      select(descriptionKey).subscribe(description => {
        this.meta.updateTag({ name: 'description', content: description });
        this.meta.updateTag({ property: 'og:description', content: description });
      });
    }
  }

  /**
   * Language-specific paths of the WordPress article/gallery being shown (Polylang gives each
   * translation its own slug), keyed by the page they belong to.
   */
  private readonly contentAlternates = signal<{ page: string; paths: Partial<Record<Lang, string>> } | null>(null);

  /** Rewrites the `<link rel="alternate" hreflang>` set for the current URL. */
  updateAlternateLinks(url: string): void {
    const path = this.pagePath(url);
    if (path.startsWith('/admin')) return this.writeAlternates({}); // admin is Spanish-only

    // Article/gallery slugs differ per language: only advertise the translations WordPress reported.
    if (translatedContentFallback(path)) {
      const content = this.contentAlternates();
      return this.writeAlternates(content?.page === path ? content.paths : {});
    }

    const paths: Partial<Record<Lang, string>> = {};
    for (const lang of AVAILABLE_LANGS) paths[lang] = path;
    this.writeAlternates(paths);
  }

  /**
   * Called by article/gallery detail pages once WordPress returned the translation slugs
   * (`{ es: '/noticias/recta-final', en: '/noticias/final-stretch' }`, app paths without prefix).
   */
  setContentAlternates(url: string, paths: Partial<Record<Lang, string>>): void {
    const page = this.pagePath(url);
    this.contentAlternates.set({ page, paths });
    this.writeAlternates(paths);
  }

  /** Path of `lang`'s version of the current article/gallery, if WordPress reported one. */
  contentAlternateFor(url: string, lang: Lang): string | null {
    const content = this.contentAlternates();
    return content?.page === this.pagePath(url) ? content.paths[lang] ?? null : null;
  }

  private pagePath(url: string): string {
    return stripLangPrefix(url.split('#')[0]);
  }

  private writeAlternates(paths: Partial<Record<Lang, string>>): void {
    const head = this.document.head;
    head.querySelectorAll('link[rel="alternate"][hreflang]').forEach(link => link.remove());

    const origin = this.document.location?.origin ?? '';
    const add = (hreflang: string, href: string) => {
      const link = this.document.createElement('link');
      link.setAttribute('rel', 'alternate');
      link.setAttribute('hreflang', hreflang);
      link.setAttribute('href', origin + href);
      head.appendChild(link);
    };
    for (const lang of AVAILABLE_LANGS) {
      const path = paths[lang];
      if (path) add(lang, localizeUrl(path, lang));
    }
    const fallback = paths[DEFAULT_LANG];
    if (fallback) add('x-default', localizeUrl(fallback, DEFAULT_LANG));
  }
}
