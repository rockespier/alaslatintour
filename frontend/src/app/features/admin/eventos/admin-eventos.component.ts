import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';

interface EventItem {
  id: string;
  nombre: string;
  circuitId: string;
  fechaInicio: string;
  fechaFin: string;
  pais: string;
  ciudad: string;
  playa?: string;
  stars: number;
  capacidadMaxima: number;
  prizeAmountUsd?: number;
  enrolledCount?: number;
  statusPublic?: string;
  lugar?: string;
  estado?: string;
  accessType?: string;
  surfScoresCode?: string;
  imagenUrl?: string;
}

interface CircuitOption { id: string; nombre: string; temporada: number; }

const ESTADOS_EVENTO = ['Borrador', 'Próximamente', 'Activo', 'Completado', 'Cancelado'];
const ACCESS_TYPES = ['Abierto', 'Restringido', 'Solo invitación'];
const STARS = [1, 2, 3, 4, 5];

@Component({
  selector: 'app-admin-eventos',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="py-8">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-3xl font-heading text-white">Eventos</h1>
          <p class="text-text-muted text-sm mt-1">Gestión de eventos del circuito.</p>
        </div>
        <button (click)="openCreate()"
                class="flex items-center gap-2 px-4 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-sm tracking-wider hover:bg-cyan-dark transition">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          Nuevo evento
        </button>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap gap-2 mb-5">
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
            @for (sk of [1,2,3,4,5]; track sk) {
              <div class="px-6 py-4 flex items-center gap-4">
                <div class="skeleton h-4 rounded w-40"></div>
                <div class="skeleton h-4 rounded w-24 ml-4"></div>
                <div class="skeleton h-4 rounded w-28 ml-4"></div>
                <div class="ml-auto skeleton h-4 rounded w-20"></div>
              </div>
            }
          </div>
        } @else if (filtered().length === 0) {
          <div class="py-16 text-center text-text-muted text-sm">No hay eventos registrados.</div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm text-left">
              <thead class="bg-navy-mid/40 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="px-6 py-3">Nombre</th>
                  <th class="px-4 py-3">Circuito</th>
                  <th class="px-4 py-3">Fechas</th>
                  <th class="px-4 py-3 text-center">Estrellas</th>
                  <th class="px-4 py-3">Lugar</th>
                  <th class="px-4 py-3 text-center">Inscritos</th>
                  <th class="px-4 py-3">Estado</th>
                  <th class="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (ev of filtered(); track ev.id) {
                  <tr class="hover:bg-navy-mid/20 transition">
                    <td class="px-6 py-4">
                      <p class="font-medium text-text-light">{{ ev.nombre }}</p>
                      @if (ev.surfScoresCode) {
                        <p class="text-[10px] text-text-muted mt-0.5 font-accent tracking-wider">{{ ev.surfScoresCode }}</p>
                      }
                    </td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ circuitName(ev.circuitId) }}</td>
                    <td class="px-4 py-4 text-text-muted text-xs whitespace-nowrap">
                      {{ fmtDate(ev.fechaInicio) }} – {{ fmtDate(ev.fechaFin) }}
                    </td>
                    <td class="px-4 py-4 text-center">
                      <span class="text-warning-brand font-accent tracking-wider text-xs">
                        {{ '★'.repeat(ev.stars) }}
                      </span>
                    </td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ ev.lugar ?? ev.ciudad }}</td>
                    <td class="px-4 py-4 text-center text-text-muted text-xs">
                      {{ ev.enrolledCount ?? 0 }}/{{ ev.capacidadMaxima }}
                    </td>
                    <td class="px-4 py-4">
                      <span [class]="estadoClass(ev.statusPublic ?? ev.estado ?? '')">
                        {{ ev.statusPublic ?? ev.estado }}
                      </span>
                    </td>
                    <td class="px-4 py-4 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <button (click)="openEdit(ev)"
                                class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-cyan-brand" title="Editar">
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                              d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.5L16.732 3.732z"/>
                          </svg>
                        </button>
                        <button (click)="confirmDelete(ev)"
                                class="p-1.5 rounded hover:bg-navy-mid transition text-text-muted hover:text-error-brand" title="Eliminar">
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
          </div>
        }
      </div>
    </div>

    <!-- Modal -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">
              {{ editingId() ? 'Editar evento' : 'Nuevo evento' }}
            </h2>
            <button (click)="closeModal()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()" class="p-6 space-y-5">

            <!-- Nombre + Circuito -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nombre *</label>
              <input formControlName="nombre" type="text" placeholder="Ej: Máncora Pro 2026"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              @if (form.get('nombre')?.invalid && form.get('nombre')?.touched) {
                <p class="text-error-brand text-xs mt-1">El nombre es obligatorio.</p>
              }
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Circuito *</label>
              <select formControlName="circuitId"
                      class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                <option value="">— Seleccionar circuito —</option>
                @for (c of circuits(); track c.id) {
                  <option [value]="c.id">{{ c.nombre }} ({{ c.temporada }})</option>
                }
              </select>
              @if (form.get('circuitId')?.invalid && form.get('circuitId')?.touched) {
                <p class="text-error-brand text-xs mt-1">Selecciona un circuito.</p>
              }
            </div>

            <!-- Fechas -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Fecha inicio *</label>
                <input formControlName="fechaInicio" type="date"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Fecha fin *</label>
                <input formControlName="fechaFin" type="date"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
              </div>
            </div>

            <!-- Lugar -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">País (código) *</label>
                <input formControlName="pais" type="text" placeholder="PE" maxlength="4"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition uppercase">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Ciudad *</label>
                <input formControlName="ciudad" type="text" placeholder="Máncora"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Playa</label>
              <input formControlName="playa" type="text" placeholder="Playa de Máncora"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
            </div>

            <!-- Stars / Capacidad / Prize -->
            <div class="grid grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Estrellas *</label>
                <select formControlName="stars"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (s of starsOptions; track s) { <option [value]="s">{{ '★'.repeat(s) }} ({{ s }})</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Capacidad máx. *</label>
                <input formControlName="capacidadMaxima" type="number" min="1" placeholder="120"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Prize (USD)</label>
                <input formControlName="prizeAmountUsd" type="number" min="0" step="500" placeholder="5000"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            </div>

            <!-- Access / Estado -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Acceso *</label>
                <select formControlName="accessType"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (a of accessTypes; track a) { <option [value]="a">{{ a }}</option> }
                </select>
              </div>
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Estado *</label>
                <select formControlName="estado"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  @for (e of estadosEvento; track e) { <option [value]="e">{{ e }}</option> }
                </select>
              </div>
            </div>

            <!-- Afiche / imagen -->
            <div>
              <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">URL del afiche</label>
              <input formControlName="imagenUrl" type="url" placeholder="https://cdn.ejemplo.com/afiche-evento.jpg"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              @if (form.get('imagenUrl')?.value) {
                <div class="mt-3 relative w-full rounded-lg overflow-hidden border border-navy-mid" style="max-height:200px">
                  <img [src]="form.get('imagenUrl')!.value" alt="Preview afiche"
                       class="w-full h-full object-cover"
                       (error)="form.get('imagenUrl')!.setValue('')">
                  <button type="button"
                          (click)="form.get('imagenUrl')!.setValue('')"
                          class="absolute top-2 right-2 w-6 h-6 rounded-full bg-navy-deepest/80 text-text-muted hover:text-white flex items-center justify-center transition">
                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                    </svg>
                  </button>
                </div>
              }
            </div>

            <!-- SurfScores (solo crear) -->
            @if (!editingId()) {
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Código SurfScores</label>
                <input formControlName="surfScoresCode" type="text" placeholder="MAN2026"
                       class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
              </div>
            }

            <div class="flex justify-end gap-3 pt-2">
              <button type="button" (click)="closeModal()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="saving() || form.invalid"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase text-xs tracking-wider hover:bg-cyan-dark transition disabled:opacity-50">
                {{ saving() ? 'Guardando...' : (editingId() ? 'Guardar cambios' : 'Crear evento') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Confirm Delete -->
    @if (deleteTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)">
        <div class="bg-navy-dark border border-error-brand/40 rounded-xl w-full max-w-sm p-6">
          <h3 class="font-heading text-lg text-white mb-2">Eliminar evento</h3>
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
export class AdminEventosComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  saving = signal(false);
  events = signal<EventItem[]>([]);
  circuits = signal<CircuitOption[]>([]);
  filterEstado = signal<string>('todos');
  modalOpen = signal(false);
  editingId = signal<string | null>(null);
  deleteTarget = signal<EventItem | null>(null);

  starsOptions = STARS;
  accessTypes = ACCESS_TYPES;
  estadosEvento = ESTADOS_EVENTO;

  estadoFilters = [
    { label: 'Todos', value: 'todos' },
    ...ESTADOS_EVENTO.map(e => ({ label: e, value: e })),
  ];

  form = this.fb.group({
    nombre: ['', Validators.required],
    circuitId: ['', Validators.required],
    fechaInicio: ['', Validators.required],
    fechaFin: ['', Validators.required],
    pais: ['', Validators.required],
    ciudad: ['', Validators.required],
    playa: [''],
    stars: [3, Validators.required],
    capacidadMaxima: [120, Validators.required],
    prizeAmountUsd: [null as number | null],
    accessType: ['Abierto', Validators.required],
    estado: ['Borrador', Validators.required],
    surfScoresCode: [''],
    imagenUrl: [''],
  });

  filtered = computed(() => {
    const f = this.filterEstado();
    const list = this.events();
    if (f === 'todos') return list;
    return list.filter(e => (e.statusPublic ?? e.estado) === f);
  });

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadEvents(), this.loadCircuits()]);
  }

  private async loadEvents(): Promise<void> {
    this.loading.set(true);
    try {
      const res = await this.api.get<any>('/events');
      this.events.set(res?.data ?? res ?? []);
    } catch {
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCircuits(): Promise<void> {
    try {
      const res = await this.api.get<any>('/circuits');
      this.circuits.set(res?.data ?? res ?? []);
    } catch {
      this.circuits.set([]);
    }
  }

  circuitName(id: string): string {
    return this.circuits().find(c => c.id === id)?.nombre ?? '—';
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      nombre: '', circuitId: '', fechaInicio: '', fechaFin: '',
      pais: '', ciudad: '', playa: '', stars: 3, capacidadMaxima: 120,
      prizeAmountUsd: null, accessType: 'Abierto', estado: 'Borrador',
      surfScoresCode: '', imagenUrl: '',
    });
    this.modalOpen.set(true);
  }

  openEdit(ev: EventItem): void {
    this.editingId.set(ev.id);
    this.form.reset({
      nombre: ev.nombre,
      circuitId: ev.circuitId,
      fechaInicio: ev.fechaInicio?.substring(0, 10) ?? '',
      fechaFin: ev.fechaFin?.substring(0, 10) ?? '',
      pais: ev.pais,
      ciudad: ev.ciudad,
      playa: ev.playa ?? '',
      stars: ev.stars,
      capacidadMaxima: ev.capacidadMaxima,
      prizeAmountUsd: ev.prizeAmountUsd ?? null,
      accessType: ev.accessType ?? 'Abierto',
      estado: ev.estado ?? 'Borrador',
      surfScoresCode: ev.surfScoresCode ?? '',
      imagenUrl: ev.imagenUrl ?? '',
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
        circuitId: v.circuitId,
        fechaInicio: v.fechaInicio,
        fechaFin: v.fechaFin,
        pais: (v.pais ?? '').toUpperCase(),
        ciudad: v.ciudad,
        playa: v.playa || '',
        stars: Number(v.stars),
        capacidadMaxima: Number(v.capacidadMaxima),
        prizeAmountUsd: v.prizeAmountUsd ? Number(v.prizeAmountUsd) : 0,
        accessType: v.accessType,
        estado: v.estado,
        imagenUrl: v.imagenUrl || null,
      };
      const id = this.editingId();
      if (id) {
        await this.api.put(`/events/${id}`, body);
      } else {
        body.surfScoresCode = v.surfScoresCode || '';
        await this.api.post('/events', body);
      }
      this.closeModal();
      await this.loadEvents();
    } finally {
      this.saving.set(false);
    }
  }

  confirmDelete(ev: EventItem): void { this.deleteTarget.set(ev); }

  async doDelete(): Promise<void> {
    const ev = this.deleteTarget();
    if (!ev) return;
    this.saving.set(true);
    try {
      await this.api.delete(`/events/${ev.id}`);
      this.deleteTarget.set(null);
      await this.loadEvents();
    } finally {
      this.saving.set(false);
    }
  }

  fmtDate(d: string): string {
    if (!d) return '';
    const dt = new Date(d);
    const m = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${dt.getDate()} ${m[dt.getMonth()]} ${dt.getFullYear()}`;
  }

  estadoClass(estado: string): string {
    const map: Record<string, string> = {
      'Activo': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand',
      'Inscripciones Abiertas': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand',
      'Próximamente': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand',
      'Completado': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-navy-mid/50 text-text-muted',
      'Cancelado': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-error-brand/15 text-error-brand',
      'Borrador': 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-warning-brand/15 text-warning-brand',
    };
    return map[estado] ?? map['Borrador'];
  }
}
