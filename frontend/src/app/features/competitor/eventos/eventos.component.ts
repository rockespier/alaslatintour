import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { LiveStatusService } from '../../../core/services/live-status.service';
import { StarRatingComponent } from '../../../shared/components/star-rating/star-rating.component';
import { sortEventsForDisplay } from '../../../core/utils/event-sort.util';
import { flagForCountryCode, flagForCountryName } from '../../../core/utils/country-flag.util';

interface Circuit {
  id: string;
  nombre: string;
}

interface EventCategory {
  id: string;
  nombre: string;
  inscritos: number;
  capacidad: number;
  tarifa: number;
}

interface EventItem {
  id: string;
  nombre: string;
  ciudad: string;
  pais: string;
  stars: number;
  statusPublic: string;
  fechaInicio: string;
  fechaFin: string;
  circuitId?: string;
  capacidadMaxima?: number;
  enrolledCount?: number;
  prizeAmountUsd?: number;
  categorias?: EventCategory[];
  inscripcionCierre?: string;
  isInvitational?: boolean;
  waveSize?: string;
  ganador?: string;
  imagenUrl?: string;
  auspiciador?: string;
}

interface ConfirmedInscriptionRow {
  fullName: string;
  country: string;
  categoryName: string;
}

interface MyInscription {
  id: string;
  eventoNombre: string;
  eventoPais: string;
  categoria: string;
  fechaInicio: string;
  fechaFin: string;
  statusPago: 'confirmado' | 'pendiente' | 'rechazado';
}

interface CompetitorStats {
  rankingActual?: number;
  puntosActual?: number;
  rankingAnterior?: number;
}

const STATUS_CLASS: Record<string, string> = {
  'Inscripciones Abiertas': 'bg-success-brand/15 text-success-brand border-success-brand/30',
  'Próximamente': 'bg-cyan-brand/15 text-cyan-brand border-cyan-brand/30',
  'Completado': 'bg-navy-mid text-text-muted border-navy-mid',
  'Cerrado': 'bg-navy-mid text-text-muted border-navy-mid',
};

@Component({
  selector: 'app-eventos',
  standalone: true,
  imports: [RouterLink, StarRatingComponent],
  template: `
    @if (auth.isCompetitor()) {
      <section class="pt-10 pb-6 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto">
          <div class="bg-gradient-to-r from-navy-dark via-navy-dark to-navy-mid rounded-2xl border border-navy-mid p-6 md:p-8 flex flex-col md:flex-row md:items-center md:justify-between gap-6">
            <div class="flex items-center gap-5">
              <div class="w-14 h-14 rounded-full bg-gradient-to-br from-cyan-brand to-orange-brand flex items-center justify-center font-heading text-2xl font-bold text-navy-deepest">
                {{ userInitial() }}
              </div>
              <div>
                <p class="font-accent uppercase tracking-[0.25em] text-cyan-brand text-xs mb-1">Temporada ALAS Global Tour 2026</p>
                <h1 class="font-heading text-2xl md:text-3xl">Hola, {{ firstName() }}</h1>
                <p class="text-sm text-text-muted mt-1">Listo para tu próxima parada en el circuito.</p>
              </div>
            </div>
            @if (competitorStats()) {
              <div class="flex gap-8 text-center">
                <div>
                  <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">Ranking {{ currentYear }}</p>
                  <p class="font-heading text-3xl text-cyan-brand">
                    {{ competitorStats()!.rankingActual ? '#' + competitorStats()!.rankingActual : '—' }}
                  </p>
                </div>
                <div>
                  <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">Puntos</p>
                  <p class="font-heading text-3xl">{{ competitorStats()!.puntosActual ?? '—' }}</p>
                </div>
              </div>
            }
          </div>
        </div>
      </section>
    } @else {
      <section class="py-14 px-4 sm:px-6 lg:px-8 border-b border-navy-mid">
        <div class="max-w-7xl mx-auto">
          <h1 class="font-heading text-4xl md:text-6xl">Eventos y Calendario 2026</h1>
          <p class="mt-3 text-text-muted max-w-2xl">Todas las paradas del circuito continental de surf profesional.</p>
        </div>
      </section>
    }

    <section class="px-4 sm:px-6 lg:px-8 pb-10">
      <div class="max-w-7xl mx-auto">
        <div class="flex items-end justify-between mb-6 flex-wrap gap-4 mt-8">
          <h2 class="font-heading text-3xl md:text-4xl">Eventos y categorías</h2>
        </div>

        <div class="flex flex-wrap gap-1 border-b border-navy-mid mb-8">
          <button (click)="selectCircuit('all')"
                  class="px-5 py-3 rounded-t-md border-b-2 font-accent uppercase text-sm tracking-wider transition"
                  [class]="circuitFilter() === 'all' ? 'border-cyan-brand text-cyan-brand bg-cyan-brand/8' : 'border-transparent text-text-muted hover:text-text-light'">
            Todos los Circuitos
          </button>
          @for (circuit of circuits(); track circuit.id) {
            <button (click)="selectCircuit(circuit.id)"
                    class="px-5 py-3 rounded-t-md border-b-2 font-accent uppercase text-sm tracking-wider transition"
                    [class]="circuitFilter() === circuit.id ? 'border-cyan-brand text-cyan-brand bg-cyan-brand/8' : 'border-transparent text-text-muted hover:text-text-light'">
              {{ circuit.nombre }}
            </button>
          }
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div class="lg:col-span-2 space-y-5">
            @if (loading()) {
              @for (sk of skeletons; track sk) {
                <div class="card-event rounded-xl p-6">
                  <div class="flex flex-col md:flex-row gap-5">
                    <div class="md:w-32">
                      <div class="skeleton h-12 rounded w-16 mb-2"></div>
                      <div class="skeleton h-3 rounded w-20"></div>
                    </div>
                    <div class="flex-1 space-y-3">
                      <div class="skeleton h-3 rounded w-32"></div>
                      <div class="skeleton h-8 rounded w-3/4"></div>
                      <div class="skeleton h-4 rounded w-full"></div>
                      <div class="skeleton h-4 rounded w-2/3"></div>
                    </div>
                  </div>
                </div>
              }
            } @else {
              @for (event of filteredEvents(); track event.id) {
                <article class="card-event rounded-xl p-6"
                         [class.opacity-70]="event.statusPublic === 'Completado'">
                  <div class="flex flex-col md:flex-row gap-5">
                    <div class="md:w-32 flex md:flex-col items-center md:items-start gap-3 md:gap-1 md:border-r md:border-navy-mid md:pr-5 flex-shrink-0">
                      <div class="font-heading text-5xl leading-none"
                           [class]="event.statusPublic === 'Completado' ? 'text-text-muted' : 'text-cyan-brand'">
                        {{ dayOf(event.fechaInicio) }}
                      </div>
                      <div>
                        <p class="font-accent uppercase text-xs text-text-muted tracking-wider">{{ monthYearOf(event.fechaInicio) }}</p>
                        <p class="font-accent uppercase text-xs text-text-muted">{{ dateRangeShort(event.fechaInicio, event.fechaFin) }}</p>
                      </div>
                      @if (event.imagenUrl) {
                        <div class="w-24 aspect-[16/10] rounded-md border border-navy-mid mt-1">
                          <img [src]="event.imagenUrl" [alt]="'Afiche ' + event.nombre" loading="lazy" referrerpolicy="no-referrer"
                               class="w-full h-full object-cover">
                        </div>
                      }
                    </div>

                    <div class="flex-1 min-w-0">
                      <div class="flex flex-wrap items-start justify-between gap-3 mb-3">
                        <div>
                          <div class="flex items-center gap-2 text-sm text-text-muted mb-1">
                            <span class="text-xl">{{ flagOf(event.pais) }}</span>
                            <span>{{ event.ciudad }}, {{ event.pais }}</span>
                            @if (event.isInvitational) {
                              <span class="ml-1 px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30">Solo invitación</span>
                            }
                            @if (event.stars === 5) {
                              <span class="ml-1 px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-orange-brand/15 text-orange-brand border border-orange-brand/30">Evento estrella</span>
                            }
                          </div>
                          <h3 class="font-heading text-2xl md:text-3xl leading-tight"
                              [class]="event.statusPublic === 'Completado' ? 'text-text-muted' : ''">
                            {{ event.nombre }}
                          </h3>
                        </div>
                        <div class="flex flex-col items-end gap-2">
                          <span class="px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider border whitespace-nowrap"
                                [class]="statusClass(event.statusPublic)">
                            {{ event.statusPublic }}
                          </span>
                          @if (isFull(event)) {
                            <span class="px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-error-brand/15 text-error-brand border border-error-brand/30">
                              Cupo lleno
                            </span>
                          }
                        </div>
                      </div>

                      <div class="flex flex-wrap items-center gap-x-6 gap-y-2 mb-4">
                        <app-star-rating [value]="event.stars" />
                        @if (event.waveSize) {
                          <span class="font-accent uppercase text-xs text-text-muted">{{ event.waveSize }}</span>
                        }
                      </div>

                      @if (event.statusPublic === 'Completado') {
                        <div class="grid grid-cols-2 md:grid-cols-3 gap-4 mb-5">
                          @if (event.ganador) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Ganador</p>
                              <p class="font-heading text-lg">{{ event.ganador }}</p>
                            </div>
                          }
                          @if (event.enrolledCount) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Participantes</p>
                              <p class="font-heading text-lg">{{ event.enrolledCount }}</p>
                            </div>
                          }
                          @if (event.auspiciador) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Auspiciador</p>
                              <p class="font-heading text-lg">{{ event.auspiciador }}</p>
                            </div>
                          }
                        </div>
                      } @else {
                        <div class="grid grid-cols-2 md:grid-cols-3 gap-4 mb-5">
                          @if (event.prizeAmountUsd) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Premio</p>
                              <p class="font-heading text-lg text-cyan-brand">{{ formatUSD(event.prizeAmountUsd) }} USD</p>
                            </div>
                          }
                          @if (event.enrolledCount !== undefined && event.capacidadMaxima) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Inscritos</p>
                              <p class="font-heading text-lg">{{ event.enrolledCount }}<span class="text-text-muted text-sm">/{{ event.capacidadMaxima }}</span></p>
                              <div class="w-full h-1.5 bg-navy-mid rounded-full mt-1 overflow-hidden">
                                <div class="h-full rounded-full transition-all"
                                     [class]="capacityColor(event.enrolledCount, event.capacidadMaxima)"
                                     [style.width.%]="capacityPct(event.enrolledCount, event.capacidadMaxima)"></div>
                              </div>
                            </div>
                          }
                          @if (circuitNombre(event); as nombreCircuito) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Circuito</p>
                              <p class="font-heading text-lg">{{ nombreCircuito }}</p>
                            </div>
                          }
                          @if (event.auspiciador) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Auspiciador</p>
                              <p class="font-heading text-lg">{{ event.auspiciador }}</p>
                            </div>
                          }
                        </div>
                      }

                      <div class="flex flex-wrap items-center gap-3">
                        @if (event.statusPublic === 'Inscripciones Abiertas') {
                          @if (isFull(event)) {
                            <button disabled
                                    class="px-5 py-2.5 rounded-md bg-navy-mid/60 text-text-muted font-accent uppercase tracking-wider text-sm cursor-not-allowed">
                              Cupo lleno
                            </button>
                          } @else {
                            <a [routerLink]="['/inscripcion', event.id]"
                               class="px-5 py-2.5 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition shadow-lg shadow-orange-brand/20">
                              Inscribirse
                            </a>
                          }
                        } @else if (event.statusPublic === 'Completado') {
                          <a routerLink="/ranking" class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition">
                            Ver resultados
                          </a>
                        } @else {
                          <button disabled class="px-5 py-2.5 rounded-md bg-navy-mid/60 text-text-muted font-accent uppercase tracking-wider text-sm cursor-not-allowed">
                            Inscripciones cerradas
                          </button>
                        }

                        @if (event.categorias?.length) {
                          <button (click)="toggleExpand(event)"
                                  class="px-5 py-2.5 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition flex items-center gap-2">
                            <span>{{ isExpanded(event.id) ? 'Ocultar detalles' : 'Ver detalles' }}</span>
                            <svg class="h-4 w-4 transition-transform" [class.rotate-180]="isExpanded(event.id)"
                                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                            </svg>
                          </button>
                        }

                        <button (click)="toggleInscritos(event)"
                                class="px-5 py-2.5 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition flex items-center gap-2">
                          <span>{{ isInscritosExpanded(event.id) ? 'Ocultar inscritos' : 'Ver inscritos confirmados' }}</span>
                          <svg class="h-4 w-4 transition-transform" [class.rotate-180]="isInscritosExpanded(event.id)"
                               fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                          </svg>
                        </button>

                        @if (isLiveScheduleAvailable(event.id)) {
                          <a [href]="liveSchedulePdfUrl()" target="_blank" rel="noopener"
                             class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition inline-flex items-center gap-2">
                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                            </svg>
                            Descargar programación (PDF)
                          </a>
                        }
                      </div>

                      @if (isExpanded(event.id) && event.categorias?.length) {
                        <div class="mt-6 pt-6 border-t border-navy-mid">
                          <h4 class="font-accent uppercase tracking-wider text-cyan-brand text-sm mb-3">Categorías y tarifas</h4>
                          <div class="overflow-x-auto">
                            <table class="w-full text-sm">
                              <thead class="text-text-muted font-accent uppercase tracking-wider text-xs">
                                <tr class="border-b border-navy-mid">
                                  <th class="text-left py-2 pr-4">Categoría</th>
                                  <th class="text-left py-2 px-2">Cupos</th>
                                  <th class="text-right py-2 pl-2">Tarifa</th>
                                  @if (event.statusPublic === 'Completado') {
                                    <th class="text-right py-2 pl-2">Ganador</th>
                                  }
                                </tr>
                              </thead>
                              <tbody class="divide-y divide-navy-mid/60">
                                @for (cat of event.categorias!; track cat.id) {
                                  <tr [class.opacity-50]="cat.inscritos >= cat.capacidad">
                                    <td class="py-2.5 pr-4">
                                      {{ cat.nombre }}
                                      @if (cat.inscritos >= cat.capacidad) {
                                        <span class="ml-2 text-[10px] font-accent uppercase text-error-brand">Llena</span>
                                      }
                                    </td>
                                    <td class="px-2 text-text-muted">{{ cat.inscritos }} / {{ cat.capacidad }}</td>
                                    <td class="py-2.5 pl-2 text-right text-cyan-brand">{{ formatUSD(cat.tarifa) }}</td>
                                    @if (event.statusPublic === 'Completado') {
                                      <td class="py-2.5 pl-2 text-right">
                                        {{ winners().get(cat.id) || (loadingWinners().has(event.id) ? 'Cargando…' : '—') }}
                                      </td>
                                    }
                                  </tr>
                                }
                              </tbody>
                            </table>
                          </div>
                          @if (event.inscripcionCierre) {
                            <p class="text-xs text-text-muted mt-3">Cierra inscripciones el {{ formatDate(event.inscripcionCierre) }}.</p>
                          }
                        </div>
                      }

                      @if (isInscritosExpanded(event.id)) {
                        <div class="mt-6 pt-6 border-t border-navy-mid">
                          <h4 class="font-accent uppercase tracking-wider text-cyan-brand text-sm mb-3">Inscritos confirmados</h4>
                          @if (loadingInscritos().has(event.id)) {
                            <p class="text-sm text-text-muted">Cargando…</p>
                          } @else if ((confirmedInscriptions().get(event.id) ?? []).length === 0) {
                            <p class="text-sm text-text-muted">Aún no hay inscritos confirmados para este evento.</p>
                          } @else {
                            <div class="overflow-x-auto">
                              <table class="w-full text-sm">
                                <thead class="text-text-muted font-accent uppercase tracking-wider text-xs">
                                  <tr class="border-b border-navy-mid">
                                    <th class="text-left py-2 pr-4">Competidor</th>
                                    <th class="text-left py-2 px-2">País</th>
                                    <th class="text-left py-2 pl-2">Categoría</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-navy-mid/60">
                                  @for (row of confirmedInscriptions().get(event.id); track row.fullName + row.categoryName) {
                                    <tr>
                                      <td class="py-2.5 pr-4 font-medium">{{ row.fullName }}</td>
                                      <td class="px-2 text-text-muted">{{ flagForCountryName(row.country) }} {{ row.country }}</td>
                                      <td class="py-2.5 pl-2 text-text-muted">{{ row.categoryName }}</td>
                                    </tr>
                                  }
                                </tbody>
                              </table>
                            </div>
                          }
                        </div>
                      }
                    </div>
                  </div>
                </article>
              }
              @if (filteredEvents().length === 0 && !loading()) {
                <p class="text-text-muted text-center py-12">No hay eventos para este circuito.</p>
              }
            }
          </div>

          <aside class="lg:col-span-1">
            <div class="sticky top-24 space-y-5">
              @if (auth.isAuthenticated()) {
                <div class="bg-navy-dark border border-navy-mid rounded-xl p-6">
                  <div class="flex items-center justify-between mb-5">
                    <h3 class="font-heading text-xl">Mis Inscripciones</h3>
                    @if (myInscriptions().length > 0) {
                      <span class="px-2 py-0.5 rounded-full text-xs font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30">
                        {{ myInscriptions().length }} activas
                      </span>
                    }
                  </div>

                  @if (loadingInscriptions()) {
                    <div class="space-y-3">
                      <div class="skeleton h-20 rounded-lg"></div>
                      <div class="skeleton h-20 rounded-lg"></div>
                    </div>
                  } @else if (myInscriptions().length === 0) {
                    <p class="text-sm text-text-muted">Aún no tienes inscripciones activas.</p>
                  } @else {
                    <div class="space-y-4">
                      @for (ins of myInscriptions(); track ins.id) {
                        <div class="border rounded-lg p-4 hover:border-cyan-brand transition"
                             [class]="ins.statusPago === 'pendiente' ? 'border-warning-brand/40 bg-warning-brand/5' : 'border-navy-mid'">
                          <div class="flex items-center justify-between mb-2">
                            <span class="text-lg">{{ ins.eventoPais }}</span>
                            <span class="px-2 py-0.5 rounded-full text-xs font-accent uppercase tracking-wider border"
                                  [class]="ins.statusPago === 'confirmado' ? 'bg-success-brand/15 text-success-brand border-success-brand/30' : 'bg-warning-brand/15 text-warning-brand border-warning-brand/30'">
                              {{ ins.statusPago === 'confirmado' ? 'Confirmado' : 'Pago Pendiente' }}
                            </span>
                          </div>
                          <h4 class="font-heading text-base leading-tight mb-1">{{ ins.eventoNombre }}</h4>
                          <p class="text-xs text-text-muted mb-3">{{ ins.categoria }} · {{ dateRangeShort(ins.fechaInicio, ins.fechaFin) }}</p>
                          @if (ins.statusPago === 'pendiente') {
                            <a [routerLink]="['/pago-playa', ins.id]"
                               class="w-full block text-center px-2 py-1.5 rounded bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-xs transition">
                              Ver token
                            </a>
                          }
                        </div>
                      }
                    </div>
                  }

                  <a routerLink="/mi-panel/inscripciones"
                     class="block text-center mt-5 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                    Ver historial completo →
                  </a>
                </div>
              }

              <div class="bg-gradient-to-br from-cyan-brand/10 to-orange-brand/5 border border-cyan-brand/20 rounded-xl p-5">
                <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs mb-2">¿Necesitas ayuda?</p>
                <h4 class="font-heading text-lg mb-2 leading-tight">Reglamento y soporte</h4>
                <p class="text-sm text-text-muted leading-relaxed mb-4">
                  Consulta el reglamento oficial o contacta a soporte para dudas sobre inscripciones y pagos.
                </p>
                <a href="/reglamento.pdf" target="_blank"
                   class="inline-flex items-center gap-1 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                  Ver reglamento
                </a>
                &nbsp;&nbsp;
                <a href="mailto:soporte@alasglobaltour.com"
                   class="inline-flex items-center gap-1 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider mt-2">
                  Contactar soporte →
                </a>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </section>
  `,
})
export class EventosComponent implements OnInit {
  flagForCountryName = flagForCountryName;

  auth = inject(AuthService);
  private api = inject(ApiService);
  private title = inject(Title);
  private meta = inject(Meta);
  private liveStatus = inject(LiveStatusService);

  readonly currentYear = new Date().getFullYear();

  loading = signal(true);
  loadingInscriptions = signal(false);
  events = signal<EventItem[]>([]);
  circuits = signal<Circuit[]>([]);
  myInscriptions = signal<MyInscription[]>([]);
  competitorStats = signal<CompetitorStats | null>(null);
  circuitFilter = signal<string>('all');
  expanded = signal<Set<string>>(new Set());
  readonly skeletons = [1, 2, 3];

  winners = signal<Map<string, string>>(new Map());
  loadingWinners = signal<Set<string>>(new Set());

  expandedInscritos = signal<Set<string>>(new Set());
  confirmedInscriptions = signal<Map<string, ConfirmedInscriptionRow[]>>(new Map());
  loadingInscritos = signal<Set<string>>(new Set());

  filteredEvents = computed(() => {
    const filter = this.circuitFilter();
    const evts = this.events();
    const list = filter === 'all'
      ? evts
      : evts.filter(e => e.circuitId === filter);
    return sortEventsForDisplay(list);
  });

  userInitial = computed(() => (this.auth.currentUser()?.fullName ?? '?')[0].toUpperCase());
  firstName = computed(() => (this.auth.currentUser()?.fullName ?? '').split(' ')[0]);

  ngOnInit(): void {
    this.title.setTitle('Eventos y Calendario 2026 — ALAS Latin Tour');
    this.meta.updateTag({ name: 'description', content: 'Calendario completo del ALAS Latin Tour 2026. Inscríbete en eventos, revisa categorías y consulta tus inscripciones.' });
    this.loadEvents().then(() => {
      if (this.auth.isAuthenticated()) this.loadMyInscriptions();
    });
    this.loadCircuits();
    if (this.auth.isCompetitor()) this.loadCompetitorStats();
    this.liveStatus.ensureLoaded();
  }

  selectCircuit(circuitId: string): void {
    if (circuitId === this.circuitFilter()) return;
    this.circuitFilter.set(circuitId);
    this.expanded.set(new Set());
  }

  private async loadEvents(): Promise<void> {
    this.loading.set(true);
    try {
      const res = await this.api.get<any>('/events?limit=100&page=1&includeCategories=true');
      this.events.set(res?.data ?? []);
    } catch {
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadCircuits(): Promise<void> {
    try {
      const res = await this.api.get<any>(`/circuits?status=Activo&year=${this.currentYear}&limit=20`);
      const currentCircuits: Circuit[] = res?.data ?? [];
      this.circuits.set(currentCircuits);
    } catch {
      this.circuits.set([]);
    }
  }

  private async loadMyInscriptions(): Promise<void> {
    const competitorId = this.auth.currentUser()?.competitorId;
    if (!competitorId) return;
    this.loadingInscriptions.set(true);
    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}/inscriptions?limit=20`);
      const raw: any[] = res?.data ?? [];
      const eventsById = new Map(this.events().map(e => [e.id, e]));
      this.myInscriptions.set(
        raw
          .filter(i => (i?.estadoCompetidor ?? '').toString().toLowerCase() !== 'completado')
          .slice(0, 5)
          .map(i => {
            const ev = eventsById.get(i?.event?.id);
            const estadoAdmin = (i?.estadoAdmin ?? '').toString().toLowerCase();
            return {
              id: i.id,
              eventoNombre: i?.event?.nombre ?? '',
              eventoPais: ev ? this.flagOf(ev.pais) : '🏄',
              categoria: i?.category?.nombre ?? '',
              fechaInicio: ev?.fechaInicio ?? '',
              fechaFin: ev?.fechaFin ?? '',
              statusPago: estadoAdmin === 'pagado' ? 'confirmado' as const : estadoAdmin === 'pendiente' ? 'pendiente' as const : 'rechazado' as const,
            };
          }),
      );
    } catch {
      this.myInscriptions.set([]);
    } finally {
      this.loadingInscriptions.set(false);
    }
  }

  private async loadCompetitorStats(): Promise<void> {
    const competitorId = this.auth.currentUser()?.competitorId;
    if (!competitorId) return;
    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}`);
      const data = res?.data ?? res;
      this.competitorStats.set({
        rankingActual: data?.rankingActual ?? data?.ranking,
        puntosActual: data?.puntosActual ?? data?.puntos,
        rankingAnterior: data?.rankingAnterior,
      });
    } catch {
      // Stats are optional — not critical
    }
  }

  isFull(event: EventItem): boolean {
    return event.enrolledCount !== undefined
      && event.capacidadMaxima !== undefined
      && event.enrolledCount >= event.capacidadMaxima;
  }

  circuitNombre(event: EventItem): string | undefined {
    return this.circuits().find(c => c.id === event.circuitId)?.nombre;
  }

  toggleExpand(event: EventItem): void {
    this.expanded.update(set => {
      const next = new Set(set);
      if (next.has(event.id)) {
        next.delete(event.id);
      } else {
        next.add(event.id);
        if (event.statusPublic === 'Completado') this.loadWinners(event);
      }
      return next;
    });
  }

  isExpanded(id: string): boolean { return this.expanded().has(id); }

  private async loadWinners(event: EventItem): Promise<void> {
    const categorias = event.categorias;
    if (!categorias?.length) return;
    const already = this.winners();
    const pending = categorias.filter(c => !already.has(c.id));
    if (pending.length === 0) return;

    this.loadingWinners.update(set => new Set(set).add(event.id));
    try {
      const entries = await Promise.all(pending.map(async (cat): Promise<[string, string]> => {
        try {
          const res = await this.api.get<any>(`/events/${event.id}/results?categoryId=${cat.id}`);
          const winner = (res?.data ?? []).find((r: any) => r.place === '1');
          return [cat.id, winner?.competitorName ?? ''];
        } catch {
          return [cat.id, ''];
        }
      }));
      this.winners.update(map => {
        const next = new Map(map);
        for (const [catId, name] of entries) next.set(catId, name);
        return next;
      });
    } finally {
      this.loadingWinners.update(set => {
        const next = new Set(set);
        next.delete(event.id);
        return next;
      });
    }
  }

  toggleInscritos(event: EventItem): void {
    this.expandedInscritos.update(set => {
      const next = new Set(set);
      if (next.has(event.id)) {
        next.delete(event.id);
      } else {
        next.add(event.id);
        this.loadConfirmedInscriptions(event);
      }
      return next;
    });
  }

  isInscritosExpanded(id: string): boolean { return this.expandedInscritos().has(id); }

  private async loadConfirmedInscriptions(event: EventItem): Promise<void> {
    if (this.confirmedInscriptions().has(event.id)) return;
    this.loadingInscritos.update(set => new Set(set).add(event.id));
    try {
      const res = await this.api.get<any>(`/events/${event.id}/inscriptions/confirmed`);
      const rows: ConfirmedInscriptionRow[] = Array.isArray(res) ? res : (res?.data ?? []);
      this.confirmedInscriptions.update(map => {
        const next = new Map(map);
        next.set(event.id, rows);
        return next;
      });
    } catch {
      this.confirmedInscriptions.update(map => {
        const next = new Map(map);
        next.set(event.id, []);
        return next;
      });
    } finally {
      this.loadingInscritos.update(set => {
        const next = new Set(set);
        next.delete(event.id);
        return next;
      });
    }
  }

  isLiveScheduleAvailable(eventId: string): boolean {
    const s = this.liveStatus.status();
    return !!(s?.isLive && s.event?.id === eventId && s.schedulePdfUrl);
  }

  liveSchedulePdfUrl(): string | null {
    return this.liveStatus.status()?.schedulePdfUrl ?? null;
  }

  flagOf(code: string): string { return flagForCountryCode(code); }
  statusClass(status: string): string { return STATUS_CLASS[status] ?? 'bg-orange-brand/15 text-orange-brand border-orange-brand/30'; }

  capacityColor(used: number, total: number): string {
    const pct = used / total;
    if (pct >= 0.9) return 'bg-error-brand';
    if (pct >= 0.7) return 'bg-orange-brand';
    return 'bg-cyan-brand';
  }

  capacityPct(used: number, total: number): number { return Math.min(100, Math.round((used / total) * 100)); }
  formatUSD(n: number): string { return '$' + n.toLocaleString('en-US'); }
  // fechaInicio/fechaFin/inscripcionCierre son fechas "solo fecha" (medianoche UTC en el backend).
  // Se usan los getters UTC para que el día mostrado no dependa del huso horario del navegador
  // (con getters locales, un usuario en Sudamérica ve el día anterior).
  dayOf(d: string): string { return d ? String(new Date(d).getUTCDate()).padStart(2, '0') : ''; }

  monthYearOf(d: string): string {
    if (!d) return '';
    const date = new Date(d);
    const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
    return `${months[date.getUTCMonth()]} ${date.getUTCFullYear()}`;
  }

  dateRangeShort(start: string, end: string): string {
    if (!start || !end) return '';
    const s = new Date(start), e = new Date(end);
    const months = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
    return `${s.getUTCDate()} - ${e.getUTCDate()} ${months[s.getUTCMonth()]}`;
  }

  formatDate(d: string): string {
    if (!d) return '';
    const date = new Date(d);
    const months = ['enero', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    return `${date.getUTCDate()} de ${months[date.getUTCMonth()]}`;
  }
}
