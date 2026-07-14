import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { StarRatingComponent } from '../../../../shared/components/star-rating/star-rating.component';

interface CalendarEvent {
  id: string;
  inscriptionId: string;
  eventoNombre: string;
  eventoPais: string;
  ciudad: string;
  stars: number;
  fechaInicio: string;
  fechaFin: string;
  categoria: string;
  statusPago: 'confirmado' | 'pendiente';
}

const FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
};

@Component({
  selector: 'app-mi-calendario',
  standalone: true,
  imports: [RouterLink, StarRatingComponent],
  template: `
    <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
      <h2 class="font-heading text-3xl">Mi Calendario</h2>
      <button (click)="exportCalendar()" [disabled]="exporting()"
              class="flex items-center gap-2 px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase text-xs tracking-wider rounded-lg transition">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
        </svg>
        {{ exporting() ? 'Exportando...' : 'Exportar .ics' }}
      </button>
    </div>

    @if (loading()) {
      <div class="space-y-4">
        @for (sk of skeletons; track sk) { <div class="skeleton h-32 rounded-2xl"></div> }
      </div>
    } @else if (events().length === 0) {
      <div class="text-center py-16">
        <div class="w-14 h-14 rounded-full bg-navy-mid flex items-center justify-center mx-auto mb-4">
          <svg class="w-7 h-7 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
          </svg>
        </div>
        <p class="font-heading text-xl text-text-muted mb-2">Sin eventos en tu calendario</p>
        <p class="text-sm text-text-muted mb-6">Inscríbete en eventos para verlos aquí.</p>
        <a routerLink="/eventos"
           class="px-6 py-2.5 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition">
          Ver eventos disponibles
        </a>
      </div>
    } @else {
      <div class="space-y-4">
        @for (event of events(); track event.inscriptionId) {
          <div class="card-event rounded-2xl p-5 flex flex-col sm:flex-row gap-4">

            <!-- Date block -->
            <div class="flex sm:flex-col items-center sm:items-start gap-3 sm:gap-0 sm:w-24 sm:border-r sm:border-navy-mid sm:pr-4 flex-shrink-0">
              <p class="font-heading text-5xl leading-none text-cyan-brand">{{ dayOf(event.fechaInicio) }}</p>
              <div>
                <p class="font-accent uppercase text-xs text-text-muted">{{ monthOf(event.fechaInicio) }}</p>
                <p class="font-accent uppercase text-xs text-text-muted">{{ yearOf(event.fechaInicio) }}</p>
              </div>
            </div>

            <!-- Event info -->
            <div class="flex-1 min-w-0">
              <div class="flex flex-wrap items-center gap-2 mb-1">
                <span class="text-lg">{{ flagOf(event.eventoPais) }}</span>
                <span class="text-sm text-text-muted">{{ event.ciudad }}, {{ event.eventoPais }}</span>
                <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider border"
                      [class]="event.statusPago === 'confirmado' ? 'bg-success-brand/15 text-success-brand border-success-brand/30' : 'bg-warning-brand/15 text-warning-brand border-warning-brand/30'">
                  {{ event.statusPago === 'confirmado' ? 'Pago confirmado' : 'Pago pendiente' }}
                </span>
              </div>
              <h3 class="font-heading text-xl leading-tight mb-1">{{ event.eventoNombre }}</h3>
              <p class="text-sm text-text-muted mb-2">{{ event.categoria }} · {{ dateRange(event.fechaInicio, event.fechaFin) }}</p>
              <app-star-rating [value]="event.stars" />
            </div>

            <!-- Action -->
            @if (event.statusPago === 'pendiente') {
              <div class="sm:self-center">
                <a [routerLink]="['/pago-playa', event.inscriptionId]"
                   class="block px-4 py-2 rounded-lg bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-xs transition text-center">
                  Ver token
                </a>
              </div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class MiCalendarioComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private platformId = inject(PLATFORM_ID);

  loading = signal(true);
  exporting = signal(false);
  events = signal<CalendarEvent[]>([]);
  readonly skeletons = [1, 2, 3];

  ngOnInit(): void { this.load(); }

  private async load(): Promise<void> {
    const competitorId = this.auth.currentUser()?.competitorId;
    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}/calendar`);
      this.events.set(res?.data ?? []);
    } catch {
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async exportCalendar(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    this.exporting.set(true);
    try {
      const competitorId = this.auth.currentUser()?.competitorId;
      const res = await this.api.get<any>(`/competitors/${competitorId}/calendar/export`);
      const icsContent: string = res?.data ?? res;
      const blob = new Blob([icsContent], { type: 'text/calendar' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = 'alas-calendario.ics'; a.click();
      URL.revokeObjectURL(url);
    } catch { /* ignore */ } finally {
      this.exporting.set(false);
    }
  }

  flagOf(code: string): string { return FLAGS[code] ?? '🏄'; }
  dayOf(d: string): string { return d ? String(new Date(d).getDate()).padStart(2, '0') : ''; }
  monthOf(d: string): string { if (!d) return ''; const months = ['Ene','Feb','Mar','Abr','May','Jun','Jul','Ago','Sep','Oct','Nov','Dic']; return months[new Date(d).getMonth()]; }
  yearOf(d: string): string { return d ? String(new Date(d).getFullYear()) : ''; }

  dateRange(start: string, end: string): string {
    if (!start || !end) return '';
    const s = new Date(start), e = new Date(end);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${s.getDate()} - ${e.getDate()} ${months[s.getMonth()]} ${s.getFullYear()}`;
  }
}
