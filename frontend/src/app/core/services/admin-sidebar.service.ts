import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const STORAGE_KEY = 'alas-admin-sidebar-collapsed';

/** Preferencia de ancho del sidebar de escritorio, persistida por navegador. */
@Injectable({ providedIn: 'root' })
export class AdminSidebarService {
  private platformId = inject(PLATFORM_ID);

  readonly collapsed = signal(this.readInitial());

  toggle(): void {
    this.set(!this.collapsed());
  }

  set(collapsed: boolean): void {
    this.collapsed.set(collapsed);
    if (isPlatformBrowser(this.platformId)) {
      try { localStorage.setItem(STORAGE_KEY, String(collapsed)); } catch { /* ignorar */ }
    }
  }

  private readInitial(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    try { return localStorage.getItem(STORAGE_KEY) === 'true'; } catch { return false; }
  }
}
