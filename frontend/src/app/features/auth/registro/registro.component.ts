import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { TranslocoModule, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { Lang } from '../../../core/i18n/languages';
import { LocalizePathPipe } from '../../../shared/pipes/localize-path.pipe';
import { EnumLabelPipe } from '../../../shared/pipes/enum-label.pipe';

const PAISES = [
  'Argentina', 'Bolivia', 'Brasil', 'Chile', 'Colombia', 'Costa Rica',
  'Ecuador', 'El Salvador', 'Guatemala', 'Honduras', 'México', 'Nicaragua',
  'Panamá', 'Paraguay', 'Perú', 'República Dominicana', 'Uruguay', 'Venezuela',
  'Otro',
];

/** Backend `PreferredLanguage` enum names (parsed case-insensitively). */
const PREFERRED_LANGUAGE: Record<Lang, string> = { es: 'Espanol', en: 'English', pt: 'Portugues' };

@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoModule, LocalizePathPipe, EnumLabelPipe],
  providers: [provideTranslocoScope('auth')],
  template: `
    <div class="min-h-screen bg-navy-deepest flex flex-col items-center justify-center px-4 py-12">

      <!-- Logo -->
      <a [routerLink]="'/' | localizePath" class="mb-6">
        <img src="/assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-14 w-auto" />
      </a>

      <div class="w-full max-w-lg">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl shadow-2xl shadow-black/40 overflow-hidden">

          <!-- Header -->
          <div class="px-8 pt-8 pb-6 border-b border-navy-mid">
            <h1 class="font-heading text-3xl mb-1">{{ 'auth.register.title' | transloco }}</h1>
            <p class="text-text-muted text-sm">{{ 'auth.register.subtitle' | transloco }}</p>
          </div>

          <!-- Step indicator -->
          <div class="px-8 py-5 flex items-center gap-0">
            @for (s of [1, 2, 3]; track s) {
              <div class="flex items-center"
                   [class]="s < 3 ? 'flex-1' : ''">
                <div class="step-circle text-sm"
                     [class.active]="step() === s"
                     [class.done]="step() > s">
                  @if (step() > s) {
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
                    </svg>
                  } @else { {{ s }} }
                </div>
                @if (s < 3) {
                  <div class="step-line" [class.done]="step() > s"></div>
                }
              </div>
            }
          </div>

          <!-- Success state -->
          @if (success()) {
            <div class="px-8 py-12 text-center">
              <div class="w-16 h-16 rounded-full bg-success-brand/20 flex items-center justify-center mx-auto mb-4">
                <svg class="w-8 h-8 text-success-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                </svg>
              </div>
              <h2 class="font-heading text-2xl mb-2">{{ 'auth.register.successTitle' | transloco }}</h2>
              <p class="text-text-muted text-sm mb-6">{{ 'auth.register.successText' | transloco }}</p>
              <a [routerLink]="'/login' | localizePath"
                class="inline-block px-6 py-2.5 bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg">
                {{ 'auth.register.login' | transloco }}
              </a>
            </div>
          } @else {

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="px-8 pb-8">

              <!-- Error general -->
              @if (error()) {
                <div class="mt-5 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
                  {{ error() }}
                </div>
              }

              <!-- ── STEP 1: Tipo de cuenta ── -->
              @if (step() === 1) {
                <div class="mt-6 space-y-4">
                  <p class="text-sm text-text-muted mb-4">{{ 'auth.register.howParticipate' | transloco }}</p>
                  <div class="grid grid-cols-2 gap-4">
                    <button type="button" (click)="setTipo('espectador')"
                      class="category-card rounded-xl p-5 text-left"
                      [class.selected]="form.value.tipo === 'espectador'">
                      <div class="text-3xl mb-3">👁️</div>
                      <p class="font-heading text-lg">{{ 'auth.register.spectator' | transloco }}</p>
                      <p class="text-xs text-text-muted mt-1">{{ 'auth.register.spectatorText' | transloco }}</p>
                    </button>
                    <button type="button" (click)="setTipo('competidor')"
                      class="category-card rounded-xl p-5 text-left"
                      [class.selected]="form.value.tipo === 'competidor'">
                      <div class="text-3xl mb-3">🏄</div>
                      <p class="font-heading text-lg">{{ 'auth.register.competitor' | transloco }}</p>
                      <p class="text-xs text-text-muted mt-1">{{ 'auth.register.competitorText' | transloco }}</p>
                    </button>
                  </div>
                  @if (form.controls.tipo.invalid && form.controls.tipo.touched) {
                    <p class="text-xs text-error-brand">{{ 'auth.register.selectType' | transloco }}</p>
                  }
                  <div class="pt-4">
                    <button type="button" (click)="goStep2()"
                      class="w-full py-3 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
                      {{ 'auth.register.continue' | transloco }}
                    </button>
                  </div>
                </div>
              }

              <!-- ── STEP 2: Datos personales ── -->
              @if (step() === 2) {
                <div class="mt-6 space-y-4">
                  <div class="grid grid-cols-2 gap-4">
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.firstName' | transloco }}</label>
                      <input formControlName="nombre" type="text" [placeholder]="'auth.register.firstNamePlaceholder' | transloco" class="input-field"
                        [class.field-error]="invalid('nombre')" />
                      @if (invalid('nombre')) {
                        <p class="mt-1 text-xs text-error-brand">{{ 'common.required' | transloco }}</p>
                      }
                    </div>
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.lastName' | transloco }}</label>
                      <input formControlName="apellido" type="text" [placeholder]="'auth.register.lastNamePlaceholder' | transloco" class="input-field"
                        [class.field-error]="invalid('apellido')" />
                      @if (invalid('apellido')) {
                        <p class="mt-1 text-xs text-error-brand">{{ 'common.required' | transloco }}</p>
                      }
                    </div>
                  </div>
                  <div>
                    <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.email' | transloco }}</label>
                    <input formControlName="email" type="email" autocomplete="email" [placeholder]="'auth.login.emailPlaceholder' | transloco" class="input-field"
                      [class.field-error]="invalid('email')" />
                    @if (invalid('email')) {
                      <p class="mt-1 text-xs text-error-brand">
                        @if (form.controls.email.errors?.['required']) { {{ 'common.required' | transloco }} }
                        @else { {{ 'common.invalidEmail' | transloco }} }
                      </p>
                    }
                  </div>
                  <div>
                    <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">
                      {{ 'auth.register.password' | transloco }} <span class="text-text-muted normal-case">{{ 'auth.register.passwordHint' | transloco }}</span>
                    </label>
                    <div class="relative">
                      <input formControlName="password" [type]="showPwd() ? 'text' : 'password'"
                        autocomplete="new-password" placeholder="••••••••" class="input-field pr-10"
                        [class.field-error]="invalid('password')" />
                      <button type="button" (click)="showPwd.set(!showPwd())" [attr.aria-label]="'auth.login.showPassword' | transloco"
                        class="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted hover:text-text-light">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M15 12a3 3 0 11-6 0 3 3 0 016 0zm-3-9C6.477 3 1 8.477 1 12s5.477 9 11 9 11-5.477 11-9-5.477-9-11-9z"/>
                        </svg>
                      </button>
                    </div>
                    @if (invalid('password')) {
                      <p class="mt-1 text-xs text-error-brand">
                        @if (form.controls.password.errors?.['required']) { {{ 'auth.register.requiredFem' | transloco }} }
                        @else if (form.controls.password.errors?.['minlength']) { {{ 'auth.register.minPassword' | transloco }} }
                        @else { {{ 'auth.register.passwordPattern' | transloco }} }
                      </p>
                    }
                  </div>
                  <div class="grid grid-cols-2 gap-4">
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.country' | transloco }}</label>
                      <select formControlName="pais" class="input-field">
                        <option value="">{{ 'auth.register.select' | transloco }}</option>
                        @for (p of paises; track p) {
                          <option [value]="p">{{ 'common.countries.' + p | transloco }}</option>
                        }
                      </select>
                    </div>
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.phone' | transloco }}</label>
                      <input formControlName="telefono" type="tel" placeholder="+56 9 1234 5678" class="input-field" />
                    </div>
                  </div>
                  <div class="flex gap-3 pt-2">
                    <button type="button" (click)="step.set(1)"
                      class="flex-1 py-3 border border-navy-mid text-text-muted font-accent uppercase text-sm rounded-lg hover:border-cyan-brand/50 transition">
                      {{ 'auth.register.back' | transloco }}
                    </button>
                    <button type="button" (click)="goStep3()"
                      class="flex-1 py-3 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
                      {{ 'auth.register.continue' | transloco }}
                    </button>
                  </div>
                </div>
              }

              <!-- ── STEP 3: Datos de competidor + Términos ── -->
              @if (step() === 3) {
                <div class="mt-6 space-y-4">

                  @if (esCompetidor()) {
                    <p class="font-accent uppercase text-xs tracking-wider text-cyan-brand mb-2">{{ 'auth.register.competitorData' | transloco }}</p>
                    <div class="grid grid-cols-2 gap-4">
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.birthDate' | transloco }}</label>
                        <input formControlName="fechaNacimiento" type="date" class="input-field"
                          [class.field-error]="invalid('fechaNacimiento')" />
                        @if (invalid('fechaNacimiento')) {
                          <p class="mt-1 text-xs text-error-brand">{{ 'auth.register.requiredFem' | transloco }}</p>
                        }
                      </div>
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.gender' | transloco }}</label>
                        <select formControlName="genero" class="input-field">
                          <option value="">{{ 'auth.register.select' | transloco }}</option>
                          <option value="Masculino">{{ 'Masculino' | enumLabel: 'gender' }}</option>
                          <option value="Femenino">{{ 'Femenino' | enumLabel: 'gender' }}</option>
                        </select>
                      </div>
                    </div>
                    <div class="grid grid-cols-2 gap-4">
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.stance' | transloco }}</label>
                        <select formControlName="postura" class="input-field">
                          <option value="">{{ 'auth.register.select' | transloco }}</option>
                          <option value="Regular">Regular</option>
                          <option value="Goofy">Goofy</option>
                        </select>
                      </div>
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.shirtSize' | transloco }}</label>
                        <select formControlName="tallaCamiseta" class="input-field">
                          <option value="">{{ 'auth.register.select' | transloco }}</option>
                          @for (t of ['XS','S','M','L','XL','XXL']; track t) {
                            <option [value]="t">{{ t }}</option>
                          }
                        </select>
                      </div>
                    </div>
                                    <div class="grid grid-cols-2 gap-4">
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.club' | transloco }}</label>
                        <input formControlName="club" type="text" [placeholder]="'auth.register.optional' | transloco" class="input-field" />
                      </div>
                      <div>
                        <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.federation' | transloco }}</label>
                        <input formControlName="federacion" type="text" [placeholder]="'auth.register.federationPlaceholder' | transloco" class="input-field" />
                      </div>
                    </div>
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.sponsors' | transloco }}</label>
                      <input formControlName="patrocinadores" type="text" [placeholder]="'auth.register.sponsorsPlaceholder' | transloco" class="input-field" />
                    </div>
                    <div>
                      <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'auth.register.idDocument' | transloco }}</label>
                      <input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" class="input-field"
                        (change)="onIdentityDocumentSelected($event)" [class.field-error]="invalid('identityDocument')" />
                      <p class="mt-1 text-xs text-text-muted">{{ 'auth.register.idDocumentHint' | transloco }}</p>
                      @if (invalid('identityDocument')) {
                        <p class="mt-1 text-xs text-error-brand">{{ 'auth.register.idDocumentRequired' | transloco }}</p>
                      }
                    </div>

                    <!-- Reglamento -->
                    <label class="flex items-start gap-3 cursor-pointer pt-2">
                      <input formControlName="reglamento" type="checkbox"
                        class="mt-0.5 w-4 h-4 rounded border-navy-mid bg-navy-deepest accent-cyan-brand flex-shrink-0" />
                      <span class="text-sm text-text-muted">
                        {{ 'auth.register.rulesAccept' | transloco }}
                        <a href="/reglamento.pdf" target="_blank" class="text-cyan-brand hover:underline">{{ 'auth.register.rulesLink' | transloco }}</a>
                        <span class="text-error-brand ml-0.5">*</span>
                      </span>
                    </label>
                    @if (form.controls.reglamento.invalid && form.controls.reglamento.touched) {
                      <p class="text-xs text-error-brand -mt-2">{{ 'auth.register.rulesRequired' | transloco }}</p>
                    }
                  }

                  <!-- Términos -->
                  <label class="flex items-start gap-3 cursor-pointer pt-1">
                    <input formControlName="terminos" type="checkbox"
                      class="mt-0.5 w-4 h-4 rounded border-navy-mid bg-navy-deepest accent-cyan-brand flex-shrink-0" />
                    <span class="text-sm text-text-muted">
                      {{ 'auth.register.termsAccept' | transloco }}
                      <a href="/terminos" target="_blank" class="text-cyan-brand hover:underline">{{ 'auth.register.termsLink' | transloco }}</a>
                      {{ 'auth.register.and' | transloco }}
                      <a href="/privacidad" target="_blank" class="text-cyan-brand hover:underline">{{ 'auth.register.privacyLink' | transloco }}</a>
                      <span class="text-error-brand ml-0.5">*</span>
                    </span>
                  </label>
                  @if (form.controls.terminos.invalid && form.controls.terminos.touched) {
                    <p class="text-xs text-error-brand -mt-2">{{ 'auth.register.termsRequired' | transloco }}</p>
                  }

                  <div class="flex gap-3 pt-2">
                    <button type="button" (click)="step.set(2)"
                      class="flex-1 py-3 border border-navy-mid text-text-muted font-accent uppercase text-sm rounded-lg hover:border-cyan-brand/50 transition">
                      {{ 'auth.register.back' | transloco }}
                    </button>
                    <button type="submit" [disabled]="loading()"
                      class="flex-1 py-3 bg-orange-brand hover:bg-orange-light disabled:opacity-60 text-white font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
                      @if (loading()) {
                        <span class="inline-flex items-center justify-center gap-2">
                          <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                          </svg>
                          {{ 'auth.register.creating' | transloco }}
                        </span>
                      } @else {
                        {{ 'auth.register.submit' | transloco }}
                      }
                    </button>
                  </div>
                </div>
              }

            </form>
          }

          <div class="px-8 py-5 border-t border-navy-mid text-center">
            <p class="text-sm text-text-muted">
              {{ 'auth.register.haveAccount' | transloco }}
              <a [routerLink]="'/login' | localizePath" class="text-cyan-brand hover:text-cyan-dark ml-1">{{ 'auth.register.login' | transloco }}</a>
            </p>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class RegistroComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private router = inject(Router);
  private language = inject(LanguageService);
  private transloco = inject(TranslocoService);

  readonly paises = PAISES;

  step = signal(1);
  loading = signal(false);
  error = signal('');
  success = signal(false);
  showPwd = signal(false);

  esCompetidor = computed(() => this.form.value.tipo === 'competidor');

  form = this.fb.group({
    tipo: ['', Validators.required],
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    apellido: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)]],
    pais: [''],
    telefono: [''],
    genero: [''],
    fechaNacimiento: [''],
    postura: [''],
    club: [''],
    tallaCamiseta: [''],
    federacion: [''],
    patrocinadores: [''],
    terminos: [false, Validators.requiredTrue],
    reglamento: [false],
    identityDocument: [null as File | null],
  });

  invalid(field: string): boolean {
    const c = this.form.get(field);
    return !!(c?.invalid && c.touched);
  }

  setTipo(tipo: 'espectador' | 'competidor'): void {
    this.form.patchValue({ tipo });
  }

  onIdentityDocumentSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.form.controls.identityDocument.setValue(file);
    this.form.controls.identityDocument.setErrors(file ? null : { required: true });
    this.form.controls.identityDocument.markAsTouched();
  }

  goStep2(): void {
    this.form.controls.tipo.markAsTouched();
    if (!this.form.value.tipo) return;
    this.step.set(2);
  }

  goStep3(): void {
    const fields: (keyof typeof this.form.controls)[] = ['nombre', 'apellido', 'email', 'password'];
    fields.forEach(f => this.form.controls[f].markAsTouched());
    if (fields.some(f => this.form.controls[f].invalid)) return;
    this.step.set(3);
  }

  async submit(): Promise<void> {
    this.form.controls.terminos.markAsTouched();
    if (this.esCompetidor()) {
      this.form.controls.reglamento.markAsTouched();
      this.form.controls.fechaNacimiento.markAsTouched();
      this.form.controls.identityDocument.markAsTouched();
    }
    if (this.form.controls.terminos.invalid) return;
    if (this.esCompetidor() && !this.form.value.reglamento) return;
    if (this.esCompetidor() && !this.form.value.identityDocument) {
      this.form.controls.identityDocument.setErrors({ required: true });
      return;
    }

    this.loading.set(true);
    this.error.set('');
    try {
      const v = this.form.value;
      const body = new FormData();
      body.append('email', v.email ?? '');
      body.append('password', v.password ?? '');
      body.append('nombre', v.nombre ?? '');
      body.append('apellido', v.apellido ?? '');
      body.append('tipo', v.tipo ?? '');
      body.append('terminos', 'true');
      body.append('newsletter', 'true');
      // Drives the language of transactional emails sent to this account.
      body.append('idiomaPreferido', PREFERRED_LANGUAGE[this.language.activeLang()]);
      if (v.pais) body.append('pais', v.pais);
      if (v.telefono) body.append('telefono', v.telefono);
      if (this.esCompetidor()) {
        body.append('reglamento', 'true');
        if (v.fechaNacimiento) body.append('fechaNacimiento', v.fechaNacimiento);
        if (v.genero) body.append('genero', v.genero);
        if (v.postura) body.append('postura', v.postura);
        if (v.club) body.append('club', v.club);
        if (v.tallaCamiseta) body.append('tallaCamiseta', v.tallaCamiseta);
        body.append('federacion', v.federacion ?? '');
        body.append('patrocinadores', v.patrocinadores ?? '');
        if (v.identityDocument) body.append('identityDocument', v.identityDocument);
      }
      await this.api.upload('/auth/register', body);
      this.success.set(true);
    } catch (err: any) {
      this.error.set(err.body?.message ?? this.transloco.translate('auth.register.error'));
    } finally {
      this.loading.set(false);
    }
  }
}
