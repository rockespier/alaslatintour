import { Component, inject, computed, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-mi-panel-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <!-- Panel header -->
    <div class="bg-gradient-to-r from-navy-dark to-navy-mid border-b border-navy-mid">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-5 flex items-center gap-4">
        <div class="w-11 h-11 rounded-full bg-gradient-to-br from-cyan-brand to-orange-brand flex items-center justify-center font-heading text-xl font-bold text-navy-deepest flex-shrink-0">
          {{ initial() }}
        </div>
        <div>
          <p class="font-accent uppercase tracking-[0.2em] text-cyan-brand text-xs">{{ heading() }}</p>
          <h1 class="font-heading text-xl leading-tight">{{ fullName() }}</h1>
        </div>
      </div>

      <!-- Tab navigation -->
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex gap-1 overflow-x-auto">
        @for (tab of tabs(); track tab.path) {
          <a [routerLink]="tab.path" routerLinkActive="!border-cyan-brand !text-cyan-brand"
             class="px-5 py-3 border-b-2 border-transparent text-text-muted hover:text-text-light font-accent uppercase text-xs tracking-wider whitespace-nowrap transition flex items-center gap-2">
            <span>{{ tab.label }}</span>
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

  fullName = computed(() => this.auth.currentUser()?.fullName ?? '');
  initial = computed(() => (this.auth.currentUser()?.fullName ?? '?')[0].toUpperCase());
  isCompetitor = computed(() => this.auth.isCompetitor());
  heading = computed(() => this.isCompetitor() ? 'Mi Panel de Competidor' : 'Mi Panel');

  tabs = computed(() => {
    const sharedTabs = [{ path: '/mi-panel/datos', label: 'Mis Datos' }];
    if (!this.isCompetitor()) {
      return sharedTabs;
    }

    return [
      { path: '/mi-panel/inscripciones', label: 'Mis Inscripciones' },
      { path: '/mi-panel/historial', label: 'Historial de Puntos' },
      { path: '/mi-panel/calendario', label: 'Mi Calendario' },
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

    if (hiddenRoutes.some(route => this.router.url === route)) {
      void this.router.navigate(['/mi-panel/datos']);
    }
  }
}
