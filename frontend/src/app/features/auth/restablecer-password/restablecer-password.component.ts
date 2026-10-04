import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { TranslocoModule, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { LocalizePathPipe } from '../../../shared/pipes/localize-path.pipe';
import { ApiService } from '../../../core/services/api.service';

function passwordPolicy(control: AbstractControl) {
  const value = String(control.value ?? '');
  if (!value) return null;
  return value.length >= 8 && /[A-Z]/.test(value) && /\d/.test(value)
    ? null
    : { passwordPolicy: true };
}

@Component({
  selector: 'app-restablecer-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoModule, LocalizePathPipe],
  providers: [provideTranslocoScope('auth')],
  template: `
    <div class="min-h-screen bg-navy-deepest flex flex-col items-center justify-center px-4 py-12">
      <a [routerLink]="'/' | localizePath" class="mb-8">
        <img src="/assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-16 w-auto" />
      </a>

      <div class="w-full max-w-md">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl p-8 shadow-2xl shadow-black/40">
          @if (!success()) {
            <div class="mb-6">
              <p class="font-accent uppercase text-xs tracking-wider text-cyan-brand mb-2">{{ 'auth.reset.kicker' | transloco }}</p>
              <h1 class="font-heading text-3xl mb-1">{{ 'auth.reset.title' | transloco }}</h1>
              <p class="text-text-muted text-sm">
                {{ 'auth.reset.intro' | transloco }}
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
                  {{ 'auth.reset.token' | transloco }}
                </label>
                <input formControlName="token" type="text" autocomplete="one-time-code"
                  [placeholder]="'auth.reset.tokenPlaceholder' | transloco"
                  class="input-field"
                  [class.field-error]="form.controls.token.invalid && form.controls.token.touched" />
                @if (form.controls.token.invalid && form.controls.token.touched) {
                  <p class="mt-1 text-xs text-error-brand">{{ 'auth.reset.tokenRequired' | transloco }}</p>
                }
              </div>

              <div>
                <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">
                  {{ 'auth.reset.newPassword' | transloco }}
                </label>
                <div class="relative">
                  <input formControlName="newPassword" [type]="showPwd() ? 'text' : 'password'"
                    autocomplete="new-password" placeholder="••••••••"
                    class="input-field pr-10"
                    [class.field-error]="form.controls.newPassword.invalid && form.controls.newPassword.touched" />
                  <button type="button" (click)="showPwd.set(!showPwd())"
                    class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-light"
                    [attr.aria-label]="'auth.reset.togglePassword' | transloco">
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
                @if (form.controls.newPassword.invalid && form.controls.newPassword.touched) {
                  <p class="mt-1 text-xs text-error-brand">
                    {{ 'auth.reset.policy' | transloco }}
                  </p>
                }
              </div>

              <button type="submit" [disabled]="loading()"
                class="w-full py-3 px-4 bg-cyan-brand hover:bg-cyan-dark disabled:opacity-60 text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
                @if (loading()) {
                  {{ 'auth.reset.updating' | transloco }}
                } @else {
                  {{ 'auth.reset.submit' | transloco }}
                }
              </button>
            </form>
          } @else {
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-success-brand/20 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-success-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                </svg>
              </div>
              <h2 class="font-heading text-2xl mb-2">{{ 'auth.reset.successTitle' | transloco }}</h2>
              <p class="text-text-muted text-sm mb-8">{{ 'auth.reset.successText' | transloco }}</p>
              <a [routerLink]="'/login' | localizePath"
                class="inline-block px-6 py-3 bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg font-bold">
                {{ 'auth.reset.login' | transloco }}
              </a>
            </div>
          }

          <p class="mt-6 text-center text-sm text-text-muted">
            <a [routerLink]="'/recuperar-password' | localizePath" class="text-cyan-brand hover:text-cyan-dark">{{ 'auth.reset.requestAnother' | transloco }}</a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class RestablecerPasswordComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  form = this.fb.group({
    token: [this.route.snapshot.queryParamMap.get('token') ?? '', Validators.required],
    newPassword: ['', [Validators.required, passwordPolicy]],
  });

  loading = signal(false);
  error = signal('');
  success = signal(false);
  showPwd = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    const token = String(this.form.value.token ?? '').replace(/\s/g, '');
    const newPassword = String(this.form.value.newPassword ?? '');
    try {
      await this.api.post('/auth/password-reset/confirm', {
        token,
        newPassword,
      });
      this.success.set(true);
      this.router.navigate([], { queryParams: {}, replaceUrl: true });
    } catch (err: any) {
      const fieldMessage = Array.isArray(err.body?.fields) ? err.body.fields[0]?.message : null;
      this.error.set(fieldMessage ?? err.body?.message ?? this.transloco.translate('auth.reset.error'));
    } finally {
      this.loading.set(false);
    }
  }
}
