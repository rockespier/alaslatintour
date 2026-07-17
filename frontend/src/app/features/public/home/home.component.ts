import { Component, inject, signal, computed, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Meta, Title, DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ApiService } from '../../../core/services/api.service';
import { RankingService, RankingRow } from '../../../core/services/ranking.service';
import { ArticleSummary, mapArticleSummary } from '../../../core/models/article';
import { sortEventsForDisplay } from '../../../core/utils/event-sort.util';
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
  imagenUrl?: string | null;
}

type ArticleCard = ArticleSummary;

interface LiveEvent {
  id: string;
  nombre: string;
  pais: string;
  ciudad: string;
  playa: string;
  fechaInicio: string;
  fechaFin: string;
  imagenUrl?: string | null;
}

interface LiveStatus {
  isLive: boolean;
  event?: LiveEvent | null;
  youTubeVideoId?: string | null;
  youTubeWidth: number;
  youTubeHeight: number;
  schedulePdfUrl?: string | null;
}

const COUNTRY_FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
  VE: '🇻🇪', BO: '🇧🇴',
};

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, DecimalPipe, ReactiveFormsModule, StarRatingComponent, SurfscoresCreditComponent, LoadingSpinnerComponent],
  template: `
    <!-- ═══ EVENTO EN VIVO ═══ -->
    @if (liveStatus()?.isLive) {
      <section class="py-10 px-4 sm:px-6 lg:px-8 bg-navy-deepest border-b border-error-brand/30">
        <div class="max-w-7xl mx-auto">
          <div class="flex items-center gap-2 mb-5">
            <span class="relative flex h-2.5 w-2.5">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-error-brand opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-error-brand"></span>
            </span>
            <span class="font-accent uppercase text-error-brand tracking-[0.2em] text-sm">En Vivo Ahora</span>
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div class="lg:col-span-2 rounded-xl overflow-hidden border border-navy-mid bg-black">
              @if (liveEmbedUrl()) {
                <div class="relative" style="padding-top: 56.25%">
                  <iframe class="absolute inset-0 w-full h-full" [src]="liveEmbedUrl()"
                          title="Streaming en vivo — ALAS Latin Tour" frameborder="0"
                          allow="autoplay; encrypted-media; picture-in-picture" allowfullscreen></iframe>
                </div>
              }
            </div>
            <div class="flex flex-col justify-center gap-4">
              <div>
                <h2 class="font-heading text-3xl md:text-4xl leading-tight">{{ liveStatus()?.event?.nombre }}</h2>
                <p class="text-text-muted mt-1">
                  {{ liveStatus()?.event?.playa }}, {{ liveStatus()?.event?.ciudad }}, {{ liveStatus()?.event?.pais }}
                </p>
              </div>
              @if (liveStatus()?.schedulePdfUrl) {
                <a [href]="liveStatus()?.schedulePdfUrl" target="_blank" rel="noopener"
                   class="inline-flex items-center justify-center gap-2 px-6 py-3 border-2 border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider rounded-md transition w-fit">
                  <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                  </svg>
                  Ver programación del evento
                </a>
              }
            </div>
          </div>
        </div>
      </section>
    }

    <!-- ═══ HERO ═══ -->
    <section class="hero-bg min-h-[92vh] flex items-center relative overflow-hidden">
      <div class="absolute inset-0 opacity-30 pointer-events-none"
           style="background-image: radial-gradient(rgba(0,129,198,0.15) 1px, transparent 1px); background-size: 32px 32px;">
           <video autoplay muted loop class="bg-video" poster="https://www.alaslatintour.com/content/dist/images/fallback.jpg" preload="auto" playsinline>
            <source src="https://www.alaslatintour.com/content/dist/images/landing.webm" type="video/webm">
            <source src="https://www.alaslatintour.com/content/dist/images/landing.mp4" type="video/mp4" />
            Tu navegador no soporta video HTML5.
        </video>	
      </div>

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
                d="M9 5V3h6v2m-7 0h8v3a4 4 0 01-8 0V5zm-2 0h2v1a6 6 0 01-6 6V9a4 4 0 014-4zm12 0h2a4 4 0 014 4v3a6 6 0 01-6-6V5zM12 12v6m-3 3h6"/>
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
              <article class="card-event rounded-xl overflow-hidden min-w-[280px] lg:min-w-0 flex flex-col">
                <div class="relative aspect-[16/10] bg-navy-mid/40">
                  @if (event.imagenUrl) {
                    <img [src]="event.imagenUrl" [alt]="event.nombre" referrerpolicy="no-referrer"
                         class="w-full h-full object-cover">
                  } @else {
                    <div class="w-full h-full flex items-center justify-center bg-gradient-to-br from-navy-mid/60 to-navy-deepest text-5xl">
                      {{ flagOf(event.pais) }}
                    </div>
                  }
                  <span [class]="statusClass(event.statusPublic)"
                        class="absolute top-3 right-3 px-2.5 py-1 rounded-full text-xs font-accent uppercase tracking-wider">
                    {{ event.statusPublic }}
                  </span>
                  @if (event.imagenUrl) {
                    <span class="absolute top-3 left-3 text-2xl drop-shadow">{{ flagOf(event.pais) }}</span>
                  }
                </div>
                <div class="p-6 flex flex-col flex-1">
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
                <span class="font-accent uppercase text-success-brand tracking-[0.2em] text-xs">
                  @if (rankingCachedAgo()) { {{ rankingCachedAgo() }} } @else { Actualizado }
                </span>
              </div>
              <h2 class="font-heading text-3xl md:text-4xl">Ranking {{ currentYear }}</h2>
              <p class="text-sm text-text-muted mt-1">{{ rankingCategoryName() }}</p>
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
                    <img [src]="article.imageUrl" [alt]="article.title" referrerpolicy="no-referrer" class="object-cover w-full h-full">
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

    <!-- ═══ CONTACTO ═══ -->
    <section id="contacto" class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-dark border-t border-navy-mid">
      <div class="max-w-3xl mx-auto">

        <div class="text-center mb-10">
          <p class="font-accent uppercase tracking-[0.25em] text-cyan-brand text-xs mb-2">Comunícate con nosotros</p>
          <h2 class="font-heading text-4xl md:text-5xl">Contacto</h2>
          <p class="text-text-muted mt-3 text-sm max-w-md mx-auto">
            ¿Tienes preguntas sobre inscripciones, eventos o el circuito? Escríbenos y te responderemos a la brevedad.
          </p>
        </div>

        @if (contactSuccess()) {
          <div class="bg-success-brand/10 border border-success-brand/30 rounded-2xl p-10 text-center">
            <div class="w-14 h-14 rounded-full bg-success-brand/20 flex items-center justify-center mx-auto mb-4">
              <svg class="w-7 h-7 text-success-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
              </svg>
            </div>
            <h3 class="font-heading text-2xl mb-2">¡Mensaje enviado!</h3>
            <p class="text-text-muted text-sm">Te responderemos al correo indicado en los próximos días hábiles.</p>
            <button (click)="contactSuccess.set(false)"
              class="mt-6 text-sm text-cyan-brand hover:text-cyan-dark">
              Enviar otro mensaje
            </button>
          </div>
        } @else {
          @if (contactError()) {
            <div class="mb-6 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
              {{ contactError() }}
            </div>
          }

          <form [formGroup]="contactForm" (ngSubmit)="submitContact()" novalidate
                class="bg-navy-deepest border border-navy-mid rounded-2xl p-6 md:p-8 space-y-5 shadow-2xl shadow-black/30">

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
              <div>
                <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Nombre</label>
                <input formControlName="nombre" type="text" placeholder="Tu nombre"
                  class="input-field"
                  [class.field-error]="contactForm.controls.nombre.invalid && contactForm.controls.nombre.touched" />
                @if (contactForm.controls.nombre.invalid && contactForm.controls.nombre.touched) {
                  <p class="mt-1 text-xs text-error-brand">Requerido</p>
                }
              </div>
              <div>
                <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Correo electrónico</label>
                <input formControlName="email" type="email" placeholder="tu@correo.com"
                  class="input-field"
                  [class.field-error]="contactForm.controls.email.invalid && contactForm.controls.email.touched" />
                @if (contactForm.controls.email.invalid && contactForm.controls.email.touched) {
                  <p class="mt-1 text-xs text-error-brand">
                    @if (contactForm.controls.email.errors?.['required']) { Requerido }
                    @else { Correo inválido }
                  </p>
                }
              </div>
            </div>

            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Asunto</label>
              <input formControlName="asunto" type="text" placeholder="¿En qué podemos ayudarte?"
                class="input-field"
                [class.field-error]="contactForm.controls.asunto.invalid && contactForm.controls.asunto.touched" />
              @if (contactForm.controls.asunto.invalid && contactForm.controls.asunto.touched) {
                <p class="mt-1 text-xs text-error-brand">Requerido</p>
              }
            </div>

            <div>
              <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-1.5">Mensaje</label>
              <textarea formControlName="mensaje" rows="5" placeholder="Escribe tu mensaje aquí..."
                class="input-field resize-none"
                [class.field-error]="contactForm.controls.mensaje.invalid && contactForm.controls.mensaje.touched">
              </textarea>
              @if (contactForm.controls.mensaje.invalid && contactForm.controls.mensaje.touched) {
                <p class="mt-1 text-xs text-error-brand">Mínimo 10 caracteres</p>
              }
            </div>

            <button type="submit" [disabled]="contactLoading()"
              class="w-full py-3 px-4 bg-cyan-brand hover:bg-cyan-dark disabled:opacity-60 text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-lg transition font-bold">
              @if (contactLoading()) {
                <span class="inline-flex items-center justify-center gap-2">
                  <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                  </svg>
                  Enviando...
                </span>
              } @else {
                Enviar mensaje
              }
            </button>

          </form>
        }
      </div>
    </section>
  `,
})
export class HomeComponent implements OnInit {
  private api = inject(ApiService);
  private rankingService = inject(RankingService);
  private fb = inject(FormBuilder);
  private title = inject(Title);
  private meta = inject(Meta);
  private sanitizer = inject(DomSanitizer);
  private platformId = inject(PLATFORM_ID);

  readonly currentYear = new Date().getFullYear();

  events = signal<EventCard[]>([]);
  articles = signal<ArticleCard[]>([]);
  ranking = signal<RankingRow[]>([]);
  loadingEvents = signal(true);
  loadingArticles = signal(true);
  loadingRanking = signal(true);
  rankingCategoryName = signal('Open Hombres');
  rankingCachedAgo = signal('');

  liveStatus = signal<LiveStatus | null>(null);
  liveEmbedUrl = computed<SafeResourceUrl | null>(() => {
    const status = this.liveStatus();
    if (!status?.isLive || !status.youTubeVideoId) return null;
    const url = `https://www.youtube.com/embed/${status.youTubeVideoId}?autoplay=1&rel=0&modestbranding=1`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  contactForm = this.fb.group({
    nombre:  ['', Validators.required],
    email:   ['', [Validators.required, Validators.email]],
    asunto:  ['', Validators.required],
    mensaje: ['', [Validators.required, Validators.minLength(10)]],
  });
  contactLoading = signal(false);
  contactSuccess = signal(false);
  contactError = signal('');

  ngOnInit(): void {
    this.title.setTitle('ALAS Latin Tour 2026 — Circuito Continental de Surf Profesional');
    this.meta.updateTag({ name: 'description', content: 'Plataforma oficial del ALAS Latin Tour. Calendario de eventos, ranking en vivo, noticias y registro de competidores del circuito continental de surf.' });
    this.meta.updateTag({ property: 'og:title', content: 'ALAS Latin Tour 2026' });
    this.meta.updateTag({ property: 'og:description', content: 'El circuito continental de surf de alto rendimiento.' });
    this.loadEvents();
    this.loadArticles();
    this.loadRanking();
    this.loadLiveStatus();
  }

  private async loadLiveStatus(): Promise<void> {
    try {
      const status = await this.api.get<LiveStatus>('/live');
      this.liveStatus.set(status ?? null);
    } catch {
      this.liveStatus.set(null);
    }
  }

  private async loadEvents(): Promise<void> {
    try {
      const res = await this.api.get<any>('/events?limit=100&page=1');
      this.events.set(sortEventsForDisplay(res?.data ?? []).slice(0, 4));
    } catch {
      this.events.set([]);
    } finally {
      this.loadingEvents.set(false);
    }
  }

  private async loadArticles(): Promise<void> {
    try {
      const res = await this.api.get<any>('/articles?limit=3');
      this.articles.set((res?.data ?? []).map(mapArticleSummary));
    } catch {
      this.articles.set([]);
    } finally {
      this.loadingArticles.set(false);
    }
  }

  private async loadRanking(): Promise<void> {
    try {
      const cats = await this.rankingService.getCategories();
      const defaultCat = cats[0];
      if (!defaultCat) {
        this.ranking.set([]);
        return;
      }
      const year = defaultCat.availableYears?.at(-1);
      const result = await this.rankingService.getRanking(defaultCat.id, year, 1, 8);
      this.ranking.set(result.rows);
      this.rankingCategoryName.set(result.categoryName || defaultCat.nombre);
      this.rankingCachedAgo.set(this.rankingService.cachedAgo(result.cachedAt));
    } catch {
      this.ranking.set([]);
    } finally {
      this.loadingRanking.set(false);
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

  async submitContact(): Promise<void> {
    if (this.contactForm.invalid) { this.contactForm.markAllAsTouched(); return; }
    this.contactLoading.set(true);
    this.contactError.set('');
    try {
      await this.api.post('/contact', this.contactForm.value);
      this.contactSuccess.set(true);
      this.contactForm.reset();
    } catch (err: any) {
      this.contactError.set(err.body?.message ?? 'No se pudo enviar el mensaje. Intenta de nuevo.');
    } finally {
      this.contactLoading.set(false);
    }
  }

}
