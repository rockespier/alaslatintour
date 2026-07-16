import { Component, inject, signal, computed, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService, AdminModule } from '../../../core/services/permissions.service';

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
    <aside class="fixed top-0 left-0 h-full w-64 bg-[#001830] border-r border-white/10 z-40 flex flex-col
                  transition-transform duration-300"
           [class.translate-x-0]="open() || isDesktop()"
           [class.-translate-x-full]="!open() && !isDesktop()">

      <!-- Logo -->
      <div class="flex items-center gap-3 px-5 py-4 border-b border-white/10">
        <img src="/assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Admin" class="h-12 w-auto" />
        
      </div>

      <!-- Nav links -->
      <nav class="flex-1 overflow-y-auto py-4">
        @for (item of visibleNavItems(); track item.route) {
          <a [routerLink]="item.route" routerLinkActive="bg-[#0081C6]/20 text-[#0081C6] border-r-2 border-[#0081C6]"
             (click)="open.set(false)"
             class="flex items-center gap-3 px-5 py-2.5 text-sm text-[#AAAAAA] hover:text-white hover:bg-white/5 relative">
            <span class="w-5 text-center text-base">{{ item.icon }}</span>
            <span>{{ item.label }}</span>
            @if (item.badge) {
              <span class="ml-auto bg-[#EF4444] text-white text-xs font-bold px-1.5 py-0.5 rounded-full min-w-5 text-center">
                {{ item.badge }}
              </span>
            }
          </a>
        }
      </nav>

      <!-- User info -->
      <div class="border-t border-white/10 px-5 py-4 flex items-center gap-3">
        <a routerLink="/admin/perfil" (click)="open.set(false)" class="flex items-center gap-3 min-w-0 flex-1 hover:opacity-80 transition">
          <div class="w-8 h-8 rounded-full bg-[#0081C6] flex items-center justify-center text-sm font-bold text-white flex-shrink-0">
            {{ initials() }}
          </div>
          <div class="min-w-0">
            <p class="text-xs font-medium text-[#EEEEEE] truncate">{{ auth.currentUser()?.fullName }}</p>
            <p class="text-xs text-[#AAAAAA]">{{ auth.currentUser()?.adminRole }}</p>
          </div>
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
