import { Component, inject, signal, computed, OnInit, afterNextRender, Injector } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { SeoService } from '../../../core/i18n/seo.service';
import { LocaleFormatService } from '../../../core/i18n/locale-format.service';
import { LocalizePathPipe } from '../../../shared/pipes/localize-path.pipe';
import { EnumLabelPipe } from '../../../shared/pipes/enum-label.pipe';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { LiveStatusService } from '../../../core/services/live-status.service';
import { StarRatingComponent } from '../../../shared/components/star-rating/star-rating.component';
import { sortEventsForDisplay } from '../../../core/utils/event-sort.util';
import { flagForCountryCode, flagForCountryName } from '../../../core/utils/country-flag.util';
import { pickCurrentCircuit } from '../../../core/utils/current-circuit.util';

interface Circuit {
  id: string;
  nombre: string;
  estado?: string;
  temporada?: number;
  lastSyncAt?: string | null;
  updatedAt?: string | null;
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
  surfScoresCode?: string;
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
  imports: [RouterLink, StarRatingComponent, TranslocoModule, LocalizePathPipe, EnumLabelPipe],
  providers: [provideTranslocoScope('competitor')],
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
                <p class="font-accent uppercase tracking-[0.25em] text-cyan-brand text-xs mb-1">{{ 'competitor.events.seasonKicker' | transloco }}</p>
                <h1 class="font-heading text-2xl md:text-3xl">{{ 'competitor.events.hello' | transloco: { name: firstName() } }}</h1>
                <p class="text-sm text-text-muted mt-1">{{ 'competitor.events.ready' | transloco }}</p>
              </div>
            </div>
            @if (competitorStats()) {
              <div class="flex gap-8 text-center">
                <div>
                  <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">{{ 'competitor.events.ranking' | transloco: { year: currentYear } }}</p>
                  <p class="font-heading text-3xl text-cyan-brand">
                    {{ competitorStats()!.rankingActual ? '#' + competitorStats()!.rankingActual : '—' }}
                  </p>
                </div>
                <div>
                  <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">{{ 'competitor.events.points' | transloco }}</p>
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
          <h1 class="font-heading text-4xl md:text-6xl">{{ 'competitor.events.title' | transloco }}</h1>
          <p class="mt-3 text-text-muted max-w-2xl">{{ 'competitor.events.intro' | transloco }}</p>
        </div>
      </section>
    }

    <section class="px-4 sm:px-6 lg:px-8 pb-10">
      <div class="max-w-7xl mx-auto">
        <div class="flex items-end justify-between mb-6 flex-wrap gap-4 mt-8">
          <h2 class="font-heading text-3xl md:text-4xl">{{ 'competitor.events.listTitle' | transloco }}</h2>
        </div>

        <div class="flex flex-wrap items-end gap-x-6 gap-y-4 pb-6 mb-8 border-b border-navy-mid">
          <div>
            <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">{{ 'competitor.events.circuit' | transloco }}</label>
            <select [value]="circuitFilter()" (change)="selectCircuit($any($event.target).value)"
                    class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light min-w-[220px] focus:outline-none focus:border-cyan-brand transition">
              <option value="all">{{ 'competitor.events.allCircuits' | transloco }}</option>
              @for (circuit of circuits(); track circuit.id) {
                <option [value]="circuit.id">{{ circuit.nombre }}</option>
              }
            </select>
          </div>
          <label class="flex items-center gap-2 cursor-pointer select-none pb-2.5">
            <input type="checkbox" [checked]="proximosOnly()" (change)="proximosOnly.set($any($event.target).checked)"
                   class="h-4 w-4 rounded border-navy-mid bg-navy-mid/40 text-cyan-brand focus:ring-cyan-brand focus:ring-offset-0">
            <span class="text-sm text-text-light">{{ 'competitor.events.upcomingOnly' | transloco }}</span>
          </label>
          <label class="flex items-center gap-2 cursor-pointer select-none pb-2.5">
            <input type="checkbox" [checked]="soloAbiertas()" (change)="soloAbiertas.set($any($event.target).checked)"
                   class="h-4 w-4 rounded border-navy-mid bg-navy-mid/40 text-cyan-brand focus:ring-cyan-brand focus:ring-offset-0">
            <span class="text-sm text-text-light">{{ 'competitor.events.openOnly' | transloco }}</span>
          </label>
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
                <article class="card-event rounded-xl p-6 scroll-mt-24"
                         [id]="'evento-' + event.id"
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
                          <img [src]="event.imagenUrl" [alt]="'competitor.events.poster' | transloco: { name: event.nombre }" loading="lazy" referrerpolicy="no-referrer"
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
                              <span class="ml-1 px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30">{{ 'competitor.events.invitational' | transloco }}</span>
                            }
                            @if (event.stars === 5) {
                              <span class="ml-1 px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-orange-brand/15 text-orange-brand border border-orange-brand/30">{{ 'competitor.events.starEvent' | transloco }}</span>
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
                            {{ event.statusPublic | enumLabel: 'eventStatusPublic' }}
                          </span>
                          @if (isFull(event)) {
                            <span class="px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-error-brand/15 text-error-brand border border-error-brand/30">
                              {{ 'competitor.events.full' | transloco }}
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
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.winner' | transloco }}</p>
                              <p class="font-heading text-lg">{{ event.ganador }}</p>
                            </div>
                          }
                          @if (event.enrolledCount) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.participants' | transloco }}</p>
                              <p class="font-heading text-lg">{{ event.enrolledCount }}</p>
                            </div>
                          }
                          @if (event.auspiciador) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.sponsor' | transloco }}</p>
                              <p class="font-heading text-lg">{{ event.auspiciador }}</p>
                            </div>
                          }
                        </div>
                      } @else {
                        <div class="grid grid-cols-2 md:grid-cols-3 gap-4 mb-5">
                          @if (event.prizeAmountUsd) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.prize' | transloco }}</p>
                              <p class="font-heading text-lg text-cyan-brand">{{ formatUSD(event.prizeAmountUsd) }} USD</p>
                            </div>
                          }
                          @if (event.enrolledCount !== undefined && event.capacidadMaxima) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.enrolled' | transloco }}</p>
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
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.circuit' | transloco }}</p>
                              <p class="font-heading text-lg">{{ nombreCircuito }}</p>
                            </div>
                          }
                          @if (event.auspiciador) {
                            <div>
                              <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">{{ 'competitor.events.sponsor' | transloco }}</p>
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
                              {{ 'competitor.events.full' | transloco }}
                            </button>
                          } @else if (!auth.isAuthenticated() || auth.isCompetitor()) {
                            <a [routerLink]="('/inscripcion/' + event.id) | localizePath"
                               class="px-5 py-2.5 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition shadow-lg shadow-orange-brand/20">
                              {{ 'competitor.events.register' | transloco }}
                            </a>
                          } @else {
                            <span class="text-xs text-text-muted font-accent uppercase tracking-wider">
                              {{ 'competitor.events.competitorsOnly' | transloco }}
                            </span>
                          }
                        } @else if (event.statusPublic === 'Completado') {
                          @if (isScheduleAvailable(event.id)) {
                            <a [href]="liveSchedulePdfUrl()" target="_blank" rel="noopener"
                               class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition inline-flex items-center gap-2">
                              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                              </svg>
                              {{ scheduleButtonLabel() }}
                            </a>
                          } @else {
                            @if (resultsPdfUrl(event.id); as pdfUrl) {
                              <a [href]="pdfUrl" target="_blank" rel="noopener"
                                 class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition">
                                {{ 'competitor.events.viewWinners' | transloco }}
                              </a>
                            }
                          }
                        } @else {
                          <button disabled class="px-5 py-2.5 rounded-md bg-navy-mid/60 text-text-muted font-accent uppercase tracking-wider text-sm cursor-not-allowed">
                            {{ 'competitor.events.closed' | transloco }}
                          </button>
                        }

                        @if (event.categorias?.length) {
                          <button (click)="toggleExpand(event)"
                                  class="px-5 py-2.5 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition flex items-center gap-2">
                            <span>{{ (isExpanded(event.id) ? 'competitor.events.hideDetails' : 'competitor.events.showDetails') | transloco }}</span>
                            <svg class="h-4 w-4 transition-transform" [class.rotate-180]="isExpanded(event.id)"
                                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                            </svg>
                          </button>
                        }

                        <button (click)="toggleInscritos(event)"
                                class="px-5 py-2.5 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition flex items-center gap-2">
                          <span>{{ (isInscritosExpanded(event.id) ? 'competitor.events.hideEnrolled' : 'competitor.events.showEnrolled') | transloco }}</span>
                          <svg class="h-4 w-4 transition-transform" [class.rotate-180]="isInscritosExpanded(event.id)"
                               fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
                          </svg>
                        </button>

                        @if (isScheduleAvailable(event.id) && event.statusPublic !== 'Completado') {
                          <a [href]="liveSchedulePdfUrl()" target="_blank" rel="noopener"
                             class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition inline-flex items-center gap-2">
                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                            </svg>
                            {{ scheduleButtonLabel() }}
                          </a>
                        }
                      </div>

                      @if (isExpanded(event.id) && event.categorias?.length) {
                        <div class="mt-6 pt-6 border-t border-navy-mid">
                          <h4 class="font-accent uppercase tracking-wider text-cyan-brand text-sm mb-3">{{ 'competitor.events.categoriesAndFees' | transloco }}</h4>
                          <div class="overflow-x-auto">
                            <table class="w-full text-sm">
                              <thead class="text-text-muted font-accent uppercase tracking-wider text-xs">
                                <tr class="border-b border-navy-mid">
                                  <th class="text-left py-2 pr-4">{{ 'competitor.events.category' | transloco }}</th>
                                  <th class="text-left py-2 px-2">{{ 'competitor.events.spots' | transloco }}</th>
                                  <th class="text-right py-2 pl-2">{{ 'competitor.events.fee' | transloco }}</th>
                                  @if (event.statusPublic === 'Completado') {
                                    <th class="text-right py-2 pl-2">{{ 'competitor.events.winner' | transloco }}</th>
                                  }
                                </tr>
                              </thead>
                              <tbody class="divide-y divide-navy-mid/60">
                                @for (cat of event.categorias!; track cat.id) {
                                  <tr [class.opacity-50]="cat.inscritos >= cat.capacidad">
                                    <td class="py-2.5 pr-4">
                                      {{ cat.nombre }}
                                      @if (cat.inscritos >= cat.capacidad) {
                                        <span class="ml-2 text-[10px] font-accent uppercase text-error-brand">{{ 'competitor.events.categoryFull' | transloco }}</span>
                                      }
                                    </td>
                                    <td class="px-2 text-text-muted">{{ cat.inscritos }} / {{ cat.capacidad }}</td>
                                    <td class="py-2.5 pl-2 text-right text-cyan-brand">{{ formatUSD(cat.tarifa) }}</td>
                                    @if (event.statusPublic === 'Completado') {
                                      <td class="py-2.5 pl-2 text-right">
                                        {{ winners().get(cat.id) || (loadingWinners().has(event.id) ? ('competitor.events.loading' | transloco) : '—') }}
                                      </td>
                                    }
                                  </tr>
                                }
                              </tbody>
                            </table>
                          </div>
                          @if (event.inscripcionCierre) {
                            <p class="text-xs text-text-muted mt-3">{{ 'competitor.events.closesOn' | transloco: { date: formatDate(event.inscripcionCierre) } }}</p>
                          }
                        </div>
                      }

                      @if (isInscritosExpanded(event.id)) {
                        <div class="mt-6 pt-6 border-t border-navy-mid">
                          <div class="flex items-center justify-between flex-wrap gap-3 mb-3">
                            <h4 class="font-accent uppercase tracking-wider text-cyan-brand text-sm">{{ 'competitor.events.confirmedTitle' | transloco }}</h4>
                            @if (event.categorias?.length) {
                              <select [value]="selectedInscritosCategoryId(event.id)"
                                      (change)="selectInscritosCategory(event.id, $any($event.target).value)"
                                      class="bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-1.5 text-xs text-text-light focus:outline-none focus:border-cyan-brand transition">
                                <option value="">{{ 'competitor.events.allCategories' | transloco }}</option>
                                @for (cat of event.categorias!; track cat.id) {
                                  <option [value]="cat.id">{{ cat.nombre }}</option>
                                }
                              </select>
                            }
                          </div>
                          @if (loadingInscritos().has(event.id)) {
                            <p class="text-sm text-text-muted">{{ 'competitor.events.loading' | transloco }}</p>
                          } @else if (filteredConfirmedInscriptions(event).length === 0) {
                            <p class="text-sm text-text-muted">{{ 'competitor.events.noConfirmed' | transloco }}</p>
                          } @else {
                            <div class="overflow-x-auto">
                              <table class="w-full text-sm">
                                <thead class="text-text-muted font-accent uppercase tracking-wider text-xs">
                                  <tr class="border-b border-navy-mid">
                                    <th class="text-left py-2 pr-4">{{ 'competitor.events.competitor' | transloco }}</th>
                                    <th class="text-left py-2 px-2">{{ 'competitor.events.country' | transloco }}</th>
                                    <th class="text-left py-2 pl-2">{{ 'competitor.events.category' | transloco }}</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-navy-mid/60">
                                  @for (row of filteredConfirmedInscriptions(event); track row.fullName + row.categoryName) {
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
                <p class="text-text-muted text-center py-12">{{ 'competitor.events.noEvents' | transloco }}</p>
              }
            }
          </div>

          <aside class="lg:col-span-1">
            <div class="sticky top-24 space-y-5">
              @if (auth.isAuthenticated()) {
                <div class="bg-navy-dark border border-navy-mid rounded-xl p-6">
                  <div class="flex items-center justify-between mb-5">
                    <h3 class="font-heading text-xl">{{ 'competitor.events.myInscriptions' | transloco }}</h3>
                    @if (myInscriptions().length > 0) {
                      <span class="px-2 py-0.5 rounded-full text-xs font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30">
                        {{ 'competitor.events.active' | transloco: { count: myInscriptions().length } }}
                      </span>
                    }
                  </div>

                  @if (loadingInscriptions()) {
                    <div class="space-y-3">
                      <div class="skeleton h-20 rounded-lg"></div>
                      <div class="skeleton h-20 rounded-lg"></div>
                    </div>
                  } @else if (myInscriptions().length === 0) {
                    <p class="text-sm text-text-muted">{{ 'competitor.events.noInscriptions' | transloco }}</p>
                  } @else {
                    <div class="space-y-4">
                      @for (ins of myInscriptions(); track ins.id) {
                        <div class="border rounded-lg p-4 hover:border-cyan-brand transition"
                             [class]="ins.statusPago === 'pendiente' ? 'border-warning-brand/40 bg-warning-brand/5' : 'border-navy-mid'">
                          <div class="flex items-center justify-between mb-2">
                            <span class="text-lg">{{ ins.eventoPais }}</span>
                            <span class="px-2 py-0.5 rounded-full text-xs font-accent uppercase tracking-wider border"
                                  [class]="ins.statusPago === 'confirmado' ? 'bg-success-brand/15 text-success-brand border-success-brand/30' : 'bg-warning-brand/15 text-warning-brand border-warning-brand/30'">
                              {{ (ins.statusPago === 'confirmado' ? 'competitor.events.confirmed' : 'competitor.events.paymentPending') | transloco }}
                            </span>
                          </div>
                          <h4 class="font-heading text-base leading-tight mb-1">{{ ins.eventoNombre }}</h4>
                          <p class="text-xs text-text-muted mb-3">{{ ins.categoria }} · {{ dateRangeShort(ins.fechaInicio, ins.fechaFin) }}</p>
                          @if (ins.statusPago === 'pendiente') {
                            <a [routerLink]="('/pago-playa/' + ins.id) | localizePath"
                               class="w-full block text-center px-2 py-1.5 rounded bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-xs transition">
                              {{ 'competitor.events.viewToken' | transloco }}
                            </a>
                          }
                        </div>
                      }
                    </div>
                  }

                  <a [routerLink]="'/mi-panel/inscripciones' | localizePath"
                     class="block text-center mt-5 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                    {{ 'competitor.events.fullHistory' | transloco }}
                  </a>
                </div>
              }

              <div class="bg-navy-dark border border-navy-mid rounded-xl p-6">
                <h3 class="font-heading text-xl mb-4">{{ 'competitor.events.howToRead' | transloco }}</h3>
                <ul class="space-y-3 text-sm text-text-muted">
                  <li class="flex items-start gap-3">
                    <span class="text-cyan-brand text-lg leading-none mt-0.5">★</span>
                    <span>{{ 'competitor.events.starsHelp' | transloco }}</span>
                  </li>
                  <li class="flex items-start gap-3">
                    <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand border border-success-brand/30 whitespace-nowrap mt-0.5">{{ 'competitor.events.openBadge' | transloco }}</span>
                    <span>{{ 'competitor.events.openHelp' | transloco }}</span>
                  </li>
                  <li class="flex items-start gap-3">
                    <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30 whitespace-nowrap mt-0.5">{{ 'competitor.events.soonBadge' | transloco }}</span>
                    <span>{{ 'competitor.events.soonHelp' | transloco }}</span>
                  </li>
                </ul>
              </div>

              <div class="bg-gradient-to-br from-cyan-brand/10 to-orange-brand/5 border border-cyan-brand/20 rounded-xl p-5">
                <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs mb-2">{{ 'competitor.events.needHelp' | transloco }}</p>
                <h4 class="font-heading text-lg mb-2 leading-tight">{{ 'competitor.events.rulesSupport' | transloco }}</h4>
                <p class="text-sm text-text-muted leading-relaxed mb-4">
                  {{ 'competitor.events.rulesSupportText' | transloco }}
                </p>
                <a href="/reglamento.pdf" target="_blank"
                   class="inline-flex items-center gap-1 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                  {{ 'competitor.events.viewRules' | transloco }}
                </a>
                &nbsp;&nbsp;
                <a href="mailto:soporte@alasglobaltour.com"
                   class="inline-flex items-center gap-1 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider mt-2">
                  {{ 'competitor.events.contactSupport' | transloco }}
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
  private seo = inject(SeoService);
  private transloco = inject(TranslocoService);
  private localeFormat = inject(LocaleFormatService);
  private liveStatus = inject(LiveStatusService);
  private route = inject(ActivatedRoute);
  private injector = inject(Injector);

  readonly currentYear = new Date().getFullYear();

  loading = signal(true);
  loadingInscriptions = signal(false);
  events = signal<EventItem[]>([]);
  circuits = signal<Circuit[]>([]);
  myInscriptions = signal<MyInscription[]>([]);
  competitorStats = signal<CompetitorStats | null>(null);
  circuitFilter = signal<string>('all');
  proximosOnly = signal(true);
  soloAbiertas = signal(false);
  expanded = signal<Set<string>>(new Set());
  readonly skeletons = [1, 2, 3];

  winners = signal<Map<string, string>>(new Map());
  loadingWinners = signal<Set<string>>(new Set());

  resultsPdfUrls = signal<Map<string, string | null>>(new Map());

  expandedInscritos = signal<Set<string>>(new Set());
  confirmedInscriptions = signal<Map<string, ConfirmedInscriptionRow[]>>(new Map());
  loadingInscritos = signal<Set<string>>(new Set());
  selectedInscritosCategory = signal<Map<string, string>>(new Map());

  filteredEvents = computed(() => {
    const filter = this.circuitFilter();
    const evts = this.events();
    let list = filter === 'all'
      ? evts
      : evts.filter(e => e.circuitId === filter);
    if (this.proximosOnly()) list = list.filter(e => this.hasNotStarted(e));
    if (this.soloAbiertas()) list = list.filter(e => e.statusPublic === 'Inscripciones Abiertas');
    return sortEventsForDisplay(list);
  });

  userInitial = computed(() => (this.auth.currentUser()?.fullName ?? '?')[0].toUpperCase());
  firstName = computed(() => (this.auth.currentUser()?.fullName ?? '').split(' ')[0]);

  ngOnInit(): void {
    this.seo.setPageMeta({ scope: 'competitor', descriptionKey: 'events.meta.description' });
    const eventsLoaded = this.loadEvents().then(() => {
      if (this.auth.isAuthenticated()) this.loadMyInscriptions();
    });
    void Promise.all([eventsLoaded, this.loadCircuits()]).then(() => this.focusLinkedEvent());
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
      const res = await this.api.get<any>('/events?limit=100&page=1');
      const events: EventItem[] = res?.data ?? [];
      this.events.set(events);
      void this.loadResultsPdfUrls(events);
      void this.loadEventCategories(events);
    } catch {
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  // GET /events no incluye las categorias del evento (no existe tal campo en el contrato) — se
  // completan por evento con GET /events/{id}/categories, mismo endpoint que ya usa admin-eventos.
  private async loadEventCategories(events: EventItem[]): Promise<void> {
    if (events.length === 0) return;
    const entries = await Promise.all(events.map(async (e): Promise<[string, EventCategory[]]> => {
      try {
        const res = await this.api.get<any>(`/events/${e.id}/categories`);
        const data: any[] = res?.data ?? [];
        return [e.id, data.map(c => ({
          id: c.categoryId,
          nombre: c.categoryName,
          inscritos: c.enrolledCount ?? 0,
          capacidad: c.capacidad ?? 0,
          tarifa: c.effectiveTariffUsd ?? 0,
        }))];
      } catch {
        return [e.id, []];
      }
    }));

    const categoriasById = new Map(entries);
    this.events.update(list => list.map(e => ({ ...e, categorias: categoriasById.get(e.id) ?? e.categorias })));
  }

  // Busca el PDF de resultados (Media Library de WordPress, nombrado con el código SurfScores
  // del evento, ej. "8178.pdf") solo para eventos completados con código asignado; el botón
  // "Ver resultados" no se muestra si el PDF no fue cargado.
  private async loadResultsPdfUrls(events: EventItem[]): Promise<void> {
    const candidates = events.filter(e => e.statusPublic === 'Completado' && e.surfScoresCode);
    if (candidates.length === 0) return;
    const entries = await Promise.all(candidates.map(async (e): Promise<[string, string | null]> => {
      try {
        const res = await this.api.get<any>(`/events/${e.id}/results-pdf`);
        return [e.id, res?.url ?? null];
      } catch {
        return [e.id, null];
      }
    }));
    this.resultsPdfUrls.update(map => {
      const next = new Map(map);
      for (const [id, url] of entries) next.set(id, url);
      return next;
    });
  }

  resultsPdfUrl(eventId: string): string | null {
    return this.resultsPdfUrls().get(eventId) ?? null;
  }

  private async loadCircuits(): Promise<void> {
    try {
      const res = await this.api.get<any>('/circuits?limit=100');
      const all: Circuit[] = res?.data ?? [];
      this.circuits.set(all);
      const currentYearCircuits = all.filter(c => c.temporada === this.currentYear);
      const current = pickCurrentCircuit(currentYearCircuits.length > 0 ? currentYearCircuits : all);
      // Se difiere al siguiente tick: el <select> nativo ignora el [value] si las <option>
      // del @for aún no existen en el DOM (mismo ciclo de Angular en que llegan los circuitos).
      if (current && !this.linkedEventId()) setTimeout(() => this.circuitFilter.set(current.id));
    } catch {
      this.circuits.set([]);
    }
  }

  // fechaInicio es fecha "solo fecha" (medianoche UTC); se compara contra la medianoche UTC de hoy
  // para que el filtro no dependa del huso horario del navegador (ver nota de dayOf más abajo).
  private hasNotStarted(event: EventItem): boolean {
    if (!event.fechaInicio) return false;
    const start = new Date(event.fechaInicio).getTime();
    const now = new Date();
    const todayUtcStart = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
    return start >= todayUtcStart;
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

  private linkedEventId(): string | undefined {
    return this.route.snapshot.fragment?.match(/^evento-(.+)$/)?.[1];
  }

  /** `/eventos#evento-<id>` (home "Ver evento"): show that event expanded and scroll to it. */
  private focusLinkedEvent(): void {
    const id = this.linkedEventId();
    const event = id ? this.events().find(e => e.id === id) : undefined;
    if (!event) return;
    if (!this.hasNotStarted(event)) this.proximosOnly.set(false);
    if (!this.isExpanded(event.id)) this.toggleExpand(event);
    // Diferido como en loadCircuits: el <select> necesita sus <option> ya renderizadas.
    setTimeout(() => {
      this.circuitFilter.set(event.circuitId && this.circuits().some(c => c.id === event.circuitId) ? event.circuitId : 'all');
      afterNextRender(() => {
        document.getElementById(`evento-${event.id}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, { injector: this.injector });
    });
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

  selectedInscritosCategoryId(eventId: string): string {
    return this.selectedInscritosCategory().get(eventId) ?? '';
  }

  selectInscritosCategory(eventId: string, categoryId: string): void {
    this.selectedInscritosCategory.update(map => {
      const next = new Map(map);
      next.set(eventId, categoryId);
      return next;
    });
  }

  filteredConfirmedInscriptions(event: EventItem): ConfirmedInscriptionRow[] {
    const rows = this.confirmedInscriptions().get(event.id) ?? [];
    const categoryId = this.selectedInscritosCategoryId(event.id);
    if (!categoryId) return rows;
    const categoryName = event.categorias?.find(c => c.id === categoryId)?.nombre;
    return categoryName ? rows.filter(r => r.categoryName === categoryName) : rows;
  }

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

  isScheduleAvailable(eventId: string): boolean {
    const s = this.liveStatus.status();
    return !!(s?.event?.id === eventId && s.schedulePdfUrl);
  }

  scheduleButtonLabel(): string {
    return this.transloco.translate(this.liveStatus.isLive() ? 'competitor.events.downloadSchedule' : 'competitor.events.winnersPdf');
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
  formatUSD(n: number): string { return this.localeFormat.usd(n); }
  // fechaInicio/fechaFin/inscripcionCierre son fechas "solo fecha" (medianoche UTC en el backend).
  // Se usan los getters UTC para que el día mostrado no dependa del huso horario del navegador
  // (con getters locales, un usuario en Sudamérica ve el día anterior).
  dayOf(d: string): string { return d ? String(new Date(d).getUTCDate()).padStart(2, '0') : ''; }

  monthYearOf(d: string): string {
    return this.localeFormat.utcDate(d, 'MMM y');
  }

  dateRangeShort(start: string, end: string): string {
    if (!start || !end) return '';
    const s = new Date(start), e = new Date(end);
    return `${s.getUTCDate()} - ${e.getUTCDate()} ${this.localeFormat.utcDate(start, 'MMM')}`;
  }

  formatDate(d: string): string {
    return this.localeFormat.utcDate(d, 'longDate');
  }
}
