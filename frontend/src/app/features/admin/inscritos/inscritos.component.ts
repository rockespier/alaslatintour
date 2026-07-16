import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type InscritosTab = 'inscritos' | 'premios' | 'puestos';

interface InscritoRow {
  id: string;
  numero: string;
  competidor: string;
  pais: string;
  rank2025: string;
  rank2026: string;
  categoria: string;
  fechaInscripcion: string;
  metodo: 'paypal' | 'beach';
  montoUsd: number;
  estado: 'Pagado' | 'Pendiente';
  federacion: string;
  licencia: string;
  transaccionId: string | null;
  notas: string;
}

interface EventoOption { id: string; nombre: string; }
interface CategoriaOption { id: string; nombre: string; }

interface ResultadoRow {
  id: string;
  place: string;
  competidor: string;
  pais: string;
  ligaPoints: number;
  prizeUsd: number | null;
  heatScoreTotal: number | null;
}

const CLASS_INPUT = 'w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition';

function fmtDateTime(dt: string): string {
  return new Date(dt).toLocaleString('es', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

@Component({
  selector: 'app-inscritos',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent, DecimalPipe],
  template: `
    <div class="py-8">
      <div class="mb-6">
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Inscritos</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Inscritos y Resultados</h1>
      </div>

      <!-- Tabs -->
      <div class="border-b border-navy-mid mb-6 overflow-x-auto">
        <nav class="flex gap-2 min-w-max">
          <button (click)="tab.set('inscritos')" [class]="tabClass('inscritos')">Inscritos</button>
          <button (click)="tab.set('premios')" [class]="tabClass('premios')">Puntajes de Premios</button>
          <button (click)="tab.set('puestos')" [class]="tabClass('puestos')">Puestos por Evento</button>
        </nav>
      </div>

      <!-- ═══ TAB: INSCRITOS ═══ -->
      @if (tab() === 'inscritos') {
        <div>
          @if (loading()) {
            <app-loading-spinner />
          } @else {
          <!-- Filter bar -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-4 mb-6 flex flex-col lg:flex-row gap-3">
            <select [class]="CLASS_INPUT + ' lg:max-w-[220px]'" [(ngModel)]="filterEvento" (ngModelChange)="loadInscritos()">
              <option value="">Todos los eventos</option>
              @for (e of eventos(); track e.id) { <option [value]="e.id">{{ e.nombre }}</option> }
            </select>
            <select [class]="CLASS_INPUT + ' lg:max-w-[180px]'" [(ngModel)]="filterCategoria" (ngModelChange)="loadInscritos()">
              <option value="">Todas las categorías</option>
              @for (c of categorias(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
            </select>
            <select [class]="CLASS_INPUT + ' lg:max-w-[180px]'" [(ngModel)]="filterEstado" (ngModelChange)="loadInscritos()">
              <option value="">Estado de pago</option>
              <option value="Pagado">Pagado</option>
              <option value="Pendiente">Pendiente</option>
            </select>
            <div class="relative flex-1">
              <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
              <input type="text" placeholder="Buscar competidor..." [(ngModel)]="searchTerm" [class]="CLASS_INPUT + ' pl-9'">
            </div>
          </div>

          @if (filterEvento) {
            <p class="text-xs text-text-muted mb-4">Mostrando inscritos de <span class="text-cyan-brand">{{ nombreEventoSeleccionado() }}</span></p>
          }

          <!-- Stats strip -->
          <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
            <div class="bg-navy-dark rounded-lg border border-navy-mid p-4">
              <p class="font-accent uppercase text-xs tracking-wider text-text-muted">Total inscritos</p>
              <p class="font-heading text-2xl text-text-light">{{ totalItems() }}</p>
            </div>
            <div class="bg-navy-dark rounded-lg border border-navy-mid p-4">
              <p class="font-accent uppercase text-xs tracking-wider text-text-muted">PayPal</p>
              <p class="font-heading text-2xl text-success-brand">{{ resumen().paypal }}</p>
            </div>
            <div class="bg-navy-dark rounded-lg border border-navy-mid p-4">
              <p class="font-accent uppercase text-xs tracking-wider text-text-muted">Pago en playa</p>
              <p class="font-heading text-2xl text-cyan-brand">{{ resumen().beach }}</p>
            </div>
            <div class="bg-navy-dark rounded-lg border border-navy-mid p-4">
              <p class="font-accent uppercase text-xs tracking-wider text-text-muted">Pendiente</p>
              <p class="font-heading text-2xl text-orange-brand">{{ resumen().pendiente }}</p>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="border-b border-navy-mid">
                  <tr>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">#</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Competidor</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">País</th>
                    <th class="px-3 py-3 text-center font-accent uppercase text-xs tracking-wider text-text-muted">Rank. 2025</th>
                    <th class="px-3 py-3 text-center font-accent uppercase text-xs tracking-wider text-text-muted">Rank. 2026</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Categoría</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Inscripción</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Pago</th>
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                    <th class="px-3 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (row of filteredInscritos(); track row.id) {
                    <tr class="hover:bg-cyan-brand/5 transition">
                      <td class="px-3 py-3 font-mono text-text-muted">{{ row.numero }}</td>
                      <td class="px-3 py-3 font-medium text-text-light">{{ row.competidor }}</td>
                      <td class="px-3 py-3 text-text-muted">{{ row.pais }}</td>
                      <td class="px-3 py-3 text-center font-mono text-sm text-text-light">{{ row.rank2025 }}</td>
                      <td class="px-3 py-3 text-center font-mono text-sm text-cyan-brand">{{ row.rank2026 }}</td>
                      <td class="px-3 py-3 text-text-muted">{{ row.categoria }}</td>
                      <td class="px-3 py-3 text-xs text-text-muted">{{ row.fechaInscripcion }}</td>
                      <td class="px-3 py-3 text-text-muted text-xs">{{ row.metodo === 'paypal' ? 'PayPal' : 'Playa' }} \${{ row.montoUsd }}</td>
                      <td class="px-3 py-3">
                        <span [class]="row.estado === 'Pagado'
                          ? 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-brand/15 text-success-brand text-xs font-accent uppercase tracking-wider'
                          : 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-brand/15 text-warning-brand text-xs font-accent uppercase tracking-wider'">
                          {{ row.estado }}
                        </span>
                      </td>
                      <td class="px-3 py-3 text-right whitespace-nowrap">
                        <button (click)="toggleExpand(row.id)" class="text-xs font-accent uppercase tracking-wider text-cyan-brand hover:text-cyan-dark mr-2">Ver detalle</button>
                        @if (row.estado === 'Pendiente' && canEdit()) {
                          <button (click)="validarPago(row)" class="text-xs font-accent uppercase tracking-wider text-success-brand hover:text-green-400">Validar pago</button>
                        }
                      </td>
                    </tr>
                    @if (expanded() === row.id) {
                      <tr>
                        <td [attr.colspan]="10" class="p-0">
                          <div [class]="row.estado === 'Pendiente'
                            ? 'bg-navy-deepest border-y-2 border-warning-brand/30 p-5'
                            : 'bg-navy-deepest border-y-2 border-cyan-brand/30 p-5'">
                            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                              <div>
                                <p class="font-accent uppercase text-xs tracking-wider text-text-muted mb-2">Perfil</p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Federación:</span> {{ row.federacion }}</p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Licencia #:</span> {{ row.licencia }}</p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Inscripción:</span> {{ row.fechaInscripcion }}</p>
                              </div>
                              <div>
                                <p class="font-accent uppercase text-xs tracking-wider text-text-muted mb-2">Pago</p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Método:</span> {{ row.metodo === 'paypal' ? 'PayPal' : 'Efectivo (playa)' }}</p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Transacción:</span> <code class="text-cyan-brand">{{ row.transaccionId ?? '—' }}</code></p>
                                <p class="text-sm text-text-light"><span class="text-text-muted">Monto:</span> USD {{ row.montoUsd }}</p>
                              </div>
                              <div>
                                <p class="font-accent uppercase text-xs tracking-wider text-text-muted mb-2">Notas</p>
                                <textarea [class]="CLASS_INPUT + ' text-xs'" rows="3" placeholder="Notas internas..." [(ngModel)]="row.notas" [disabled]="!canEdit()"></textarea>
                              </div>
                            </div>
                            @if (row.estado === 'Pendiente' && canEdit()) {
                              <button (click)="validarPago(row)" class="px-4 py-2 bg-success-brand hover:bg-green-600 text-white font-accent uppercase tracking-wider text-sm rounded-md transition">Confirmar Pago en Playa</button>
                            }
                          </div>
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>
            <div class="border-t border-navy-mid px-4 py-3 text-xs text-text-muted">
              <p>Mostrando {{ filteredInscritos().length }} de {{ totalItems() }} inscritos</p>
            </div>
          </div>
          }
        </div>
      }

      <!-- ═══ TAB: PUNTAJES DE PREMIOS ═══ -->
      @if (tab() === 'premios') {
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
          <div class="mb-6">
            <h2 class="font-heading text-xl text-white mb-1">Distribución de premios por evento</h2>
            <p class="text-sm text-text-muted">Monto en USD que corresponde a cada puesto, calculado desde el pozo total del evento (<code>prizeAmountUsd</code>) según sus estrellas y los porcentajes definidos en Configuración → Ranking.</p>
          </div>

          <div class="flex flex-col sm:flex-row gap-3 mb-6">
            <select [class]="CLASS_INPUT + ' sm:max-w-[320px]'" [(ngModel)]="premiosEventoId" (ngModelChange)="loadPrizeDistribution()">
              <option value="">— Selecciona un evento —</option>
              @for (e of eventos(); track e.id) { <option [value]="e.id">{{ e.nombre }}</option> }
            </select>
            @if (premiosStars() > 0) {
              <p class="flex items-center text-xs text-text-muted">Evento de <span class="text-warning-brand mx-1">{{ '★'.repeat(premiosStars()) }}</span></p>
            }
          </div>

          @if (!premiosEventoId) {
            <p class="text-sm text-text-muted">Selecciona un evento para ver su distribución de premios.</p>
          } @else if (premiosLoading()) {
            <app-loading-spinner />
          } @else if (premiosRows().length === 0) {
            <p class="text-sm text-text-muted">Este evento no tiene premio configurado (prizeAmountUsd = 0) o no hay distribución definida.</p>
          } @else {
            <div class="bg-navy-deepest rounded-lg border border-navy-mid overflow-hidden max-w-md">
              <div class="overflow-x-auto">
                <table class="w-full text-sm">
                  <thead class="border-b border-navy-mid bg-navy-dark/50">
                    <tr>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Puesto</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Premio USD</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-navy-mid/40">
                    @for (row of premiosRows(); track row.placeLabel) {
                      <tr class="hover:bg-cyan-brand/5 transition">
                        <td class="px-4 py-3 font-heading text-text-light">{{ row.placeLabel }}</td>
                        <td class="px-4 py-3 text-right text-success-brand">\${{ row.prizeUsd | number:'1.0-2' }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }
        </div>
      }

      <!-- ═══ TAB: PUESTOS POR EVENTO ═══ -->
      @if (tab() === 'puestos') {
        <div>
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
            <div class="flex flex-col sm:flex-row gap-3">
              <select [class]="CLASS_INPUT + ' sm:max-w-[280px]'" [(ngModel)]="puestosEventoId" (ngModelChange)="onPuestosEventoChange()">
                <option value="">— Selecciona un evento —</option>
                @for (e of eventos(); track e.id) { <option [value]="e.id">{{ e.nombre }}</option> }
              </select>
              <select [class]="CLASS_INPUT + ' sm:max-w-[220px]'" [(ngModel)]="puestosCategoriaId" (ngModelChange)="loadResultados()">
                <option value="">Todas las categorías</option>
                @for (c of puestosCategorias(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
              </select>
            </div>
          </div>

          @if (!puestosEventoId) {
            <p class="text-sm text-text-muted">Selecciona un evento para ver sus resultados.</p>
          } @else if (puestosLoading()) {
            <app-loading-spinner />
          } @else if (resultados().length === 0) {
            <p class="text-sm text-text-muted">Aún no hay resultados cargados para este evento/categoría.</p>
          } @else {
            <!-- Podium -->
            <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
              <h3 class="font-heading text-lg text-white mb-6 text-center">Podio</h3>
              <div class="grid grid-cols-3 items-end gap-3 max-w-2xl mx-auto">
                @for (p of podio(); track p.place) {
                  <div class="text-center">
                    <div [class]="'mx-auto mb-3 rounded-full flex items-center justify-center font-heading text-white ' + p.avatarClass">{{ p.iniciales }}</div>
                    <p class="font-medium text-sm text-text-light">{{ p.competidor }}</p>
                    <p class="text-xs text-text-muted mb-3">{{ p.pais }}</p>
                    <div [class]="p.podiumClass">
                      <p [class]="p.numberClass">{{ p.place }}</p>
                      <p class="text-xs font-accent uppercase tracking-wider" [class]="p.place === '1' ? 'text-white/80' : 'text-text-muted'">{{ p.heatScoreTotal ?? '—' }} pts</p>
                    </div>
                  </div>
                }
              </div>
            </div>

            <!-- Results table -->
            <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
              <div class="px-6 py-4 border-b border-navy-mid">
                <h3 class="font-heading text-lg text-white">Resultados completos</h3>
              </div>
              <div class="overflow-x-auto">
                <table class="w-full text-sm">
                  <thead class="border-b border-navy-mid">
                    <tr>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Puesto</th>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Competidor</th>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">País</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Puntos de Liga</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Premio</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Heat Score Total</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-navy-mid/50">
                    @for (r of resultados(); track r.id) {
                      <tr class="hover:bg-cyan-brand/5 transition">
                        <td [class]="puestoClass(r.place)">{{ r.place }}</td>
                        <td class="px-4 py-3 font-medium text-text-light">{{ r.competidor }}</td>
                        <td class="px-4 py-3 text-text-muted">{{ r.pais }}</td>
                        <td class="px-4 py-3 text-right text-text-light">{{ r.ligaPoints }}</td>
                        <td class="px-4 py-3 text-right text-success-brand">{{ r.prizeUsd !== null ? '$' + r.prizeUsd : '—' }}</td>
                        <td class="px-4 py-3 text-right font-mono text-text-light">{{ r.heatScoreTotal ?? '—' }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              <div class="px-6 py-3 border-t border-navy-mid text-xs text-text-muted">
                <a href="https://surfscores.com" target="_blank" rel="noopener" class="text-cyan-brand hover:underline">{{ attribution() }}</a>
              </div>
            </div>
          }
        </div>
      }
    </div>

    <!-- Toast -->
    @if (toast().show) {
      <div class="fixed bottom-6 right-6 z-50 bg-navy-dark border border-success-brand/50 rounded-lg shadow-2xl px-5 py-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-success-brand" fill="currentColor" viewBox="0 0 20 20"><path d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"/></svg>
        <p class="text-sm text-text-light">{{ toast().message }}</p>
      </div>
    }
  `,
})
export class InscritosComponent implements OnInit {
  private api = inject(ApiService);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Inscripciones'));

  CLASS_INPUT = CLASS_INPUT;

  tab = signal<InscritosTab>('inscritos');
  tabClass(t: InscritosTab): string {
    return this.tab() === t
      ? 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-cyan-brand border-b-2 border-cyan-brand whitespace-nowrap'
      : 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-text-muted border-b-2 border-transparent hover:text-text-light transition whitespace-nowrap';
  }

  loading = signal(true);

  eventos = signal<EventoOption[]>([]);
  categorias = signal<CategoriaOption[]>([]);

  searchTerm = '';
  filterEvento = '';
  filterCategoria = '';
  filterEstado = '';

  expanded = signal<string | null>(null);
  toggleExpand(id: string): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  totalItems = signal(0);
  inscritos = signal<InscritoRow[]>([]);

  filteredInscritos = computed(() => {
    const term = this.searchTerm.trim().toLowerCase();
    return this.inscritos().filter(r => {
      if (term && !r.competidor.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  resumen = computed(() => {
    const list = this.inscritos();
    return {
      paypal: list.filter(r => r.metodo === 'paypal').length,
      beach: list.filter(r => r.metodo === 'beach').length,
      pendiente: list.filter(r => r.estado === 'Pendiente').length,
    };
  });

  nombreEventoSeleccionado(): string {
    return this.eventos().find(e => e.id === this.filterEvento)?.nombre ?? '';
  }

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      const [eventsRes, categoriesRes] = await Promise.all([
        this.api.get<any>('/events?limit=100'),
        this.api.get<any>('/categories'),
      ]);
      this.eventos.set((eventsRes?.data ?? []).map((e: any) => ({ id: e.id, nombre: e.nombre })));
      this.categorias.set((categoriesRes?.data ?? []).map((c: any) => ({ id: c.id, nombre: c.nombre })));
      await this.loadInscritos();
    } catch {
      this.showToast('Error al cargar los inscritos');
    } finally {
      this.loading.set(false);
    }
  }

  async loadInscritos(): Promise<void> {
    const params = new URLSearchParams({ limit: '100' });
    if (this.filterEvento) params.set('eventId', this.filterEvento);
    if (this.filterCategoria) params.set('categoryId', this.filterCategoria);
    if (this.filterEstado) params.set('status', this.filterEstado);
    const res = await this.api.get<any>(`/inscriptions?${params.toString()}`);
    const data: any[] = res?.data ?? [];
    this.inscritos.set(data.map(r => this.mapRow(r)));
    this.totalItems.set(res?.pagination?.totalItems ?? data.length);
  }

  private mapRow(r: any): InscritoRow {
    return {
      id: r.id,
      numero: r.sequentialNumber,
      competidor: r.fullName,
      pais: r.country,
      rank2025: r.ranking2025 ?? '—',
      rank2026: r.ranking2026 ?? '—',
      categoria: r.categoria,
      fechaInscripcion: fmtDateTime(r.inscripcionDate),
      metodo: r.paymentMethod,
      montoUsd: r.montoUsd,
      estado: r.estadoAdmin,
      federacion: r.federacion,
      licencia: r.licenciaNumber,
      transaccionId: r.transaccionId ?? null,
      notas: r.notas ?? '',
    };
  }

  async validarPago(row: InscritoRow): Promise<void> {
    try {
      await this.api.put<any>(`/inscriptions/${row.id}`, { estadoAdmin: 'Pagado', notes: row.notas });
      this.inscritos.update(list => list.map(r => r.id === row.id ? { ...r, estado: 'Pagado' as const } : r));
      this.showToast('Pago validado correctamente');
      this.expanded.set(null);
    } catch {
      this.showToast('Error al validar el pago');
    }
  }

  // ─── Puntajes de Premios (por evento, calculado por el backend) ──
  premiosEventoId = '';
  premiosLoading = signal(false);
  premiosStars = signal(0);
  premiosRows = signal<{ placeLabel: string; prizeUsd: number }[]>([]);

  async loadPrizeDistribution(): Promise<void> {
    if (!this.premiosEventoId) {
      this.premiosRows.set([]);
      this.premiosStars.set(0);
      return;
    }
    this.premiosLoading.set(true);
    try {
      const res = await this.api.get<any>(`/events/${this.premiosEventoId}/prize-distribution`);
      this.premiosRows.set((res?.data ?? []).map((r: any) => ({ placeLabel: r.placeLabel, prizeUsd: r.prizeUsd })));
      this.premiosStars.set(res?.stars ?? 0);
    } catch {
      this.showToast('Error al cargar la distribución de premios');
      this.premiosRows.set([]);
    } finally {
      this.premiosLoading.set(false);
    }
  }

  // ─── Puestos por evento (resultados reales, vía SurfScores) ─────
  puestosEventoId = '';
  puestosCategoriaId = '';
  puestosCategorias = signal<CategoriaOption[]>([]);
  puestosLoading = signal(false);
  resultados = signal<ResultadoRow[]>([]);
  attribution = signal('Results by SurfScores.com');

  async onPuestosEventoChange(): Promise<void> {
    this.puestosCategoriaId = '';
    this.resultados.set([]);
    if (!this.puestosEventoId) {
      this.puestosCategorias.set([]);
      return;
    }
    try {
      const res = await this.api.get<any>(`/events/${this.puestosEventoId}/categories`);
      this.puestosCategorias.set((res?.data ?? []).map((c: any) => ({ id: c.categoryId ?? c.id, nombre: c.categoryName ?? c.nombre })));
    } catch {
      this.puestosCategorias.set([]);
    }
    await this.loadResultados();
  }

  async loadResultados(): Promise<void> {
    if (!this.puestosEventoId) return;
    this.puestosLoading.set(true);
    try {
      const params = this.puestosCategoriaId ? `?categoryId=${this.puestosCategoriaId}` : '';
      const res = await this.api.get<any>(`/events/${this.puestosEventoId}/results${params}`);
      this.attribution.set(res?.attribution ?? 'Results by SurfScores.com');
      this.resultados.set((res?.data ?? []).map((r: any) => ({
        id: r.id,
        place: r.place,
        competidor: r.competitorName,
        pais: r.country,
        ligaPoints: r.ligaPoints,
        prizeUsd: r.prizeUsd ?? null,
        heatScoreTotal: r.heatScoreTotal ?? null,
      })).sort((a: ResultadoRow, b: ResultadoRow) => (parseInt(a.place, 10) || 999) - (parseInt(b.place, 10) || 999)));
    } catch {
      this.showToast('Error al cargar los resultados');
      this.resultados.set([]);
    } finally {
      this.puestosLoading.set(false);
    }
  }

  podio = computed(() => {
    const byPlace = new Map(this.resultados().map(r => [r.place, r]));
    const configs: Record<string, { avatarClass: string; podiumClass: string; numberClass: string }> = {
      '1': {
        avatarClass: 'w-20 h-20 text-3xl shadow-lg shadow-warning-brand/30 bg-gradient-to-br from-warning-brand to-yellow-600',
        podiumClass: 'bg-gradient-to-t from-warning-brand to-warning-brand/40 rounded-t-lg border border-warning-brand border-b-0 h-44 flex flex-col items-center justify-end pb-3',
        numberClass: 'font-heading text-5xl text-white',
      },
      '2': {
        avatarClass: 'w-16 h-16 text-2xl bg-gradient-to-br from-navy-mid to-text-muted',
        podiumClass: 'bg-gradient-to-t from-navy-mid to-navy-mid/40 rounded-t-lg border border-navy-mid border-b-0 h-32 flex flex-col items-center justify-end pb-3',
        numberClass: 'font-heading text-4xl text-text-light',
      },
      '3': {
        avatarClass: 'w-16 h-16 text-2xl bg-gradient-to-br from-orange-brand to-orange-600',
        podiumClass: 'bg-gradient-to-t from-orange-brand to-orange-brand/40 rounded-t-lg border border-orange-brand border-b-0 h-24 flex flex-col items-center justify-end pb-3',
        numberClass: 'font-heading text-3xl text-white',
      },
    };
    return ['2', '1', '3']
      .filter(place => byPlace.has(place))
      .map(place => {
        const r = byPlace.get(place)!;
        return {
          place: r.place,
          competidor: r.competidor,
          pais: r.pais,
          heatScoreTotal: r.heatScoreTotal,
          iniciales: r.competidor.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase() ?? '').join(''),
          ...configs[place],
        };
      });
  });

  puestoClass(puesto: string): string {
    if (puesto === '1') return 'px-4 py-3 font-heading text-warning-brand text-lg';
    if (puesto === '2') return 'px-4 py-3 font-heading text-text-light text-lg';
    if (puesto === '3') return 'px-4 py-3 font-heading text-orange-brand text-lg';
    return 'px-4 py-3 font-heading text-text-muted';
  }

  toast = signal<{ show: boolean; message: string }>({ show: false, message: '' });
  showToast(message: string): void {
    this.toast.set({ show: true, message });
    setTimeout(() => this.toast.set({ show: false, message: '' }), 3000);
  }
}
