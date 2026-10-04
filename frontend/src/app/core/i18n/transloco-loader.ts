import { Injectable, PLATFORM_ID, PendingTasks, TransferState, inject, makeStateKey } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { Translation, TranslocoLoader } from '@jsverse/transloco';
import { Observable, from, of } from 'rxjs';
import { finalize, map, tap } from 'rxjs/operators';

type TranslationModule = { default: Translation };

/**
 * Translations are bundled as lazy JS chunks instead of fetched over HTTP:
 * the single Express process behind IIS never has to call itself during SSR,
 * and the client reuses what the server rendered through TransferState, so
 * hydration never re-downloads or flashes untranslated keys.
 *
 * Key = Transloco path: `<lang>` for the root file, `<scope>/<lang>` for scopes.
 */
const TRANSLATION_FILES: Record<string, () => Promise<TranslationModule>> = {
  es: () => import('../../../i18n/es.json'),
  en: () => import('../../../i18n/en.json'),
  pt: () => import('../../../i18n/pt.json'),
  'public/es': () => import('../../../i18n/public/es.json'),
  'public/en': () => import('../../../i18n/public/en.json'),
  'public/pt': () => import('../../../i18n/public/pt.json'),
  'quienes-somos/es': () => import('../../../i18n/quienes-somos/es.json'),
  'quienes-somos/en': () => import('../../../i18n/quienes-somos/en.json'),
  'quienes-somos/pt': () => import('../../../i18n/quienes-somos/pt.json'),
  'auth/es': () => import('../../../i18n/auth/es.json'),
  'auth/en': () => import('../../../i18n/auth/en.json'),
  'auth/pt': () => import('../../../i18n/auth/pt.json'),
  'competitor/es': () => import('../../../i18n/competitor/es.json'),
  'competitor/en': () => import('../../../i18n/competitor/en.json'),
  'competitor/pt': () => import('../../../i18n/competitor/pt.json'),
};

@Injectable({ providedIn: 'root' })
export class TranslocoBundleLoader implements TranslocoLoader {
  private readonly transferState = inject(TransferState);
  private readonly pendingTasks = inject(PendingTasks);
  private readonly isServer = isPlatformServer(inject(PLATFORM_ID));

  getTranslation(path: string): Observable<Translation> {
    const key = makeStateKey<Translation>(`i18n:${path}`);
    const transferred = this.transferState.get(key, null);
    if (transferred) return of(transferred);

    const load = TRANSLATION_FILES[path];
    if (!load) return of({});

    // Keeps SSR from serializing the page before the chunk resolves.
    const done = this.pendingTasks.add();
    return from(load()).pipe(
      map(module => module.default),
      tap(translation => {
        if (this.isServer) this.transferState.set(key, translation);
      }),
      finalize(done),
    );
  }
}
