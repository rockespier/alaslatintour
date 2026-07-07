import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private platformId = inject(PLATFORM_ID);
  private auth = inject(AuthService);
  private baseUrl = `${environment.apiUrl}/v1`;

  private buildHeaders(): Record<string, string> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    const token = this.auth.getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    const res = await fetch(url, {
      method,
      headers: this.buildHeaders(),
      body: body ? JSON.stringify(body) : undefined,
    });

    if (res.status === 401) {
      this.auth.logout();
      throw new Error('No autenticado');
    }
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw Object.assign(new Error(err.message ?? res.statusText), { status: res.status, body: err });
    }
    if (res.status === 204) return undefined as T;
    return res.json();
  }

  get<T>(path: string): Promise<T> { return this.request('GET', path); }
  post<T>(path: string, body: unknown): Promise<T> { return this.request('POST', path, body); }
  put<T>(path: string, body: unknown): Promise<T> { return this.request('PUT', path, body); }
  delete<T>(path: string): Promise<T> { return this.request('DELETE', path); }
}
