import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

interface Category {
  id: string;
  nombre: string;
  descripcion?: string;
  gender: 'Masculino' | 'Femenino' | 'Ambos';
  ageRestriction: boolean;
  minAge?: number | null;
  maxAge?: number | null;
  successorCategory?: { id: string; nombre: string } | null;
  status: 'Activo' | 'Inactivo';
  createdAt?: string;
  surfScoresCode?: string | null;
}

@Component({
  selector: 'app-categorias',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <!-- Header -->
    <div class="py-8">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-3xl font-heading text-white">Categorías</h1>
          <p class="text-text-muted text-sm mt-1">Gestión de categorías de competencia y sus tarifas base.</p>
        </div>
        <button (click)="openCreate()"
                class="flex items-center gap-2 px-4 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-sm tracking-wider hover:bg-cyan-dark transition">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          Nueva categoría
        </button>
      </div>

      <!-- Filters -->
      <div class="flex gap-2 mb-5">
        @for (f of statusFilters; track f.value) {
          <button (click)="filterStatus.set(f.value)"
                  [class]="filterStatus() === f.value
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
            @for (sk of [1,2,3,4,5]; track sk) {
              <div class="px-6 py-4 flex items-center gap-4">
                <div class="skeleton h-4 rounded w-40"></div>
                <div class="skeleton h-4 rounded w-20 ml-4"></div>
                <div class="skeleton h-4 rounded w-24 ml-4"></div>
                <div class="skeleton h-4 rounded w-28 ml-4"></div>
                <div class="ml-auto skeleton h-4 rounded w-16"></div>
              </div>
            }
          </div>
        } @else if (filtered().length === 0) {
          <div class="py-16 text-center text-text-muted text-sm">No hay categorías registradas.</div>
        } @else {
          <table class="w-full text-sm text-left">
            <thead class="bg-navy-mid/40 text-text-muted font-accent uppercase tracking-wider text-xs">
              <tr>
                <th class="px-6 py-3">Nombre</th>
                <th class="px-4 py-3">Género</th>
                <th class="px-4 py-3">Edad</th>
                <th class="px-4 py-3">Categoría sucesora</th>
                <th class="px-4 py-3">Estado</th>
                <th class="px-4 py-3 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-navy-mid">
              @for (cat of filtered(); track cat.id) {
                <tr class="hover:bg-navy-mid/20 transition">
                  <td class="px-6 py-4 font-medium text-text-light">{{ cat.nombre }}</td>
                  <td class="px-4 py-4 text-text-muted">{{ cat.gender }}</td>
                  <td class="px-4 py-4 text-text-muted">
                    @if (cat.ageRestriction) {
                      {{ cat.minAge }}–{{ cat.maxAge }} años
                    } @else {
                      <span class="text-text-muted/50">Sin restricción</span>
                    }
                  </td>
                  <td class="px-4 py-4 text-text-muted">
                    {{ cat.successorCategory?.nombre ?? '—' }}
                  </td>
                  <td class="px-4 py-4">
                    <span [class]="cat.status === 'Activo'
                      ? 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand'
                      : 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-navy-mid/50 text-text-muted'">
                      {{ cat.status }}
                    </span>
                  </td>
                  <td class="px-4 py-4 text-right">
                    <div class="flex items-center justify-end gap-2">
                      <button (click)="openEdit(cat)"
                              class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-cyan-brand"
                              title="Editar">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.5L16.732 3.732z"/>
                        </svg>
                      </button>
                      <button (click)="confirmDelete(cat)"
                              class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-error-brand"
                              title="Eliminar">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>
    </div>

    <!-- Modal Create/Edit -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)"
           (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">
              {{ editingId() ? 'Editar categoría' : 'Nueva categoría' }}
            </h2>
            <button (click)="closeModal()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()" class="p-6 space-y-5">

            <!-- Nombre -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nombre *</label>
              <input formControlName="nombre" type="text" placeholder="Ej: Sub-16 Masculino"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              @if (form.get('nombre')?.invalid && form.get('nombre')?.touched) {
                <p class="text-error-brand text-xs mt-1">El nombre es obligatorio.</p>
              }
            </div>

            <!-- Descripción -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Descripción</label>
              <textarea formControlName="descripcion" rows="2" placeholder="Descripción opcional..."
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition resize-none"></textarea>
            </div>

            <!-- Género / Estado -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Género *</label>
                <select formControlName="gender"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="Masculino">Masculino</option>
                  <option value="Femenino">Femenino</option>
                  <option value="Ambos">Ambos</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Estado *</label>
                <select formControlName="status"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="Activo">Activo</option>
                  <option value="Inactivo">Inactivo</option>
                </select>
              </div>
            </div>

            <!-- Restricción de edad -->
            <div>
              <label class="flex items-center gap-3 cursor-pointer">
                <div class="relative">
                  <input type="checkbox" formControlName="ageRestriction" class="sr-only peer">
                  <div class="w-10 h-5 bg-navy-mid rounded-full peer peer-checked:bg-cyan-brand transition"></div>
                  <div class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full transition peer-checked:translate-x-5"></div>
                </div>
                <span class="text-sm text-text-light">Restricción de edad</span>
              </label>
            </div>

            @if (form.get('ageRestriction')?.value) {
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Edad mínima *</label>
                  <input formControlName="minAge" type="number" min="0" max="99" placeholder="12"
                         class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                </div>
                <div>
                  <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Edad máxima *</label>
                  <input formControlName="maxAge" type="number" min="0" max="99" placeholder="15"
                         class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                </div>
              </div>
            }

            <!-- Categoría sucesora -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Categoría sucesora</label>
              <select formControlName="successorCategoryId"
                      class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                <option value="">Sin sucesor</option>
                @for (cat of successorOptions(); track cat.id) {
                  <option [value]="cat.id">{{ cat.nombre }}</option>
                }
              </select>
              <p class="text-text-muted/60 text-xs mt-1">Categoría a la que pasa el competidor al superar la edad máxima.</p>
            </div>

            <!-- Código SurfScores -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Código SurfScores</label>
              <input formControlName="surfScoresCode" type="text" placeholder="Ej: OPEN-M"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              <p class="text-text-muted/60 text-xs mt-1">Código usado para enlazar esta categoría con la API de SurfScores.</p>
            </div>

            <!-- Footer -->
            <div class="flex justify-end gap-3 pt-2">
              <button type="button" (click)="closeModal()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="saving() || form.invalid"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-xs tracking-wider hover:bg-cyan-dark transition disabled:opacity-50">
                {{ saving() ? 'Guardando...' : (editingId() ? 'Guardar cambios' : 'Crear categoría') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Confirm Delete -->
    @if (deleteTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)">
        <div class="bg-navy-dark border border-error-brand/40 rounded-xl w-full max-w-sm p-6">
          <h3 class="font-heading text-lg text-white mb-2">Eliminar categoría</h3>
          <p class="text-text-muted text-sm mb-5">
            ¿Eliminar <strong class="text-text-light">{{ deleteTarget()!.nombre }}</strong>? Esta acción no se puede deshacer.
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
export class CategoriasComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);
  categories = signal<Category[]>([]);
  filterStatus = signal<'todos' | 'Activo' | 'Inactivo'>('todos');
  modalOpen = signal(false);
  editingId = signal<string | null>(null);
  deleteTarget = signal<Category | null>(null);

  statusFilters = [
    { label: 'Todos', value: 'todos' as const },
    { label: 'Activos', value: 'Activo' as const },
    { label: 'Inactivos', value: 'Inactivo' as const },
  ];

  form = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    gender: ['Masculino', Validators.required],
    ageRestriction: [false],
    minAge: [null as number | null],
    maxAge: [null as number | null],
    successorCategoryId: [''],
    status: ['Activo', Validators.required],
    surfScoresCode: [''],
  });

  filtered = computed(() => {
    const f = this.filterStatus();
    const list = this.categories();
    return f === 'todos' ? list : list.filter(c => c.status === f);
  });

  successorOptions = computed(() => {
    const id = this.editingId();
    return this.categories().filter(c => c.id !== id);
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const res = await this.api.get<any>('/categories');
      this.categories.set(res?.data ?? res ?? []);
    } catch {
      this.categories.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      nombre: '', descripcion: '', gender: 'Masculino',
      ageRestriction: false, minAge: null, maxAge: null,
      successorCategoryId: '', status: 'Activo', surfScoresCode: '',
    });
    this.modalOpen.set(true);
  }

  openEdit(cat: Category): void {
    this.editingId.set(cat.id);
    this.form.reset({
      nombre: cat.nombre,
      descripcion: cat.descripcion ?? '',
      gender: cat.gender,
      ageRestriction: cat.ageRestriction,
      minAge: cat.minAge ?? null,
      maxAge: cat.maxAge ?? null,
      successorCategoryId: cat.successorCategory?.id ?? '',
      status: cat.status,
      surfScoresCode: cat.surfScoresCode ?? '',
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
      const body: any = {
        nombre: v.nombre,
        descripcion: v.descripcion || '',
        gender: v.gender,
        ageRestriction: v.ageRestriction,
        minAge: v.ageRestriction ? v.minAge : null,
        maxAge: v.ageRestriction ? v.maxAge : null,
        successorCategoryId: v.successorCategoryId || null,
        status: v.status,
        surfScoresCode: v.surfScoresCode || null,
      };

      const id = this.editingId();
      if (id) {
        await this.api.put(`/categories/${id}`, body);
      } else {
        await this.api.post('/categories', body);
      }
      this.closeModal();
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  confirmDelete(cat: Category): void {
    this.deleteTarget.set(cat);
  }

  async doDelete(): Promise<void> {
    const cat = this.deleteTarget();
    if (!cat) return;
    this.saving.set(true);
    try {
      await this.api.delete(`/categories/${cat.id}`);
      this.deleteTarget.set(null);
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }
}
