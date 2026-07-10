import { Component, inject, signal, input, OnInit, effect } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { GalleryDetail, GalleryAsset } from '../../../core/models/gallery';

@Component({
  selector: 'app-galeria-detalle',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (loading()) {
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 pt-10 pb-20">
        <div class="skeleton h-3 rounded w-40 mb-8"></div>
        <div class="skeleton h-10 rounded w-2/3 mb-4"></div>
        <div class="skeleton h-3 rounded w-1/4 mb-10"></div>
        <div class="grid grid-cols-2 md:grid-cols-3 gap-3">
          @for (sk of skeletons; track sk) { <div class="skeleton rounded-lg h-52"></div> }
        </div>
      </div>
    } @else if (notFound()) {
      <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-24 text-center">
        <p class="font-heading text-3xl text-text-muted mb-4">Galería no encontrada</p>
        <a routerLink="/noticias" class="text-cyan-brand font-accent uppercase tracking-wider">← Volver a Noticias</a>
      </div>
    } @else if (gallery()) {
      <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 pt-10 pb-20">

        <!-- Breadcrumb -->
        <nav class="text-xs text-text-muted font-accent uppercase tracking-wider mb-6">
          <a routerLink="/noticias" class="hover:text-cyan-brand">Noticias</a>
          <span class="mx-2">/</span>
          <span class="text-cyan-brand">Galería</span>
        </nav>

        <!-- Header -->
        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-10">
          <div>
            <h1 class="font-heading text-4xl md:text-5xl leading-tight mb-2">{{ gallery()!.title }}</h1>
            @if (gallery()!.eventDate) {
              <p class="text-text-muted text-sm">{{ formatDate(gallery()!.eventDate!) }}</p>
            }
            <p class="text-xs text-text-muted mt-1">
              {{ totalPhotos() }} {{ totalPhotos() === 1 ? 'foto' : 'fotos' }}
            </p>
          </div>
          @if (gallery()!.pressDownloadLink) {
            <a [href]="gallery()!.pressDownloadLink!" target="_blank" rel="noreferrer"
               class="flex-shrink-0 flex items-center gap-2 px-5 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase text-xs tracking-wider transition">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
              </svg>
              Descargar set de prensa
            </a>
          }
        </div>

        <!-- Gallery days -->
        @for (day of gallery()!.galleryDays; track day.dayName) {
          <section class="mb-14">
            <h2 class="font-accent uppercase tracking-widest text-xs text-cyan-brand mb-5 border-b border-navy-mid pb-3">
             {{ day.dayName }}
            </h2>
            <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
              @for (asset of day.assets; track asset.id) {
                @if (asset.type === 'photo') {
                  <div class="group relative overflow-hidden rounded-lg bg-navy-mid cursor-zoom-in"
                       [style.aspect-ratio]="aspectRatio(asset)"
                       (click)="openLightbox(asset)">
                    <img [src]="asset.url" [alt]="day.dayName" referrerpolicy="no-referrer"
                         class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500">
                    <div class="absolute inset-0 bg-navy-deepest/0 group-hover:bg-navy-deepest/30 transition flex items-center justify-center">
                      <svg class="h-8 w-8 text-white opacity-0 group-hover:opacity-100 transition" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v3m0 0v3m0-3h3m-3 0H7"/>
                      </svg>
                    </div>
                  </div>
                } @else {
                  <div class="group relative overflow-hidden rounded-lg bg-navy-mid" style="aspect-ratio:16/9">
                    <div class="absolute inset-0 flex items-center justify-center">
                      <div class="w-14 h-14 rounded-full bg-cyan-brand/20 border-2 border-cyan-brand flex items-center justify-center">
                        <svg class="h-6 w-6 text-cyan-brand ml-1" fill="currentColor" viewBox="0 0 24 24">
                          <path d="M8 5v14l11-7z"/>
                        </svg>
                      </div>
                    </div>
                  </div>
                }
              }
            </div>
          </section>
        }

      </div>

      <!-- Lightbox -->
      @if (lightboxAsset()) {
        <div class="fixed inset-0 z-50 bg-navy-deepest/95 flex items-center justify-center p-4"
             (click)="closeLightbox()">
          <button class="absolute top-4 right-4 text-text-muted hover:text-white transition" aria-label="Cerrar">
            <svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
          <img [src]="lightboxAsset()!.url" referrerpolicy="no-referrer"
               class="max-h-[90vh] max-w-[90vw] object-contain rounded-lg shadow-2xl"
               (click)="$event.stopPropagation()">
        </div>
      }
    }
  `,
})
export class GaleriaDetalleComponent implements OnInit {
  readonly slug = input<string>('');

  private api = inject(ApiService);
  private titleSvc = inject(Title);
  private meta = inject(Meta);

  loading = signal(true);
  notFound = signal(false);
  gallery = signal<GalleryDetail | null>(null);
  lightboxAsset = signal<GalleryAsset | null>(null);
  readonly skeletons = [1, 2, 3, 4, 5, 6];

  totalPhotos = () => {
    const g = this.gallery();
    if (!g) return 0;
    return g.galleryDays.reduce((sum, d) => sum + d.assets.filter(a => a.type === 'photo').length, 0);
  };

  constructor() {
    effect(() => {
      const s = this.slug();
      if (s) this.load(s);
    });
  }

  ngOnInit(): void {
    const s = this.slug();
    if (s) this.load(s);
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.notFound.set(false);
    try {
      const res = await this.api.get<any>(`/galleries/${slug}`);
      const raw = res?.data ?? res;
      if (!raw?.id) { this.notFound.set(true); return; }
      this.gallery.set(raw as GalleryDetail);
      this.titleSvc.setTitle(`${raw.title} — ALAS Latin Tour`);
      this.meta.updateTag({ name: 'description', content: `Galería oficial: ${raw.title}` });
    } catch (err: any) {
      if (err?.status === 404) this.notFound.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  openLightbox(asset: GalleryAsset): void { this.lightboxAsset.set(asset); }
  closeLightbox(): void { this.lightboxAsset.set(null); }

  aspectRatio(asset: GalleryAsset): string {
    if (asset.width && asset.height) return `${asset.width}/${asset.height}`;
    return '4/3';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const months = ['ene','feb','mar','abr','may','jun','jul','ago','sep','oct','nov','dic'];
    return `${d.getDate()} de ${months[d.getMonth()]}, ${d.getFullYear()}`;
  }
}
