import { Component, inject, signal, input, OnInit, effect } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';

interface ArticleDetail {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  content: string;
  category: string;
  imageUrl?: string;
  publishedAt: string;
  featured?: boolean;
  readingTime?: number;
  author?: { name: string; role: string };
  tags?: string[];
}

interface RelatedArticle {
  id: string;
  slug: string;
  title: string;
  category: string;
  imageUrl?: string;
  publishedAt: string;
}

@Component({
  selector: 'app-noticia-detalle',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (loading()) {
      <!-- SKELETON -->
      <article class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-10 pb-20">
        <div class="skeleton h-3 rounded w-48 mb-6"></div>
        <div class="space-y-4 mb-8">
          <div class="skeleton h-10 rounded w-full"></div>
          <div class="skeleton h-10 rounded w-4/5"></div>
          <div class="skeleton h-10 rounded w-2/3"></div>
        </div>
        <div class="skeleton h-3 rounded w-48 mb-8"></div>
        <div class="skeleton h-80 rounded-2xl mb-10"></div>
        <div class="space-y-4">
          @for (line of [1,2,3,4,5,6,7,8]; track line) {
            <div class="skeleton h-4 rounded" [style.width]="line % 3 === 0 ? '80%' : '100%'"></div>
          }
        </div>
      </article>
    } @else if (notFound()) {
      <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-24 text-center">
        <p class="font-heading text-3xl text-text-muted mb-4">Artículo no encontrado</p>
        <a routerLink="/noticias" class="text-cyan-brand font-accent uppercase tracking-wider">← Volver a Noticias</a>
      </div>
    } @else {
      <article class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-10 pb-20">
        <!-- Breadcrumb -->
        <nav class="text-xs text-text-muted font-accent uppercase tracking-wider mb-6">
          <a routerLink="/noticias" class="hover:text-cyan-brand">Noticias</a>
          <span class="mx-2">/</span>
          <span [class]="categoryClass(article()!.category)">{{ article()!.category }}</span>
        </nav>

        <!-- Header -->
        <header class="mb-8">
          <h1 class="font-heading text-4xl md:text-5xl lg:text-6xl leading-[1.05] mb-6">
            {{ article()!.title }}
          </h1>
          <div class="flex flex-wrap items-center gap-4 text-sm text-text-muted">
            @if (article()!.author) {
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-full bg-gradient-to-br from-cyan-brand to-navy-mid flex items-center justify-center font-heading text-sm text-white">
                  {{ initials(article()!.author!.name) }}
                </div>
                <div>
                  <p class="text-text-light">{{ article()!.author!.name }}</p>
                  <p class="text-xs">{{ article()!.author!.role }}</p>
                </div>
              </div>
              <span class="hidden sm:inline text-navy-mid">•</span>
            }
            <span>{{ formatDate(article()!.publishedAt) }}</span>
            @if (article()!.readingTime) {
              <span class="hidden sm:inline text-navy-mid">•</span>
              <span>{{ article()!.readingTime }} min de lectura</span>
            }
            <!-- Share buttons -->
            <div class="ml-auto flex items-center gap-2">
              <button class="w-9 h-9 rounded-md bg-navy-dark border border-navy-mid hover:border-cyan-brand hover:text-cyan-brand transition flex items-center justify-center text-text-muted"
                      aria-label="Compartir">
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m9.032 4.026a3 3 0 10-2.684 0m0-11.052a3 3 0 102.684 0M6.316 10.658L13 7m0 10l-6.684-3.658"/>
                </svg>
              </button>
            </div>
          </div>
        </header>

        <!-- Hero image -->
        @if (article()!.imageUrl) {
          <figure class="mb-10 rounded-2xl overflow-hidden border border-navy-mid">
            <div class="h-64 sm:h-80 md:h-[420px] relative">
              <img [src]="article()!.imageUrl" [alt]="article()!.title" class="object-cover w-full h-full">
              <span class="absolute bottom-4 right-4 px-3 py-1.5 bg-navy-deepest/80 text-xs text-text-muted rounded backdrop-blur">
                Foto: ALAS Media
              </span>
            </div>
          </figure>
        } @else {
          <figure class="mb-10 rounded-2xl overflow-hidden border border-navy-mid">
            <div class="h-64 sm:h-80 md:h-[420px] bg-gradient-to-br from-cyan-brand/30 via-navy-mid to-orange-brand/30"></div>
          </figure>
        }

        <!-- Body -->
        <div class="prose-article" [innerHTML]="safeContent()"></div>

        <!-- Tags -->
        @if (article()!.tags?.length) {
          <div class="mt-10 flex flex-wrap gap-2">
            @for (tag of article()!.tags!; track tag) {
              <span class="px-3 py-1 rounded-full bg-navy-dark border border-navy-mid text-xs text-text-muted">
                #{{ tag }}
              </span>
            }
          </div>
        }
      </article>

      <!-- RELATED -->
      @if (related().length) {
        <section class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 pb-20">
          <div class="border-t border-navy-mid pt-12">
            <h2 class="font-heading text-3xl mb-8">También te puede interesar</h2>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
              @for (rel of related(); track rel.id) {
                <a [routerLink]="['/noticias', rel.slug]"
                   class="bg-navy-dark rounded-xl overflow-hidden border border-navy-mid hover:border-cyan-brand/40 transition group">
                  <div class="h-40 bg-gradient-to-br from-cyan-brand/20 via-navy-mid to-orange-brand/20 overflow-hidden">
                    @if (rel.imageUrl) {
                      <img [src]="rel.imageUrl" [alt]="rel.title" class="object-cover w-full h-full">
                    }
                  </div>
                  <div class="p-5">
                    <span class="font-accent uppercase tracking-wider text-xs text-cyan-brand">{{ rel.category }}</span>
                    <h3 class="font-heading text-lg mt-2 mb-2 group-hover:text-cyan-brand transition leading-snug">
                      {{ rel.title }}
                    </h3>
                    <p class="text-xs text-text-muted">{{ formatDate(rel.publishedAt) }}</p>
                  </div>
                </a>
              }
            </div>
          </div>
        </section>
      }
    }
  `,
})
export class NoticiaDetalleComponent implements OnInit {
  readonly slug = input<string>('');

  private api = inject(ApiService);
  private titleSvc = inject(Title);
  private meta = inject(Meta);

  loading = signal(true);
  notFound = signal(false);
  article = signal<ArticleDetail | null>(null);
  related = signal<RelatedArticle[]>([]);

  constructor() {
    effect(() => {
      const s = this.slug();
      if (s) this.loadArticle(s);
    });
  }

  ngOnInit(): void {
    const s = this.slug();
    if (s) this.loadArticle(s);
  }

  private async loadArticle(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const res = await this.api.get<any>(`/articles/${slug}`);
      const data: ArticleDetail = res?.data ?? res;
      if (!data?.id) { this.notFound.set(true); return; }
      this.article.set(data);
      this.setMeta(data);
      this.loadRelated(data.category, data.id);
    } catch (err: any) {
      if (err?.status === 404) this.notFound.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadRelated(category: string, excludeId: string): Promise<void> {
    try {
      const res = await this.api.get<any>(`/articles?category=${encodeURIComponent(category)}&limit=4`);
      const items: RelatedArticle[] = (res?.data ?? []).filter((a: any) => a.id !== excludeId).slice(0, 3);
      this.related.set(items);
    } catch {
      this.related.set([]);
    }
  }

  private setMeta(a: ArticleDetail): void {
    this.titleSvc.setTitle(`${a.title} — ALAS Latin Tour`);
    this.meta.updateTag({ name: 'description', content: a.excerpt });
    this.meta.updateTag({ property: 'og:title', content: a.title });
    this.meta.updateTag({ property: 'og:description', content: a.excerpt });
    this.meta.updateTag({ property: 'og:type', content: 'article' });
    if (a.imageUrl) this.meta.updateTag({ property: 'og:image', content: a.imageUrl });
  }

  safeContent(): string {
    const content = this.article()?.content ?? '';
    if (!content) return `<p>${this.article()?.excerpt ?? ''}</p>`;
    if (content.startsWith('<')) return content;
    return content.split('\n\n').map((p: string) => `<p>${p}</p>`).join('');
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      'Resultados': 'text-success-brand',
      'Circuito': 'text-cyan-brand',
      'Entrevista': 'text-orange-brand',
      'Reglamento': 'text-warning-brand',
    };
    return map[cat] ?? 'text-text-muted';
  }

  initials(name: string): string {
    return name.split(' ').slice(0, 2).map(n => n[0]).join('').toUpperCase();
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${d.getDate()} de ${months[d.getMonth()]}, ${d.getFullYear()}`;
  }
}
