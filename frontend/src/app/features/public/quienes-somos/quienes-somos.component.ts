import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslocoModule, provideTranslocoScope } from '@jsverse/transloco';
import { SeoService } from '../../../core/i18n/seo.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { ApiService } from '../../../core/services/api.service';

/** `GET /v1/pages/quienes-somos`: text editable in WordPress, as plain-text paragraphs. */
interface PageSection { title: string; paragraphs: string[]; }
interface PageContent { intro: string[]; sections: PageSection[]; }

/**
 * Order of the H2 sections of the WordPress page. Anything missing or empty keeps the built-in
 * translated text, so the page never renders blank if WordPress is down or not filled in yet.
 */
const WP_SECTIONS = ['history', 'origins', 'tour', 'mission', 'vision', 'scope'] as const;
type WpSection = (typeof WP_SECTIONS)[number];

interface TeamMember {
  name: string;
  /** Key under `quienesSomos.team.roles`. */
  role: string;
  initials: string;
  color: string;
}

@Component({
  selector: 'app-quienes-somos',
  standalone: true,
  imports: [TranslocoModule],
  providers: [provideTranslocoScope('quienes-somos')],
  template: `
    <ng-container *transloco="let t; prefix: 'quienesSomos'">
    <!-- ═══ HERO BANNER ═══ -->
    <section class="hero-banner py-24 px-4 sm:px-6 lg:px-8 relative overflow-hidden">
      <div class="absolute inset-0 opacity-20 pointer-events-none"
           style="background-image: radial-gradient(rgba(0,129,198,0.2) 1px, transparent 1px); background-size: 40px 40px;"></div>
      <div class="max-w-5xl mx-auto relative z-10 text-center">
        <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-sm mb-4">{{ t('hero.kicker') }}</p>
        <h1 class="font-heading font-bold text-5xl sm:text-7xl mb-6 leading-tight">
          {{ t('hero.titleLine1') }}<br><span class="text-cyan-brand">{{ t('hero.titleLine2') }}</span>
        </h1>
        @if (wpIntro(); as intro) {
          @for (p of intro; track $index) {
            <p class="text-lg md:text-xl text-text-muted max-w-3xl mx-auto leading-relaxed" [class.mt-4]="!$first">{{ p }}</p>
          }
        } @else {
          <p class="text-lg md:text-xl text-text-muted max-w-3xl mx-auto leading-relaxed">{{ t('hero.intro') }}</p>
        }
      </div>
    </section>

    <!-- ═══ HISTORIA ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
        <div>
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">{{ t('history.kicker') }}</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3 mb-6">{{ wp('history')?.title || t('history.title') }}</h2>
          <div class="space-y-4 text-text-muted leading-relaxed">
            @if (wp('history'); as section) {
              @for (p of section.paragraphs; track $index) { <p>{{ p }}</p> }
            } @else {
              @for (p of historyParagraphs; track p) {
                <p>{{ t('history.' + p) }}</p>
              }
            }
          </div>
        </div>

        <!-- Timeline -->
        <div class="relative pl-8 border-l-2 border-navy-mid space-y-8">
          @for (year of timelineYears; track year) {
            <div class="relative">
              <div class="timeline-dot absolute -left-[41px] top-1"></div>
              <span class="font-heading text-cyan-brand text-xl">{{ year }}</span>
              <h3 class="font-heading text-lg mt-1 mb-1">{{ t('timeline.' + year + '.title') }}</h3>
              <p class="text-sm text-text-muted leading-relaxed">{{ t('timeline.' + year + '.description') }}</p>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ═══ LOS ORÍGENES ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-dark">
      <div class="max-w-4xl mx-auto">
        <div class="mb-10">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">{{ t('origins.kicker') }}</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">{{ wp('origins')?.title || t('origins.title') }}</h2>
        </div>
        <div class="space-y-4 text-text-muted leading-relaxed text-sm md:text-base">
          @if (wp('origins'); as section) {
            @for (p of section.paragraphs; track $index) { <p>{{ p }}</p> }
          } @else {
            @for (p of originsParagraphs; track p) {
              <p>{{ t('origins.' + p) }}</p>
            }
          }
        </div>
      </div>
    </section>

    <!-- ═══ TOUR LATINO PROFESIONAL ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-4xl mx-auto">
        <div class="mb-10">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">{{ t('tour.kicker') }}</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">{{ wp('tour')?.title || t('tour.title') }}</h2>
        </div>
        <div class="space-y-4 text-text-muted leading-relaxed text-sm md:text-base">
          @if (wp('tour'); as section) {
            @for (p of section.paragraphs; track $index) { <p>{{ p }}</p> }
          } @else {
            @for (p of tourParagraphs; track p) {
              <p>{{ t('tour.' + p) }}</p>
            }
          }
        </div>
      </div>
    </section>

    <!-- ═══ MISIÓN / VISIÓN / ALCANCE ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-navy-deepest to-navy-dark">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">{{ t('identity.kicker') }}</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">{{ t('identity.title') }}</h2>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          @for (item of identity; track item.key) {
            <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
              <div class="text-3xl mb-4">{{ item.icon }}</div>
              @if (wp(item.key); as section) {
                <h3 class="font-heading text-2xl text-cyan-brand mb-3">{{ section.title || t('identity.' + item.key + 'Title') }}</h3>
                @for (p of section.paragraphs; track $index) {
                  <p class="text-text-muted leading-relaxed text-sm" [class.mt-3]="!$first">{{ p }}</p>
                }
              } @else {
                <h3 class="font-heading text-2xl text-cyan-brand mb-3">{{ t('identity.' + item.key + 'Title') }}</h3>
                <p class="text-text-muted leading-relaxed text-sm">{{ t('identity.' + item.key) }}</p>
              }
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ═══ ESTADÍSTICAS ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-dark">
      <div class="max-w-5xl mx-auto">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
          @for (stat of stats; track stat.label) {
            <div class="py-8">
              <div class="font-heading text-5xl md:text-6xl text-cyan-brand leading-none">{{ stat.value }}</div>
              <div class="font-accent uppercase text-xs tracking-[0.2em] text-text-muted mt-3">{{ t('stats.' + stat.label) }}</div>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ═══ EQUIPO ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">{{ t('team.kicker') }}</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">{{ t('team.title') }}</h2>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          @for (member of team; track member.name) {
            <div class="bg-navy-dark rounded-2xl p-6 border border-navy-mid hover:border-cyan-brand/40 transition text-center group">
              <div class="w-16 h-16 rounded-full flex items-center justify-center text-xl font-heading font-bold mx-auto mb-4 group-hover:scale-105 transition-transform text-white"
                   [style.background]="member.color">
                {{ member.initials }}
              </div>
              <h3 class="font-heading text-lg leading-tight">{{ member.name }}</h3>
              <p class="font-accent uppercase text-xs text-cyan-brand tracking-wider mt-1">{{ t('team.roles.' + member.role) }}</p>
            </div>
          }
        </div>
      </div>
    </section>
    </ng-container>
  `,
})
export class QuienesSomosComponent implements OnInit {
  private seo = inject(SeoService);

  // Each paragraph is one translation key, so long texts are translated as a whole.
  readonly historyParagraphs = ['p1', 'p2', 'p3'];
  readonly originsParagraphs = ['p1', 'p2', 'p3', 'p4', 'p5', 'p6'];
  readonly tourParagraphs = ['p1', 'p2', 'p3', 'p4', 'p5'];
  readonly timelineYears = ['1998', '1999', '2001', '2002', '2005', '2007', '2008', '2015', '2023'];

  readonly identity: { key: WpSection; icon: string }[] = [
    { key: 'mission', icon: '🎯' },
    { key: 'vision', icon: '🌊' },
    { key: 'scope', icon: '🌎' },
  ];

  readonly stats = [
    { value: '2001', label: 'founded' },
    { value: '18', label: 'countries' },
    { value: '2015', label: 'qualifier' },
    { value: '3', label: 'continents' },
  ];

  readonly team: TeamMember[] = [
    { name: 'Karin Sierralta', role: 'president', initials: 'KS', color: 'linear-gradient(135deg,#0081C6,#004F8E)' },
    { name: 'Renzo Dañino', role: 'technical', initials: 'RD', color: 'linear-gradient(135deg,#F97316,#003873)' },
    { name: 'Antonio Sotillo', role: 'events', initials: 'AS', color: 'linear-gradient(135deg,#22C55E,#003873)' },
    { name: 'Leslie Ramos', role: 'administration', initials: 'LR', color: 'linear-gradient(135deg,#FBBF24,#003873)' },
    { name: 'Jose Duarte', role: 'press', initials: 'JD', color: 'linear-gradient(135deg,#0081C6,#003873)' },
    { name: 'Pablo Panizo', role: 'marketing', initials: 'PP', color: 'linear-gradient(135deg,#F97316,#004F8E)' },
    { name: 'Alejandro Castillo', role: 'technology', initials: 'AC', color: 'linear-gradient(135deg,#22C55E,#004F8E)' },
  ];

  private readonly api = inject(ApiService);
  private readonly language = inject(LanguageService);
  private readonly page = signal<PageContent | null>(null);

  ngOnInit(): void {
    // The page title comes from the route (`titles.about`).
    this.seo.setPageMeta({ scope: 'quienes-somos', descriptionKey: 'meta.description' });
    void this.loadPage();
  }

  /** Fetched during SSR too, so the WordPress text is in the indexed HTML. */
  private async loadPage(): Promise<void> {
    try {
      this.page.set(await this.api.get<PageContent>(`/pages/quienes-somos?lang=${this.language.activeLang()}`));
    } catch {
      this.page.set(null); // 404 / WordPress down: keep the built-in text
    }
  }

  wpIntro(): string[] | null {
    const intro = this.page()?.intro ?? [];
    return intro.length ? intro : null;
  }

  wp(key: WpSection): PageSection | null {
    const section = this.page()?.sections[WP_SECTIONS.indexOf(key)];
    return section?.paragraphs.length ? section : null;
  }
}
