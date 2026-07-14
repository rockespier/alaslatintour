import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

interface EventoApi {
  id: string;
  circuitId: string;
  nombre: string;
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
  estado?: string;
  lugar?: string;
}

interface CircuitoOption { id: string; nombre: string; }

interface CategoriaTarifa {
  nombre: string;
  cupos: string;
  tarifa: string;
}

const COUNTRY_FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
  VE: '🇻🇪', BO: '🇧🇴',
};

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

@Component({
  selector: 'app-calendario',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, DecimalPipe],
  template: `
    <section class="py-14 px-4 sm:px-6 lg:px-8 border-b border-navy-mid">
      <div class="max-w-7xl mx-auto">
        <h1 class="font-heading text-4xl md:text-6xl text-white">Calendario de Eventos</h1>
        <p class="mt-3 text-text-muted max-w-2xl">Todas las paradas del ALAS Latin Tour 2026: fechas, sedes, categorías, cupos y premios de cada evento del circuito.</p>
      </div>
    </section>

    <section class="px-4 sm:px-6 lg:px-8 py-10 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        @if (loading()) {
          <app-loading-spinner label="Cargando calendario..." />
        } @else {
          <div class="flex items-end justify-between mb-6 flex-wrap gap-4">
            <div class="flex items-center gap-2 text-sm text-text-muted">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"/></svg>
              <span>Filtrar por circuito:</span>
            </div>
          </div>

          <div class="flex flex-wrap gap-2 border-b border-navy-mid pb-1 mb-8">
            <button (click)="tab.set('all')" [class]="tabClass('all')">Todos los Circuitos</button>
            @for (c of circuitos(); track c.id) {
              <button (click)="tab.set(c.id)" [class]="tabClass(c.id)">{{ c.nombre }}</button>
            }
          </div>

          <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
            <!-- Events list -->
            <div class="lg:col-span-2 space-y-5">
              @if (visibleEventos().length === 0) {
                <p class="text-text-muted text-sm py-8 text-center">No hay eventos para este circuito.</p>
              }
              @for (ev of visibleEventos(); track ev.id) {
                <article [class]="'rounded-xl p-6 border border-cyan-brand/15 bg-gradient-to-br from-navy-mid via-navy-dark to-navy-deepest hover:border-cyan-brand/45 transition' + (isPast(ev) ? ' opacity-70' : '')">
                  <div class="flex flex-col md:flex-row gap-5">
                    <div class="md:w-32 flex md:flex-col items-center md:items-start gap-3 md:gap-1 md:border-r md:border-navy-mid md:pr-5">
                      <div [class]="isPast(ev) ? 'font-heading text-5xl text-text-muted leading-none' : 'font-heading text-5xl text-cyan-brand leading-none'">{{ diaInicio(ev) }}</div>
                      <div>
                        <p class="font-accent uppercase text-xs text-text-muted tracking-wider">{{ mesAnio(ev) }}</p>
                        <p class="font-accent uppercase text-xs text-text-muted">{{ rangoFechas(ev) }}</p>
                      </div>
                    </div>
                    <div class="flex-1">
                      <div class="flex flex-wrap items-start justify-between gap-3 mb-3">
                        <div>
                          <div class="flex items-center gap-2 text-sm text-text-muted mb-1">
                            <span class="text-xl">{{ flagOf(ev.pais) }}</span><span>{{ ev.lugar ?? (ev.playa || ev.ciudad) }}</span>
                          </div>
                          <h3 [class]="ev.statusPublic === 'Completado' ? 'font-heading text-2xl md:text-3xl leading-tight text-text-muted' : 'font-heading text-2xl md:text-3xl leading-tight text-white'">{{ ev.nombre }}</h3>
                        </div>
                        <span [class]="estadoClass(ev.statusPublic)">{{ ev.statusPublic ?? ev.estado }}</span>
                      </div>

                      <div class="flex flex-wrap items-center gap-x-6 gap-y-3 mb-4">
                        <div class="flex items-center gap-1 text-lg">
                          @for (s of [1,2,3,4,5]; track s) {
                            <span [class]="s <= ev.stars ? 'text-cyan-brand' : 'text-navy-mid'">★</span>
                          }
                        </div>
                      </div>

                      <div class="grid grid-cols-2 md:grid-cols-3 gap-4 mb-5">
                        @if (ev.statusPublic === 'Completado') {
                          <div>
                            <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Participantes</p>
                            <p class="font-heading text-lg text-white">{{ ev.enrolledCount ?? 0 }}</p>
                          </div>
                          <div>
                            <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Estado</p>
                            <p class="font-heading text-lg text-text-muted">Finalizado</p>
                          </div>
                        } @else {
                          <div>
                            <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Premio</p>
                            <p class="font-heading text-lg text-cyan-brand">{{ ev.prizeAmountUsd ? ('$' + (ev.prizeAmountUsd | number:'1.0-0') + ' USD') : '—' }}</p>
                          </div>
                          <div>
                            <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Inscritos</p>
                            <p class="font-heading text-lg text-white">{{ ev.enrolledCount ?? 0 }}<span class="text-text-muted text-sm">/{{ ev.capacidadMaxima }}</span></p>
                            <div class="w-full h-1.5 bg-navy-mid rounded-full mt-1 overflow-hidden">
                              <div class="h-full bg-cyan-brand" [style.width.%]="capacidadPct(ev)"></div>
                            </div>
                          </div>
                          <div>
                            <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-0.5">Circuito</p>
                            <p class="font-heading text-lg text-white">{{ circuitoNombre(ev.circuitId) }}</p>
                          </div>
                        }
                      </div>

                      <div class="flex flex-wrap items-center gap-3">
                        @if (ev.statusPublic === 'Inscripciones Abiertas') {
                          <a [routerLink]="['/inscripcion', ev.id]" class="px-5 py-2.5 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition shadow-lg shadow-orange-brand/20">Inscribirse</a>
                        } @else if (ev.statusPublic === 'Completado') {
                          <a routerLink="/ranking" class="px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition">Ver resultados finales</a>
                        } @else {
                          <button disabled class="px-5 py-2.5 rounded-md bg-navy-mid/60 text-text-muted font-accent uppercase tracking-wider text-sm cursor-not-allowed">Inscripciones cerradas</button>
                        }
                        <button (click)="toggleExpand(ev.id)" class="px-5 py-2.5 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition flex items-center gap-2">
                          <span>{{ expanded() === ev.id ? 'Ocultar detalles' : 'Ver detalles' }}</span>
                          <svg [class]="'h-4 w-4 transition-transform' + (expanded() === ev.id ? ' rotate-180' : '')" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/></svg>
                        </button>
                      </div>

                      @if (expanded() === ev.id) {
                        <div class="mt-6 pt-6 border-t border-navy-mid">
                          @if (loadingCategorias()) {
                            <p class="text-sm text-text-muted">Cargando categorías...</p>
                          } @else if (categoriasDetalle().length > 0) {
                            <h4 class="font-accent uppercase tracking-wider text-cyan-brand text-sm mb-3">Categorías y tarifas</h4>
                            <div class="overflow-x-auto">
                              <table class="w-full text-sm">
                                <thead class="text-text-muted font-accent uppercase tracking-wider text-xs">
                                  <tr class="border-b border-navy-mid">
                                    <th class="text-left py-2 pr-4">Categoría</th>
                                    <th class="text-right py-2 pl-2">Tarifa</th>
                                  </tr>
                                </thead>
                                <tbody class="divide-y divide-navy-mid/60">
                                  @for (c of categoriasDetalle(); track c.nombre) {
                                    <tr>
                                      <td class="py-2.5 pr-4 text-text-light">{{ c.nombre }}</td>
                                      <td class="py-2.5 pl-2 text-right text-cyan-brand">{{ c.tarifa }}</td>
                                    </tr>
                                  }
                                </tbody>
                              </table>
                            </div>
                          } @else {
                            <p class="text-sm text-text-muted">Este evento aún no tiene categorías habilitadas.</p>
                          }
                        </div>
                      }
                    </div>
                  </div>
                </article>
              }
            </div>

            <!-- Sidebar -->
            <aside class="lg:col-span-1">
              <div class="sticky top-24 space-y-5">
                <div class="bg-navy-dark border border-navy-mid rounded-xl p-6">
                  <h3 class="font-heading text-xl text-white mb-4">Cómo leer el calendario</h3>
                  <ul class="space-y-3 text-sm text-text-muted">
                    <li class="flex items-start gap-3">
                      <span class="text-cyan-brand text-lg leading-none mt-0.5">★</span>
                      <span>Las estrellas indican el nivel del evento (1 a 5): a mayor número, más puntos otorga al ranking.</span>
                    </li>
                    <li class="flex items-start gap-3">
                      <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand border border-success-brand/30 whitespace-nowrap mt-0.5">Abiertas</span>
                      <span>Inscripciones activas — quedan cupos disponibles.</span>
                    </li>
                    <li class="flex items-start gap-3">
                      <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30 whitespace-nowrap mt-0.5">Próximamente</span>
                      <span>Fecha confirmada, inscripciones aún no abren.</span>
                    </li>
                  </ul>
                  <a routerLink="/eventos" class="block text-center mt-6 px-5 py-2.5 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition">Empezar inscripción</a>
                </div>

                <div class="bg-gradient-to-br from-cyan-brand/10 to-orange-brand/5 border border-cyan-brand/20 rounded-xl p-5">
                  <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs mb-2">¿Necesitas ayuda?</p>
                  <h4 class="font-heading text-lg text-white mb-2 leading-tight">Reglamento y soporte</h4>
                  <p class="text-sm text-text-muted leading-relaxed mb-4">Consulta nuestro reglamento oficial o contacta a soporte para resolver dudas sobre inscripciones y pagos.</p>
                  <a routerLink="/quienes-somos" class="inline-flex items-center gap-1 text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                    Ver reglamento
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"/></svg>
                  </a>
                </div>
              </div>
            </aside>
          </div>
        }
      </div>
    </section>
  `,
})
export class CalendarioComponent implements OnInit {
  private title = inject(Title);
  private meta = inject(Meta);
  private api = inject(ApiService);

  loading = signal(true);
  eventos = signal<EventoApi[]>([]);
  circuitos = signal<CircuitoOption[]>([]);
  tab = signal<string>('all');
  expanded = signal<string | null>(null);

  loadingCategorias = signal(false);
  categoriasDetalle = signal<{ nombre: string; tarifa: string }[]>([]);

  tabClass(t: string): string {
    return this.tab() === t
      ? 'px-5 py-3 rounded-t-md border-b-2 border-cyan-brand font-accent uppercase text-sm tracking-wider text-cyan-brand bg-cyan-brand/[0.08]'
      : 'px-5 py-3 rounded-t-md border-b-2 border-transparent font-accent uppercase text-sm tracking-wider text-text-muted hover:text-text-light transition';
  }

  visibleEventos = computed(() => {
    const t = this.tab();
    const list = t === 'all' ? this.eventos() : this.eventos().filter(e => e.circuitId === t);
    return [...list].sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio));
  });

  async toggleExpand(id: string): Promise<void> {
    if (this.expanded() === id) {
      this.expanded.set(null);
      return;
    }
    this.expanded.set(id);
    this.loadingCategorias.set(true);
    this.categoriasDetalle.set([]);
    try {
      const res = await this.api.get<{ useCircuitTariffs: boolean; data: any[] }>(`/events/${id}/categories`);
      this.categoriasDetalle.set((res.data ?? []).map(c => ({
        nombre: c.categoryName,
        tarifa: `$${c.effectiveTariffUsd ?? 0}`,
      })));
    } catch {
      this.categoriasDetalle.set([]);
    } finally {
      this.loadingCategorias.set(false);
    }
  }

  async ngOnInit(): Promise<void> {
    this.title.setTitle('Calendario de Eventos — ALAS Latin Tour 2026');
    this.meta.updateTag({ name: 'description', content: 'Calendario completo del ALAS Latin Tour 2026. Fechas, sedes, categorías y cupos de todos los eventos del circuito continental de surf.' });

    this.loading.set(true);
    try {
      const [eventosRes, circuitosRes] = await Promise.all([
        this.api.get<any>('/events?limit=100'),
        this.api.get<any>('/circuits?limit=50'),
      ]);
      this.eventos.set(eventosRes?.data ?? []);
      this.circuitos.set((circuitosRes?.data ?? []).map((c: any) => ({ id: c.id, nombre: c.nombre })));
    } catch {
      this.eventos.set([]);
      this.circuitos.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  circuitoNombre(circuitId: string): string {
    return this.circuitos().find(c => c.id === circuitId)?.nombre ?? '—';
  }

  flagOf(pais: string): string {
    return COUNTRY_FLAGS[pais?.toUpperCase()] ?? '🏳️';
  }

  diaInicio(ev: EventoApi): string {
    return new Date(ev.fechaInicio).getDate().toString().padStart(2, '0');
  }

  mesAnio(ev: EventoApi): string {
    const d = new Date(ev.fechaInicio);
    return `${MESES[d.getMonth()]} ${d.getFullYear()}`;
  }

  rangoFechas(ev: EventoApi): string {
    const inicio = new Date(ev.fechaInicio);
    const fin = new Date(ev.fechaFin);
    const mesInicio = MESES[inicio.getMonth()];
    return `${inicio.getDate().toString().padStart(2, '0')} – ${fin.getDate().toString().padStart(2, '0')} ${mesInicio.charAt(0).toUpperCase() + mesInicio.slice(1)}`;
  }

  capacidadPct(ev: EventoApi): number {
    if (!ev.capacidadMaxima) return 0;
    return Math.min(100, Math.round(((ev.enrolledCount ?? 0) / ev.capacidadMaxima) * 100));
  }

  isPast(ev: EventoApi): boolean {
    return ev.statusPublic === 'Completado' || ev.statusPublic === 'Cerrado';
  }

  estadoClass(estado?: string): string {
    const map: Record<string, string> = {
      'Inscripciones Abiertas': 'px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand border border-success-brand/30 whitespace-nowrap',
      'Próximamente': 'px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30 whitespace-nowrap',
      'Completado': 'px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-navy-mid text-text-muted border border-navy-mid whitespace-nowrap',
      'Cerrado': 'px-3 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-navy-mid text-text-muted border border-navy-mid whitespace-nowrap',
    };
    return map[estado ?? ''] ?? map['Próximamente'];
  }
}
