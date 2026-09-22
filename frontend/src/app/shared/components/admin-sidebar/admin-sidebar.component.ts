import { Component, inject, signal, computed, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService, AdminModule } from '../../../core/services/permissions.service';
import { AdminThemeService } from '../../../core/services/admin-theme.service';
import { AdminSidebarService } from '../../../core/services/admin-sidebar.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  module: AdminModule;
  badge?: number;
}

@Component({
  selector: 'app-admin-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <!-- Mobile overlay -->
    @if (open()) {
      <div class="fixed inset-0 bg-black/50 z-30 lg:hidden" (click)="open.set(false)"></div>
    }

    <!-- Sidebar -->
    <aside class="fixed top-0 left-0 h-full bg-[#001830] border-r border-white/10 z-40 flex flex-col
                  transition-transform duration-300"
           [class.w-64]="!sidebar.collapsed() || !isDesktop()"
           [class.w-20]="sidebar.collapsed() && isDesktop()"
           [class.translate-x-0]="open() || isDesktop()"
           [class.-translate-x-full]="!open() && !isDesktop()">

      <!-- Logo -->
      <div class="flex items-center gap-3 px-5 py-4 border-b border-white/10" [class.justify-center]="sidebar.collapsed() && isDesktop()">
        <img src="/assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Admin" class="h-12 w-auto" />
        @if (isDesktop()) {
          <button (click)="sidebar.toggle()" class="ml-auto p-1 text-[#AAAAAA] hover:text-white" [attr.aria-label]="sidebar.collapsed() ? 'Expandir menú' : 'Colapsar menú'">
            {{ sidebar.collapsed() ? '›' : '‹' }}
          </button>
        }
      </div>

      <!-- Nav links -->
      <nav class="flex-1 overflow-y-auto py-4">
        @for (item of visibleNavItems(); track item.route) {
          <a [routerLink]="item.route" routerLinkActive="bg-[#0081C6]/20 text-[#0081C6] border-r-2 border-[#0081C6]"
             (click)="open.set(false)"
             class="flex items-center gap-3 px-5 py-2.5 text-sm text-[#AAAAAA] hover:text-white hover:bg-white/5 relative"
             [class.justify-center]="sidebar.collapsed() && isDesktop()"
             [attr.title]="sidebar.collapsed() && isDesktop() ? item.label : null">
            <span class="w-5 text-center text-base">{{ item.icon }}</span>
            @if (!sidebar.collapsed() || !isDesktop()) { <span>{{ item.label }}</span> }
            @if (item.badge) {
              <span class="ml-auto bg-[#EF4444] text-white text-xs font-bold px-1.5 py-0.5 rounded-full min-w-5 text-center">
                {{ item.badge }}
              </span>
            }
          </a>
        }
      </nav>

      <!-- Modo oscuro / claro -->
      @if (!sidebar.collapsed() || !isDesktop()) { <div class="border-t border-white/10 px-5 py-3">
        <button (click)="theme.toggle()"
                class="w-full flex items-center justify-between gap-2 px-3 py-2 rounded-md text-xs text-[#AAAAAA] hover:text-white hover:bg-white/5 transition"
                title="Cambiar modo oscuro / claro">
          <span class="flex items-center gap-2">
            <span class="text-base">{{ theme.theme() === 'dark' ? '🌙' : '☀️' }}</span>
            <span>{{ theme.theme() === 'dark' ? 'Modo oscuro' : 'Modo claro' }}</span>
          </span>
          <span class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors flex-shrink-0"
                [class]="theme.theme() === 'dark' ? 'bg-white/15' : 'bg-[#0081C6]'">
            <span class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform"
                  [class]="theme.theme() === 'dark' ? 'translate-x-0.5' : 'translate-x-4'"></span>
          </span>
        </button>
      </div> }

      <!-- User info -->
      <div class="border-t border-white/10 px-5 py-4 flex items-center gap-3" [class.justify-center]="sidebar.collapsed() && isDesktop()">
        <a routerLink="/admin/perfil" (click)="open.set(false)" class="flex items-center gap-3 min-w-0 flex-1 hover:opacity-80 transition">
          <div class="w-8 h-8 rounded-full bg-[#0081C6] flex items-center justify-center text-sm font-bold text-white flex-shrink-0">
            {{ initials() }}
          </div>
          @if (!sidebar.collapsed() || !isDesktop()) { <div class="min-w-0">
            <p class="text-xs font-medium text-[#EEEEEE] truncate">{{ auth.currentUser()?.fullName }}</p>
            <p class="text-xs text-[#AAAAAA]">{{ auth.currentUser()?.adminRole }}</p>
          </div> }
        </a>
        <button (click)="auth.logout()" class="text-[#AAAAAA] hover:text-white p-1 flex-shrink-0" title="Cerrar sesión">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
          </svg>
        </button>
      </div>
    </aside>

    <!-- Mobile toggle button -->
    <button (click)="open.set(!open())"
      class="fixed top-4 left-4 z-50 lg:hidden bg-[#002359] border border-white/20 rounded-lg p-2 text-white">
      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
      </svg>
    </button>
  `,
})
export class AdminSidebarComponent implements OnInit {
  auth = inject(AuthService);
  theme = inject(AdminThemeService);
  sidebar = inject(AdminSidebarService);
  private api = inject(ApiService);
  private permissions = inject(PermissionsService);
  private platformId = inject(PLATFORM_ID);

  open = signal(false);
  isDesktop = signal(true);
  pendingTokens = signal(0);

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: '📊', route: '/admin', module: 'Dashboard' },
    { label: 'Usuarios', icon: '👥', route: '/admin/usuarios', module: 'Usuarios' },
    { label: 'Competidores', icon: '🏄', route: '/admin/competidores', module: 'Usuarios' },
    { label: 'Circuitos', icon: '🌊', route: '/admin/circuitos', module: 'Circuitos' },
    { label: 'Eventos', icon: '📅', route: '/admin/eventos', module: 'Eventos' },
    { label: 'Categorías', icon: '🏷️', route: '/admin/categorias', module: 'Categorias' },
    { label: 'Inscritos', icon: '📋', route: '/admin/inscritos', module: 'Inscripciones' },
    { label: 'Pagos', icon: '💳', route: '/admin/pagos', module: 'Pagos' },
    { label: 'Tokens', icon: '🔑', route: '/admin/tokens', module: 'Tokens' },
    { label: 'Configuración', icon: '⚙️', route: '/admin/configuracion', module: 'Configuracion' },
  ];

  visibleNavItems = computed(() => this.navItems.filter(item => this.permissions.canView(item.module)));

  initials = () => {
    const name = this.auth.currentUser()?.fullName ?? '';
    return name.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase();
  };

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.checkDesktop();
    window.addEventListener('resize', () => this.checkDesktop());
    this.loadPendingTokens();
  }

  private checkDesktop(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.isDesktop.set(window.innerWidth >= 1024);
  }

  private async loadPendingTokens(): Promise<void> {
    try {
      const res = await this.api.get<any>('/payments/beach/tokens?status=pendiente&limit=1');
      const count = res?.pagination?.totalItems ?? 0;
      this.pendingTokens.set(count);
      const tokenItem = this.navItems.find(n => n.route === '/admin/tokens');
      if (tokenItem && count > 0) tokenItem.badge = count;
    } catch {
      // silencioso — no bloquea la navegación
    }
  }
}
