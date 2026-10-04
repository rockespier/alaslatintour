import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { RouteReuseStrategy, provideRouter, withComponentInputBinding, withViewTransitions, withInMemoryScrolling } from '@angular/router';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { routes } from './app.routes';
import { provideI18n } from './core/i18n/i18n.providers';
import { LangRouteReuseStrategy } from './core/i18n/lang-route-reuse.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withViewTransitions(),
      withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }),
    ),
    { provide: RouteReuseStrategy, useClass: LangRouteReuseStrategy },
    provideClientHydration(withEventReplay()),
    // withFetch: works under Node during SSR and lets Angular's PendingTasks
    // hold rendering until in-flight requests resolve (raw fetch() does not).
    provideHttpClient(withFetch()),
    provideI18n(),
  ],
};
