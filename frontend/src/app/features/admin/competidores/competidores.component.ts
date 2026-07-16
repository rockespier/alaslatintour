import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface Competitor {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
  fechaNacimiento?: string | null;
  genero?: string;
  pais: string;
  telefono?: string;
  club?: string;
  postura?: string;
  tallaCamiseta?: string;
  numeroCamiseta?: string;
  patrocinadores?: string;
  federacion?: string;
  licenseStatus?: string;
  licenseNumber?: string | null;
  expirationDate?: string | null;
  enabledCategories?: string[];
  createdAt?: string;
}

interface CategoryOption { id: string; nombre: string; }

const PAISES = [
  'Argentina', 'Bolivia', 'Brasil', 'Chile', 'Colombia', 'Costa Rica',
  'Ecuador', 'El Salvador', 'Guatemala', 'Honduras', 'México', 'Nicaragua',
  'Panamá', 'Paraguay', 'Perú', 'República Dominicana', 'Uruguay', 'Venezuela',
];
const POSTURAS = ['Regular', 'Goofy'];
const TALLAS = ['XS', 'S', 'M', 'L', 'XL', 'XXL'];
const LICENSE_STATUSES = ['Activa', 'Pendiente de validación'];
const PAGE_SIZE = 20;

@Component({
  selector: 'app-competidores',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, PaginationComponent],
  template: `
    <div class="py-8">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-3xl font-heading text-white">Competidores</h1>
          <p class="text-text-muted text-sm mt-1">Gestión de perfiles y licencias de competidores.</p>
        </div>
        @if (canEdit()) {
          <button (click)="openCreate()"
                  class="flex items-center gap-2 px-4 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-sm tracking-wider hover:bg-cyan-dark transition">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            Nuevo competidor
          </button>
        }
      </div>

      <!-- Filters -->
      <div class="bg-navy-dark rounded-xl border border-navy-mid p-4 mb-6 flex flex-col lg:flex-row gap-3 lg:items-center">
        <div class="relative flex-1 max-w-md">
          <input type="text" placeholder="Buscar por nombre o email..." [value]="searchTerm()"
                 (input)="onSearchChange($event)"
                 class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
        </div>
        <select [value]="filterCountry()" (change)="onFilterCountryChange($event)"
                class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition sm:max-w-[180px]">
          <option value="">Todos los países</option>
          @for (p of paises; track p) { <option [value]="p">{{ p }}</option> }
        </select>
        <div class="flex gap-2">
          @for (f of licenseFilters; track f.value) {
            <button (click)="filterLicense.set(f.value)"
                    [class]="filterLicense() === f.value
                      ? 'px-3 py-1.5 rounded-md bg-cyan-brand text-navy-deepest text-xs font-accent uppercase tracking-wider'
                      : 'px-3 py-1.5 rounded-md border border-navy-mid text-text-muted text-xs font-accent uppercase tracking-wider hover:border-cyan-brand hover:text-text-light transition'">
              {{ f.label }}
            </button>
          }
        </div>
      </div>

      <!-- Table -->
      <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
        @if (loading()) {
          <div class="divide-y divide-navy-mid">
            @for (sk of [1,2,3,4,5]; track sk) {
              <div class="px-6 py-4 flex items-center gap-4">
                <div class="skeleton h-4 rounded w-40"></div>
                <div class="skeleton h-4 rounded w-32 ml-4"></div>
                <div class="skeleton h-4 rounded w-20 ml-4"></div>
                <div class="ml-auto skeleton h-4 rounded w-20"></div>
              </div>
            }
          </div>
        } @else if (competitors().length === 0) {
          <div class="py-16 text-center text-text-muted text-sm">No hay competidores registrados.</div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm text-left">
              <thead class="bg-navy-mid/40 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="px-6 py-3">Competidor</th>
                  <th class="px-4 py-3">País</th>
                  <th class="px-4 py-3">Género</th>
                  <th class="px-4 py-3">Licencia</th>
                  <th class="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (c of competitors(); track c.id) {
                  <tr class="hover:bg-navy-mid/20 transition">
                    <td class="px-6 py-4">
                      <p class="font-medium text-text-light">{{ c.nombre }} {{ c.apellido }}</p>
                      <p class="text-xs text-text-muted mt-0.5">{{ c.email }}</p>
                    </td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ c.pais }}</td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ c.genero }}</td>
                    <td class="px-4 py-4">
                      <span [class]="licenseClass(c.licenseStatus)">{{ c.licenseStatus ?? '—' }}</span>
                    </td>
                    <td class="px-4 py-4 text-right whitespace-nowrap">
                      @if (canEdit()) {
                        <button (click)="openLicense(c)"
                                class="text-xs font-accent uppercase tracking-wider text-cyan-brand hover:text-cyan-dark mr-3">
                          Licencia
                        </button>
                        <button (click)="openEdit(c)"
                                class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-text-light mr-3">
                          Editar
                        </button>
                        <button (click)="openPassword(c)"
                                class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-text-light mr-3">
                          Contraseña
                        </button>
                        <button (click)="confirmDelete(c)"
                                class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-error-brand">
                          Eliminar
                        </button>
                      } @else {
                        <span class="text-xs text-text-muted">—</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="border-t border-navy-mid px-4 py-3">
            <app-pagination [currentPage]="currentPage()" [totalPages]="totalPages()" [totalItems]="totalItems()"
                             (pageChange)="onPageChange($event)" />
          </div>
        }
      </div>
    </div>

    <!-- Modal: Crear/Editar -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">
              {{ editingId() ? 'Editar competidor' : 'Nuevo competidor' }}
            </h2>
            <button (click)="closeModal()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()" class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nombre *</label>
                <input formControlName="nombre" type="text"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Apellido *</label>
                <input formControlName="apellido" type="text"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Email *</label>
              <input formControlName="email" type="email"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Fecha de nacimiento *</label>
                <input formControlName="fechaNacimiento" type="date"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Género *</label>
                <select formControlName="genero"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="Masculino">Masculino</option>
                  <option value="Femenino">Femenino</option>
                </select>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">País *</label>
                <select formControlName="pais"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (p of paises; track p) { <option [value]="p">{{ p }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Teléfono</label>
                <input formControlName="telefono" type="text" placeholder="+51 999 111 222"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Postura</label>
                <select formControlName="postura"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="">Sin especificar</option>
                  @for (p of posturas; track p) { <option [value]="p">{{ p }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Talla de camiseta</label>
                <select formControlName="tallaCamiseta"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="">Sin especificar</option>
                  @for (t of tallas; track t) { <option [value]="t">{{ t }}</option> }
                </select>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Club / Escuela</label>
                <input formControlName="club" type="text"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Federación</label>
                <input formControlName="federacion" type="text" placeholder="Ej: FENTA"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Número de camiseta</label>
                <input formControlName="numeroCamiseta" type="text" placeholder="#12"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Patrocinadores</label>
                <input formControlName="patrocinadores" type="text"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            @if (saveError()) {
              <p class="text-error-brand text-xs">{{ saveError() }}</p>
            }

            <div class="flex justify-end gap-3 pt-2">
              <button type="button" (click)="closeModal()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="saving() || form.invalid"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-xs tracking-wider hover:bg-cyan-dark transition disabled:opacity-50">
                {{ saving() ? 'Guardando...' : (editingId() ? 'Guardar cambios' : 'Crear competidor') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Modal: Licencia -->
    @if (licenseTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeLicense()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">Licencia — {{ licenseTarget()!.nombre }} {{ licenseTarget()!.apellido }}</h2>
            <button (click)="closeLicense()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="licenseForm" (ngSubmit)="saveLicense()" class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Estado *</label>
                <select formControlName="status"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (s of licenseStatuses; track s) { <option [value]="s">{{ s }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Número de licencia</label>
                <input formControlName="licenseNumber" type="text" placeholder="ALAS-2026-001"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Vencimiento</label>
              <input formControlName="expirationDate" type="date"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Categorías habilitadas</label>
              <div class="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto border border-navy-mid rounded-md p-3">
                @for (cat of categoryOptions(); track cat.id) {
                  <label class="flex items-center gap-2 text-sm text-text-light cursor-pointer">
                    <input type="checkbox" [checked]="isCategoryChecked(cat.id)" (change)="toggleLicenseCategory(cat.id)"
                           class="h-4 w-4 rounded border-navy-mid bg-navy-dark text-cyan-brand focus:ring-cyan-brand">
                    {{ cat.nombre }}
                  </label>
                }
              </div>
            </div>

            @if (licenseError()) {
              <p class="text-error-brand text-xs">{{ licenseError() }}</p>
            }

            <div class="flex justify-end gap-3 pt-2">
              <button type="button" (click)="closeLicense()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="savingLicense()"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-xs tracking-wider hover:bg-cyan-dark transition disabled:opacity-50">
                {{ savingLicense() ? 'Guardando...' : 'Guardar licencia' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Modal: Cambiar Contraseña -->
    @if (passwordTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="closePassword()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-sm p-6" (click)="$event.stopPropagation()">
          <h3 class="font-heading text-lg text-white mb-1">Cambiar contraseña</h3>
          <p class="text-text-muted text-sm mb-4">{{ passwordTarget()!.nombre }} {{ passwordTarget()!.apellido }}</p>
          <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nueva contraseña</label>
          <input type="password" [(ngModel)]="newPasswordValue" autocomplete="new-password"
                 class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
          <p class="text-[11px] text-text-muted mt-1 mb-4">Mínimo 8 caracteres, 1 mayúscula y 1 dígito.</p>
          @if (passwordError()) {
            <p class="text-error-brand text-xs mb-3">{{ passwordError() }}</p>
          }
          <div class="flex justify-end gap-3">
            <button (click)="closePassword()"
                    class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-xs transition">
              Cancelar
            </button>
            <button (click)="confirmPasswordChange()" [disabled]="savingPassword()"
                    class="px-4 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-xs hover:bg-cyan-dark transition disabled:opacity-50">
              {{ savingPassword() ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Confirm Delete -->
    @if (deleteTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)">
        <div class="bg-navy-dark border border-error-brand/40 rounded-xl w-full max-w-sm p-6">
          <h3 class="font-heading text-lg text-white mb-2">Eliminar competidor</h3>
          <p class="text-text-muted text-sm mb-5">
            ¿Eliminar <strong class="text-text-light">{{ deleteTarget()!.nombre }} {{ deleteTarget()!.apellido }}</strong>?
            Esta acción no se puede deshacer.
          </p>
          <div class="flex justify-end gap-3">
            <button (click)="deleteTarget.set(null)"
                    class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-xs transition">
              Cancelar
            </button>
            <button (click)="doDelete()" [disabled]="saving()"
                    class="px-4 py-2 rounded-md bg-error-brand text-white font-accent uppercase tracking-wider text-xs hover:bg-error-brand/80 transition disabled:opacity-50">
              {{ saving() ? 'Eliminando...' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class CompetidoresComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Usuarios'));

  loading = signal(true);
  saving = signal(false);
  saveError = signal<string | null>(null);
  competitors = signal<Competitor[]>([]);
  categoryOptions = signal<CategoryOption[]>([]);

  searchTerm = signal('');
  filterCountry = signal('');
  filterLicense = signal<'todos' | 'Activa' | 'Pendiente de validación'>('todos');
  currentPage = signal(1);
  totalPages = signal(1);
  totalItems = signal(0);

  modalOpen = signal(false);
  editingId = signal<string | null>(null);
  deleteTarget = signal<Competitor | null>(null);

  licenseTarget = signal<Competitor | null>(null);
  savingLicense = signal(false);
  licenseError = signal<string | null>(null);
  licenseCategories = signal<string[]>([]);

  passwordTarget = signal<Competitor | null>(null);
  newPasswordValue = '';
  savingPassword = signal(false);
  passwordError = signal<string | null>(null);

  paises = PAISES;
  posturas = POSTURAS;
  tallas = TALLAS;
  licenseStatuses = LICENSE_STATUSES;
  licenseFilters = [
    { label: 'Todos', value: 'todos' as const },
    { label: 'Activa', value: 'Activa' as const },
    { label: 'Pendiente', value: 'Pendiente de validación' as const },
  ];

  form = this.fb.group({
    nombre: ['', Validators.required],
    apellido: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    fechaNacimiento: ['', Validators.required],
    genero: ['Masculino', Validators.required],
    pais: ['Perú', Validators.required],
    telefono: [''],
    club: [''],
    postura: [''],
    tallaCamiseta: [''],
    numeroCamiseta: [''],
    patrocinadores: [''],
    federacion: [''],
  });

  licenseForm = this.fb.group({
    status: ['Pendiente de validación', Validators.required],
    licenseNumber: [''],
    expirationDate: [''],
  });

  async ngOnInit(): Promise<void> {
    await Promise.all([this.load(), this.loadCategories()]);
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const params = new URLSearchParams();
      params.set('page', String(this.currentPage()));
      params.set('limit', String(PAGE_SIZE));
      if (this.searchTerm().trim()) params.set('search', this.searchTerm().trim());
      if (this.filterCountry()) params.set('country', this.filterCountry());
      if (this.filterLicense() !== 'todos') params.set('licenseStatus', this.filterLicense());

      const res = await this.api.get<any>(`/competitors?${params.toString()}`);
      this.competitors.set(res?.data ?? []);
      this.totalPages.set(res?.pagination?.totalPages ?? 1);
      this.totalItems.set(res?.pagination?.totalItems ?? (res?.data?.length ?? 0));
    } catch {
      this.competitors.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCategories(): Promise<void> {
    try {
      const res = await this.api.get<any>('/categories');
      this.categoryOptions.set(res?.data ?? []);
    } catch {
      this.categoryOptions.set([]);
    }
  }

  onSearchChange(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1);
    void this.load();
  }

  onFilterCountryChange(event: Event): void {
    this.filterCountry.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
    void this.load();
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    void this.load();
  }

  licenseClass(status?: string): string {
    return status === 'Activa'
      ? 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand'
      : 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-warning-brand/15 text-warning-brand';
  }

  openCreate(): void {
    this.editingId.set(null);
    this.saveError.set(null);
    this.form.reset({
      nombre: '', apellido: '', email: '', fechaNacimiento: '', genero: 'Masculino',
      pais: 'Perú', telefono: '', club: '', postura: '', tallaCamiseta: '',
      numeroCamiseta: '', patrocinadores: '', federacion: '',
    });
    this.modalOpen.set(true);
  }

  openEdit(c: Competitor): void {
    this.editingId.set(c.id);
    this.saveError.set(null);
    this.form.reset({
      nombre: c.nombre,
      apellido: c.apellido,
      email: c.email,
      fechaNacimiento: c.fechaNacimiento?.substring(0, 10) ?? '',
      genero: c.genero ?? 'Masculino',
      pais: c.pais,
      telefono: c.telefono ?? '',
      club: c.club ?? '',
      postura: c.postura ?? '',
      tallaCamiseta: c.tallaCamiseta ?? '',
      numeroCamiseta: c.numeroCamiseta ?? '',
      patrocinadores: c.patrocinadores ?? '',
      federacion: c.federacion ?? '',
    });
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.saveError.set(null);
    try {
      const v = this.form.getRawValue();
      const body = {
        nombre: v.nombre,
        apellido: v.apellido,
        email: v.email,
        fechaNacimiento: v.fechaNacimiento,
        genero: v.genero,
        pais: v.pais,
        telefono: v.telefono || '',
        club: v.club || '',
        postura: v.postura || '',
        tallaCamiseta: v.tallaCamiseta || '',
        numeroCamiseta: v.numeroCamiseta || '',
        patrocinadores: v.patrocinadores || '',
        federacion: v.federacion || '',
      };
      const id = this.editingId();
      if (id) {
        await this.api.put(`/competitors/${id}`, body);
      } else {
        await this.api.post('/competitors', body);
      }
      this.closeModal();
      await this.load();
    } catch (err: any) {
      this.saveError.set(err?.body?.message ?? err?.message ?? 'No se pudo guardar el competidor.');
    } finally {
      this.saving.set(false);
    }
  }

  confirmDelete(c: Competitor): void { this.deleteTarget.set(c); }

  async doDelete(): Promise<void> {
    const c = this.deleteTarget();
    if (!c) return;
    this.saving.set(true);
    try {
      await this.api.delete(`/competitors/${c.id}`);
      this.deleteTarget.set(null);
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  openLicense(c: Competitor): void {
    this.licenseTarget.set(c);
    this.licenseError.set(null);
    this.licenseCategories.set(c.enabledCategories ?? []);
    this.licenseForm.reset({
      status: c.licenseStatus ?? 'Pendiente de validación',
      licenseNumber: c.licenseNumber ?? '',
      expirationDate: c.expirationDate?.substring(0, 10) ?? '',
    });
  }

  closeLicense(): void {
    this.licenseTarget.set(null);
  }

  isCategoryChecked(categoryId: string): boolean {
    return this.licenseCategories().includes(categoryId);
  }

  toggleLicenseCategory(categoryId: string): void {
    this.licenseCategories.update(list =>
      list.includes(categoryId) ? list.filter(id => id !== categoryId) : [...list, categoryId],
    );
  }

  async saveLicense(): Promise<void> {
    const target = this.licenseTarget();
    if (!target || this.licenseForm.invalid) {
      this.licenseForm.markAllAsTouched();
      return;
    }
    this.savingLicense.set(true);
    this.licenseError.set(null);
    try {
      const v = this.licenseForm.getRawValue();
      await this.api.put(`/competitors/${target.id}/license`, {
        status: v.status,
        licenseNumber: v.licenseNumber || null,
        expirationDate: v.expirationDate || null,
        enabledCategories: this.licenseCategories(),
      });
      this.closeLicense();
      await this.load();
    } catch (err: any) {
      this.licenseError.set(err?.body?.message ?? err?.message ?? 'No se pudo guardar la licencia.');
    } finally {
      this.savingLicense.set(false);
    }
  }

  openPassword(c: Competitor): void {
    this.passwordTarget.set(c);
    this.newPasswordValue = '';
    this.passwordError.set(null);
  }

  closePassword(): void {
    this.passwordTarget.set(null);
  }

  async confirmPasswordChange(): Promise<void> {
    const c = this.passwordTarget();
    if (!c) return;
    if (!/^(?=.*[A-Z])(?=.*\d).{8,}$/.test(this.newPasswordValue)) {
      this.passwordError.set('La contraseña debe tener al menos 8 caracteres, 1 mayúscula y 1 dígito.');
      return;
    }
    this.savingPassword.set(true);
    this.passwordError.set(null);
    try {
      await this.api.post(`/competitors/${c.id}/password`, { newPassword: this.newPasswordValue });
      this.closePassword();
    } catch (err: any) {
      this.passwordError.set(err?.body?.message ?? 'No se pudo actualizar la contraseña.');
    } finally {
      this.savingPassword.set(false);
    }
  }
}
