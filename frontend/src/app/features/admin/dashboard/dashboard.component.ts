import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type EstadoEvento = 'Activo' | 'Próximamente' | 'Borrador' | 'Completado' | 'Cancelado';

interface DashboardEventRow {
  id: string;
  nombre: string;
  pais: string;
  fechas: string;
  stars: number;
  inscritos: number;
  capacidad: number;
  estado: EstadoEvento;
}

interface DashboardCircuito {
  nombre: string;
  subtitulo: string;
  estado: 'Activo' | 'Próximo';
  eventos: number;
  competidores: number;
  premioTotal: string;
}

interface DashboardInscripcion {
  fecha: string;
  competidor: string;
  evento: string;
  categoria: string;
}

interface DashboardAlert {
  module: string;
  level: 'warning' | 'info' | 'error';
  title: string;
  message: string;
  count?: number;
}

const COUNTRY_FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
  VE: '🇻🇪', BO: '🇧🇴',
};

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

function rangoFechas(inicio: string, fin: string): string {
  const di = new Date(inicio), df = new Date(fin);
  const mi = MESES[di.getMonth()], mf = MESES[df.getMonth()];
  return mi === mf ? `${di.getDate()} – ${df.getDate()} ${mi}` : `${di.getDate()} ${mi} – ${df.getDate()} ${mf}`;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [LoadingSpinnerComponent, DecimalPipe],
  template: `
    <div class="space-y-10">
      <div>
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Dashboard</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Panel de Administración</h1>
      </div>

      @if (loading()) {
        <app-loading-spinner />
      } @else {

      <!-- ===== ALERTAS ===== -->
      @if (alerts().length > 0) {
        <section class="space-y-3">
          @for (a of alerts(); track a.module) {
            <button (click)="goTo('/admin/' + a.module)"
                    [class]="alertClass(a.level)">
              <svg class="h-5 w-5 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M8.485 3.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 3.495zM10 6a1 1 0 011 1v3a1 1 0 11-2 0V7a1 1 0 011-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
              </svg>
              <div class="text-left">
                <p class="font-heading text-sm">{{ a.title }}</p>
                <p class="text-xs opacity-80">{{ a.message }}</p>
              </div>
              @if (a.count) {
                <span class="ml-auto font-heading text-lg">{{ a.count }}</span>
              }
            </button>
          }
        </section>
      }

      <!-- ===== STATS ===== -->
      <section>
        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5">
          <div class="bg-navy-deepest border border-navy-mid rounded-xl p-5 hover:-translate-y-0.5 transition">
            <div class="flex items-start justify-between mb-3">
              <div class="w-10 h-10 rounded-lg bg-cyan-brand/15 border border-cyan-brand/30 flex items-center justify-center">
                <svg class="h-5 w-5 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/></svg>
              </div>
            </div>
            <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Eventos activos</p>
            <p class="font-heading text-4xl text-cyan-brand">{{ statEventosActivos() }}</p>
          </div>

          <div class="bg-navy-deepest border border-navy-mid rounded-xl p-5 hover:-translate-y-0.5 transition">
            <div class="flex items-start justify-between mb-3">
              <div class="w-10 h-10 rounded-lg bg-success-brand/15 border border-success-brand/30 flex items-center justify-center">
                <svg class="h-5 w-5 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>
              </div>
            </div>
            <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Total inscripciones</p>
            <p class="font-heading text-4xl text-success-brand">{{ statTotalInscripciones() }}</p>
          </div>

          <button (click)="goTo('/admin/tokens')"
                  class="text-left bg-navy-deepest border border-orange-brand/40 rounded-xl p-5 hover:border-orange-brand transition">
            <div class="flex items-start justify-between mb-3">
              <div class="w-10 h-10 rounded-lg bg-orange-brand/15 border border-orange-brand/30 flex items-center justify-center">
                <svg class="h-5 w-5 text-orange-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/></svg>
              </div>
              @if (statTokensPendientes() > 0) {
                <span class="text-xs font-accent uppercase tracking-wider text-orange-brand animate-pulse">Requiere acción</span>
              }
            </div>
            <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Tokens pendientes</p>
            <p class="font-heading text-4xl text-orange-brand flex items-baseline gap-2">{{ statTokensPendientes() }} <span class="text-sm text-text-muted font-body normal-case tracking-normal">→ revisar</span></p>
          </button>

          <div class="bg-navy-deepest border border-navy-mid rounded-xl p-5 hover:-translate-y-0.5 transition">
            <div class="flex items-start justify-between mb-3">
              <div class="w-10 h-10 rounded-lg bg-cyan-brand/15 border border-cyan-brand/30 flex items-center justify-center">
                <svg class="h-5 w-5 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
              </div>
            </div>
            <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-1">Recaudación mes</p>
            <p class="font-heading text-4xl text-cyan-brand">\${{ statRecaudacion() | number:'1.0-0' }} <span class="text-sm text-text-muted font-body normal-case tracking-normal">USD</span></p>
          </div>
        </div>
      </section>

      <!-- ===== EVENTOS ===== -->
      <section>
        <div class="flex flex-wrap items-end justify-between gap-3 mb-5">
          <div>
            <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-xs mb-1">Gestión</p>
            <h2 class="font-heading text-2xl text-white">Eventos del circuito</h2>
          </div>
          <button (click)="goTo('/admin/eventos')"
                  class="inline-flex items-center gap-2 px-4 py-2 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition shadow-lg shadow-orange-brand/20">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"/></svg>
            Nuevo evento
          </button>
        </div>

        <div class="bg-navy-deepest border border-navy-mid rounded-xl overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-navy-mid/30 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="text-left px-5 py-3">Evento</th>
                  <th class="text-left px-3 py-3">País</th>
                  <th class="text-left px-3 py-3">Fechas</th>
                  <th class="text-left px-3 py-3">Estrellas</th>
                  <th class="text-left px-3 py-3">Inscritos</th>
                  <th class="text-left px-3 py-3">Estado</th>
                  <th class="text-right px-5 py-3">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (ev of eventos(); track ev.id) {
                  <tr class="hover:bg-cyan-brand/5 transition">
                    <td class="px-5 py-4 font-heading text-text-light">{{ ev.nombre }}</td>
                    <td class="px-3 py-4">{{ ev.pais }}</td>
                    <td class="px-3 py-4 text-text-muted">{{ ev.fechas }}</td>
                    <td class="px-3 py-4 text-cyan-brand">{{ '★'.repeat(ev.stars) }}</td>
                    <td class="px-3 py-4">{{ ev.inscritos }}<span class="text-text-muted">/{{ ev.capacidad }}</span></td>
                    <td class="px-3 py-4"><span [class]="estadoEventoClass(ev.estado)">{{ ev.estado }}</span></td>
                    <td class="px-5 py-4 text-right">
                      <div class="inline-flex gap-1">
                        <button (click)="goTo('/admin/eventos')" class="p-1.5 rounded hover:bg-navy-mid text-cyan-brand" title="Ver">
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
                        </button>
                        <button (click)="goTo('/admin/eventos')" class="p-1.5 rounded hover:bg-navy-mid text-warning-brand" title="Editar">
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <!-- ===== CIRCUITOS ===== -->
      <section>
        <div class="flex items-end justify-between gap-3 mb-5">
          <div>
            <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-xs mb-1">Estructura</p>
            <h2 class="font-heading text-2xl text-white">Circuitos de la temporada</h2>
          </div>
          <button (click)="goTo('/admin/circuitos')"
                  class="inline-flex items-center gap-2 px-4 py-2 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"/></svg>
            Nuevo circuito
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
          @for (c of circuitos(); track c.nombre) {
            <div class="bg-navy-deepest border border-navy-mid rounded-xl p-5 hover:border-cyan-brand transition">
              <div class="flex items-center justify-between mb-4">
                <span [class]="c.estado === 'Activo'
                  ? 'px-2 py-0.5 rounded-full text-xs bg-success-brand/15 text-success-brand border border-success-brand/30 font-accent uppercase tracking-wider'
                  : 'px-2 py-0.5 rounded-full text-xs bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30 font-accent uppercase tracking-wider'">
                  {{ c.estado }}
                </span>
                <button (click)="goTo('/admin/circuitos')" class="text-text-muted hover:text-cyan-brand">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01"/></svg>
                </button>
              </div>
              <h3 class="font-heading text-xl text-white mb-1">{{ c.nombre }}</h3>
              <p class="text-xs text-text-muted mb-4">{{ c.subtitulo }}</p>
              <dl class="space-y-2 text-sm">
                <div class="flex justify-between"><dt class="text-text-muted">Eventos</dt><dd class="font-heading text-text-light">{{ c.eventos }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Competidores</dt><dd class="font-heading text-text-light">{{ c.competidores }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Premio total</dt><dd class="font-heading text-cyan-brand">{{ c.premioTotal }}</dd></div>
              </dl>
            </div>
          }
        </div>
      </section>

      <!-- ===== ÚLTIMOS INSCRITOS ===== -->
      <section>
        <div class="flex items-end justify-between gap-3 mb-5">
          <div>
            <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-xs mb-1">Actividad reciente</p>
            <h2 class="font-heading text-2xl text-white">Últimos inscritos</h2>
          </div>
          <button (click)="goTo('/admin/inscritos')" class="text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">Ver todos los inscritos →</button>
        </div>

        <div class="bg-navy-deepest border border-navy-mid rounded-xl overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-navy-mid/30 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="text-left px-5 py-3">Fecha</th>
                  <th class="text-left px-3 py-3">Competidor</th>
                  <th class="text-left px-3 py-3">Evento</th>
                  <th class="text-left px-5 py-3">Categoría</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (i of inscripciones(); track i.fecha + i.competidor) {
                  <tr class="hover:bg-cyan-brand/5 transition">
                    <td class="px-5 py-3 text-text-muted whitespace-nowrap">{{ i.fecha }}</td>
                    <td class="px-3 py-3 font-medium text-text-light">{{ i.competidor }}</td>
                    <td class="px-3 py-3 text-text-muted">{{ i.evento }}</td>
                    <td class="px-5 py-3 text-text-light">{{ i.categoria }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <footer class="pt-6 border-t border-navy-mid flex flex-col md:flex-row items-center justify-between gap-3 text-xs text-text-muted">
        <p>© 2026 ALAS Latin Tour — Panel administrativo</p>
      </footer>
      }
    </div>
  `,
})
export class AdminDashboardComponent implements OnInit {
  private router = inject(Router);
  private api = inject(ApiService);

  loading = signal(true);

  statEventosActivos = signal(0);
  statTotalInscripciones = signal(0);
  statTokensPendientes = signal(0);
  statRecaudacion = signal(0);

  eventos = signal<DashboardEventRow[]>([]);
  circuitos = signal<DashboardCircuito[]>([]);
  inscripciones = signal<DashboardInscripcion[]>([]);
  alerts = signal<DashboardAlert[]>([]);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      const [dash, eventsRes, circuitsRes] = await Promise.all([
        this.api.get<any>('/admin/dashboard'),
        this.api.get<any>('/events?limit=100'),
        this.api.get<any>('/circuits?limit=6'),
      ]);

      this.statEventosActivos.set(dash?.kpis?.totalEventosActivos ?? 0);
      this.statTotalInscripciones.set(dash?.kpis?.totalInscripciones ?? 0);
      this.statTokensPendientes.set(dash?.kpis?.tokensPendientes ?? 0);
      this.statRecaudacion.set(dash?.kpis?.recaudacionMesUsd ?? 0);
      this.alerts.set(dash?.alerts ?? []);

      const eventsById = new Map<string, any>((eventsRes?.data ?? []).map((e: any) => [e.id, e]));
      this.eventos.set((dash?.activeEvents ?? []).map((ae: any) => {
        const full = eventsById.get(ae.id);
        return {
          id: ae.id,
          nombre: ae.nombre,
          pais: full ? (COUNTRY_FLAGS[full.pais?.toUpperCase()] ?? '🏳️') + ' ' + full.pais : '—',
          fechas: full ? rangoFechas(full.fechaInicio, full.fechaFin) : new Date(ae.fechaInicio).toLocaleDateString('es'),
          stars: full?.stars ?? 0,
          inscritos: ae.inscritosCount,
          capacidad: full?.capacidadMaxima ?? 0,
          estado: ae.estado,
        };
      }));

      this.circuitos.set((circuitsRes?.data ?? []).map((c: any) => ({
        nombre: c.nombre,
        subtitulo: `Temporada ${c.temporada} · ${c.region}`,
        estado: c.estado === 'Activo' ? 'Activo' : 'Próximo',
        eventos: c.eventsCount,
        competidores: c.competidoresCount,
        premioTotal: `$${Math.round(c.totalPrizeUsd).toLocaleString('es')}`,
      })));

      this.inscripciones.set((dash?.recentInscriptions ?? []).map((i: any) => ({
        fecha: new Date(i.inscripcionAt).toLocaleString('es', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }),
        competidor: i.competitorName,
        evento: i.evento,
        categoria: i.categoria,
      })));
    } finally {
      this.loading.set(false);
    }
  }

  goTo(path: string): void {
    this.router.navigateByUrl(path);
  }

  alertClass(level: DashboardAlert['level']): string {
    const base = 'w-full flex items-start gap-3 rounded-xl border p-4 transition hover:-translate-y-0.5';
    const map: Record<DashboardAlert['level'], string> = {
      warning: `${base} bg-warning-brand/10 border-warning-brand/30 text-warning-brand`,
      error: `${base} bg-error-brand/10 border-error-brand/30 text-error-brand`,
      info: `${base} bg-cyan-brand/10 border-cyan-brand/30 text-cyan-brand`,
    };
    return map[level] ?? map.info;
  }

  estadoEventoClass(estado: string): string {
    const map: Record<string, string> = {
      'Activo': 'px-2 py-1 rounded-full text-xs bg-success-brand/15 text-success-brand border border-success-brand/30 font-accent uppercase tracking-wider',
      'Próximamente': 'px-2 py-1 rounded-full text-xs bg-cyan-brand/15 text-cyan-brand border border-cyan-brand/30 font-accent uppercase tracking-wider',
      'Borrador': 'px-2 py-1 rounded-full text-xs bg-navy-mid text-text-muted border border-navy-mid font-accent uppercase tracking-wider',
      'Completado': 'px-2 py-1 rounded-full text-xs bg-navy-mid text-text-muted border border-navy-mid font-accent uppercase tracking-wider',
      'Cancelado': 'px-2 py-1 rounded-full text-xs bg-error-brand/15 text-error-brand border border-error-brand/30 font-accent uppercase tracking-wider',
    };
    return map[estado] ?? map['Borrador'];
  }
}
