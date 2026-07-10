import { Component, inject, signal, OnInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';
import { RankingService, RankingCategory, RankingRow } from '../../../core/services/ranking.service';
import { SurfscoresCreditComponent } from '../../../shared/components/surfscores-credit/surfscores-credit.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-ranking',
  standalone: true,
  imports: [DecimalPipe, SurfscoresCreditComponent, LoadingSpinnerComponent, PaginationComponent],
  template: `
    <!-- Hero -->
    <section class="bg-gradient-to-b from-navy-dark to-navy-deepest pt-12 pb-8 px-4 sm:px-6 lg:px-8 border-b border-navy-mid">
      <div class="max-w-7xl mx-auto">
        <p class="font-accent uppercase tracking-[0.25em] text-cyan-brand text-xs mb-2">
          Circuito Continental · Temporada {{ currentYear }}
        </p>
        <h1 class="font-heading text-5xl md:text-6xl mb-2">Ranking {{ currentYear }}</h1>
        <p class="text-text-muted text-sm max-w-xl">
          Posiciones actualizadas desde SurfScores. Los puntos se calculan con base en los resultados
          de cada parada del circuito.
        </p>
      </div>
    </section>

    <section class="py-10 px-4 sm:px-6 lg:px-8 bg-navy-deepest min-h-[60vh]">
      <div class="max-w-7xl mx-auto">

        <!-- Category tabs -->
        @if (categories().length > 0) {
          <div class="flex gap-2 flex-wrap mb-6">
            @for (cat of categories(); track cat.id) {
              <button
                (click)="selectCategory(cat)"
                [class]="selectedCatId() === cat.id
                  ? 'px-4 py-2 rounded-full font-accent uppercase text-xs tracking-wider bg-cyan-brand text-navy-deepest'
                  : 'px-4 py-2 rounded-full font-accent uppercase text-xs tracking-wider border border-navy-mid text-text-muted hover:border-cyan-brand/50 hover:text-text-light transition'">
                {{ cat.nombre }}
              </button>
            }
          </div>
        }

        <!-- Year selector (only if multiple years) -->
        @if (availableYears().length > 1) {
          <div class="flex items-center gap-3 mb-6">
            <span class="font-accent uppercase text-xs text-text-muted tracking-wider">Temporada:</span>
            <div class="flex gap-2">
              @for (y of availableYears(); track y) {
                <button
                  (click)="selectYear(y)"
                  [class]="selectedYear() === y
                    ? 'px-3 py-1 rounded font-accent text-xs bg-orange-brand text-white'
                    : 'px-3 py-1 rounded font-accent text-xs border border-navy-mid text-text-muted hover:border-orange-brand/50 transition'">
                  {{ y }}
                </button>
              }
            </div>
          </div>
        }

        <!-- Ranking card -->
        <div class="bg-navy-dark border border-navy-mid rounded-2xl overflow-hidden shadow-2xl shadow-cyan-brand/5">

          <header class="p-5 md:p-6 border-b border-navy-mid flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div class="flex items-center gap-3">
              <span class="live-dot"></span>
              <div>
                <p class="font-accent uppercase text-success-brand tracking-[0.2em] text-xs">
                  @if (cachedAgo()) { {{ cachedAgo() }} } @else { En vivo }
                </p>
                <p class="font-heading text-xl">{{ selectedCatName() }}</p>
              </div>
            </div>
            <p class="text-xs text-text-muted">
              {{ totalItems() }} competidores
            </p>
          </header>

          @if (loading()) {
            <div class="p-8">
              <app-loading-spinner label="Cargando ranking..." />
            </div>
          } @else if (error()) {
            <div class="p-10 text-center">
              <p class="text-error-brand mb-3">No se pudo cargar el ranking.</p>
              <button (click)="reload()"
                class="px-5 py-2 border border-cyan-brand text-cyan-brand font-accent uppercase text-xs tracking-wider rounded hover:bg-cyan-brand hover:text-navy-deepest transition">
                Reintentar
              </button>
            </div>
          } @else if (rows().length === 0) {
            <p class="text-text-muted text-sm py-12 text-center">Sin datos de ranking disponibles.</p>
          } @else {
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-navy-mid/40 font-accent uppercase tracking-wider text-text-muted text-xs">
                  <tr>
                    <th class="px-4 py-3 text-left w-12">Pos</th>
                    <th class="px-4 py-3 text-left">Surfista</th>
                    <th class="px-4 py-3 text-left hidden sm:table-cell">País</th>
                    <th class="px-4 py-3 text-right">Puntos</th>
                    <th class="px-4 py-3 text-right hidden md:table-cell">Eventos</th>
                    <th class="px-4 py-3 text-right">Var.</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (row of rows(); track row.position) {
                    <tr class="ranking-row transition">
                      <td class="px-4 py-3 font-heading text-lg"
                          [class]="row.position === 1 ? 'text-cyan-brand' : row.position <= 3 ? 'text-text-light' : 'text-text-muted'">
                        @if (row.position <= 3) {
                          <span class="inline-flex items-center justify-center w-7 h-7 rounded-full text-sm"
                                [class]="row.position === 1 ? 'bg-warning-brand/20 text-warning-brand' : 'bg-navy-mid/50'">
                            {{ row.position }}
                          </span>
                        } @else {
                          {{ row.position }}
                        }
                      </td>
                      <td class="px-4 py-3">
                        <div class="font-medium">{{ row.name }}</div>
                        <div class="sm:hidden text-xs text-text-muted">{{ row.flag }} {{ row.country }}</div>
                      </td>
                      <td class="px-4 py-3 text-text-muted hidden sm:table-cell">
                        {{ row.flag }} {{ row.country }}
                      </td>
                      <td class="px-4 py-3 text-right font-heading text-lg">{{ row.points | number }}</td>
                      <td class="px-4 py-3 text-right text-text-muted hidden md:table-cell">{{ row.events }}</td>
                      <td class="px-4 py-3 text-right font-medium text-sm"
                          [class]="row.change > 0 ? 'text-success-brand' : row.change < 0 ? 'text-error-brand' : 'text-text-muted'">
                        @if (row.change > 0) { ▲{{ row.change }} }
                        @else if (row.change < 0) { ▼{{ -row.change }} }
                        @else { — }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <!-- Pagination -->
            @if (totalPages() > 1) {
              <div class="px-4 py-4 border-t border-navy-mid flex justify-center">
                <app-pagination
                  [currentPage]="currentPage()"
                  [totalPages]="totalPages()"
                  (pageChange)="goToPage($event)" />
              </div>
            }
          }

          <footer class="p-5 border-t border-navy-mid bg-navy-deepest/40 flex justify-center">
            <app-surfscores-credit />
          </footer>
        </div>

      </div>
    </section>
  `,
})
export class RankingComponent implements OnInit {
  private rankingService = inject(RankingService);
  private titleSvc = inject(Title);
  private metaSvc = inject(Meta);

  readonly currentYear = new Date().getFullYear();

  categories = signal<RankingCategory[]>([]);
  selectedCatId = signal('');
  selectedCatName = signal('');
  availableYears = signal<number[]>([]);
  selectedYear = signal(this.currentYear);

  rows = signal<RankingRow[]>([]);
  loading = signal(true);
  error = signal(false);
  cachedAgo = signal('');
  currentPage = signal(1);
  totalPages = signal(1);
  totalItems = signal(0);

  ngOnInit(): void {
    this.titleSvc.setTitle('Ranking ALAS Latin Tour 2026 — Posiciones del Circuito');
    this.metaSvc.updateTag({ name: 'description', content: 'Ranking oficial del ALAS Latin Tour 2026. Posiciones actualizadas de surfistas profesionales del circuito latinoamericano.' });
    this.init();
  }

  private async init(): Promise<void> {
    try {
      const cats = await this.rankingService.getCategories();
      this.categories.set(cats);
      if (cats.length > 0) {
        const first = cats[0];
        const years = first.availableYears ?? [];
        const defaultYear = years.at(-1) ?? this.currentYear;
        this.selectedCatId.set(first.id);
        this.selectedCatName.set(first.nombre);
        this.availableYears.set(years);
        this.selectedYear.set(defaultYear);
        await this.fetchRanking(first.id, defaultYear, 1);
      } else {
        this.loading.set(false);
      }
    } catch {
      this.error.set(true);
      this.loading.set(false);
    }
  }

  async selectCategory(cat: RankingCategory): Promise<void> {
    if (cat.id === this.selectedCatId()) return;
    this.selectedCatId.set(cat.id);
    this.selectedCatName.set(cat.nombre);
    const years = cat.availableYears ?? [];
    const defaultYear = years.at(-1) ?? this.currentYear;
    this.availableYears.set(years);
    this.selectedYear.set(defaultYear);
    await this.fetchRanking(cat.id, defaultYear, 1);
  }

  async selectYear(year: number): Promise<void> {
    if (year === this.selectedYear()) return;
    this.selectedYear.set(year);
    await this.fetchRanking(this.selectedCatId(), year, 1);
  }

  async goToPage(page: number): Promise<void> {
    await this.fetchRanking(this.selectedCatId(), this.selectedYear(), page);
  }

  reload(): void {
    this.error.set(false);
    this.fetchRanking(this.selectedCatId(), this.selectedYear(), this.currentPage());
  }

  private async fetchRanking(catId: string, year: number, page: number): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const result = await this.rankingService.getRanking(catId, year, page, 20);
      this.rows.set(result.rows);
      this.currentPage.set(result.currentPage);
      this.totalPages.set(result.totalPages);
      this.totalItems.set(result.totalItems);
      this.cachedAgo.set(this.rankingService.cachedAgo(result.cachedAt));
    } catch {
      this.error.set(true);
      this.rows.set([]);
    } finally {
      this.loading.set(false);
    }
  }
}
