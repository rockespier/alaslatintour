import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';

const PAISES = [
  'Argentina','Bolivia','Brasil','Chile','Colombia','Costa Rica',
  'Ecuador','El Salvador','Guatemala','Honduras','México','Nicaragua',
  'Panamá','Paraguay','Perú','República Dominicana','Uruguay','Venezuela','Otro',
];

@Component({
  selector: 'app-datos-personales',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="max-w-2xl">
      <h2 class="font-heading text-3xl mb-1">Mis Datos</h2>
      <p class="text-text-muted text-sm mb-8">Actualiza tu información personal para el circuito.</p>

      @if (successMsg()) {
        <div class="mb-6 px-4 py-3 rounded-lg bg-success-brand/10 border border-success-brand/30 text-success-brand text-sm">
          {{ successMsg() }}
        </div>
      }
      @if (errorMsg()) {
        <div class="mb-6 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
          {{ errorMsg() }}
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
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Nombre</label>
              <input formControlName="nombre" type="text" class="input-field"
                     [class.field-error]="form.controls.nombre.invalid && form.controls.nombre.touched" />
              @if (form.controls.nombre.invalid && form.controls.nombre.touched) {
                <p class="mt-1 text-xs text-error-brand">Requerido</p>
              }
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Apellido</label>
              <input formControlName="apellido" type="text" class="input-field"
                     [class.field-error]="form.controls.apellido.invalid && form.controls.apellido.touched" />
              @if (form.controls.apellido.invalid && form.controls.apellido.touched) {
                <p class="mt-1 text-xs text-error-brand">Requerido</p>
              }
            </div>
          </div>

          <div>
            <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Correo electrónico</label>
            <input formControlName="email" type="email" class="input-field opacity-60" readonly />
            <p class="mt-1 text-xs text-text-muted">El correo no puede modificarse. Contáctanos si necesitas cambiarlo.</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Teléfono</label>
              <input formControlName="telefono" type="tel" class="input-field" placeholder="+51 999 999 999" />
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">País</label>
              <select formControlName="pais" class="input-field">
                <option value="">Seleccionar...</option>
                @for (p of paises; track p) { <option [value]="p">{{ p }}</option> }
              </select>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Postura</label>
              <select formControlName="postura" class="input-field">
                <option value="">Seleccionar...</option>
                <option value="Regular">Regular (izquierda)</option>
                <option value="Goofy">Goofy (derecha)</option>
              </select>
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Talla camiseta</label>
              <select formControlName="tallaCamiseta" class="input-field">
                <option value="">—</option>
                @for (t of tallas; track t) { <option [value]="t">{{ t }}</option> }
              </select>
            </div>
            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Club / Academia</label>
              <input formControlName="club" type="text" class="input-field" placeholder="Nombre del club" />
            </div>
          </div>

          <div class="pt-4 border-t border-navy-mid flex items-center justify-end">
            <button type="submit" [disabled]="saving() || form.invalid"
                    class="px-8 py-3 rounded-md font-accent uppercase tracking-wider text-sm transition"
                    [class]="!saving() && form.valid ? 'bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-bold' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
              {{ saving() ? 'Guardando...' : 'Guardar cambios' }}
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
    const userId = this.auth.currentUser()?.id;
    try {
      const res = await this.api.get<any>(`/competitors/${userId}`);
      const d = res?.data ?? res;
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
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.successMsg.set('');
    this.errorMsg.set('');
    try {
      const userId = this.auth.currentUser()?.id;
      await this.api.put(`/competitors/${userId}`, this.form.getRawValue());
      this.successMsg.set('Datos actualizados correctamente.');
    } catch (err: any) {
      this.errorMsg.set(err?.body?.message ?? 'No se pudo guardar. Intenta de nuevo.');
    } finally {
      this.saving.set(false);
    }
  }
}
