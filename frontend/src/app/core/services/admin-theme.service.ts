import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type AdminTheme = 'dark' | 'light';

const STORAGE_KEY = 'alas-admin-theme';

/** Preferencia de modo oscuro/claro del panel Administrador (persistida por navegador). */
@Injectable({ providedIn: 'root' })
export class AdminThemeService {
  private platformId = inject(PLATFORM_ID);

  readonly theme = signal<AdminTheme>(this.readInitial());

  toggle(): void {
    this.set(this.theme() === 'dark' ? 'light' : 'dark');
  }

  set(theme: AdminTheme): void {
    this.theme.set(theme);
    if (isPlatformBrowser(this.platformId)) {
      try { localStorage.setItem(STORAGE_KEY, theme); } catch { /* ignorar */ }
    }
  }

  private readInitial(): AdminTheme {
    if (!isPlatformBrowser(this.platformId)) return 'dark';
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved === 'light' ? 'light' : 'dark';
    } catch {
      return 'dark';
    }
  }
}
