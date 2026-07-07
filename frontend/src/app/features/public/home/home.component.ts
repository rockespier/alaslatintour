import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { StarRatingComponent } from '../../../shared/components/star-rating/star-rating.component';
import { SurfscoresCreditComponent } from '../../../shared/components/surfscores-credit/surfscores-credit.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

interface EventCard {
  id: string;
  nombre: string;
  ciudad: string;
  pais: string;
  stars: number;
  statusPublic: string;
  fechaInicio: string;
  fechaFin: string;
}

interface ArticleCard {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  category: string;
  imageUrl: string;
  publishedAt: string;
}

interface RankingRow {
  position: number;
  name: string;
  country: string;
  flag: string;
  points: number;
  change: number;
}

const COUNTRY_FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
  VE: '🇻🇪', BO: '🇧🇴',
};

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, DecimalPipe, StarRatingComponent, SurfscoresCreditComponent, LoadingSpinnerComponent],
  template: `
    <!-- ═══ HERO ═══ -->
    <section class="hero-bg min-h-[92vh] flex items-center relative overflow-hidden">
      <div class="absolute inset-0 opacity-30 pointer-events-none"
           style="background-image: radial-gradient(rgba(0,129,198,0.15) 1px, transparent 1px); background-size: 32px 32px;"></div>

      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10 py-20">
        <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-sm md:text-base mb-4">
          Temporada 2026 · Circuito Continental de Surfistas Profesionales
        </p>
        <h1 class="font-heading font-bold leading-[0.9] text-5xl sm:text-7xl md:text-8xl">
          ALAS<br />
          <span class="text-orange-brand">TOUR</span><br />
          <span class="text-text-light/90">2026</span>
        </h1>
        <p class="mt-6 max-w-2xl text-lg md:text-xl text-text-muted font-light leading-relaxed">
          El circuito continental de surf de alto rendimiento. Doce países, dieciocho paradas, una sola pasión: las olas del continente.
        </p>
        <div class="mt-10 flex flex-col sm:flex-row gap-4">
          <a routerLink="/eventos"
             class="inline-flex items-center justify-center gap-2 px-8 py-4 bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider rounded-md transition shadow-lg shadow-orange-brand/20">
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
            </svg>
            Ver Calendario
          </a>
          <a href="#ranking"
             class="inline-flex items-center justify-center gap-2 px-8 py-4 border-2 border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider rounded-md transition">
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2z"/>
            </svg>
            Ranking Actual
          </a>
        </div>

        <!-- Quick stats -->
        <div class="mt-16 grid grid-cols-2 md:grid-cols-4 gap-6 max-w-3xl">
          <div><div class="font-heading text-3xl text-cyan-brand">18</div><div class="font-accent uppercase text-xs text-text-muted tracking-wider">Eventos</div></div>
          <div><div class="font-heading text-3xl text-cyan-brand">12</div><div class="font-accent uppercase text-xs text-text-muted tracking-wider">Países</div></div>
          <div><div class="font-heading text-3xl text-cyan-brand">340+</div><div class="font-accent uppercase text-xs text-text-muted tracking-wider">Competidores</div></div>
          <div><div class="font-heading text-3xl text-cyan-brand">6</div><div class="font-accent uppercase text-xs text-text-muted tracking-wider">Categorías</div></div>
        </div>
      </div>

      <div class="absolute bottom-0 left-0 right-0 h-24 wave-clip bg-navy-deepest"></div>
    </section>

    <!-- ═══ PRÓXIMAS PARADAS ═══ -->
    <section id="eventos" class="py-14 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        <div class="flex flex-col md:flex-row md:items-end md:justify-between mb-10 gap-4">
          <div class="flex items-center gap-3 flex-wrap">
            <h2 class="font-heading text-4xl md:text-5xl">Próximas paradas del circuito</h2>
            <span class="px-2.5 py-1 rounded-full text-xs font-accent uppercase tracking-wider bg-orange-brand/20 text-orange-brand border border-orange-brand/30">Calendario 2026</span>
          </div>
          <a routerLink="/eventos" class="font-accent uppercase text-sm text-cyan-brand hover:text-cyan-dark tracking-wider flex items-center gap-1">
            Ver todos los eventos
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"/>
            </svg>
          </a>
        </div>

        @if (loadingEvents()) {
          <app-loading-spinner label="Cargando eventos..." />
        } @else if (events().length === 0) {
          <p class="text-text-muted text-sm py-8 text-center">No hay eventos próximos disponibles.</p>
        } @else {
          <div class="flex gap-5 overflow-x-auto pb-4 scroll-snap-x lg:grid lg:grid-cols-4 lg:overflow-visible">
            @for (event of events(); track event.id) {
              <article class="card-event rounded-xl p-6 min-w-[280px] lg:min-w-0 flex flex-col">
                <div class="flex items-center justify-between mb-4">
                  <span class="text-3xl">{{ flagOf(event.pais) }}</span>
                  <span [class]="statusClass(event.statusPublic)"
                        class="px-2.5 py-1 rounded-full text-xs font-accent uppercase tracking-wider">
                    {{ event.statusPublic }}
                  </span>
                </div>
                <h3 class="font-heading text-2xl leading-tight mb-2">{{ event.nombre }}</h3>
                <p class="text-sm text-text-muted mb-4">{{ event.ciudad }}, {{ event.pais }}</p>
                <app-star-rating [value]="event.stars" class="mb-4" />
                <div class="mt-auto pt-4 border-t border-navy-mid">
                  <p class="font-accent uppercase text-xs text-text-muted tracking-wider">
                    {{ formatDateRange(event.fechaInicio, event.fechaFin) }}
                  </p>
                </div>
                <div class="mt-3">
                  <a [routerLink]="['/inscripcion', event.id]"
                     class="block w-full text-center py-2 px-4 bg-cyan-brand/10 hover:bg-cyan-brand/20 text-cyan-brand font-accent uppercase text-xs tracking-wider rounded border border-cyan-brand/30 hover:border-cyan-brand/60 transition">
                    Ver evento
                  </a>
                </div>
              </article>
            }
          </div>
        }
      </div>
    </section>

    <!-- ═══ RANKING EN VIVO ═══ -->
    <section id="ranking" class="py-14 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-navy-deepest to-navy-dark">
      <div class="max-w-7xl mx-auto">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl overflow-hidden shadow-2xl shadow-cyan-brand/5">
          <header class="p-6 md:p-8 border-b border-navy-mid flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <div class="flex items-center gap-3 mb-2">
                <span class="live-dot"></span>
                <span class="font-accent uppercase text-success-brand tracking-[0.2em] text-xs">Actualizado</span>
              </div>
              <h2 class="font-heading text-3xl md:text-4xl">Ranking 2026</h2>
              <p class="text-sm text-text-muted mt-1">Categoría Open Hombres</p>
            </div>
          </header>

          @if (loadingRanking()) {
            <app-loading-spinner />
          } @else {
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-navy-mid/40 font-accent uppercase tracking-wider text-text-muted text-xs">
                  <tr>
                    <th class="px-4 py-3 text-left">Pos</th>
                    <th class="px-4 py-3 text-left">Surfista</th>
                    <th class="px-4 py-3 text-left">País</th>
                    <th class="px-4 py-3 text-right">Puntos</th>
                    <th class="px-4 py-3 text-right">Var.</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (row of ranking(); track row.position) {
                    <tr class="ranking-row transition">
                      <td class="px-4 py-3 font-heading text-lg"
                          [class]="row.position === 1 ? 'text-cyan-brand' : row.position <= 3 ? 'text-text-light' : 'text-text-muted'">
                        {{ row.position }}
                      </td>
                      <td class="px-4 py-3 font-medium">{{ row.name }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ row.flag }} {{ row.country }}</td>
                      <td class="px-4 py-3 text-right font-heading text-lg">{{ row.points | number }}</td>
                      <td class="px-4 py-3 text-right font-medium"
                          [class]="row.change > 0 ? 'text-success-brand' : row.change < 0 ? 'text-error-brand' : 'text-text-muted'">
                        @if (row.change > 0) { ▲ {{ row.change }} }
                        @else if (row.change < 0) { ▼ {{ -row.change }} }
                        @else { — }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }

          <footer class="p-6 flex items-center justify-between bg-navy-deepest/40 border-t border-navy-mid">
            <app-surfscores-credit />
            <a routerLink="/ranking"
               class="px-6 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition">
              Ver Ranking Completo
            </a>
          </footer>
        </div>
      </div>
    </section>

    <!-- ═══ ÚLTIMAS NOTICIAS ═══ -->
    <section class="py-14 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        <div class="flex flex-col md:flex-row md:items-end md:justify-between mb-10 gap-4">
          <h2 class="font-heading text-4xl md:text-5xl">Últimas Noticias</h2>
          <a routerLink="/noticias" class="font-accent uppercase text-sm text-cyan-brand hover:text-cyan-dark tracking-wider">
            Ver todas →
          </a>
        </div>

        @if (loadingArticles()) {
          <app-loading-spinner />
        } @else if (articles().length === 0) {
          <p class="text-text-muted text-sm py-8 text-center">No hay noticias disponibles.</p>
        } @else {
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            @for (article of articles(); track article.id) {
              <article class="bg-navy-dark rounded-xl overflow-hidden border border-navy-mid hover:border-cyan-brand/40 transition group">
                <div class="h-48 bg-gradient-to-br from-cyan-brand/30 via-navy-mid to-orange-brand/30 relative overflow-hidden">
                  @if (article.imageUrl) {
                    <img [src]="article.imageUrl" [alt]="article.title" class="object-cover w-full h-full">
                  }
                  <span class="absolute top-3 left-3 px-3 py-1 font-accent uppercase text-xs tracking-wider rounded"
                        [class]="categoryBadgeClass(article.category)">
                    {{ article.category }}
                  </span>
                </div>
                <div class="p-6">
                  <h3 class="font-heading text-xl mb-2 group-hover:text-cyan-brand transition leading-snug">
                    {{ article.title }}
                  </h3>
                  <p class="text-sm text-text-muted mb-4 line-clamp-2">{{ article.excerpt }}</p>
                  <div class="flex items-center justify-between text-xs text-text-muted">
                    <span>{{ formatDate(article.publishedAt) }}</span>
                    <a [routerLink]="['/noticias', article.slug]"
                       class="text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                      Leer más →
                    </a>
                  </div>
                </div>
              </article>
            }
          </div>
        }
      </div>
    </section>
  `,
})
export class HomeComponent implements OnInit {
  private api = inject(ApiService);
  private title = inject(Title);
  private meta = inject(Meta);
  private platformId = inject(PLATFORM_ID);

  events = signal<EventCard[]>([]);
  articles = signal<ArticleCard[]>([]);
  ranking = signal<RankingRow[]>(this.mockRanking());
  loadingEvents = signal(true);
  loadingArticles = signal(true);
  loadingRanking = signal(false);

  ngOnInit(): void {
    this.title.setTitle('ALAS Latin Tour 2026 — Circuito Continental de Surf Profesional');
    this.meta.updateTag({ name: 'description', content: 'Plataforma oficial del ALAS Latin Tour. Calendario de eventos, ranking en vivo, noticias y registro de competidores del circuito continental de surf.' });
    this.meta.updateTag({ property: 'og:title', content: 'ALAS Latin Tour 2026' });
    this.meta.updateTag({ property: 'og:description', content: 'El circuito continental de surf de alto rendimiento.' });
    this.loadEvents();
    this.loadArticles();
  }

  private async loadEvents(): Promise<void> {
    try {
      const res = await this.api.get<any>('/events?limit=4&page=1');
      this.events.set(res?.data ?? []);
    } catch {
      this.events.set([]);
    } finally {
      this.loadingEvents.set(false);
    }
  }

  private async loadArticles(): Promise<void> {
    try {
      const res = await this.api.get<any>('/articles?limit=3&featured=true');
      this.articles.set(res?.data ?? []);
    } catch {
      this.articles.set([]);
    } finally {
      this.loadingArticles.set(false);
    }
  }

  flagOf(countryCode: string): string {
    return COUNTRY_FLAGS[countryCode] ?? '🏄';
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      'Inscripciones Abiertas': 'bg-success-brand/20 text-success-brand border border-success-brand/30',
      'Próximamente': 'bg-warning-brand/20 text-warning-brand border border-warning-brand/30',
      'Completado': 'bg-gray-500/20 text-gray-400 border border-gray-500/30',
      'Cerrado': 'bg-gray-500/20 text-gray-400 border border-gray-500/30',
    };
    return map[status] ?? 'bg-orange-brand/20 text-orange-brand border border-orange-brand/30';
  }

  categoryBadgeClass(category: string): string {
    const map: Record<string, string> = {
      'Resultados': 'bg-orange-brand text-white',
      'Circuito': 'bg-cyan-brand text-navy-deepest',
      'Entrevista': 'bg-warning-brand text-navy-deepest',
    };
    return map[category] ?? 'bg-navy-mid text-text-light';
  }

  formatDateRange(start: string, end: string): string {
    if (!start) return '';
    const s = new Date(start);
    const e = new Date(end);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `Del ${s.getDate()} al ${e.getDate()} de ${months[s.getMonth()]}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${d.getDate()} ${months[d.getMonth()]}, ${d.getFullYear()}`;
  }

  private mockRanking(): RankingRow[] {
    return [
      { position: 1, name: 'Gabriel Villani',  country: 'Brasil',     flag: '🇧🇷', points: 12840, change: 2 },
      { position: 2, name: 'Mateo Díaz',        country: 'Perú',       flag: '🇵🇪', points: 11925, change: -1 },
      { position: 3, name: 'Sebastián Mora',    country: 'Chile',      flag: '🇨🇱', points: 10460, change: 1 },
      { position: 4, name: 'Tomás Herrera',     country: 'Argentina',  flag: '🇦🇷', points: 9815,  change: 0 },
      { position: 5, name: 'Lucas Vieira',      country: 'Brasil',     flag: '🇧🇷', points: 9210,  change: 3 },
      { position: 6, name: 'Diego Núñez',       country: 'México',     flag: '🇲🇽', points: 8770,  change: -2 },
      { position: 7, name: 'Rodrigo Alas',      country: 'Costa Rica', flag: '🇨🇷', points: 8205,  change: 1 },
      { position: 8, name: 'Pablo Medina',      country: 'Ecuador',    flag: '🇪🇨', points: 7890,  change: 0 },
    ];
  }
}
