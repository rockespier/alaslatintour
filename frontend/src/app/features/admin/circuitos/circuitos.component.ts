import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { ImportExcelModalComponent } from '../../../shared/components/import-excel-modal/import-excel-modal.component';

interface Circuit {
  id: string;
  nombre: string;
  temporada: number;
  descripcion?: string;
  region: string;
  modalidad: string;
  estado: string;
  surfScoresCode?: string;
  eventsCount?: number;
  competidoresCount?: number;
  totalPrizeUsd?: number;
  lastSyncAt?: string | null;
  createdAt?: string;
}

const REGIONS = ['Latinoamérica', 'América del Sur', 'América Central', 'América del Norte'];
const MODALIDADES = ['Shortboard', 'Longboard', 'Mixed'];
const ESTADOS = ['Activo', 'Borrador', 'Archivado', 'Próximo'];

@Component({
  selector: 'app-circuitos',
  standalone: true,
  imports: [ReactiveFormsModule, ImportExcelModalComponent],
  template: `
    <div class="py-8">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-3xl font-heading text-white">Circuitos</h1>
          <p class="text-text-muted text-sm mt-1">Gestión de circuitos del ALAS Latin Tour.</p>
        </div>
        @if (canEdit()) {
          <div class="flex items-center gap-2">
            <button (click)="downloadTemplate()"
                    class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase text-xs tracking-wider rounded-md transition">
              Descargar plantilla
            </button>
            <button (click)="openImport()"
                    class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase text-xs tracking-wider rounded-md transition">
              Importar Excel
            </button>
            <button (click)="openCreate()"
                    class="flex items-center gap-2 px-4 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-sm tracking-wider hover:bg-cyan-dark transition">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
              </svg>
              Nuevo circuito
            </button>
          </div>
        }
      </div>

      <!-- Stat cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Circuitos Activos</p>
          <p class="font-heading text-4xl text-cyan-brand">{{ statActivos() }}</p>
        </div>
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Total Eventos en Circuitos</p>
          <p class="font-heading text-4xl text-text-light">{{ statTotalEventos() }}</p>
        </div>
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Competidores Inscritos</p>
          <p class="font-heading text-4xl text-success-brand">{{ statTotalCompetidores() }}</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex gap-2 mb-5">
        @for (f of estadoFilters; track f.value) {
          <button (click)="filterEstado.set(f.value)"
                  [class]="filterEstado() === f.value
                    ? 'px-3 py-1.5 rounded-md bg-cyan-brand text-navy-deepest text-xs font-accent uppercase tracking-wider'
                    : 'px-3 py-1.5 rounded-md border border-navy-mid text-text-muted text-xs font-accent uppercase tracking-wider hover:border-cyan-brand hover:text-text-light transition'">
            {{ f.label }}
          </button>
        }
      </div>

      <!-- Table -->
      <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
        @if (loading()) {
          <div class="divide-y divide-navy-mid">
            @for (sk of [1,2,3,4]; track sk) {
              <div class="px-6 py-4 flex items-center gap-4">
                <div class="skeleton h-4 rounded w-40"></div>
                <div class="skeleton h-4 rounded w-16 ml-4"></div>
                <div class="skeleton h-4 rounded w-24 ml-4"></div>
                <div class="skeleton h-4 rounded w-20 ml-4"></div>
                <div class="ml-auto skeleton h-4 rounded w-16"></div>
              </div>
            }
          </div>
        } @else if (filtered().length === 0) {
          <div class="py-16 text-center text-text-muted text-sm">No hay circuitos registrados.</div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm text-left">
              <thead class="bg-navy-mid/40 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="px-6 py-3">Nombre</th>
                  <th class="px-4 py-3">Temporada</th>
                  <th class="px-4 py-3">Región</th>
                  <th class="px-4 py-3">Modalidad</th>
                  <th class="px-4 py-3">Estado</th>
                  <th class="px-4 py-3 text-center">Eventos</th>
                  <th class="px-4 py-3 text-center">Competidores</th>
                  <th class="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (c of filtered(); track c.id) {
                  <tr class="hover:bg-navy-mid/20 transition">
                    <td class="px-6 py-4">
                      <p class="font-medium text-text-light">{{ c.nombre }}</p>
                      @if (c.surfScoresCode) {
                        <p class="text-[10px] text-text-muted mt-0.5 font-accent tracking-wider">{{ c.surfScoresCode }}</p>
                      }
                    </td>
                    <td class="px-4 py-4 text-text-muted">{{ c.temporada }}</td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ c.region }}</td>
                    <td class="px-4 py-4 text-text-muted">{{ c.modalidad }}</td>
                    <td class="px-4 py-4">
                      <span [class]="estadoClass(c.estado)">{{ c.estado }}</span>
                    </td>
                    <td class="px-4 py-4 text-center text-text-muted">{{ c.eventsCount ?? 0 }}</td>
                    <td class="px-4 py-4 text-center text-text-muted">{{ c.competidoresCount ?? 0 }}</td>
                    <td class="px-4 py-4 text-right">
                      @if (canEdit()) {
                        <div class="flex items-center justify-end gap-2">
                          <button (click)="openEdit(c)"
                                  class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-cyan-brand" title="Editar">
                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.5L16.732 3.732z"/>
                            </svg>
                          </button>
                          <button (click)="confirmDelete(c)"
                                  class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-error-brand" title="Eliminar">
                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                            </svg>
                          </button>
                        </div>
                      } @else {
                        <span class="text-xs text-text-muted">—</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>

    <!-- Modal -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">
              {{ editingId() ? 'Editar circuito' : 'Nuevo circuito' }}
            </h2>
            <button (click)="closeModal()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()" class="p-6 space-y-5">

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nombre *</label>
              <input formControlName="nombre" type="text" placeholder="Ej: ALAS Latin Tour 2026"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              @if (form.get('nombre')?.invalid && form.get('nombre')?.touched) {
                <p class="text-error-brand text-xs mt-1">El nombre es obligatorio.</p>
              }
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Temporada *</label>
                <input formControlName="temporada" type="number" placeholder="2026"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Código SurfScores</label>
                <input formControlName="surfScoresCode" type="text" placeholder="ALT2026"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Descripción</label>
              <textarea formControlName="descripcion" rows="2" placeholder="Descripción del circuito..."
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition resize-none"></textarea>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Región *</label>
                <select formControlName="region"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (r of regions; track r) { <option [value]="r">{{ r }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Modalidad *</label>
                <select formControlName="modalidad"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (m of modalidades; track m) { <option [value]="m">{{ m }}</option> }
                </select>
              </div>
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Estado *</label>
              <select formControlName="estado"
                      class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                @for (e of estados; track e) { <option [value]="e">{{ e }}</option> }
              </select>
            </div>

            <div class="flex justify-end gap-3 pt-2">
              <button type="button" (click)="closeModal()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="saving() || form.invalid"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-xs tracking-wider hover:bg-cyan-dark transition disabled:opacity-50">
                {{ saving() ? 'Guardando...' : (editingId() ? 'Guardar cambios' : 'Crear circuito') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <app-import-excel-modal [open]="importOpen()" importPath="/circuits/import" entityLabel="circuitos"
                             (close)="importOpen.set(false)" (imported)="onImported()" />

    <!-- Confirm Delete -->
    @if (deleteTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)">
        <div class="bg-navy-dark border border-error-brand/40 rounded-xl w-full max-w-sm p-6">
          <h3 class="font-heading text-lg text-white mb-2">Eliminar circuito</h3>
          <p class="text-text-muted text-sm mb-5">
            ¿Eliminar <strong class="text-text-light">{{ deleteTarget()!.nombre }}</strong>?
            Esta acción no se puede deshacer.
          </p>
          <div class="flex justify-end gap-3">
            <button (click)="deleteTarget.set(null)"
                    class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
              Cancelar
            </button>
            <button (click)="doDelete()" [disabled]="saving()"
                    class="px-4 py-2 rounded-md bg-error-brand text-white font-accent uppercase text-xs tracking-wider hover:bg-error-brand/80 transition disabled:opacity-50">
              {{ saving() ? 'Eliminando...' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class CircuitosComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Circuitos'));

  loading = signal(true);
  saving = signal(false);
  circuits = signal<Circuit[]>([]);
  filterEstado = signal<string>('todos');
  modalOpen = signal(false);
  editingId = signal<string | null>(null);
  deleteTarget = signal<Circuit | null>(null);

  importOpen = signal(false);

  regions = REGIONS;
  modalidades = MODALIDADES;
  estados = ESTADOS;

  estadoFilters = [
    { label: 'Todos', value: 'todos' },
    ...ESTADOS.map(e => ({ label: e, value: e })),
  ];

  form = this.fb.group({
    nombre: ['', Validators.required],
    temporada: [new Date().getFullYear(), Validators.required],
    descripcion: [''],
    region: ['Latinoamérica', Validators.required],
    modalidad: ['Shortboard', Validators.required],
    estado: ['Borrador', Validators.required],
    surfScoresCode: [''],
  });

  filtered = computed(() => {
    const f = this.filterEstado();
    const list = this.circuits();
    return f === 'todos' ? list : list.filter(c => c.estado === f);
  });

  statActivos = computed(() => this.circuits().filter(c => c.estado === 'Activo').length);
  statTotalEventos = computed(() => this.circuits().reduce((sum, c) => sum + (c.eventsCount ?? 0), 0));
  statTotalCompetidores = computed(() => this.circuits().reduce((sum, c) => sum + (c.competidoresCount ?? 0), 0));

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const res = await this.api.get<any>('/circuits');
      this.circuits.set(res?.data ?? res ?? []);
    } catch {
      this.circuits.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      nombre: '', temporada: new Date().getFullYear(), descripcion: '',
      region: 'Latinoamérica', modalidad: 'Shortboard', estado: 'Borrador', surfScoresCode: '',
    });
    this.modalOpen.set(true);
  }

  openEdit(c: Circuit): void {
    this.editingId.set(c.id);
    this.form.reset({
      nombre: c.nombre,
      temporada: c.temporada,
      descripcion: c.descripcion ?? '',
      region: c.region,
      modalidad: c.modalidad,
      estado: c.estado,
      surfScoresCode: c.surfScoresCode ?? '',
    });
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    try {
      const v = this.form.getRawValue();
      const body = {
        nombre: v.nombre,
        temporada: Number(v.temporada),
        descripcion: v.descripcion || '',
        region: v.region,
        modalidad: v.modalidad,
        estado: v.estado,
        surfScoresCode: v.surfScoresCode || '',
      };
      const id = this.editingId();
      if (id) {
        await this.api.put(`/circuits/${id}`, body);
      } else {
        await this.api.post('/circuits', body);
      }
      this.closeModal();
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  confirmDelete(c: Circuit): void { this.deleteTarget.set(c); }

  async doDelete(): Promise<void> {
    const c = this.deleteTarget();
    if (!c) return;
    this.saving.set(true);
    try {
      await this.api.delete(`/circuits/${c.id}`);
      this.deleteTarget.set(null);
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  async downloadTemplate(): Promise<void> {
    await this.api.downloadFile('/circuits/template', 'circuits-template.xlsx');
  }

  openImport(): void {
    this.importOpen.set(true);
  }

  async onImported(): Promise<void> {
    await this.load();
  }

  estadoClass(estado: string): string {
    const map: Record<string, string> = {
      'Activo': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand',
      'Borrador': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-warning-brand/15 text-warning-brand',
      'Próximo': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand',
      'Archivado': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-navy-mid/50 text-text-muted',
    };
    return map[estado] ?? map['Archivado'];
  }
}
