import { Component, inject, computed, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { TranslocoModule, provideTranslocoScope } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { stripLangPrefix } from '../../core/i18n/languages';

@Component({
  selector: 'app-mi-panel-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslocoModule],
  providers: [provideTranslocoScope('competitor')],
  template: `
    <!-- Panel header -->
    <div class="bg-gradient-to-r from-navy-dark to-navy-mid border-b border-navy-mid">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-5 flex items-center gap-4">
        <div class="w-11 h-11 rounded-full bg-gradient-to-br from-cyan-brand to-orange-brand flex items-center justify-center font-heading text-xl font-bold text-navy-deepest flex-shrink-0">
          {{ initial() }}
        </div>
        <div>
          <p class="font-accent uppercase tracking-[0.2em] text-cyan-brand text-xs">{{ heading() | transloco }}</p>
          <h1 class="font-heading text-xl leading-tight">{{ fullName() }}</h1>
        </div>
      </div>

      <!-- Tab navigation -->
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex gap-1 overflow-x-auto">
        @for (tab of tabs(); track tab.path) {
          <a [routerLink]="language.localize(tab.path)" routerLinkActive="!border-cyan-brand !text-cyan-brand"
             class="px-5 py-3 border-b-2 border-transparent text-text-muted hover:text-text-light font-accent uppercase text-xs tracking-wider whitespace-nowrap transition flex items-center gap-2">
            <span>{{ 'competitor.panel.tabs.' + tab.key | transloco }}</span>
          </a>
        }
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <router-outlet />
    </div>
  `,
})
export class MiPanelLayoutComponent implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  protected language = inject(LanguageService);

  fullName = computed(() => this.auth.currentUser()?.fullName ?? '');
  initial = computed(() => (this.auth.currentUser()?.fullName ?? '?')[0].toUpperCase());
  isCompetitor = computed(() => this.auth.isCompetitor());
  heading = computed(() => this.isCompetitor() ? 'competitor.panel.competitorHeading' : 'competitor.panel.heading');

  tabs = computed(() => {
    const sharedTabs = [{ path: '/mi-panel/datos', key: 'datos' }];
    if (!this.isCompetitor()) {
      return sharedTabs;
    }

    return [
      { path: '/mi-panel/inscripciones', key: 'inscripciones' },
      { path: '/mi-panel/historial', key: 'historial' },
      { path: '/mi-panel/calendario', key: 'calendario' },
      ...sharedTabs,
    ];
  });

  ngOnInit(): void {
    if (this.isCompetitor()) {
      return;
    }

    const hiddenRoutes = [
      '/mi-panel',
      '/mi-panel/inscripciones',
      '/mi-panel/historial',
      '/mi-panel/calendario',
    ];

    const url = stripLangPrefix(this.router.url);
    if (hiddenRoutes.some(route => url === route)) {
      void this.router.navigateByUrl(this.language.localize('/mi-panel/datos'));
    }
  }
}
