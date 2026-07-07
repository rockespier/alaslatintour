import { Component, OnInit, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

interface TeamMember {
  name: string;
  role: string;
  country: string;
  initials: string;
  color: string;
}

@Component({
  selector: 'app-quienes-somos',
  standalone: true,
  imports: [],
  template: `
    <!-- ═══ HERO BANNER ═══ -->
    <section class="hero-banner py-24 px-4 sm:px-6 lg:px-8 relative overflow-hidden">
      <div class="absolute inset-0 opacity-20 pointer-events-none"
           style="background-image: radial-gradient(rgba(0,129,198,0.2) 1px, transparent 1px); background-size: 40px 40px;"></div>
      <div class="max-w-5xl mx-auto relative z-10 text-center">
        <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-sm mb-4">Sobre Nosotros</p>
        <h1 class="font-heading font-bold text-5xl sm:text-7xl mb-6 leading-tight">
          Quiénes<br><span class="text-cyan-brand">Somos</span>
        </h1>
        <p class="text-lg md:text-xl text-text-muted max-w-3xl mx-auto leading-relaxed">
          La Asociación Latinoamericana de Surfistas Profesionales, impulsando el surf de alto rendimiento
          en el continente desde hace más de dos décadas.
        </p>
      </div>
    </section>

    <!-- ═══ HISTORIA ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
        <div>
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Nuestra historia</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3 mb-6">Más de dos décadas construyendo el surf latinoamericano</h2>
          <div class="space-y-4 text-text-muted leading-relaxed">
            <p>
              ALAS nació en 1998 en Lima, Perú, cuando un grupo de surfistas profesionales de la región
              decidió crear un circuito unificado que reconociera el talento local y le diera
              proyección internacional.
            </p>
            <p>
              A lo largo de los años, la asociación expandió su presencia a 12 países, consolidando
              el ALAS Latin Tour como el referente continental del surf profesional. Sus competencias
              son el puente entre los circuitos nacionales y el world tour de la WSL.
            </p>
            <p>
              Hoy, más de 340 atletas compiten cada temporada en categorías que van desde sub-14
              hasta Open, con eventos que recorren las mejores olas del Pacífico y el Atlántico
              latinoamericano.
            </p>
          </div>
        </div>

        <!-- Timeline -->
        <div class="relative pl-8 border-l-2 border-navy-mid space-y-8">
          @for (hito of timeline; track hito.year) {
            <div class="relative">
              <div class="timeline-dot absolute -left-[41px] top-1"></div>
              <span class="font-heading text-cyan-brand text-xl">{{ hito.year }}</span>
              <h3 class="font-heading text-lg mt-1 mb-1">{{ hito.title }}</h3>
              <p class="text-sm text-text-muted leading-relaxed">{{ hito.description }}</p>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ═══ MISIÓN / VISIÓN / VALORES ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-navy-deepest to-navy-dark">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Nuestra identidad</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Misión, Visión y Valores</h2>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">🎯</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Misión</h3>
            <p class="text-text-muted leading-relaxed text-sm">
              Organizar y promover el surf de alto rendimiento en América Latina, ofreciendo a
              los atletas un circuito profesional de clase mundial que les permita desarrollar
              su carrera y proyectarse internacionalmente.
            </p>
          </div>
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">🌊</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Visión</h3>
            <p class="text-text-muted leading-relaxed text-sm">
              Ser el circuito continental de surf más competitivo y reconocido del mundo,
              posicionando el talento latinoamericano en la escena global y contribuyendo
              al desarrollo sostenible del deporte en la región.
            </p>
          </div>
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">⭐</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Valores</h3>
            <ul class="text-text-muted text-sm space-y-2">
              @for (v of values; track v) {
                <li class="flex items-center gap-2">
                  <span class="w-1.5 h-1.5 rounded-full bg-cyan-brand flex-shrink-0"></span>
                  {{ v }}
                </li>
              }
            </ul>
          </div>
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
              <div class="font-accent uppercase text-xs tracking-[0.2em] text-text-muted mt-3">{{ stat.label }}</div>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ═══ EQUIPO DIRECTIVO ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Las personas detrás</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Equipo Directivo</h2>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          @for (member of team; track member.name) {
            <div class="bg-navy-dark rounded-2xl p-6 border border-navy-mid hover:border-cyan-brand/40 transition text-center group">
              <div class="w-16 h-16 rounded-full flex items-center justify-center text-xl font-heading font-bold mx-auto mb-4 group-hover:scale-105 transition-transform text-white"
                   [style.background]="member.color">
                {{ member.initials }}
              </div>
              <h3 class="font-heading text-lg leading-tight">{{ member.name }}</h3>
              <p class="font-accent uppercase text-xs text-cyan-brand tracking-wider mt-1">{{ member.role }}</p>
              <p class="text-xs text-text-muted mt-2">{{ member.country }}</p>
            </div>
          }
        </div>
      </div>
    </section>
  `,
})
export class QuienesSomosComponent implements OnInit {
  private title = inject(Title);
  private meta = inject(Meta);

  timeline = [
    { year: '1998', title: 'Fundación de ALAS', description: 'Un grupo de surfistas profesionales de Perú, Brasil y Chile funda la asociación en Lima.' },
    { year: '2003', title: 'Expansión continental', description: 'El circuito llega a 8 países con 12 eventos anuales y más de 200 atletas inscritos.' },
    { year: '2010', title: 'Alianza con WSL', description: 'Se formaliza el acuerdo de clasificación para el World Surf League Championship Tour.' },
    { year: '2019', title: 'Digitalización total', description: 'Sistema de resultados en tiempo real integrado con SurfScores y transmisiones en vivo.' },
    { year: '2026', title: 'Nueva plataforma', description: 'Lanzamiento de la plataforma PWA con pagos online, ranking en vivo y panel del competidor.' },
  ];

  values = [
    'Excelencia deportiva',
    'Desarrollo sostenible',
    'Inclusión y diversidad',
    'Integridad competitiva',
    'Transparencia institucional',
  ];

  stats = [
    { value: '26+', label: 'Años de historia' },
    { value: '12', label: 'Países miembros' },
    { value: '340+', label: 'Competidores activos' },
    { value: '18', label: 'Eventos anuales' },
  ];

  team: TeamMember[] = [
    { name: 'Rodrigo Valdivia', role: 'Presidente', country: 'Lima, Perú', initials: 'RV', color: 'linear-gradient(135deg,#0081C6,#004F8E)' },
    { name: 'Ana Paula Ferreira', role: 'Dir. Competencias', country: 'Florianópolis, Brasil', initials: 'AF', color: 'linear-gradient(135deg,#F97316,#003873)' },
    { name: 'Camilo Estrada', role: 'Dir. Técnico', country: 'Pichilemu, Chile', initials: 'CE', color: 'linear-gradient(135deg,#22C55E,#003873)' },
    { name: 'Valentina Reyes', role: 'Dir. Comunicaciones', country: 'Buenos Aires, Argentina', initials: 'VR', color: 'linear-gradient(135deg,#FBBF24,#003873)' },
  ];

  ngOnInit(): void {
    this.title.setTitle('Quiénes Somos — ALAS Latin Tour');
    this.meta.updateTag({ name: 'description', content: 'Conoce la Asociación Latinoamericana de Surfistas Profesionales: nuestra historia, misión, visión y el equipo que impulsa el surf continental.' });
    this.meta.updateTag({ property: 'og:title', content: 'Quiénes Somos — ALAS Latin Tour' });
  }
}
