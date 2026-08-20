import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LiveStatusService } from '../../../core/services/live-status.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="fixed top-0 left-0 right-0 z-50 bg-[#002359]/95 backdrop-blur-sm border-b border-white/10">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between h-16">

          <!-- Logo -->
          <a routerLink="/" class="flex items-center gap-3">            
            <img src="assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-20 w-auto" />
          </a>

          <!-- Nav desktop -->
          <div class="hidden lg:flex items-center gap-6">
            <a routerLink="/" routerLinkActive="text-[#0081C6]" [routerLinkActiveOptions]="{exact: true}"
               class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">Inicio</a>
            <a routerLink="/quienes-somos" routerLinkActive="text-[#0081C6]"
               class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">Quiénes Somos</a>
            <a routerLink="/noticias" routerLinkActive="text-[#0081C6]"
               class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">Noticias</a>
            <a routerLink="/eventos" routerLinkActive="text-[#0081C6]"
               class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">Eventos</a>
            <a routerLink="/ranking" routerLinkActive="text-[#0081C6]"
               class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">Ranking</a>
          </div>

          <!-- Auth actions -->
          <div class="hidden lg:flex items-center gap-3">
            @if (auth.isAuthenticated()) {
              @if (auth.isAdmin()) {
                <a routerLink="/admin" class="text-sm text-[#AAAAAA] hover:text-white">Admin</a>
              }
              <a [routerLink]="profileRoute()" class="text-sm text-[#EEEEEE] hover:text-[#0081C6]">
                {{ auth.currentUser()?.fullName }}
              </a>
              <button (click)="auth.logout()"
                class="text-sm text-[#AAAAAA] hover:text-white px-3 py-1.5 rounded border border-white/10 hover:border-white/30">
                Salir
              </button>
            } @else {
              <a routerLink="/login"
                class="text-sm text-[#EEEEEE] hover:text-[#0081C6] px-3 py-1.5 rounded border border-white/10 hover:border-[#0081C6]">
                Iniciar sesión
              </a>
              <a routerLink="/registro"
                class="text-sm bg-[#0081C6] hover:bg-[#0070b0] text-white px-4 py-1.5 rounded font-medium">
                Registrarse
              </a>
            }
            @if (live.isLive()) {
              <a routerLink="/" fragment="en-vivo"
                class="inline-flex items-center gap-2 bg-error-brand hover:bg-[#dc2626] text-white px-4 py-1.5 rounded-full font-accent uppercase tracking-wider text-xs font-semibold shadow-lg shadow-error-brand/30 transition">
                <span class="relative flex h-2 w-2">
                  <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-white opacity-75"></span>
                  <span class="relative inline-flex rounded-full h-2 w-2 bg-white"></span>
                </span>
                En Vivo
              </a>
            }
          </div>

          <!-- Live badge + Hamburger mobile -->
          <div class="flex items-center gap-2 lg:hidden">
            @if (live.isLive()) {
              <a routerLink="/" fragment="en-vivo" (click)="menuOpen.set(false)"
                class="inline-flex items-center gap-1.5 bg-error-brand text-white px-3 py-1 rounded-full font-accent uppercase tracking-wider text-[11px] font-semibold shadow-lg shadow-error-brand/30">
                <span class="relative flex h-1.5 w-1.5">
                  <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-white opacity-75"></span>
                  <span class="relative inline-flex rounded-full h-1.5 w-1.5 bg-white"></span>
                </span>
                En Vivo
              </a>
            }
            <button (click)="menuOpen.set(!menuOpen())" class="p-2 rounded text-[#EEEEEE]">
              @if (menuOpen()) {
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              } @else {
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
                </svg>
              }
            </button>
          </div>
        </div>

        <!-- Mobile menu -->
        @if (menuOpen()) {
          <div class="lg:hidden border-t border-white/10 py-4 flex flex-col gap-3">
            <a routerLink="/" (click)="menuOpen.set(false)" class="text-sm text-[#EEEEEE] py-1">Inicio</a>
            <a routerLink="/quienes-somos" (click)="menuOpen.set(false)" class="text-sm text-[#EEEEEE] py-1">Quiénes Somos</a>
            <a routerLink="/noticias" (click)="menuOpen.set(false)" class="text-sm text-[#EEEEEE] py-1">Noticias</a>
            <a routerLink="/eventos" (click)="menuOpen.set(false)" class="text-sm text-[#EEEEEE] py-1">Eventos</a>
            <a routerLink="/ranking" (click)="menuOpen.set(false)" class="text-sm text-[#EEEEEE] py-1">Ranking</a>
            <div class="pt-2 border-t border-white/10 flex gap-3">
              @if (!auth.isAuthenticated()) {
                <a routerLink="/login" (click)="menuOpen.set(false)"
                   class="text-sm text-[#EEEEEE] px-3 py-1.5 rounded border border-white/20">Iniciar sesión</a>
                <a routerLink="/registro" (click)="menuOpen.set(false)"
                   class="text-sm bg-[#0081C6] text-white px-4 py-1.5 rounded font-medium">Registrarse</a>
              } @else {
                <button (click)="auth.logout()" class="text-sm text-[#AAAAAA]">Cerrar sesión</button>
              }
            </div>
          </div>
        }
      </div>
    </nav>
  `,
})
export class NavbarComponent {
  auth = inject(AuthService);
  live = inject(LiveStatusService);
  menuOpen = signal(false);

  profileRoute = () => this.auth.isAdmin() ? '/admin' : '/mi-panel';

  constructor() {
    this.live.ensureLoaded();
  }
}
