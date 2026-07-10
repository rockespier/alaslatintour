import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserInfo } from '../../../core/models';

@Component({
  selector: 'app-login',
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

          <h1 class="font-heading text-3xl mb-1">Iniciar sesión</h1>
          <p class="text-text-muted text-sm mb-8">Accede a tu cuenta del circuito</p>

          <!-- Error -->
          @if (error()) {
            <div class="mb-6 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
              {{ error() }}
            </div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="space-y-5">

            <!-- Email -->
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

            <!-- Password -->
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">
                Contraseña
              </label>
              <div class="relative">
                <input formControlName="password"
                  [type]="showPwd() ? 'text' : 'password'"
                  autocomplete="current-password"
                  placeholder="••••••••"
                  class="input-field pr-10"
                  [class.field-error]="form.controls.password.invalid && form.controls.password.touched" />
                <button type="button" (click)="showPwd.set(!showPwd())"
                  class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-light">
                  @if (showPwd()) {
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
                    </svg>
                  } @else {
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0zm-3-9C6.477 3 1 8.477 1 12s5.477 9 11 9 11-5.477 11-9-5.477-9-11-9z"/>
                    </svg>
                  }
                </button>
              </div>
              @if (form.controls.password.invalid && form.controls.password.touched) {
                <p class="mt-1 text-xs text-error-brand">Ingresa tu contraseña</p>
              }
            </div>

            <!-- Remember me + forgot -->
            <div class="flex items-center justify-between">
              <label class="flex items-center gap-2 cursor-pointer">
                <input formControlName="rememberMe" type="checkbox"
                  class="w-4 h-4 rounded border-navy-mid bg-navy-deepest accent-cyan-brand" />
                <span class="text-sm text-text-muted">Recordarme</span>
              </label>
              <a routerLink="/recuperar-password"
                class="text-sm text-cyan-brand hover:text-cyan-dark">
                ¿Olvidaste tu contraseña?
              </a>
            </div>

            <!-- Submit -->
            <button type="submit" [disabled]="loading()"
              class="w-full py-3 px-4 bg-cyan-brand hover:bg-cyan-dark disabled:opacity-60 text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
              @if (loading()) {
                <span class="inline-flex items-center gap-2">
                  <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                  </svg>
                  Verificando...
                </span>
              } @else {
                Iniciar sesión
              }
            </button>

          </form>

          <p class="mt-6 text-center text-sm text-text-muted">
            ¿No tienes cuenta?
            <a routerLink="/registro" class="text-cyan-brand hover:text-cyan-dark ml-1">Regístrate gratis</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    rememberMe: [false],
  });

  loading = signal(false);
  error = signal('');
  showPwd = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    try {
      const { email, password, rememberMe } = this.form.value;
      const res = await this.api.post<any>('/auth/login', { email, password, rememberMe });
      const user: UserInfo = {
        id: res.user?.id ?? '',
        email: res.user?.email ?? '',
        fullName: res.user?.fullName ?? '',
        tipo: res.user?.tipo ?? 'espectador',
        adminRole: res.user?.adminRole,
      };
      this.auth.setSession(res.accessToken, user);

      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
      if (returnUrl) {
        this.router.navigateByUrl(returnUrl);
      } else if (user.adminRole) {
        this.router.navigate(['/admin']);
      } else if (user.tipo === 'competidor') {
        this.router.navigate(['/mi-panel']);
      } else {
        this.router.navigate(['/']);
      }
    } catch (err: any) {
      this.error.set(err.body?.message ?? 'Correo o contraseña incorrectos.');
    } finally {
      this.loading.set(false);
    }
  }
}
