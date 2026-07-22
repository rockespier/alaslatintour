import { Component, inject, signal, computed, OnInit, afterNextRender } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { ArticleSummary, mapArticleSummary } from '../../../core/models/article';
import { GalleryCard } from '../../../core/models/gallery';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type Tab = 'todas' | 'noticias' | 'resultados' | 'fotos' | 'videos';
type GalleryTab = 'fotos' | 'videos';

type Article = ArticleSummary;

const CATEGORY_MAP: Record<string, string> = {
  'Resultados': 'bg-success-brand/15 text-success-brand',
  'Circuito': 'bg-cyan-brand/15 text-cyan-brand',
  'Entrevista': 'bg-orange-brand/15 text-orange-brand',
  'Reglamento': 'bg-warning-brand/15 text-warning-brand',
  'Tecnología': 'bg-navy-mid/50 text-text-muted',
};

const MOCK_VIDEOS = [
  { title: 'Final Open Hombres — Roca Bruja Classic', location: 'Lobitos, Perú', date: '14 jun 2026', duration: '18:42' },
  { title: 'Highlights Sayulita Masters 2026', location: 'Sayulita, México', date: '20 sep 2026', duration: '24:15' },
  { title: 'Entrevista: Camila Restrepo — Top 10 a los 17', location: 'Matanzas, Chile', date: '5 jun 2026', duration: '08:30' },
  { title: 'Resumen Mid-Season — Open Hombres 2026', location: 'ALAS Latin Tour', date: '25 jun 2026', duration: '35:00' },
];

@Component({
  selector: 'app-noticias',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent],
  template: `
    <!-- PAGE HEADER -->
    <section class="py-14 px-4 sm:px-6 lg:px-8 border-b border-navy-mid">
      <div class="max-w-7xl mx-auto">
        <h1 class="font-heading text-4xl md:text-6xl">Noticias y Galería</h1>
        <p class="mt-3 text-text-muted max-w-2xl">Resultados, crónicas, entrevistas y fotografía oficial del circuito latinoamericano.</p>
      </div>
    </section>

    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">

      <!-- FILTER TABS -->
      <div class="flex flex-wrap items-center gap-6 md:gap-10 border-b border-navy-mid mb-10 font-accent uppercase tracking-wider text-sm text-text-muted">
        @for (t of tabs; track t.key) {
          <button (click)="activeTab.set(t.key)"
                  [class]="activeTab() === t.key ? 'tab-btn active' : 'tab-btn hover:text-text-light'">
            {{ t.label }}
          </button>
        }
      </div>

      <!-- FEATURED ARTICLE -->
      @if (loading()) {
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-0 bg-navy-dark border border-navy-mid rounded-2xl overflow-hidden mb-14">
          <div class="h-64 lg:h-auto skeleton"></div>
          <div class="p-8 lg:p-10 space-y-4">
            <div class="skeleton h-3 rounded w-1/4"></div>
            <div class="skeleton h-8 rounded w-5/6"></div>
            <div class="skeleton h-8 rounded w-4/6"></div>
            <div class="skeleton h-4 rounded w-full mt-4"></div>
            <div class="skeleton h-4 rounded w-full"></div>
            <div class="skeleton h-4 rounded w-3/4"></div>
          </div>
        </div>
      } @else if (featured()) {
        <article class="grid grid-cols-1 lg:grid-cols-2 gap-0 bg-navy-dark border border-navy-mid rounded-2xl overflow-hidden mb-14 hover:border-cyan-brand/40 transition">
          <div class="h-64 lg:h-auto bg-gradient-to-br from-cyan-brand/30 via-navy-mid to-orange-brand/40 relative">
            <span class="absolute top-4 left-4 px-3 py-1 bg-orange-brand text-white font-accent uppercase text-xs tracking-wider rounded z-10">Destacado</span>
            @if (featured()!.imageUrl) {
              <img [src]="featured()!.imageUrl" [alt]="featured()!.title" referrerpolicy="no-referrer" class="object-cover w-full h-full">
            }
          </div>
          <div class="p-8 lg:p-10 flex flex-col justify-center">
            <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs mb-3">
              {{ featured()!.category }} · ALAS Latin Tour
            </p>
            <h2 class="font-heading text-3xl md:text-4xl leading-tight mb-4">{{ featured()!.title }}</h2>
            <p class="text-text-muted leading-relaxed mb-5">{{ featured()!.excerpt }}</p>
            <div class="flex items-center justify-between text-xs text-text-muted">
              <span>{{ formatDate(featured()!.publishedAt) }}{{ featured()!.readingTime ? ' · ' + featured()!.readingTime + ' min de lectura' : '' }}</span>
              <a [routerLink]="['/noticias', featured()!.slug]" class="text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider">
                Leer artículo →
              </a>
            </div>
          </div>
        </article>
      }

      <!-- ARTICLE GRID -->
      <h2 class="font-heading text-3xl mb-6">Últimos artículos</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-10">
        @if (loading()) {
          @for (sk of skeletons; track sk) {
            <div class="bg-navy-dark rounded-xl overflow-hidden border border-navy-mid">
              <div class="h-44 skeleton"></div>
              <div class="p-5 space-y-3">
                <div class="skeleton h-3 rounded w-1/4"></div>
                <div class="skeleton h-5 rounded w-5/6"></div>
                <div class="skeleton h-3 rounded w-full"></div>
                <div class="skeleton h-3 rounded w-2/3"></div>
                <div class="skeleton h-3 rounded w-1/3 mt-2"></div>
              </div>
            </div>
          }
        } @else {
          @for (article of filteredArticles(); track article.id) {
            <article class="bg-navy-dark rounded-xl overflow-hidden border border-navy-mid hover:border-cyan-brand/40 transition group">
              <div class="h-44 bg-gradient-to-br from-navy-mid via-cyan-brand/10 to-navy-dark overflow-hidden">
                @if (article.imageUrl) {
                  <img [src]="article.imageUrl" [alt]="article.title" referrerpolicy="no-referrer" class="object-cover w-full h-full">
                }
              </div>
              <div class="p-5">
                <span class="px-2 py-0.5 text-[10px] font-accent uppercase tracking-wider rounded"
                      [class]="categoryClass(article.category)">
                  {{ article.category }}
                </span>
                <h3 class="font-heading text-lg mt-3 mb-2 group-hover:text-cyan-brand transition leading-snug">
                  {{ article.title }}
                </h3>
                <p class="text-sm text-text-muted mb-3 line-clamp-2">{{ article.excerpt }}</p>
                <div class="flex items-center justify-between text-xs text-text-muted">
                  <span>{{ formatDate(article.publishedAt) }}</span>
                  <a [routerLink]="['/noticias', article.slug]"
                     class="text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider text-[10px]">
                    Leer →
                  </a>
                </div>
              </div>
            </article>
          }
        }
      </div>

      <!-- LOAD MORE -->
      @if (!loading() && hasMore()) {
        <div class="flex justify-center mb-16">
          <button (click)="loadMore()"
                  class="px-8 py-3 bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider rounded-md transition">
            Cargar más artículos
          </button>
        </div>
      }

      <!-- GALERÍA -->
      <section>
        <div class="flex flex-col sm:flex-row sm:items-end justify-between mb-6 gap-4">
          <h2 class="font-heading text-4xl">Galería</h2>
          <div class="flex items-center gap-2">
            <button (click)="galleryTab.set('fotos')"
                    [class]="galleryTab() === 'fotos' ? 'bg-cyan-brand text-navy-deepest' : 'border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light'"
                    class="px-4 py-1.5 rounded-md font-accent uppercase text-xs tracking-wider transition flex items-center gap-1.5">
              <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
              </svg>
              Fotos
            </button>
            <button (click)="galleryTab.set('videos')"
                    [class]="galleryTab() === 'videos' ? 'bg-cyan-brand text-navy-deepest' : 'border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light'"
                    class="px-4 py-1.5 rounded-md font-accent uppercase text-xs tracking-wider transition flex items-center gap-1.5">
              <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M15 10l4.553-2.069A1 1 0 0121 8.87v6.26a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"/>
              </svg>
              Videos
            </button>
          </div>
        </div>

        @if (galleryTab() === 'fotos') {
          <p class="text-xs text-text-muted mb-5">
            Fotografía oficial del circuito. Haz clic en <strong class="text-text-light">Descargar</strong> para acceder al set completo en alta resolución.
          </p>
          @if (loadingGalleries()) {
            <app-loading-spinner label="Cargando galería..." />
          } @else if (galleries().length === 0) {
            <p class="text-text-muted text-sm py-8 text-center">No hay fotografías disponibles.</p>
          } @else {
            <div class="columns-2 sm:columns-3 lg:columns-4 gap-3">
              @for (g of galleries(); track g.id; let i = $index) {
                <a [routerLink]="['/galerias', g.slug]"
                   class="group relative overflow-hidden rounded-xl bg-navy-mid block break-inside-avoid mb-3">
                  <div class="relative overflow-hidden" [style.height]="galleryTileHeight(i)">
                    @if (g.coverImageUrl) {
                      <img [src]="g.coverImageUrl" [alt]="g.title" referrerpolicy="no-referrer"
                           class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500">
                    } @else {
                      <div class="w-full h-full bg-gradient-to-br from-navy-mid to-navy-deepest"></div>
                    }
                    <div class="absolute inset-0 p-3 flex flex-col justify-end opacity-0 group-hover:opacity-100 transition-opacity"
                         style="background:linear-gradient(180deg,transparent 40%,rgba(0,35,89,0.92))">
                      <p class="text-xs font-accent uppercase tracking-wider text-text-light mb-2 line-clamp-2">{{ g.title }}</p>
                      <div class="flex items-center justify-between">
                        <span class="text-[10px] text-text-muted">{{ g.photoCount }} fotos</span>
                        <span class="px-2 py-0.5 rounded bg-cyan-brand text-navy-deepest text-[10px] font-accent uppercase tracking-wider">Ver →</span>
                      </div>
                    </div>
                    <span class="absolute top-2 right-2 px-2 py-0.5 rounded bg-navy-deepest/70 text-[10px] font-accent uppercase tracking-wider text-text-muted">
                      {{ g.photoCount }} fotos
                    </span>
                  </div>
                </a>
              }
            </div>
          }
        }

        @if (galleryTab() === 'videos') {
          <p class="text-xs text-text-muted mb-5">Videos oficiales del circuito en 480p y 1080p.</p>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            @for (video of mockVideos; track video.title) {
              <div class="bg-navy-dark rounded-xl overflow-hidden border border-navy-mid hover:border-cyan-brand/40 transition group">
                <div class="relative h-44 bg-gradient-to-br from-navy-mid to-navy-deepest flex items-center justify-center">
                  <div class="w-14 h-14 rounded-full bg-cyan-brand/20 border-2 border-cyan-brand flex items-center justify-center">
                    <svg class="h-6 w-6 text-cyan-brand ml-1" fill="currentColor" viewBox="0 0 24 24">
                      <path d="M8 5v14l11-7z"/>
                    </svg>
                  </div>
                  <span class="absolute top-3 left-3 px-2 py-0.5 rounded bg-navy-deepest/80 text-[10px] font-accent uppercase tracking-wider text-cyan-brand">
                    {{ video.duration }}
                  </span>
                </div>
                <div class="p-4">
                  <h3 class="font-heading text-base mb-1 group-hover:text-cyan-brand transition">{{ video.title }}</h3>
                  <p class="text-xs text-text-muted mb-3">{{ video.location }} · {{ video.date }}</p>
                  <div class="flex gap-2">
                    <a href="#" class="flex-1 text-center px-3 py-1.5 rounded-md border border-navy-mid hover:border-cyan-brand text-[11px] font-accent uppercase tracking-wider text-text-muted hover:text-cyan-brand transition">
                      480p
                    </a>
                    <a href="#" class="flex-1 text-center px-3 py-1.5 rounded-md border border-warning-brand/40 hover:border-warning-brand text-[11px] font-accent uppercase tracking-wider text-warning-brand hover:bg-warning-brand/10 transition">
                      1080p
                    </a>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </section>
    </div>
  `,
})
export class NoticiasComponent implements OnInit {
  private api = inject(ApiService);
  private title = inject(Title);
  private meta = inject(Meta);

  constructor() {
    afterNextRender(() => { this.loadGalleries(); });
  }

  activeTab = signal<Tab>('todas');
  galleryTab = signal<GalleryTab>('fotos');
  loading = signal(true);
  articles = signal<Article[]>([]);
  featured = signal<Article | null>(null);
  page = signal(1);
  hasMore = signal(false);
  skeletons = [1, 2, 3, 4, 5, 6];

  loadingGalleries = signal(false);
  galleries = signal<GalleryCard[]>([]);
  mockVideos = MOCK_VIDEOS;

  tabs: { key: Tab; label: string }[] = [
    { key: 'todas', label: 'Todas' },
    { key: 'noticias', label: 'Noticias' },
    { key: 'resultados', label: 'Resultados' },
    { key: 'fotos', label: 'Fotos' },
    { key: 'videos', label: 'Videos' },
  ];

  filteredArticles = computed(() => {
    const tab = this.activeTab();
    const list = this.articles();
    if (tab === 'todas') return list;
    const map: Record<Tab, string> = {
      todas: '',
      noticias: 'Circuito',
      resultados: 'Resultados',
      fotos: 'Fotografía',
      videos: 'Video',
    };
    return list.filter(a => a.category === map[tab]);
  });

  ngOnInit(): void {
    this.title.setTitle('Noticias y Galería — ALAS Latin Tour');
    this.meta.updateTag({ name: 'description', content: 'Resultados, crónicas, entrevistas y fotografía oficial del circuito latinoamericano de surf profesional ALAS Latin Tour.' });
    this.meta.updateTag({ property: 'og:title', content: 'Noticias — ALAS Latin Tour' });
    this.loadArticles();
  }

  private async loadGalleries(retriesLeft = 1): Promise<void> {
    this.loadingGalleries.set(true);
    try {
      const res = await this.api.get<any>('/galleries');
      this.galleries.set((res as any)?.data ?? []);
      this.loadingGalleries.set(false);
    } catch {
      if (retriesLeft > 0) {
        this.loadGalleries(retriesLeft - 1);
        return;
      }
      this.galleries.set([]);
      this.loadingGalleries.set(false);
    }
  }

  private async loadArticles(append = false): Promise<void> {
    if (!append) this.loading.set(true);
    try {
      const res = await this.api.get<any>(`/articles?page=${this.page()}&limit=7`);
      const raw: any[] = res?.data ?? [];
      const all = raw.map(mapArticleSummary);

      if (!append) {
        const featuredIndex = all.findIndex(a => a.featured);
        if (featuredIndex !== -1) {
          this.featured.set(all[featuredIndex]);
          this.articles.set(all.filter((_, i) => i !== featuredIndex));
        } else {
          this.featured.set(null);
          this.articles.set(all);
        }
      } else {
        this.articles.update(prev => [...prev, ...all]);
      }

      const pagination = res?.pagination;
      this.hasMore.set(pagination ? this.page() < pagination.totalPages : false);
    } catch {
      this.articles.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async loadMore(): Promise<void> {
    this.page.update(p => p + 1);
    await this.loadArticles(true);
  }

  private readonly TILE_HEIGHTS = ['180px', '240px', '200px', '260px', '190px', '220px'];
  galleryTileHeight(i: number): string {
    return this.TILE_HEIGHTS[i % this.TILE_HEIGHTS.length];
  }

  categoryClass(cat: string): string {
    return CATEGORY_MAP[cat] ?? 'bg-navy-mid/50 text-text-muted';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${d.getDate()} ${months[d.getMonth()]}, ${d.getFullYear()}`;
  }
}
