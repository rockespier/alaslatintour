import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ImportExcelModalComponent } from '../../../shared/components/import-excel-modal/import-excel-modal.component';

type InscritosTab = 'inscritos' | 'premios' | 'puestos';

interface InscritoRow {
  id: string;
  numero: string;
  competidor: string;
  pais: string;
  rank2025: string;
  rank2026: string;
  categoria: string;
  evento: string;
  fechaInscripcion: string;
  metodo: 'paypal' | 'beach';
  montoUsd: number;
  estado: 'Pagado' | 'Pendiente';
  federacion: string;
  licencia: string;
  transaccionId: string | null;
  notas: string;
}

interface EventoOption { id: string; nombre: string; circuitId: string; }
interface CircuitoOption { id: string; nombre: string; }
interface CategoriaOption { id: string; nombre: string; }

interface ResultadoRow {
  id: string;
  competitorId: string;
  place: string;
  competidor: string;
  pais: string;
  ligaPoints: number;
  prizeUsd: number | null;
  heatScoreTotal: number | null;
}

interface FilaResultado {
  competitorId: string;
  nombre: string;
  pais: string;
  place: string;
  ligaPoints: number | null;
  prizeUsd: number | null;
  heatScoreTotal: number | null;
}

interface PremioConfigRow {
  label: string;
  p1: number; p2: number; p3: number; p4: number; p5: number; p6: number; p7: number;
}

const CLASS_INPUT = 'w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition';
const PREMIO_CELL_CLASS = 'w-14 bg-navy-mid/40 border border-navy-mid rounded px-1 py-1 text-right text-sm text-text-light focus:outline-none focus:border-cyan-brand transition disabled:opacity-50';
const PLACE_INPUT_CLASS = 'w-16 bg-navy-mid/40 border border-navy-mid rounded px-2 py-1 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition';
const HEAT_SCORE_INPUT_CLASS = 'w-20 bg-navy-mid/40 border border-navy-mid rounded px-2 py-1 text-sm text-right font-mono text-text-light focus:outline-none focus:border-cyan-brand transition';

function fmtDateTime(dt: string): string {
  return new Date(dt).toLocaleString('es', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

@Component({
  selector: 'app-inscritos',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent, DecimalPipe, ImportExcelModalComponent],
  template: `
    <div class="py-8">
      <div class="mb-6">
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Inscritos</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Inscritos y Resultados</h1>
      </div>

      <!-- Tabs -->
      <div class="border-b border-navy-mid mb-6 overflow-x-auto">
        <nav class="flex gap-2 min-w-max">
          <button (click)="selectTab('inscritos')" [class]="tabClass('inscritos')">Inscritos</button>
          <button (click)="selectTab('premios')" [class]="tabClass('premios')">Puntajes de Premios</button>
          <button (click)="selectTab('puestos')" [class]="tabClass('puestos')">Puestos por Evento</button>
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
            <select [class]="CLASS_INPUT + ' lg:max-w-[180px]'" [(ngModel)]="filterCircuito" (ngModelChange)="onFilterCircuitoChange()">
              <option value="">Todos los circuitos</option>
              @for (c of circuitos(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
            </select>
            <select [class]="CLASS_INPUT + ' lg:max-w-[220px]'" [(ngModel)]="filterEvento" (ngModelChange)="onFilterEventoChange()">
              <option value="">Todos los eventos</option>
              @for (e of eventosFiltrados(); track e.id) { <option [value]="e.id">{{ e.nombre }}</option> }
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
            <button (click)="exportarInscritos()" [disabled]="exportando()" class="px-4 py-2 border border-orange-brand/50 hover:border-orange-brand text-orange-brand font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center whitespace-nowrap disabled:opacity-50">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>
              {{ exportando() ? 'Exportando...' : 'Exportar XLSX' }}
            </button>
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
                    <th class="px-3 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Evento</th>
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
                      <td class="px-3 py-3 text-text-muted">{{ row.evento }}</td>
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
                          <button (click)="validarPago(row)" class="text-xs font-accent uppercase tracking-wider text-success-brand hover:text-green-400 mr-2">Validar pago</button>
                        }
                        <button (click)="exportarFicha(row)" class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-text-light">Exportar ficha</button>
                      </td>
                    </tr>
                    @if (expanded() === row.id) {
                      <tr>
                        <td [attr.colspan]="11" class="p-0">
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
        <div class="space-y-6">
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <div class="mb-6">
              <h2 class="font-heading text-xl text-white mb-1">Distribución de premios por puesto y estrellas del evento</h2>
              <p class="text-sm text-text-muted">Porcentaje del pozo total del evento (<code>prizeAmountUsd</code>) que corresponde a cada puesto según el nivel de estrellas. Haz clic en una celda para editar — esta es la misma configuración de Configuración → Ranking.</p>
            </div>

            @if (!canViewPremiosConfig()) {
              <p class="text-sm text-text-muted">No tienes permiso para ver la configuración de premios. Contacta a un administrador.</p>
            } @else if (premiosConfigLoading()) {
              <app-loading-spinner />
            } @else {
              <div class="bg-navy-deepest rounded-lg border border-navy-mid overflow-hidden">
                <div class="overflow-x-auto">
                  <table class="w-full text-sm">
                    <thead class="border-b border-navy-mid bg-navy-dark/50">
                      <tr>
                        <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Puesto</th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★</span></th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★★</span></th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-navy-mid/40">
                      @for (row of premiosConfigRows(); track row.label) {
                        <tr class="hover:bg-cyan-brand/5 transition">
                          <td class="px-4 py-3 font-heading text-text-light">{{ row.label }}</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p1" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p2" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p3" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p4" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p5" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p6" [disabled]="!canEditPremiosConfig()">%</td>
                          <td class="px-4 py-3 text-right"><input type="number" min="0" [class]="PREMIO_CELL_CLASS" [(ngModel)]="row.p7" [disabled]="!canEditPremiosConfig()">%</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              </div>

              @if (canEditPremiosConfig()) {
                <div class="mt-6 flex justify-end">
                  <button (click)="savePremiosConfig()" [disabled]="premiosConfigSaving()" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-white font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                    {{ premiosConfigSaving() ? 'Guardando...' : 'Guardar Distribución' }}
                  </button>
                </div>
              }
            }
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <div class="mb-6">
              <h2 class="font-heading text-lg text-white mb-1">Vista previa en USD por evento</h2>
              <p class="text-sm text-text-muted">Monto en USD que corresponde a cada puesto, calculado desde el pozo total del evento seleccionado (<code>prizeAmountUsd</code>) según la distribución anterior.</p>
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
        </div>
      }

      <!-- ═══ TAB: PUESTOS POR EVENTO ═══ -->
      @if (tab() === 'puestos') {
        <div>
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
            <div class="flex flex-col sm:flex-row gap-3">
              <select [class]="CLASS_INPUT + ' sm:max-w-[220px]'" [(ngModel)]="puestosCircuitoId" (ngModelChange)="onPuestosCircuitoChange()">
                <option value="">Todos los circuitos</option>
                @for (c of circuitos(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
              </select>
              <select [class]="CLASS_INPUT + ' sm:max-w-[280px]'" [(ngModel)]="puestosEventoId" (ngModelChange)="onPuestosEventoChange()">
                <option value="">— Selecciona un evento —</option>
                @for (e of puestosEventosFiltrados(); track e.id) { <option [value]="e.id">{{ e.nombre }}</option> }
              </select>
              <select [class]="CLASS_INPUT + ' sm:max-w-[220px]'" [(ngModel)]="puestosCategoriaId" (ngModelChange)="onPuestosCategoriaChange()">
                <option value="">Todas las categorías</option>
                @for (c of puestosCategorias(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
              </select>
              @if (puestosEventoId && puestosCategoriaId && canEdit()) {
                <div class="flex gap-2 sm:ml-auto">
                  <button (click)="descargarPlantillaResultados()" [disabled]="descargandoPlantilla()"
                          class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition whitespace-nowrap disabled:opacity-50">
                    {{ descargandoPlantilla() ? 'Descargando...' : 'Descargar plantilla' }}
                  </button>
                  <button (click)="puestosImportOpen.set(true)"
                          class="px-4 py-2 border border-orange-brand/50 hover:border-orange-brand text-orange-brand font-accent uppercase tracking-wider text-sm rounded-md transition whitespace-nowrap">
                    Importar Excel
                  </button>
                </div>
              }
            </div>
          </div>

          <app-import-excel-modal [open]="puestosImportOpen()" [importPath]="puestosImportPath()" entityLabel="resultados"
                                   (close)="puestosImportOpen.set(false)" (imported)="onResultadosImported()" />

          @if (!puestosEventoId) {
            <p class="text-sm text-text-muted">Selecciona un evento para ver sus resultados.</p>
          } @else if (puestosLoading()) {
            <app-loading-spinner />
          } @else {
            @if (resultados().length > 0) {
              <!-- Podium -->
              <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
                <h3 class="font-heading text-lg text-white mb-6 text-center">Podio</h3>
                <div class="grid grid-cols-3 items-end gap-3 max-w-2xl mx-auto">
                  @for (p of podio(); track p.slot) {
                    <div class="text-center">
                      <div [class]="'mx-auto mb-3 rounded-full flex items-center justify-center font-heading text-white ' + p.avatarClass">{{ p.iniciales }}</div>
                      <p class="font-medium text-sm text-text-light">{{ p.competidor }}</p>
                      <p class="text-xs text-text-muted mb-3">{{ p.pais }}</p>
                      <div [class]="p.podiumClass">
                        <p [class]="p.numberClass">{{ p.slot }}</p>
                        <p class="text-xs font-accent uppercase tracking-wider" [class]="p.slot === '1' ? 'text-white/80' : 'text-text-muted'">{{ p.heatScoreTotal ?? '—' }} pts</p>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }

            @if (puestosCategoriaId && canEdit()) {
              <!-- Editable results (full roster) -->
              <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
                <div class="px-6 py-4 border-b border-navy-mid flex flex-col sm:flex-row justify-between gap-3 sm:items-center">
                  <h3 class="font-heading text-lg text-white">Resultados completos</h3>
                  <button (click)="guardarResultados()" [disabled]="guardandoResultados() || filasResultados().length === 0" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-white font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50 whitespace-nowrap">
                    {{ guardandoResultados() ? 'Guardando...' : 'Guardar resultados' }}
                  </button>
                </div>
                @if (filasResultados().length === 0) {
                  <p class="px-6 py-4 text-sm text-text-muted">No hay competidores inscritos en este evento/categoría.</p>
                } @else {
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
                        @for (f of filasResultados(); track f.competitorId) {
                          <tr class="hover:bg-cyan-brand/5 transition">
                            <td [class]="puestoClass(f.place)">
                              <input type="text" placeholder="—" [(ngModel)]="f.place" [class]="PLACE_INPUT_CLASS">
                            </td>
                            <td class="px-4 py-3 font-medium text-text-light">{{ f.nombre }}</td>
                            <td class="px-4 py-3 text-text-muted">{{ f.pais }}</td>
                            <td class="px-4 py-3 text-right text-text-light">{{ f.ligaPoints ?? '—' }}</td>
                            <td class="px-4 py-3 text-right text-success-brand">{{ f.prizeUsd !== null ? '$' + f.prizeUsd : '—' }}</td>
                            <td class="px-4 py-3 text-right">
                              <input type="number" step="0.01" placeholder="—" [(ngModel)]="f.heatScoreTotal" [class]="HEAT_SCORE_INPUT_CLASS">
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                }
                <div class="px-6 py-3 border-t border-navy-mid text-xs text-text-muted">
                  <a href="https://surfscores.com" target="_blank" rel="noopener" class="text-cyan-brand hover:underline">{{ attribution() }}</a>
                </div>
              </div>
            } @else if (resultados().length === 0) {
              <p class="text-sm text-text-muted">Aún no hay resultados cargados para este evento/categoría.</p>
            } @else {
              <!-- Read-only results table -->
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
  canViewPremiosConfig = computed(() => this.permissions.canView('Configuracion'));
  canEditPremiosConfig = computed(() => this.permissions.canEdit('Configuracion'));

  CLASS_INPUT = CLASS_INPUT;
  PREMIO_CELL_CLASS = PREMIO_CELL_CLASS;
  PLACE_INPUT_CLASS = PLACE_INPUT_CLASS;
  HEAT_SCORE_INPUT_CLASS = HEAT_SCORE_INPUT_CLASS;

  tab = signal<InscritosTab>('inscritos');
  tabClass(t: InscritosTab): string {
    return this.tab() === t
      ? 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-cyan-brand border-b-2 border-cyan-brand whitespace-nowrap'
      : 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-text-muted border-b-2 border-transparent hover:text-text-light transition whitespace-nowrap';
  }

  selectTab(t: InscritosTab): void {
    this.tab.set(t);
    if (t === 'premios' && !this.premiosConfigLoaded && this.canViewPremiosConfig()) {
      this.premiosConfigLoaded = true;
      void this.loadPremiosConfig();
    }
  }

  loading = signal(true);

  eventos = signal<EventoOption[]>([]);
  circuitos = signal<CircuitoOption[]>([]);
  categorias = signal<CategoriaOption[]>([]);

  searchTerm = '';
  filterCircuito = '';
  filterEvento = '';
  filterCategoria = '';
  filterEstado = '';

  private categoriasGlobal: CategoriaOption[] = [];

  eventosFiltrados = computed(() => {
    const circuitoId = this.filterCircuito;
    return circuitoId ? this.eventos().filter(e => e.circuitId === circuitoId) : this.eventos();
  });

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
      const [eventos, categoriesRes, circuitsRes] = await Promise.all([
        this.fetchAllEvents(),
        this.api.get<any>('/categories'),
        this.api.get<any>('/circuits'),
      ]);
      this.eventos.set(eventos);
      this.categoriasGlobal = (categoriesRes?.data ?? []).map((c: any) => ({ id: c.id, nombre: c.nombre }));
      this.categorias.set(this.categoriasGlobal);
      this.circuitos.set((circuitsRes?.data ?? []).map((c: any) => ({ id: c.id, nombre: c.nombre })));
      await this.loadInscritos();
    } catch {
      this.showToast('Error al cargar los inscritos');
    } finally {
      this.loading.set(false);
    }
  }

  // Recorre todas las páginas para que los selectores de evento no omitan
  // eventos cuando hay más de una página de resultados.
  private async fetchAllEvents(): Promise<EventoOption[]> {
    const limit = 100;
    let page = 1;
    const all: EventoOption[] = [];
    for (;;) {
      const res = await this.api.get<any>(`/events?limit=${limit}&page=${page}`);
      const data: any[] = res?.data ?? [];
      all.push(...data.map((e: any) => ({ id: e.id, nombre: e.nombre, circuitId: e.circuitId })));
      const totalItems: number = res?.pagination?.totalItems ?? all.length;
      if (data.length === 0 || all.length >= totalItems) break;
      page += 1;
    }
    return all;
  }

  async onFilterCircuitoChange(): Promise<void> {
    if (this.filterEvento && !this.eventosFiltrados().some(e => e.id === this.filterEvento)) {
      this.filterEvento = '';
      this.filterCategoria = '';
      this.categorias.set(this.categoriasGlobal);
    }
    await this.loadInscritos();
  }

  async onFilterEventoChange(): Promise<void> {
    this.filterCategoria = '';
    await this.loadCategoriasForEvento();
    await this.loadInscritos();
  }

  private async loadCategoriasForEvento(): Promise<void> {
    if (!this.filterEvento) {
      this.categorias.set(this.categoriasGlobal);
      return;
    }
    try {
      const res = await this.api.get<any>(`/events/${this.filterEvento}/categories`);
      this.categorias.set((res?.data ?? []).map((c: any) => ({ id: c.categoryId ?? c.id, nombre: c.categoryName ?? c.nombre })));
    } catch {
      this.categorias.set(this.categoriasGlobal);
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
      evento: r.eventoNombre,
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

  exportando = signal(false);
  async exportarInscritos(): Promise<void> {
    this.exportando.set(true);
    try {
      const params = new URLSearchParams();
      if (this.filterEvento) params.set('eventId', this.filterEvento);
      if (this.filterCategoria) params.set('categoryId', this.filterCategoria);
      if (this.filterEstado) params.set('status', this.filterEstado);
      const query = params.toString();
      await this.api.downloadFile(`/inscriptions/export${query ? `?${query}` : ''}`, 'inscritos.xlsx');
    } catch {
      this.showToast('Error al exportar los inscritos');
    } finally {
      this.exportando.set(false);
    }
  }

  async exportarFicha(row: InscritoRow): Promise<void> {
    try {
      await this.api.downloadFile(`/inscriptions/${row.id}/export`, `ficha-${row.numero}.xlsx`);
    } catch {
      this.showToast('Error al exportar la ficha');
    }
  }

  // ─── Puntajes de Premios ──────────────────────────────────────────
  premiosEventoId = '';
  premiosLoading = signal(false);
  premiosStars = signal(0);
  premiosRows = signal<{ placeLabel: string; prizeUsd: number }[]>([]);

  premiosConfigLoaded = false;
  premiosConfigLoading = signal(false);
  premiosConfigSaving = signal(false);
  premiosConfigRows = signal<PremioConfigRow[]>([]);
  private premiosConfigRaw: any = null;

  async loadPremiosConfig(): Promise<void> {
    this.premiosConfigLoading.set(true);
    try {
      const raw = await this.api.get<any>('/admin/settings');
      this.premiosConfigRaw = raw;
      this.premiosConfigRows.set((raw?.ranking?.prizeDistribution ?? []).map((row: any) => ({
        label: row.placeLabel,
        p1: row.star1Percent, p2: row.star2Percent, p3: row.star3Percent, p4: row.star4Percent,
        p5: row.star5Percent, p6: row.star6Percent, p7: row.star7Percent,
      })));
    } catch {
      this.showToast('Error al cargar la configuración de premios');
    } finally {
      this.premiosConfigLoading.set(false);
    }
  }

  async savePremiosConfig(): Promise<void> {
    if (!this.premiosConfigRaw) return;
    this.premiosConfigSaving.set(true);
    try {
      const payload = {
        ...this.premiosConfigRaw,
        ranking: {
          ...this.premiosConfigRaw.ranking,
          prizeDistribution: this.premiosConfigRows().map(row => ({
            placeLabel: row.label,
            star1Percent: row.p1, star2Percent: row.p2, star3Percent: row.p3, star4Percent: row.p4,
            star5Percent: row.p5, star6Percent: row.p6, star7Percent: row.p7,
          })),
        },
      };
      const res = await this.api.put<any>('/admin/settings', payload);
      this.premiosConfigRaw = res;
      this.showToast('Distribución de premios guardada correctamente');
    } catch {
      this.showToast('Error al guardar la distribución de premios');
    } finally {
      this.premiosConfigSaving.set(false);
    }
  }

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

  // ─── Puestos por evento ───────────────────────────────────────────
  puestosCircuitoId = '';
  puestosEventoId = '';
  puestosCategoriaId = '';
  puestosCategorias = signal<CategoriaOption[]>([]);
  puestosLoading = signal(false);
  resultados = signal<ResultadoRow[]>([]);
  attribution = signal('Results by SurfScores.com');

  rosterCompetidores = signal<{ competitorId: string; nombre: string; pais: string }[]>([]);
  filasResultados = signal<FilaResultado[]>([]);
  guardandoResultados = signal(false);

  descargandoPlantilla = signal(false);
  puestosImportOpen = signal(false);
  puestosImportPath = computed(() => `/events/${this.puestosEventoId}/results/import?categoryId=${this.puestosCategoriaId}`);

  async descargarPlantillaResultados(): Promise<void> {
    if (!this.puestosEventoId || !this.puestosCategoriaId) return;
    this.descargandoPlantilla.set(true);
    try {
      await this.api.downloadFile(
        `/events/${this.puestosEventoId}/results/template?categoryId=${this.puestosCategoriaId}`,
        'resultados-template.xlsx',
      );
    } catch {
      this.showToast('Error al descargar la plantilla de resultados');
    } finally {
      this.descargandoPlantilla.set(false);
    }
  }

  async onResultadosImported(): Promise<void> {
    await this.onPuestosCategoriaChange();
    this.showToast('Resultados importados correctamente');
  }

  puestosEventosFiltrados = computed(() => {
    const circuitoId = this.puestosCircuitoId;
    return circuitoId ? this.eventos().filter(e => e.circuitId === circuitoId) : this.eventos();
  });

  onPuestosCircuitoChange(): void {
    if (this.puestosEventoId && !this.puestosEventosFiltrados().some(e => e.id === this.puestosEventoId)) {
      this.puestosEventoId = '';
      this.puestosCategoriaId = '';
      this.puestosCategorias.set([]);
      this.resultados.set([]);
      this.rosterCompetidores.set([]);
      this.filasResultados.set([]);
    }
  }

  async onPuestosEventoChange(): Promise<void> {
    this.puestosCategoriaId = '';
    this.resultados.set([]);
    this.rosterCompetidores.set([]);
    this.filasResultados.set([]);
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
    await this.onPuestosCategoriaChange();
  }

  async onPuestosCategoriaChange(): Promise<void> {
    if (!this.puestosEventoId) return;
    this.puestosLoading.set(true);
    try {
      await Promise.all([this.loadResultados(), this.loadRoster()]);
    } finally {
      this.puestosLoading.set(false);
    }
    this.buildFilasResultados();
  }

  async loadResultados(): Promise<void> {
    if (!this.puestosEventoId) return;
    try {
      const params = this.puestosCategoriaId ? `?categoryId=${this.puestosCategoriaId}` : '';
      const res = await this.api.get<any>(`/events/${this.puestosEventoId}/results${params}`);
      this.attribution.set(res?.attribution ?? 'Results by SurfScores.com');
      this.resultados.set((res?.data ?? []).map((r: any) => ({
        id: r.id,
        competitorId: r.competitorId,
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
    }
  }

  async loadRoster(): Promise<void> {
    this.rosterCompetidores.set([]);
    if (!this.puestosEventoId || !this.puestosCategoriaId) return;
    try {
      const res = await this.api.get<any>(`/inscriptions?eventId=${this.puestosEventoId}&categoryId=${this.puestosCategoriaId}&limit=200`);
      const data: any[] = res?.data ?? [];
      this.rosterCompetidores.set(data.map((r: any) => ({ competitorId: r.competitorId, nombre: r.fullName, pais: r.country })));
    } catch {
      this.rosterCompetidores.set([]);
    }
  }

  private buildFilasResultados(): void {
    const results = new Map(this.resultados().map(r => [r.competitorId, r]));
    const filas: FilaResultado[] = this.rosterCompetidores().map(c => {
      const r = results.get(c.competitorId);
      return {
        competitorId: c.competitorId,
        nombre: c.nombre,
        pais: c.pais,
        place: r?.place ?? '',
        ligaPoints: r?.ligaPoints ?? null,
        prizeUsd: r?.prizeUsd ?? null,
        heatScoreTotal: r?.heatScoreTotal ?? null,
      };
    });
    filas.sort((a, b) => {
      const pa = parseInt(this.normalizePlace(a.place), 10);
      const pb = parseInt(this.normalizePlace(b.place), 10);
      const va = Number.isNaN(pa) ? Number.MAX_SAFE_INTEGER : pa;
      const vb = Number.isNaN(pb) ? Number.MAX_SAFE_INTEGER : pb;
      if (va !== vb) return va - vb;
      return a.nombre.localeCompare(b.nombre);
    });
    this.filasResultados.set(filas);
  }

  async guardarResultados(): Promise<void> {
    if (!this.puestosEventoId || !this.puestosCategoriaId) return;

    const results = this.filasResultados()
      .filter(f => f.place.trim().length > 0)
      .map(f => ({ competitorId: f.competitorId, place: f.place.trim(), ligaPoints: 0, prizeUsd: null, heatOla1: f.heatScoreTotal, heatOla2: null }));

    if (results.length === 0) {
      this.showToast('Ingresa al menos un puesto antes de guardar');
      return;
    }

    const seenPlaces = new Set<string>();
    for (const r of results) {
      const key = r.place.toLowerCase();
      if (seenPlaces.has(key)) {
        this.showToast(`El puesto "${r.place}" está repetido, corrígelo antes de guardar`);
        return;
      }
      seenPlaces.add(key);
    }

    this.guardandoResultados.set(true);
    try {
      await this.api.post<any>(`/events/${this.puestosEventoId}/results`, {
        categoryId: this.puestosCategoriaId,
        results,
      });
      this.showToast('Resultados guardados correctamente');
      await Promise.all([this.loadResultados(), this.loadRoster()]);
      this.buildFilasResultados();
    } catch (err: any) {
      this.showToast(err?.message ?? 'Error al guardar los resultados');
    } finally {
      this.guardandoResultados.set(false);
    }
  }

  private normalizePlace(place: string): string {
    return (place ?? '').replace(/[^0-9]/g, '');
  }

  podio = computed(() => {
    const byNormalizedPlace = new Map(this.resultados().map(r => [this.normalizePlace(r.place), r]));
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
      .filter(slot => byNormalizedPlace.has(slot))
      .map(slot => {
        const r = byNormalizedPlace.get(slot)!;
        return {
          slot,
          competidor: r.competidor,
          pais: r.pais,
          heatScoreTotal: r.heatScoreTotal,
          iniciales: r.competidor.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase() ?? '').join(''),
          ...configs[slot],
        };
      });
  });

  puestoClass(puesto: string): string {
    const normalized = this.normalizePlace(puesto);
    if (normalized === '1') return 'px-4 py-3 font-heading text-warning-brand text-lg';
    if (normalized === '2') return 'px-4 py-3 font-heading text-text-light text-lg';
    if (normalized === '3') return 'px-4 py-3 font-heading text-orange-brand text-lg';
    return 'px-4 py-3 font-heading text-text-muted';
  }

  toast = signal<{ show: boolean; message: string }>({ show: false, message: '' });
  showToast(message: string): void {
    this.toast.set({ show: true, message });
    setTimeout(() => this.toast.set({ show: false, message: '' }), 3000);
  }
}
