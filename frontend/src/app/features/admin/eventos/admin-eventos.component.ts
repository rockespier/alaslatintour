import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { ImportExcelModalComponent } from '../../../shared/components/import-excel-modal/import-excel-modal.component';

interface EventItem {
  id: string;
  nombre: string;
  circuitId: string;
  fechaInicio: string;
  fechaFin: string;
  pais: string;
  ciudad: string;
  playa?: string;
  auspiciador?: string | null;
  stars: number;
  capacidadMaxima: number;
  prizeAmountUsd?: number;
  enrolledCount?: number;
  statusPublic?: string;
  lugar?: string;
  estado?: string;
  accessType?: string;
  eventType?: string;
  surfScoresCode?: string;
  imagenUrl?: string;
}

interface CircuitOption { id: string; nombre: string; temporada: number; }

interface CategoryOption {
  id: string;
  nombre: string;
  descripcion?: string;
  status: 'Activo' | 'Inactivo';
}

interface EventCategoryEntry {
  categoryId: string;
  categoryName?: string;
  gender?: string;
  stars: number | null;
  customTariffUsd: number | null;
  capacidad: number | null;
  effectiveTariffUsd?: number;
  enrolledCount?: number;
}

interface SurfScoresImportedEvent {
  eventId: string;
  nombre: string;
  surfScoresCode: string;
  categoriesLinked: number;
  unmatchedCategoryCodes: string[];
}

interface SurfScoresSkippedEvent {
  nombre: string;
  surfScoresCode: string;
  reason: string;
}

interface SurfScoresImportResult {
  totalFetched: number;
  created: SurfScoresImportedEvent[];
  skipped: SurfScoresSkippedEvent[];
}

const ESTADOS_EVENTO = ['Borrador', 'Próximamente', 'Activo', 'Completado', 'Cancelado'];
const ACCESS_TYPES = ['Abierto', 'Restringido', 'Solo invitación'];
const EVENT_TYPES = ['Regular', 'Prime', 'SuperPrime'];
const STARS = [1, 2, 3, 4, 5, 6, 7];
const PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-eventos',
  standalone: true,
  imports: [ReactiveFormsModule, ImportExcelModalComponent],
  template: `
    <div class="py-8">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-3xl font-heading text-white">Eventos</h1>
          <p class="text-text-muted text-sm mt-1">Gestión de eventos del circuito.</p>
        </div>
      </div>

      <!-- Stat cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Eventos Activos</p>
          <p class="font-heading text-4xl text-cyan-brand">{{ statActivos() }}</p>
        </div>
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Eventos Próximos</p>
          <p class="font-heading text-4xl text-text-light">{{ statProximos() }}</p>
        </div>
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Eventos Completados</p>
          <p class="font-heading text-4xl text-text-muted">{{ statCompletados() }}</p>
        </div>
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
          <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Inscripciones Abiertas</p>
          <p class="font-heading text-4xl text-success-brand">{{ statInscripcionesAbiertas() }}</p>
        </div>
      </div>

      <!-- Filter bar + New Event -->
      <div class="bg-navy-dark rounded-xl border border-navy-mid p-4 mb-6 flex flex-col lg:flex-row gap-3 lg:items-center lg:justify-between">
        <div class="flex flex-col sm:flex-row gap-3 flex-1 flex-wrap">
          <div class="relative flex-1 max-w-md">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <input type="text" placeholder="Buscar evento..." [value]="searchTerm()"
                   (input)="onSearchChange($event)"
                   class="w-full bg-navy-mid/40 border border-navy-mid rounded-md pl-9 pr-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
          </div>
          <select [value]="filterCircuitId()" (change)="onFilterCircuitChange($event)"
                  class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition sm:max-w-[180px]">
            <option value="">Todos los circuitos</option>
            @for (c of circuits(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
          </select>
          <select [value]="filterEstado()" (change)="onFilterEstadoChange($event)"
                  class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition sm:max-w-[160px]">
            <option value="todos">Todos los estados</option>
            @for (e of estadosEvento; track e) { <option [value]="e">{{ e }}</option> }
          </select>
          <select [value]="filterCountry()" (change)="onFilterCountryChange($event)"
                  class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition sm:max-w-[160px]">
            <option value="">Todos los países</option>
            @for (p of countryOptions(); track p) { <option [value]="p">{{ p }}</option> }
          </select>
        </div>
        @if (canEdit()) {
          <div class="flex items-center gap-2">
            <button (click)="downloadTemplate()"
                    class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase text-xs tracking-wider rounded-md transition">
              Descargar plantilla
            </button>
            <button (click)="importOpen.set(true)"
                    class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase text-xs tracking-wider rounded-md transition">
              Importar Excel
            </button>
            <button (click)="openImport()"
                    class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center whitespace-nowrap">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v2a2 2 0 002 2h12a2 2 0 002-2v-2M7 10l5 5 5-5M12 15V3"/>
              </svg>
              Importar de SurfScores
            </button>
            <button (click)="openCreate()"
                    class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center whitespace-nowrap">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
              </svg>
              Nuevo evento
            </button>
          </div>
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
                  <th class="px-6 py-3">Evento</th>
                  <th class="px-4 py-3">País</th>
                  <th class="px-4 py-3">Circuito</th>
                  <th class="px-4 py-3">Fechas</th>
                  <th class="px-4 py-3 text-center">Estrellas</th>
                  <th class="px-4 py-3">Inscritos/Cap.</th>
                  <th class="px-4 py-3">Estado</th>
                  <th class="px-4 py-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (ev of pagedEvents(); track ev.id) {
                  <tr class="hover:bg-navy-mid/20 transition">
                    <td class="px-6 py-4">
                      <p class="font-medium text-text-light">{{ ev.nombre }}</p>
                      @if (ev.eventType || ev.auspiciador) {
                        <p class="text-[10px] text-text-muted mt-0.5">
                          {{ (ev.eventType ?? 'Regular') + (ev.auspiciador ? ' · ' + ev.auspiciador : '') }}
                        </p>
                      }
                      @if (ev.surfScoresCode) {
                        <p class="text-[10px] text-text-muted mt-0.5 font-accent tracking-wider">{{ ev.surfScoresCode }}</p>
                      }
                    </td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ flagOf(ev.pais) }} {{ ev.pais }}</td>
                    <td class="px-4 py-4 text-text-muted text-xs">{{ circuitName(ev.circuitId) }}</td>
                    <td class="px-4 py-4 text-text-muted text-xs whitespace-nowrap">
                      {{ fmtDate(ev.fechaInicio) }} – {{ fmtDate(ev.fechaFin) }}
                    </td>
                    <td class="px-4 py-4 text-center">
                      <span class="text-warning-brand font-accent tracking-wider text-xs">
                        {{ '★'.repeat(ev.stars) }}
                      </span>
                    </td>
                    <td class="px-4 py-4">
                      <div class="flex items-center gap-2">
                        <span class="font-medium text-text-light whitespace-nowrap">{{ ev.enrolledCount ?? 0 }}/{{ ev.capacidadMaxima }}</span>
                        <div class="w-20 h-1.5 bg-navy-mid rounded-full overflow-hidden">
                          <div class="h-full bg-cyan-brand" [style.width.%]="capacidadPct(ev)"></div>
                        </div>
                      </div>
                    </td>
                    <td class="px-4 py-4">
                      <span [class]="estadoClass(ev.statusPublic ?? ev.estado ?? '')">
                        {{ ev.statusPublic ?? ev.estado }}
                      </span>
                    </td>
                    <td class="px-4 py-4 text-right whitespace-nowrap">
                      <button (click)="verInscritos(ev)"
                              class="text-xs font-accent uppercase tracking-wider text-cyan-brand hover:text-cyan-dark mr-3">
                        Ver Inscritos
                      </button>
                      @if (canEdit()) {
                        <button (click)="openEdit(ev)"
                                class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-text-light mr-3">
                          Editar
                        </button>
                        <button (click)="confirmDelete(ev)"
                                class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-error-brand">
                          Eliminar
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="border-t border-navy-mid px-4 py-3 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-text-muted">
            <p>Mostrando {{ pagedEvents().length }} de {{ filtered().length }} eventos</p>
            <div class="flex items-center gap-2">
              <button (click)="prevPage()" [disabled]="currentPage() <= 1"
                      class="px-3 py-1.5 border border-navy-mid rounded-md hover:border-cyan-brand transition disabled:opacity-40 disabled:hover:border-navy-mid">
                Anterior
              </button>
              <span class="px-2">Página {{ currentPage() }} de {{ totalPages() }}</span>
              <button (click)="nextPage()" [disabled]="currentPage() >= totalPages()"
                      class="px-3 py-1.5 border border-navy-mid rounded-md hover:border-cyan-brand transition disabled:opacity-40 disabled:hover:border-navy-mid">
                Siguiente
              </button>
            </div>
          </div>
        }
      </div>
    </div>

    <!-- Modal -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-3xl max-h-[90vh] overflow-y-auto"
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

          <!-- Tabs -->
          <div class="px-6 border-b border-navy-mid flex gap-2">
            <button type="button" (click)="modalTab.set('general')"
                    [class]="modalTab() === 'general'
                      ? 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-cyan-brand border-b-2 border-cyan-brand'
                      : 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-text-muted border-b-2 border-transparent hover:text-text-light transition'">
              Datos Generales
            </button>
            <button type="button" (click)="modalTab.set('categorias')"
                    [class]="modalTab() === 'categorias'
                      ? 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-cyan-brand border-b-2 border-cyan-brand'
                      : 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-text-muted border-b-2 border-transparent hover:text-text-light transition'">
              Categorías habilitadas
            </button>
            <button type="button" (click)="modalTab.set('tarifas')"
                    [class]="modalTab() === 'tarifas'
                      ? 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-cyan-brand border-b-2 border-cyan-brand'
                      : 'px-4 py-2.5 font-accent uppercase tracking-wider text-xs text-text-muted border-b-2 border-transparent hover:text-text-light transition'">
              Tarifas
            </button>
          </div>

          <form [formGroup]="form" (ngSubmit)="save()">

            <!-- Tab: Datos Generales -->
            @if (modalTab() === 'general') {
              <div class="p-6 space-y-5">
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

                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Fecha inicio *</label>
                    <input formControlName="fechaInicio" type="date"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
                    @if (form.get('fechaInicio')?.invalid && form.get('fechaInicio')?.touched) {
                      <p class="text-error-brand text-xs mt-1">La fecha de inicio es obligatoria.</p>
                    }
                  </div>
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Fecha fin *</label>
                    <input formControlName="fechaFin" type="date"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition [color-scheme:dark]">
                    @if (form.get('fechaFin')?.invalid && form.get('fechaFin')?.touched) {
                      <p class="text-error-brand text-xs mt-1">La fecha de fin es obligatoria.</p>
                    }
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">País (código) *</label>
                    <input formControlName="pais" type="text" placeholder="PE" maxlength="4"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition uppercase">
                    @if (form.get('pais')?.invalid && form.get('pais')?.touched) {
                      <p class="text-error-brand text-xs mt-1">El país es obligatorio.</p>
                    }
                  </div>
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Ciudad *</label>
                    <input formControlName="ciudad" type="text" placeholder="Máncora"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                    @if (form.get('ciudad')?.invalid && form.get('ciudad')?.touched) {
                      <p class="text-error-brand text-xs mt-1">La ciudad es obligatoria.</p>
                    }
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Playa *</label>
                  <input formControlName="playa" type="text" placeholder="Playa de Máncora"
                         class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                  @if (form.get('playa')?.invalid && form.get('playa')?.touched) {
                    <p class="text-error-brand text-xs mt-1">La playa es obligatoria.</p>
                  }
                </div>

                <div>
                  <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Auspiciador</label>
                  <input formControlName="auspiciador" type="text" placeholder="Ej: Monster Energy"
                         class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                </div>

                <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
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
                    @if (form.get('capacidadMaxima')?.invalid && form.get('capacidadMaxima')?.touched) {
                      <p class="text-error-brand text-xs mt-1">La capacidad es obligatoria.</p>
                    }
                  </div>
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Prize (USD)</label>
                    <input formControlName="prizeAmountUsd" type="number" min="0" step="500" placeholder="5000"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                  </div>
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Tipo de evento *</label>
                    <select formControlName="eventType"
                            class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                      @for (type of eventTypes; track type) { <option [value]="type">{{ type }}</option> }
                    </select>
                  </div>
                </div>

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

                <div>
                  <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Afiche del evento</label>
                  <div class="flex items-center gap-3">
                    <input #posterInput type="file" accept="image/jpeg,image/png,image/webp" class="hidden"
                           (change)="onPosterFileSelected($event)">
                    <button type="button" (click)="posterInput.click()" [disabled]="uploadingPoster()"
                            class="px-3 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition disabled:opacity-50">
                      {{ uploadingPoster() ? 'Subiendo...' : 'Subir imagen' }}
                    </button>
                    <input formControlName="imagenUrl" type="url" placeholder="https://cdn.ejemplo.com/afiche-evento.jpg"
                           class="flex-1 bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                  </div>
                  <p class="text-text-muted/70 text-[11px] mt-1.5">Formatos permitidos: JPG, PNG o WEBP.</p>
                  @if (uploadError()) {
                    <p class="text-error-brand text-xs mt-1">{{ uploadError() }}</p>
                  }
                  @if (form.get('imagenUrl')?.value) {
                    <div class="mt-3 relative w-full rounded-lg overflow-hidden border border-navy-mid" style="max-height:200px">
                      <img [src]="form.get('imagenUrl')!.value" alt="Preview afiche" referrerpolicy="no-referrer"
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

                @if (!editingId()) {
                  <div>
                    <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Código SurfScores</label>
                    <input formControlName="surfScoresCode" type="text" placeholder="MAN2026"
                           class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                  </div>
                }
              </div>
            }

            <!-- Tab: Categorías habilitadas -->
            @if (modalTab() === 'categorias') {
              <div class="p-6 space-y-3">
                <p class="text-sm text-text-muted mb-3">Selecciona las categorías habilitadas para este evento y, opcionalmente, un nivel de estrellas propio para cada una.</p>
                @if (categoryOptions().length === 0) {
                  <p class="text-text-muted text-sm py-6 text-center">No hay categorías registradas.</p>
                }
                @for (cat of categoryOptions(); track cat.id) {
                  <div class="rounded-lg border border-navy-mid p-3">
                    <label class="flex items-center justify-between cursor-pointer">
                      <div class="flex items-center gap-3">
                        <input type="checkbox" [checked]="isCategoryEnabled(cat.id)" (change)="toggleCategory(cat.id, cat.nombre)"
                               class="h-4 w-4 rounded border-navy-mid bg-navy-dark text-cyan-brand focus:ring-cyan-brand">
                        <div>
                          <p class="font-medium text-text-light">
                            {{ cat.nombre }}
                            @if (categoryGender(cat.id); as gender) {
                              <span class="text-[10px] text-text-muted font-accent uppercase tracking-wider ml-1">({{ gender }})</span>
                            }
                          </p>
                          @if (cat.descripcion) {
                            <p class="text-xs text-text-muted">{{ cat.descripcion }}</p>
                          }
                        </div>
                      </div>
                      @if (cat.status === 'Inactivo') {
                        <span class="text-xs font-accent uppercase tracking-wider text-text-muted">Inactiva</span>
                      }
                    </label>
                    @if (isCategoryEnabled(cat.id)) {
                      <div class="mt-3 pl-7 grid gap-3 sm:grid-cols-2">
                        <div>
                          <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nivel de estrellas</label>
                          <select (change)="onCategoryStarsChange(cat.id, $event)"
                                  class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                            <option value="" [selected]="isCategoryStarSelected(cat.id, null)">Usar nivel del evento ({{ '★'.repeat(form.get('stars')?.value ?? 0) }})</option>
                            @for (s of starsOptions; track s) {
                              <option [value]="s" [selected]="isCategoryStarSelected(cat.id, s)">{{ '★'.repeat(s) }} ({{ s }})</option>
                            }
                          </select>
                        </div>
                        <div>
                          <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Cupo máximo *</label>
                          <input type="number" min="0" [value]="categoryCapacity(cat.id) ?? ''"
                                 (input)="onCategoryCapacityChange(cat.id, $event)"
                                 placeholder="Ej: 32"
                                 class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            }

            <!-- Tab: Tarifas -->
            @if (modalTab() === 'tarifas') {
              <div class="p-6 space-y-4">
                @if (eventCategories().length === 0) {
                  <p class="text-text-muted text-sm py-6 text-center">Habilita categorías en la pestaña anterior para configurar sus tarifas.</p>
                } @else {
                  <div class="bg-navy-dark rounded-lg border border-navy-mid p-4">
                    <label class="flex items-center justify-between cursor-pointer">
                      <div>
                        <p class="font-medium text-text-light mb-1">Usar tarifas del circuito</p>
                        <p class="text-xs text-text-muted">Las tarifas se calculan según la categoría y el nivel de estrellas asignado.</p>
                      </div>
                      <button type="button" (click)="useCircuitTariffs.set(!useCircuitTariffs())"
                              [class]="useCircuitTariffs() ? 'bg-cyan-brand' : 'bg-navy-mid'"
                              class="relative w-12 h-6 rounded-full transition flex-shrink-0">
                        <span [class]="useCircuitTariffs() ? 'translate-x-6' : 'translate-x-0.5'"
                              class="absolute top-0.5 w-5 h-5 rounded-full bg-white transition-transform"></span>
                      </button>
                    </label>
                  </div>

                  @if (useCircuitTariffs()) {
                    <div class="bg-cyan-brand/10 border border-cyan-brand/30 rounded-lg p-4">
                      @if (editingId()) {
                        <p class="text-sm mb-2">Tarifas resueltas desde el circuito:</p>
                        <ul class="text-sm space-y-1">
                          @for (c of eventCategories(); track c.categoryId) {
                            <li class="flex justify-between">
                              <span class="text-text-muted">{{ c.categoryName }} ({{ c.stars ? '★'.repeat(c.stars) : 'nivel del evento' }}):</span>
                              <span class="font-mono">USD {{ c.effectiveTariffUsd ?? 0 }}</span>
                            </li>
                          }
                        </ul>
                      } @else {
                        <p class="text-sm text-text-muted">Los valores de tarifa se calcularán al guardar el evento por primera vez.</p>
                      }
                    </div>
                  } @else {
                    <div class="bg-navy-dark rounded-lg border border-navy-mid overflow-hidden">
                      <table class="w-full text-sm">
                        <thead class="border-b border-navy-mid">
                          <tr>
                            <th class="px-3 py-2 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Categoría</th>
                            <th class="px-3 py-2 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Tarifa USD</th>
                          </tr>
                        </thead>
                        <tbody class="divide-y divide-navy-mid/50">
                          @for (c of eventCategories(); track c.categoryId) {
                            <tr>
                              <td class="px-3 py-2 text-text-light">{{ c.categoryName }}</td>
                              <td class="px-3 py-2">
                                @if (isEditingCell(c.categoryId)) {
                                  <input type="number" min="0" [value]="c.customTariffUsd ?? ''"
                                         (blur)="commitCellEdit(c.categoryId, $event)"
                                         (keydown.enter)="commitCellEdit(c.categoryId, $event)"
                                         class="bg-navy-mid/40 border border-cyan-brand rounded px-2 py-1 text-sm text-text-light w-28 focus:outline-none">
                                } @else {
                                  <span (click)="startCellEdit(c.categoryId)" class="cursor-pointer hover:text-cyan-brand">
                                    \${{ c.customTariffUsd ?? 0 }}
                                  </span>
                                }
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  }
                }
                @if (categoriesError()) {
                  <p class="text-error-brand text-xs">{{ categoriesError() }}</p>
                }
              </div>
            }

            @if (saveError()) {
              <p class="text-error-brand text-xs px-6 -mt-2">{{ saveError() }}</p>
            }

            @if (eventCategories().length > 0 && categoryCapacityTotal() !== eventCapacity()) {
              <p class="text-warning-brand text-xs px-6 -mt-2">
                El cupo por categorías suma {{ categoryCapacityTotal() }}; debe coincidir con el cupo total del evento ({{ eventCapacity() }}).
              </p>
            }

            <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
              <button type="button" (click)="closeModal()"
                      class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-sm transition">
                Cancelar
              </button>
              <button type="submit" [disabled]="saving() || form.invalid"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-sm hover:bg-cyan-dark transition disabled:opacity-50">
                {{ saving() ? 'Guardando...' : (editingId() ? 'Guardar cambios' : 'Crear evento') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Modal: Importar de SurfScores -->
    @if (importModalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="closeImport()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">Importar eventos de SurfScores</h2>
            <button (click)="closeImport()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <div class="p-6 space-y-5">
            @if (!importResult()) {
              <div>
                <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Circuito destino *</label>
                <select [value]="importCircuitId()" (change)="onImportCircuitChange($event)"
                        class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
                  <option value="">— Seleccionar circuito —</option>
                  @for (c of circuits(); track c.id) {
                    <option [value]="c.id">{{ c.nombre }} ({{ c.temporada }})</option>
                  }
                </select>
                <p class="text-text-muted/60 text-xs mt-1.5">Se traerán los eventos de la organización configurada en Configuración → Integraciones → SurfScores y se crearán en estado "Borrador".</p>
              </div>
              @if (importError()) {
                <p class="text-error-brand text-xs">{{ importError() }}</p>
              }
            } @else {
              <div class="space-y-4">
                <p class="text-sm text-text-muted">
                  Se encontraron <strong class="text-text-light">{{ importResult()!.totalFetched }}</strong> eventos en SurfScores.
                </p>

                @if (importResult()!.created.length > 0) {
                  <div>
                    <p class="text-xs font-accent uppercase tracking-wider text-success-brand mb-2">Creados ({{ importResult()!.created.length }})</p>
                    <ul class="space-y-2">
                      @for (ev of importResult()!.created; track ev.eventId) {
                        <li class="rounded-lg border border-navy-mid p-3">
                          <p class="text-text-light font-medium">{{ ev.nombre }}</p>
                          <p class="text-xs text-text-muted mt-0.5">
                            {{ ev.categoriesLinked }} categoría(s) enlazada(s)
                            @if (ev.unmatchedCategoryCodes.length > 0) {
                              · {{ ev.unmatchedCategoryCodes.length }} sin match
                            }
                          </p>
                          @if (ev.unmatchedCategoryCodes.length > 0) {
                            <p class="text-[11px] text-warning-brand mt-1">Sin coincidencia: {{ ev.unmatchedCategoryCodes.join(', ') }}</p>
                          }
                        </li>
                      }
                    </ul>
                  </div>
                }

                @if (importResult()!.skipped.length > 0) {
                  <div>
                    <p class="text-xs font-accent uppercase tracking-wider text-warning-brand mb-2">Omitidos ({{ importResult()!.skipped.length }})</p>
                    <ul class="space-y-2">
                      @for (ev of importResult()!.skipped; track ev.surfScoresCode) {
                        <li class="rounded-lg border border-warning-brand/30 bg-warning-brand/5 p-3">
                          <p class="text-text-light font-medium">{{ ev.nombre }}</p>
                          <p class="text-xs text-warning-brand mt-0.5">{{ ev.reason }}</p>
                        </li>
                      }
                    </ul>
                  </div>
                }

                @if (importResult()!.created.length === 0 && importResult()!.skipped.length === 0) {
                  <p class="text-text-muted text-sm py-6 text-center">No se encontraron eventos para importar.</p>
                }
              </div>
            }
          </div>

          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" (click)="closeImport()"
                    class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-sm transition">
              {{ importResult() ? 'Cerrar' : 'Cancelar' }}
            </button>
            @if (!importResult()) {
              <button type="button" (click)="runImport()" [disabled]="importing() || !importCircuitId()"
                      class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-sm hover:bg-cyan-dark transition disabled:opacity-50">
                {{ importing() ? 'Importando...' : 'Importar' }}
              </button>
            }
          </div>
        </div>
      </div>
    }

    <app-import-excel-modal [open]="importOpen()" importPath="/events/import" entityLabel="eventos"
                             (close)="importOpen.set(false)" (imported)="onImported()" />

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
export class AdminEventosComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Eventos'));
  importOpen = signal(false);

  loading = signal(true);
  saving = signal(false);
  events = signal<EventItem[]>([]);
  circuits = signal<CircuitOption[]>([]);
  categoryOptions = signal<CategoryOption[]>([]);

  searchTerm = signal('');
  filterCircuitId = signal('');
  filterEstado = signal('todos');
  filterCountry = signal('');
  currentPage = signal(1);

  modalOpen = signal(false);
  modalTab = signal<'general' | 'categorias' | 'tarifas'>('general');
  editingId = signal<string | null>(null);
  deleteTarget = signal<EventItem | null>(null);

  importModalOpen = signal(false);
  importCircuitId = signal('');
  importing = signal(false);
  importError = signal<string | null>(null);
  importResult = signal<SurfScoresImportResult | null>(null);

  uploadingPoster = signal(false);
  uploadError = signal<string | null>(null);
  saveError = signal<string | null>(null);
  categoriesError = signal<string | null>(null);

  eventCategories = signal<EventCategoryEntry[]>([]);
  useCircuitTariffs = signal(true);
  editingCell = signal<string | null>(null);

  starsOptions = STARS;
  accessTypes = ACCESS_TYPES;
  estadosEvento = ESTADOS_EVENTO;
  eventTypes = EVENT_TYPES;

  form = this.fb.group({
    nombre: ['', Validators.required],
    circuitId: ['', Validators.required],
    fechaInicio: ['', Validators.required],
    fechaFin: ['', Validators.required],
    pais: ['', Validators.required],
    ciudad: ['', Validators.required],
    playa: ['', Validators.required],
    auspiciador: [''],
    stars: [3, Validators.required],
    capacidadMaxima: [120, Validators.required],
    prizeAmountUsd: [null as number | null],
    eventType: ['Regular', Validators.required],
    accessType: ['Abierto', Validators.required],
    estado: ['Borrador', Validators.required],
    surfScoresCode: [''],
    imagenUrl: [''],
  });

  // Contadores de la cabecera basados en el estado administrativo (estado) y el estado publico
  // (statusPublic) ya resueltos por el backend. Ambos pueden solaparse (Activo <-> Inscripciones
  // Abiertas) porque asi lo mapea Event.GetPublicStatus() en el backend.
  statActivos = computed(() => this.events().filter(e => e.estado === 'Activo').length);
  statProximos = computed(() => this.events().filter(e => e.estado === 'Próximamente').length);
  statCompletados = computed(() => this.events().filter(e => e.estado === 'Completado').length);
  statInscripcionesAbiertas = computed(() => this.events().filter(e => e.statusPublic === 'Inscripciones Abiertas').length);

  countryOptions = computed(() =>
    Array.from(new Set(this.events().map(e => e.pais).filter(Boolean))).sort(),
  );

  categoryCapacityTotal = computed(() =>
    this.eventCategories().reduce((total, category) => total + (category.capacidad ?? 0), 0),
  );

  filtered = computed(() => {
    const search = this.searchTerm().trim().toLowerCase();
    const circuitId = this.filterCircuitId();
    const estado = this.filterEstado();
    const country = this.filterCountry();

    return this.events().filter(e => {
      if (search && !e.nombre.toLowerCase().includes(search)) return false;
      if (circuitId && e.circuitId !== circuitId) return false;
      if (estado !== 'todos' && e.estado !== estado) return false;
      if (country && e.pais !== country) return false;
      return true;
    });
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / PAGE_SIZE)));

  pagedEvents = computed(() => {
    const page = this.currentPage();
    const start = (page - 1) * PAGE_SIZE;
    return this.filtered().slice(start, start + PAGE_SIZE);
  });

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadEvents(), this.loadCircuits(), this.loadCategories()]);
  }

  private async loadEvents(): Promise<void> {
    this.loading.set(true);
    try {
      const res = await this.api.get<any>('/events?limit=100');
      this.events.set(res?.data ?? res ?? []);
    } catch {
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async downloadTemplate(): Promise<void> {
    await this.api.downloadFile('/events/template', 'events-template.xlsx');
  }

  async onImported(): Promise<void> {
    await this.loadEvents();
  }

  private async loadCircuits(): Promise<void> {
    try {
      const res = await this.api.get<any>('/circuits');
      this.circuits.set(res?.data ?? res ?? []);
    } catch {
      this.circuits.set([]);
    }
  }

  private async loadCategories(): Promise<void> {
    try {
      const res = await this.api.get<any>('/categories');
      this.categoryOptions.set(res?.data ?? res ?? []);
    } catch {
      this.categoryOptions.set([]);
    }
  }

  private async loadEventCategories(eventId: string): Promise<void> {
    try {
      const res = await this.api.get<{ useCircuitTariffs: boolean; data: EventCategoryEntry[] }>(`/events/${eventId}/categories`);
      this.useCircuitTariffs.set(res.useCircuitTariffs);
      this.eventCategories.set(res.data ?? []);
    } catch {
      this.eventCategories.set([]);
    }
  }

  circuitName(id: string): string {
    return this.circuits().find(c => c.id === id)?.nombre ?? '—';
  }

  capacidadPct(ev: EventItem): number {
    if (!ev.capacidadMaxima) return 0;
    return Math.min(100, Math.round(((ev.enrolledCount ?? 0) / ev.capacidadMaxima) * 100));
  }

  flagOf(pais: string): string {
    const flags: Record<string, string> = {
      PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
      CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
      VE: '🇻🇪', BO: '🇧🇴',
    };
    return flags[pais?.toUpperCase()] ?? '🏳️';
  }

  onSearchChange(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1);
  }

  onFilterCircuitChange(event: Event): void {
    this.filterCircuitId.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
  }

  onFilterEstadoChange(event: Event): void {
    this.filterEstado.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
  }

  onFilterCountryChange(event: Event): void {
    this.filterCountry.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
  }

  prevPage(): void {
    this.currentPage.update(p => Math.max(1, p - 1));
  }

  nextPage(): void {
    this.currentPage.update(p => Math.min(this.totalPages(), p + 1));
  }

  verInscritos(ev: EventItem): void {
    this.router.navigate(['/admin/inscritos'], { queryParams: { eventId: ev.id } });
  }

  // ─── Categorías habilitadas ───────────────────────────────────

  isCategoryEnabled(categoryId: string): boolean {
    return this.eventCategories().some(c => c.categoryId === categoryId);
  }

  isCategoryStarSelected(categoryId: string, star: number | null): boolean {
    const entry = this.eventCategories().find(c => c.categoryId === categoryId);
    return (entry?.stars ?? null) === star;
  }

  categoryGender(categoryId: string): string | null {
    return this.eventCategories().find(c => c.categoryId === categoryId)?.gender ?? null;
  }

  categoryCapacity(categoryId: string): number | null {
    return this.eventCategories().find(c => c.categoryId === categoryId)?.capacidad ?? null;
  }

  eventCapacity(): number {
    return Number(this.form.get('capacidadMaxima')?.value ?? 0);
  }

  toggleCategory(categoryId: string, categoryName: string): void {
    if (this.isCategoryEnabled(categoryId)) {
      this.eventCategories.update(list => list.filter(c => c.categoryId !== categoryId));
    } else {
      this.eventCategories.update(list => [...list, {
        categoryId, categoryName, stars: null,
        customTariffUsd: null, capacidad: 0,
      }]);
    }
  }

  onCategoryStarsChange(categoryId: string, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const stars = value ? Number(value) : null;
    this.eventCategories.update(list =>
      list.map(c => c.categoryId === categoryId ? { ...c, stars } : c),
    );
  }

  onCategoryCapacityChange(categoryId: string, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const capacidad = raw === '' ? null : Number(raw);
    this.eventCategories.update(list =>
      list.map(c => c.categoryId === categoryId ? { ...c, capacidad } : c),
    );
  }

  // ─── Tarifas ───────────────────────────────────────────────────

  isEditingCell(categoryId: string): boolean {
    return this.editingCell() === categoryId;
  }

  startCellEdit(categoryId: string): void {
    this.editingCell.set(categoryId);
  }

  commitCellEdit(categoryId: string, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const value = raw === '' ? null : Number(raw);
    this.eventCategories.update(list =>
      list.map(c => c.categoryId === categoryId ? { ...c, customTariffUsd: value } : c),
    );
    this.editingCell.set(null);
  }

  // ─── Afiche ──────────────────────────────────────────────────

  async onPosterFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadError.set(null);
    this.uploadingPoster.set(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const uploaded = await this.api.upload<{ url: string }>('/uploads/event-poster', formData);
      this.form.get('imagenUrl')!.setValue(uploaded.url);
    } catch (err: any) {
      this.uploadError.set(err?.message ?? 'No se pudo subir la imagen.');
    } finally {
      this.uploadingPoster.set(false);
      input.value = '';
    }
  }

  // ─── CRUD ────────────────────────────────────────────────────

  openCreate(): void {
    this.editingId.set(null);
    this.modalTab.set('general');
    this.uploadError.set(null);
    this.saveError.set(null);
    this.categoriesError.set(null);
    this.eventCategories.set([]);
    this.useCircuitTariffs.set(true);
    this.form.reset({
      nombre: '', circuitId: '', fechaInicio: '', fechaFin: '',
      pais: '', ciudad: '', playa: '', auspiciador: '', stars: 3, capacidadMaxima: 120,
      prizeAmountUsd: null, eventType: 'Regular', accessType: 'Abierto', estado: 'Borrador',
      surfScoresCode: '', imagenUrl: '',
    });
    this.modalOpen.set(true);
  }

  openEdit(ev: EventItem): void {
    this.editingId.set(ev.id);
    this.modalTab.set('general');
    this.uploadError.set(null);
    this.saveError.set(null);
    this.categoriesError.set(null);
    this.eventCategories.set([]);
    this.useCircuitTariffs.set(true);
    this.form.reset({
      nombre: ev.nombre,
      circuitId: ev.circuitId,
      fechaInicio: ev.fechaInicio?.substring(0, 10) ?? '',
      fechaFin: ev.fechaFin?.substring(0, 10) ?? '',
      pais: ev.pais,
      ciudad: ev.ciudad,
      playa: ev.playa ?? '',
      auspiciador: ev.auspiciador ?? '',
      stars: ev.stars,
      capacidadMaxima: ev.capacidadMaxima,
      prizeAmountUsd: ev.prizeAmountUsd ?? null,
      eventType: ev.eventType ?? 'Regular',
      accessType: ev.accessType ?? 'Abierto',
      estado: ev.estado ?? 'Borrador',
      surfScoresCode: ev.surfScoresCode ?? '',
      imagenUrl: ev.imagenUrl ?? '',
    });
    this.modalOpen.set(true);
    void this.loadEventCategories(ev.id);
  }

  closeModal(): void {
    this.modalOpen.set(false);
    this.editingId.set(null);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.saveError.set('Completa todos los campos obligatorios (*) antes de guardar.');
      return;
    }
    this.saving.set(true);
    this.saveError.set(null);
    this.categoriesError.set(null);
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
        auspiciador: v.auspiciador || null,
        stars: Number(v.stars),
        capacidadMaxima: Number(v.capacidadMaxima),
        prizeAmountUsd: v.prizeAmountUsd ? Number(v.prizeAmountUsd) : 0,
        eventType: v.eventType,
        accessType: v.accessType,
        estado: v.estado,
        imagenUrl: v.imagenUrl || null,
        surfScoresCode: v.surfScoresCode || '',
      };

      let eventId = this.editingId();
      if (eventId) {
        await this.api.put(`/events/${eventId}`, body);
      } else {
        const created = await this.api.post<{ id: string }>('/events', body);
        eventId = created.id;
        this.editingId.set(eventId);
      }

      try {
        await this.api.put(`/events/${eventId}/categories`, {
          useCircuitTariffs: this.useCircuitTariffs(),
          categories: this.eventCategories().map(c => ({
            categoryId: c.categoryId,
            stars: c.stars,
            customTariffUsd: c.customTariffUsd,
            capacidad: c.capacidad,
          })),
        });
      } catch (catErr: any) {
        this.categoriesError.set(catErr?.message ?? 'El evento se guardó, pero no se pudieron guardar las categorías.');
        this.modalTab.set('categorias');
        await this.loadEvents();
        return;
      }

      this.closeModal();
      await this.loadEvents();
    } catch (err: any) {
      const fields = err?.body?.fields as { field: string; message: string }[] | undefined;
      this.saveError.set(
        fields?.length ? fields.map(f => f.message).join(' ') : (err?.message ?? 'No se pudo guardar el evento.'),
      );
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

  // ─── Importar de SurfScores ────────────────────────────────────

  openImport(): void {
    this.importCircuitId.set(this.filterCircuitId() || '');
    this.importError.set(null);
    this.importResult.set(null);
    this.importModalOpen.set(true);
  }

  closeImport(): void {
    this.importModalOpen.set(false);
    if (this.importResult()) {
      void this.loadEvents();
    }
  }

  onImportCircuitChange(event: Event): void {
    this.importCircuitId.set((event.target as HTMLSelectElement).value);
  }

  async runImport(): Promise<void> {
    const circuitId = this.importCircuitId();
    if (!circuitId) return;

    this.importing.set(true);
    this.importError.set(null);
    try {
      const result = await this.api.post<SurfScoresImportResult>(`/circuits/${circuitId}/surfscores-import`, {});
      this.importResult.set(result);
    } catch (err: any) {
      this.importError.set(err?.body?.fields?.[0]?.message ?? err?.message ?? 'No se pudo importar desde SurfScores.');
    } finally {
      this.importing.set(false);
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
