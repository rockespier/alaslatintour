import { Component, inject, signal, input, OnInit, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DomSanitizer, Meta, SafeHtml, Title } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { ArticleDetail, ArticleSummary, mapArticleDetail, mapArticleSummary } from '../../../core/models/article';

type RelatedArticle = ArticleSummary;

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
              <button type="button"
                      class="w-9 h-9 rounded-md bg-navy-dark border border-navy-mid hover:border-cyan-brand hover:text-cyan-brand transition flex items-center justify-center text-text-muted"
                      aria-label="Compartir en Facebook"
                      (click)="shareOn('facebook')">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M22 12.06C22 6.505 17.523 2 12 2S2 6.505 2 12.06c0 5.02 3.657 9.184 8.438 9.94v-7.03H7.898v-2.91h2.54V9.845c0-2.507 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562v1.876h2.773l-.443 2.91h-2.33V22c4.78-.756 8.438-4.92 8.438-9.94z"/>
                </svg>
              </button>
              <button type="button"
                      class="w-9 h-9 rounded-md bg-navy-dark border border-navy-mid hover:border-cyan-brand hover:text-cyan-brand transition flex items-center justify-center text-text-muted"
                      aria-label="Compartir en X"
                      (click)="shareOn('x')">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                </svg>
              </button>
              <button type="button"
                      class="w-9 h-9 rounded-md bg-navy-dark border border-navy-mid hover:border-cyan-brand hover:text-cyan-brand transition flex items-center justify-center text-text-muted"
                      aria-label="Compartir en WhatsApp"
                      (click)="shareOn('whatsapp')">
                <svg class="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12.04 2C6.58 2 2.13 6.45 2.13 11.91c0 1.75.46 3.45 1.32 4.95L2.05 22l5.29-1.39a9.87 9.87 0 0 0 4.7 1.2h.01c5.46 0 9.91-4.45 9.91-9.91 0-2.65-1.03-5.14-2.9-7.01A9.82 9.82 0 0 0 12.04 2zm0 18.13h-.01a8.2 8.2 0 0 1-4.18-1.14l-.3-.18-3.14.82.84-3.06-.2-.32a8.2 8.2 0 0 1-1.27-4.37c0-4.55 3.71-8.26 8.27-8.26a8.22 8.22 0 0 1 5.85 2.43 8.19 8.19 0 0 1 2.42 5.83c0 4.56-3.71 8.25-8.28 8.25zm4.53-6.18c-.25-.12-1.47-.72-1.7-.8-.23-.09-.39-.12-.56.12-.17.25-.64.8-.78.96-.15.17-.29.19-.53.06-.25-.12-1.05-.39-2-1.23-.74-.66-1.24-1.47-1.38-1.72-.15-.25-.02-.38.11-.51.11-.11.25-.29.37-.43.12-.15.16-.25.24-.42.08-.17.04-.31-.02-.43-.06-.12-.56-1.35-.77-1.85-.2-.48-.41-.42-.56-.43h-.48c-.17 0-.43.06-.66.31-.23.25-.86.84-.86 2.05s.88 2.38 1 2.54c.12.17 1.73 2.64 4.2 3.7.59.25 1.05.4 1.4.52.59.19 1.13.16 1.55.1.47-.07 1.47-.6 1.68-1.18.21-.58.21-1.08.15-1.18-.06-.1-.23-.16-.48-.28z"/>
                </svg>
              </button>
              <button type="button"
                      class="w-9 h-9 rounded-md bg-navy-dark border border-navy-mid hover:border-cyan-brand hover:text-cyan-brand transition flex items-center justify-center text-text-muted"
                      [attr.aria-label]="linkCopied() ? 'Enlace copiado' : 'Copiar enlace'"
                      (click)="copyLink()">
                @if (linkCopied()) {
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                  </svg>
                } @else {
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M13.828 10.172a4 4 0 010 5.656l-3 3a4 4 0 01-5.656-5.656l1.5-1.5M10.172 13.828a4 4 0 010-5.656l3-3a4 4 0 015.656 5.656l-1.5 1.5"/>
                  </svg>
                }
              </button>
            </div>
          </div>
        </header>

        <!-- Hero image -->
        @if (article()!.imageUrl) {
          <figure class="mb-10 rounded-2xl overflow-hidden border border-navy-mid">
            <div class="h-64 sm:h-80 md:h-[420px] relative">
              <img [src]="article()!.imageUrl" [alt]="article()!.title" referrerpolicy="no-referrer" class="object-cover w-full h-full">
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
                      <img [src]="rel.imageUrl" [alt]="rel.title" referrerpolicy="no-referrer" class="object-cover w-full h-full">
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
  private sanitizer = inject(DomSanitizer);
  private platformId = inject(PLATFORM_ID);

  loading = signal(true);
  notFound = signal(false);
  article = signal<ArticleDetail | null>(null);
  related = signal<RelatedArticle[]>([]);
  linkCopied = signal(false);

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
      const raw = res?.data ?? res;
      if (!raw?.id) { this.notFound.set(true); return; }
      const data = mapArticleDetail(raw);
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
      const items: RelatedArticle[] = (res?.data ?? [])
        .map(mapArticleSummary)
        .filter((a: RelatedArticle) => a.id !== excludeId)
        .slice(0, 3);
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

  safeContent(): SafeHtml {
    const content = this.article()?.content ?? '';
    const html = !content
      ? `<p>${this.article()?.excerpt ?? ''}</p>`
      : content.startsWith('<')
        ? content
        : content.split('\n\n').map((p: string) => `<p>${p}</p>`).join('');
    // WordPress-hosted images reject requests carrying a cross-origin Referer
    // header (hotlink protection), so strip it for embedded content images too.
    const withReferrerPolicy = html.replace(/<img /gi, '<img referrerpolicy="no-referrer" ');
    return this.sanitizer.bypassSecurityTrustHtml(withReferrerPolicy);
  }

  shareOn(network: 'facebook' | 'x' | 'whatsapp'): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const url = encodeURIComponent(window.location.href);
    const text = encodeURIComponent(this.article()?.title ?? '');
    const shareUrls: Record<typeof network, string> = {
      facebook: `https://www.facebook.com/sharer/sharer.php?u=${url}`,
      x: `https://twitter.com/intent/tweet?url=${url}&text=${text}`,
      whatsapp: `https://api.whatsapp.com/send?text=${text}%20${url}`,
    };
    window.open(shareUrls[network], '_blank', 'noopener,noreferrer,width=600,height=500');
  }

  async copyLink(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      await navigator.clipboard.writeText(window.location.href);
      this.linkCopied.set(true);
      setTimeout(() => this.linkCopied.set(false), 2000);
    } catch {
      // navigator.clipboard puede fallar por permisos del navegador; se ignora silenciosamente.
    }
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
