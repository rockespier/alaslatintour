import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';
import { StarRatingComponent } from '../../../../shared/components/star-rating/star-rating.component';
import { TranslocoModule, provideTranslocoScope } from '@jsverse/transloco';
import { LocaleFormatService } from '../../../../core/i18n/locale-format.service';

interface PointEntry {
  eventoNombre: string;
  ubicacion: string;
  stars: number;
  categoria: string;
  puesto: string;
  puntos: number;
  fechaInicio: string;
}

@Component({
  selector: 'app-historial-puntos',
  standalone: true,
  imports: [DecimalPipe, StarRatingComponent, TranslocoModule],
  providers: [provideTranslocoScope('competitor')],
  template: `
    <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
      <h2 class="font-heading text-3xl">{{ 'competitor.history.title' | transloco }}</h2>

      <!-- Year selector -->
      <div class="flex gap-2">
        @for (y of availableYears; track y) {
          <button (click)="selectYear(y)"
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
          <p class="font-accent uppercase text-xs text-cyan-brand tracking-wider mb-1">{{ 'competitor.history.accumulated' | transloco: { year: selectedYear() } }}</p>
          <p class="font-heading text-5xl">{{ totalPoints() | number }}</p>
        </div>
        <div class="text-right">
          <p class="font-accent uppercase text-xs text-text-muted tracking-wider mb-1">{{ 'competitor.history.events' | transloco }}</p>
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
        <p class="font-heading text-xl text-text-muted mb-2">{{ 'competitor.history.emptyTitle' | transloco: { year: selectedYear() } }}</p>
        <p class="text-sm text-text-muted">{{ 'competitor.history.emptyText' | transloco }}</p>
      </div>
    } @else {
      <div class="overflow-x-auto rounded-2xl border border-navy-mid">
        <table class="w-full text-sm">
          <thead class="bg-navy-mid/40 font-accent uppercase tracking-wider text-text-muted text-xs">
            <tr>
              <th class="px-5 py-3 text-left">{{ 'competitor.history.event' | transloco }}</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">{{ 'competitor.history.category' | transloco }}</th>
              <th class="px-4 py-3 text-center hidden md:table-cell">{{ 'competitor.history.stars' | transloco }}</th>
              <th class="px-4 py-3 text-center">{{ 'competitor.history.position' | transloco }}</th>
              <th class="px-4 py-3 text-right">{{ 'competitor.history.points' | transloco }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-navy-mid/50">
            @for (entry of history(); track entry.eventoNombre + entry.fechaInicio) {
              <tr class="hover:bg-navy-mid/20 transition">
                <td class="px-5 py-4">
                  <div>
                    <p class="font-medium leading-tight">{{ entry.eventoNombre }}</p>
                    <p class="text-xs text-text-muted">{{ entry.ubicacion }} · {{ formatDate(entry.fechaInicio) }}</p>
                  </div>
                </td>
                <td class="px-4 py-4 text-text-muted hidden sm:table-cell">{{ entry.categoria }}</td>
                <td class="px-4 py-4 hidden md:table-cell">
                  <div class="flex justify-center">
                    <app-star-rating [value]="entry.stars" />
                  </div>
                </td>
                <td class="px-4 py-4 text-center">
                  <span class="font-heading text-xl"
                        [class]="isTopResult(entry.puesto) ? 'text-cyan-brand' : ''">
                    #{{ entry.puesto }}
                  </span>
                </td>
                <td class="px-4 py-4 text-right font-heading text-lg text-cyan-brand">{{ entry.puntos | number }}</td>
              </tr>
            }
          </tbody>
          <tfoot class="bg-navy-mid/20 font-accent uppercase text-xs tracking-wider">
            <tr>
              <td colspan="4" class="px-5 py-3 text-text-muted hidden md:table-cell">{{ 'competitor.history.seasonTotal' | transloco: { year: selectedYear() } }}</td>
              <td colspan="4" class="px-5 py-3 text-text-muted md:hidden">{{ 'competitor.history.total' | transloco }}</td>
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
  private localeFormat = inject(LocaleFormatService);

  readonly currentYear = new Date().getFullYear();
  availableYears = [this.currentYear, this.currentYear - 1];
  selectedYear = signal(this.currentYear);

  loading = signal(true);
  history = signal<PointEntry[]>([]);
  totalPoints = computed(() => this.history().reduce((sum, e) => sum + e.puntos, 0));
  readonly skeletons = [1, 2, 3, 4, 5];

  ngOnInit(): void { this.load(); }

  selectYear(year: number): void {
    if (year === this.selectedYear()) return;
    this.selectedYear.set(year);
    this.load();
  }

  private async load(): Promise<void> {
    const competitorId = this.auth.currentUser()?.competitorId;
    const year = this.selectedYear();
    this.loading.set(true);
    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}/points-history?year=${year}&limit=50`);
      // Ignore a slower response for a year the user already switched away from.
      if (year === this.selectedYear()) this.history.set(res?.data ?? []);
    } catch {
      if (year === this.selectedYear()) this.history.set([]);
    } finally {
      if (year === this.selectedYear()) this.loading.set(false);
    }
  }

  isTopResult(puesto: string): boolean {
    const n = parseInt(puesto, 10);
    return !isNaN(n) && n >= 1 && n <= 3;
  }

  formatDate(d: string): string {
    return this.localeFormat.utcDate(d);
  }
}
