import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { DEFAULT_LANG, Lang, localizeUrl } from './languages';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly document = inject(DOCUMENT);

  private readonly lang = signal<Lang>(DEFAULT_LANG);
  readonly activeLang = this.lang.asReadonly();

  constructor() {
    // Runs on the server too, so the SSR HTML already carries <html lang>.
    effect(() => {
      this.document.documentElement.lang = this.lang();
    });
  }

  setActiveLang(lang: Lang): void {
    this.lang.set(lang);
    if (this.transloco.getActiveLang() !== lang) {
      this.transloco.setActiveLang(lang);
    }
  }

  /** App-absolute path in the active language: `/noticias` → `/en/noticias`. */
  localize(path: string): string {
    return localizeUrl(path, this.lang());
  }
}
