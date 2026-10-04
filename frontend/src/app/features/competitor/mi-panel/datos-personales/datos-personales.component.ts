import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { TranslocoModule, provideTranslocoScope } from '@jsverse/transloco';

const PAISES = [
  'Argentina','Bolivia','Brasil','Chile','Colombia','Costa Rica',
  'Ecuador','El Salvador','Guatemala','Honduras','México','Nicaragua',
  'Panamá','Paraguay','Perú','República Dominicana','Uruguay','Venezuela','Otro',
];

@Component({
  selector: 'app-datos-personales',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoModule],
  providers: [provideTranslocoScope('competitor')],
  template: `
    <div class="max-w-2xl">
      <h2 class="font-heading text-3xl mb-1">{{ 'competitor.personal.title' | transloco }}</h2>
      <p class="text-text-muted text-sm mb-8">{{ 'competitor.personal.intro' | transloco }}</p>

      @if (successMsg()) {
        <div class="mb-6 px-4 py-3 rounded-lg bg-success-brand/10 border border-success-brand/30 text-success-brand text-sm">
          {{ successMsg() | transloco }}
        </div>
      }
      @if (errorMsg()) {
        <div class="mb-6 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
          {{ errorMsg() | transloco }}
        </div>
      }

      @if (loading()) {
        <div class="space-y-4">
          @for (sk of skeletons; track sk) { <div class="skeleton h-14 rounded-lg"></div> }
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="save()" novalidate class="space-y-5">

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.firstName' | transloco }}</label>
              <input formControlName="nombre" type="text" class="input-field"
                     [class.field-error]="form.controls.nombre.invalid && form.controls.nombre.touched" />
              @if (form.controls.nombre.invalid && form.controls.nombre.touched) {
                <p class="mt-1 text-xs text-error-brand">{{ 'common.required' | transloco }}</p>
              }
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.lastName' | transloco }}</label>
              <input formControlName="apellido" type="text" class="input-field"
                     [class.field-error]="form.controls.apellido.invalid && form.controls.apellido.touched" />
              @if (form.controls.apellido.invalid && form.controls.apellido.touched) {
                <p class="mt-1 text-xs text-error-brand">{{ 'common.required' | transloco }}</p>
              }
            </div>
          </div>

          <div>
            <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.email' | transloco }}</label>
            <input formControlName="email" type="email" class="input-field opacity-60" readonly />
            <p class="mt-1 text-xs text-text-muted">{{ 'competitor.personal.emailLocked' | transloco }}</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.phone' | transloco }}</label>
              <input formControlName="telefono" type="tel" class="input-field" placeholder="+51 999 999 999" />
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.country' | transloco }}</label>
              <select formControlName="pais" class="input-field">
                <option value="">{{ 'competitor.personal.select' | transloco }}</option>
                @for (p of paises; track p) { <option [value]="p">{{ 'common.countries.' + p | transloco }}</option> }
              </select>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.stance' | transloco }}</label>
              <select formControlName="postura" class="input-field">
                <option value="">{{ 'competitor.personal.select' | transloco }}</option>
                <option value="Regular">{{ 'competitor.personal.regular' | transloco }}</option>
                <option value="Goofy">{{ 'competitor.personal.goofy' | transloco }}</option>
              </select>
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.shirtSize' | transloco }}</label>
              <select formControlName="tallaCamiseta" class="input-field">
                <option value="">—</option>
                @for (t of tallas; track t) { <option [value]="t">{{ t }}</option> }
              </select>
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">{{ 'competitor.personal.club' | transloco }}</label>
              <input formControlName="club" type="text" class="input-field" [placeholder]="'competitor.personal.clubPlaceholder' | transloco" />
            </div>
          </div>

          <div class="pt-4 border-t border-navy-mid flex items-center justify-end">
            <button type="submit" [disabled]="saving() || form.invalid"
                    class="px-8 py-3 rounded-md font-accent uppercase tracking-wider text-sm transition"
                    [class]="!saving() && form.valid ? 'bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-bold' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
              {{ (saving() ? 'competitor.personal.saving' : 'competitor.personal.save') | transloco }}
            </button>
          </div>
        </form>
      }
    </div>
  `,
})
export class DatosPersonalesComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);
  successMsg = signal('');
  errorMsg = signal('');
  private currentCompetitor = signal<any | null>(null);
  readonly skeletons = [1, 2, 3, 4];
  readonly paises = PAISES;
  readonly tallas = ['XS', 'S', 'M', 'L', 'XL', 'XXL'];

  form = this.fb.group({
    nombre:       ['', Validators.required],
    apellido:     ['', Validators.required],
    email:        [{ value: '', disabled: true }],
    telefono:     [''],
    pais:         [''],
    postura:      [''],
    tallaCamiseta:[''],
    club:         [''],
  });

  ngOnInit(): void { this.load(); }

  private async load(): Promise<void> {
    this.errorMsg.set('');
    try {
      const competitorId = await this.resolveCompetitorId();
      if (!competitorId) {
        this.form.patchValue({ email: this.auth.currentUser()?.email ?? '' });
        this.errorMsg.set('competitor.personal.noProfile');
        return;
      }

      const res = await this.api.get<any>(`/competitors/${competitorId}`);
      const d = res?.data ?? res;
      this.currentCompetitor.set(d);
      this.form.patchValue({
        nombre:        d?.nombre ?? d?.firstName ?? '',
        apellido:      d?.apellido ?? d?.lastName ?? '',
        email:         d?.email ?? this.auth.currentUser()?.email ?? '',
        telefono:      d?.telefono ?? '',
        pais:          d?.pais ?? '',
        postura:       d?.postura ?? '',
        tallaCamiseta: d?.tallaCamiseta ?? '',
        club:          d?.club ?? '',
      });
    } catch {
      this.form.patchValue({ email: this.auth.currentUser()?.email ?? '' });
      this.errorMsg.set('competitor.personal.loadError');
    } finally {
      this.loading.set(false);
    }
  }

  private async resolveCompetitorId(): Promise<string | undefined> {
    const user = this.auth.currentUser();
    if (user?.competitorId) return user.competitorId;
    if (!user?.email) return undefined;

    const competitor = await this.api.get<any>('/competitors/me');
    const competitorId = competitor?.id;
    if (competitorId) {
      const token = this.auth.getToken();
      if (token) this.auth.setSession(token, { ...user, competitorId });
    }

    return competitorId;
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.successMsg.set('');
    this.errorMsg.set('');
    try {
      const competitorId = await this.resolveCompetitorId();
      if (!competitorId) {
        this.errorMsg.set('competitor.personal.noProfile');
        return;
      }

      await this.api.put(`/competitors/${competitorId}`, this.buildUpdatePayload());
      this.successMsg.set('competitor.personal.saved');
    } catch (err: any) {
      this.errorMsg.set(err?.body?.message ?? 'competitor.personal.saveError');
    } finally {
      this.saving.set(false);
    }
  }

  private buildUpdatePayload(): Record<string, unknown> {
    const original = this.currentCompetitor() ?? {};
    const formValue = this.form.getRawValue();

    return {
      nombre: formValue.nombre,
      apellido: formValue.apellido,
      email: original.email ?? formValue.email ?? this.auth.currentUser()?.email ?? '',
      fechaNacimiento: original.fechaNacimiento ?? original.birthDate ?? '2000-01-01',
      genero: original.genero ?? original.gender ?? 'Masculino',
      pais: formValue.pais || original.pais || '',
      telefono: formValue.telefono ?? original.telefono ?? '',
      club: formValue.club ?? original.club ?? '',
      postura: formValue.postura || original.postura || 'Regular',
      tallaCamiseta: formValue.tallaCamiseta || original.tallaCamiseta || 'M',
      numeroCamiseta: original.numeroCamiseta ?? original.shirtNumber ?? '',
      patrocinadores: original.patrocinadores ?? original.sponsors ?? '',
      federacion: original.federacion ?? '',
    };
  }
}
