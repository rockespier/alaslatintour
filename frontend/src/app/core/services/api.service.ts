import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private baseUrl = `${environment.apiUrl}/v1`;

  private buildHeaders(): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    const token = this.auth.getToken();
    if (token) headers = headers.set('Authorization', `Bearer ${token}`);
    return headers;
  }

  private async request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T> {
    const url = `${this.baseUrl}${path}`;
    try {
      return await firstValueFrom(
        this.http.request<T>(method, url, { headers: this.buildHeaders(), body }),
      );
    } catch (err) {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 401) this.auth.logout();
        throw Object.assign(new Error(err.error?.message ?? err.statusText), { status: err.status, body: err.error });
      }
      throw err;
    }
  }

  get<T>(path: string): Promise<T> { return this.request('GET', path); }
  post<T>(path: string, body: unknown): Promise<T> { return this.request('POST', path, body); }
  put<T>(path: string, body: unknown): Promise<T> { return this.request('PUT', path, body); }
  delete<T>(path: string): Promise<T> { return this.request('DELETE', path); }
}
