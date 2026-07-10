import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-recuperar-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-navy-deepest flex flex-col items-center justify-center px-4 py-12">

      <!-- Logo -->
      <a routerLink="/" class="mb-8">
        <img src="/assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-16 w-auto" />
      </a>

      <div class="w-full max-w-md">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl p-8 shadow-2xl shadow-black/40">

          @if (!sent()) {

            <div class="mb-6">
              <h1 class="font-heading text-3xl mb-1">Recuperar contraseña</h1>
              <p class="text-text-muted text-sm">
                Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña.
              </p>
            </div>

            @if (error()) {
              <div class="mb-5 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
                {{ error() }}
              </div>
            }

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="space-y-5">
              <div>
                <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">
                  Correo electrónico
                </label>
                <input formControlName="email" type="email" autocomplete="email"
                  placeholder="tu@correo.com"
                  class="input-field"
                  [class.field-error]="form.controls.email.invalid && form.controls.email.touched" />
                @if (form.controls.email.invalid && form.controls.email.touched) {
                  <p class="mt-1 text-xs text-error-brand">
                    @if (form.controls.email.errors?.['required']) { Requerido }
                    @else { Correo inválido }
                  </p>
                }
              </div>

              <button type="submit" [disabled]="loading()"
                class="w-full py-3 px-4 bg-cyan-brand hover:bg-cyan-dark disabled:opacity-60 text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
                @if (loading()) {
                  <span class="inline-flex items-center justify-center gap-2">
                    <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                    </svg>
                    Enviando...
                  </span>
                } @else {
                  Enviar enlace
                }
              </button>
            </form>

          } @else {

            <!-- Success -->
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-success-brand/20 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-success-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                </svg>
              </div>
              <h2 class="font-heading text-2xl mb-2">Correo enviado</h2>
              <p class="text-text-muted text-sm mb-2">
                Si el correo <strong class="text-text-light">{{ form.value.email }}</strong> está registrado,
                recibirás un enlace en los próximos minutos.
              </p>
              <p class="text-xs text-text-muted mb-8">Revisa también tu carpeta de spam.</p>
              <button (click)="sent.set(false)"
                class="text-sm text-cyan-brand hover:text-cyan-dark">
                Intentar con otro correo
              </button>
            </div>

          }

          <p class="mt-6 text-center text-sm text-text-muted">
            <a routerLink="/login" class="text-cyan-brand hover:text-cyan-dark">← Volver al inicio de sesión</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class RecuperarPasswordComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  loading = signal(false);
  error = signal('');
  sent = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    try {
      await this.api.post('/auth/forgot-password', { email: this.form.value.email });
      this.sent.set(true);
    } catch {
      // Always show success to avoid email enumeration
      this.sent.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
