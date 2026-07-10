import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { StarRatingComponent } from '../../../../shared/components/star-rating/star-rating.component';

interface PointEntry {
  eventoNombre: string;
  eventoPais: string;
  eventoPaisFlag: string;
  eventStars: number;
  categoria: string;
  posicion: number;
  puntos: number;
  fecha: string;
}

const FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
};

@Component({
  selector: 'app-historial-puntos',
  standalone: true,
  imports: [DecimalPipe, StarRatingComponent],
  template: `
    <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
      <h2 class="font-heading text-3xl">Historial de Puntos</h2>

      <!-- Year selector -->
      <div class="flex gap-2">
        @for (y of availableYears; track y) {
          <button (click)="selectedYear.set(y)"
                  class="px-4 py-1.5 rounded font-accent text-xs transition"
                  [class]="selectedYear() === y ? 'bg-orange-brand text-white' : 'border border-navy-mid text-text-muted hover:border-orange-brand/50'">
            {{ y }}
          </button>
        }
      </div>
    </div>

    <!-- Total points card -->
    @if (!loading() && history().length > 0) {
      <div class="bg-gradient-to-r from-cyan-brand/10 to-navy-mid border border-cyan-brand/20 rounded-2xl p-6 mb-6 flex items-center justify-between">
        <div>
          <p class="font-accent uppercase text-xs text-cyan-brand tracking-wider mb-1">Puntos acumulados {{ selectedYear() }}</p>
          <p class="font-heading text-5xl">{{ totalPoints() | number }}</p>
        </div>
        <div class="text-right">
          <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">Eventos</p>
          <p class="font-heading text-3xl">{{ history().length }}</p>
        </div>
      </div>
    }

    @if (loading()) {
      <div class="space-y-3">
        @for (sk of skeletons; track sk) { <div class="skeleton h-16 rounded-xl"></div> }
      </div>
    } @else if (history().length === 0) {
      <div class="text-center py-16">
        <p class="font-heading text-xl text-text-muted mb-2">Sin historial para {{ selectedYear() }}</p>
        <p class="text-sm text-text-muted">Los puntos se registran al completar eventos del circuito.</p>
      </div>
    } @else {
      <div class="overflow-x-auto rounded-2xl border border-navy-mid">
        <table class="w-full text-sm">
          <thead class="bg-navy-mid/40 font-accent uppercase tracking-wider text-text-muted text-xs">
            <tr>
              <th class="px-5 py-3 text-left">Evento</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">Categoría</th>
              <th class="px-4 py-3 text-center hidden md:table-cell">Estrellas</th>
              <th class="px-4 py-3 text-center">Posición</th>
              <th class="px-4 py-3 text-right">Puntos</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-navy-mid/50">
            @for (entry of history(); track entry.eventoNombre + entry.fecha) {
              <tr class="hover:bg-navy-mid/20 transition">
                <td class="px-5 py-4">
                  <div class="flex items-center gap-2">
                    <span class="text-lg">{{ flagOf(entry.eventoPais) }}</span>
                    <div>
                      <p class="font-medium leading-tight">{{ entry.eventoNombre }}</p>
                      <p class="text-xs text-text-muted">{{ formatDate(entry.fecha) }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-4 py-4 text-text-muted hidden sm:table-cell">{{ entry.categoria }}</td>
                <td class="px-4 py-4 hidden md:table-cell">
                  <div class="flex justify-center">
                    <app-star-rating [value]="entry.eventStars" />
                  </div>
                </td>
                <td class="px-4 py-4 text-center">
                  <span class="font-heading text-xl"
                        [class]="entry.posicion <= 3 ? 'text-cyan-brand' : ''">
                    #{{ entry.posicion }}
                  </span>
                </td>
                <td class="px-4 py-4 text-right font-heading text-lg text-cyan-brand">{{ entry.puntos | number }}</td>
              </tr>
            }
          </tbody>
          <tfoot class="bg-navy-mid/20 font-accent uppercase text-xs tracking-wider">
            <tr>
              <td colspan="4" class="px-5 py-3 text-text-muted hidden md:table-cell">Total temporada {{ selectedYear() }}</td>
              <td colspan="4" class="px-5 py-3 text-text-muted md:hidden">Total</td>
              <td class="px-4 py-3 text-right font-heading text-xl text-cyan-brand">{{ totalPoints() | number }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    }
  `,
})
export class HistorialPuntosComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  readonly currentYear = new Date().getFullYear();
  availableYears = [this.currentYear, this.currentYear - 1];
  selectedYear = signal(this.currentYear);

  loading = signal(true);
  history = signal<PointEntry[]>([]);
  totalPoints = computed(() => this.history().reduce((sum, e) => sum + e.puntos, 0));
  readonly skeletons = [1, 2, 3, 4, 5];

  ngOnInit(): void { this.load(); }

  private async load(): Promise<void> {
    const userId = this.auth.currentUser()?.id;
    this.loading.set(true);
    try {
      const res = await this.api.get<any>(`/competitors/${userId}/points-history?year=${this.selectedYear()}&limit=50`);
      this.history.set(res?.data ?? []);
    } catch {
      this.history.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  flagOf(code: string): string { return FLAGS[code] ?? '🏄'; }

  formatDate(d: string): string {
    if (!d) return '';
    const date = new Date(d);
    const months = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    return `${date.getDate()} ${months[date.getMonth()]} ${date.getFullYear()}`;
  }
}
