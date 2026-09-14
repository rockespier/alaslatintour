import { Component, OnInit, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

interface TeamMember {
  name: string;
  role: string;
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
          ALAS GLOBAL TOUR es una asociación fundada el 30 de abril de 2001 en Lima, Perú, integrada
          inicialmente por líderes del surf de Latinoamérica, con objetivos orientados al fortalecimiento,
          desarrollo y promoción de esta disciplina deportiva en la región.
        </p>
      </div>
    </section>

    <!-- ═══ HISTORIA ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
        <div>
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Nuestra historia</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3 mb-6">De un torneo de naciones a un tour continental</h2>
          <div class="space-y-4 text-text-muted leading-relaxed">
            <p>
              En el año 2002 se dio inicio al Tour Latino Profesional y, posteriormente, en 2005,
              la organización se consolidó como una de las asociaciones promotoras líderes del surf
              en América. Desde el año 2015 ALAS es clasificatorio para los juegos panamericanos de
              Lima 2019, Santiago 2023 y Lima 2027.
            </p>
            <p>
              En 2023 inicia ALAS Global Tour, luego de más de dos décadas de realización de eventos
              primero a nivel latinoamericano y luego continental, ampliando su alcance para incluir
              deportistas de toda América, Europa y Asia.
            </p>
            <p>
              ALAS GLOBAL TOUR continuará trabajando para que, en los próximos años, miles de
              surfistas puedan competir en eventos a lo largo de todo el continente.
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

    <!-- ═══ LOS ORÍGENES ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-dark">
      <div class="max-w-4xl mx-auto">
        <div class="mb-10">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Los orígenes</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Cómo nació la Asociación Latinoamericana</h2>
        </div>
        <div class="space-y-4 text-text-muted leading-relaxed text-sm md:text-base">
          <p>
            La idea de una Asociación Latinoamericana nace en El Salvador en 1998, cuando nuestro
            querido amigo y presidente de la asociación salvadoreña de surf, el "Chute", organiza el
            primer evento latinoamericano de naciones.
          </p>
          <p>
            En dicho evento participaron tablistas de Panamá, República Dominicana, Guatemala, Costa
            Rica y Perú. Luego de confraternizar todos los tablistas y dirigentes, se planeó la
            siguiente fecha para Perú en el año 2000.
          </p>
          <p>
            En 1999, en Mar del Plata, Argentina (Panamericano), algunos dirigentes latinos afianzan
            su deseo de sacar adelante una asociación solo latina, "con la finalidad de promover el
            surf en los países latinos, sobre todo en países bastante jóvenes", para mejorar el nivel
            en todo sentido y encontrarse a mediano plazo a la par con nuestros vecinos de Brasil y
            EE. UU.
          </p>
          <p>
            Lima fue la sede del II Latinoamericano por equipos. La playa La Pampilla de Miraflores
            fue el escenario de 3 días de confraternidad y mucho surf. Participaron países como
            Venezuela, Ecuador, Chile, Argentina, Costa Rica y Panamá.
          </p>
          <p>
            La idea de una asociación se volvía más fuerte, pues estos eventos debían continuar en
            paralelo a los Panamericanos por equipos. Inmediatamente se realiza el 3er
            Latinoamericano por equipos en la playa Montañita, Ecuador, y se decide realizar una
            reunión exclusiva para fundar (legalmente) la Asociación Latinoamericana. La sede sería
            Lima en 60 días; se envió invitaciones a todos los países latinos, de los cuales
            contestaron Ecuador, Colombia, Argentina, Chile, Venezuela, El Salvador, Panamá, Costa
            Rica, República Dominicana, Perú y Puerto Rico.
          </p>
          <p>
            La reunión se efectuó según lo planeado y fue todo un éxito, pues todos los países
            mencionados anteriormente se hicieron presentes en persona o vía email. Los que
            asistieron fueron: Edgar Severino de República Dominicana, Antonio Sotillo de Venezuela,
            Víctor Arce de Costa Rica, Alcides Guerrero de Ecuador y Karin Sierralta de Perú.
            Finalmente las elecciones se realizaron. Fueron 5 días de trabajo y surf (sin olvidar la
            esencia). Se crearon los estatutos, reglamentos y planes de trabajo.
          </p>
        </div>
      </div>
    </section>

    <!-- ═══ TOUR LATINO PROFESIONAL ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-4xl mx-auto">
        <div class="mb-10">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">De los eventos de naciones al circuito profesional</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Tour Latino Profesional</h2>
        </div>
        <div class="space-y-4 text-text-muted leading-relaxed text-sm md:text-base">
          <p>
            En el año 2002, los eventos de naciones pierden atractivo debido a los eventos de
            naciones de PASA (Juegos Panamericanos) e ISA (Juegos Mundiales). Entonces se inicia el
            proyecto del primer Tour Regional.
          </p>
          <p>
            El éxito de este Tour permitió a todos los atletas de alta competencia de cada país
            mantenerse en campeonatos, ganar dinero, tener exposición internacional y elevar su nivel
            competitivo. También se convirtió en una oportunidad idónea y económica para que las
            marcas expongan sus productos a través del Tour Latino.
          </p>
          <p>
            En el 2002 se inició nuestro circuito con 4 fechas: Perú, Ecuador, República Dominicana y
            Venezuela. Para el año 2004, el segundo del Tour Latino, se planificaron 7 fechas, las
            cuales fueron realizadas con éxito entre los países de Perú, Ecuador, Panamá, Costa Rica,
            República Dominicana y Venezuela, con dos eventos de preclasificación ALAS realizados en
            Colombia y Guatemala.
          </p>
          <p>
            En el 2005, el rápido crecimiento del Tour obliga a replantear las políticas
            administrativas de una asociación para transformarse en una empresa. Se planifica un
            circuito sólido y de primer nivel en la región, manejado de manera profesional. Gracias a
            ello, ALAS logra realizarse en 12 países, consolidándose como la mayor organización de
            surf en todo el continente. Dos años más tarde se inicia el reconocimiento de la ISA.
          </p>
          <p>
            Argentina, Brasil, Chile, Perú, Ecuador, Colombia, Venezuela, Panamá, Costa Rica, El
            Salvador, México, República Dominicana, Puerto Rico y Barbados son parte de la familia
            ALAS. Para el 2008 se integrarían Guatemala, Nicaragua, Guadalupe y Trinidad y Tobago.
          </p>
        </div>
      </div>
    </section>

    <!-- ═══ MISIÓN / VISIÓN / ALCANCE ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-navy-deepest to-navy-dark">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Nuestra identidad</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Misión, Visión y Alcance</h2>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">🎯</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Misión</h3>
            <p class="text-text-muted leading-relaxed text-sm">
              Fortalecer, desarrollar y promover el surf como disciplina deportiva en América
              Latina, integrando desde su fundación a los líderes del surf de la región.
            </p>
          </div>
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">🌊</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Visión</h3>
            <p class="text-text-muted leading-relaxed text-sm">
              Continuar trabajando para que, en los próximos años, miles de surfistas puedan
              competir en eventos a lo largo de todo el continente.
            </p>
          </div>
          <div class="stat-card rounded-2xl p-8 border border-navy-mid hover:border-cyan-brand/40 transition">
            <div class="text-3xl mb-4">🌎</div>
            <h3 class="font-heading text-2xl text-cyan-brand mb-3">Alcance</h3>
            <p class="text-text-muted leading-relaxed text-sm">
              Desde 2023, ALAS Global Tour amplía su alcance para incluir deportistas de toda
              América, Europa y Asia, luego de más de dos décadas de eventos primero a nivel
              latinoamericano y luego continental.
            </p>
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

    <!-- ═══ EQUIPO ═══ -->
    <section class="py-20 px-4 sm:px-6 lg:px-8 bg-navy-deepest">
      <div class="max-w-7xl mx-auto">
        <div class="text-center mb-14">
          <span class="font-accent uppercase text-xs tracking-[0.25em] text-orange-brand">Las personas detrás</span>
          <h2 class="font-heading text-4xl md:text-5xl mt-3">Nuestro Equipo</h2>
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
    { year: '1998', title: 'Nace la idea', description: 'El Salvador organiza el primer evento latinoamericano de naciones con Panamá, Rep. Dominicana, Guatemala, Costa Rica y Perú.' },
    { year: '1999', title: 'Se afianza el proyecto', description: 'En el Panamericano de Mar del Plata, Argentina, los dirigentes latinos impulsan la idea de una asociación solo latina.' },
    { year: '2001', title: 'Fundación oficial', description: 'El 30 de abril se funda ALAS en Lima, Perú, tras el 3er Latinoamericano por equipos en Montañita, Ecuador.' },
    { year: '2002', title: 'Tour Latino Profesional', description: 'Arranca el circuito profesional con 4 fechas: Perú, Ecuador, República Dominicana y Venezuela.' },
    { year: '2005', title: 'Consolidación regional', description: 'ALAS se transforma en una organización profesional presente en 12 países del continente.' },
    { year: '2007', title: 'Reconocimiento ISA', description: 'Se inicia el reconocimiento de la International Surfing Association para el circuito ALAS.' },
    { year: '2008', title: 'Nuevos países', description: 'Se integran Guatemala, Nicaragua, Guadalupe y Trinidad y Tobago a la familia ALAS.' },
    { year: '2015', title: 'Clasificatorio panamericano', description: 'ALAS se convierte en clasificatorio para los Juegos Panamericanos de Lima 2019, Santiago 2023 y Lima 2027.' },
    { year: '2023', title: 'ALAS Global Tour', description: 'El circuito amplía su alcance a deportistas de toda América, Europa y Asia.' },
  ];

  stats = [
    { value: '2001', label: 'Fundación en Lima, Perú' },
    { value: '12', label: 'Países consolidados (2005)' },
    { value: '2015', label: 'Clasificatorio panamericano desde' },
    { value: '3', label: 'Continentes: América, Europa y Asia' },
  ];

  team: TeamMember[] = [
    { name: 'Karin Sierralta', role: 'Presidente del directorio', initials: 'KS', color: 'linear-gradient(135deg,#0081C6,#004F8E)' },
    { name: 'Renzo Dañino', role: 'Director técnico', initials: 'RD', color: 'linear-gradient(135deg,#F97316,#003873)' },
    { name: 'Antonio Sotillo', role: 'Director de Eventos', initials: 'AS', color: 'linear-gradient(135deg,#22C55E,#003873)' },
    { name: 'Leslie Ramos', role: 'Administración', initials: 'LR', color: 'linear-gradient(135deg,#FBBF24,#003873)' },
    { name: 'Jose Duarte', role: 'Prensa', initials: 'JD', color: 'linear-gradient(135deg,#0081C6,#003873)' },
    { name: 'Pablo Panizo', role: 'Marketing', initials: 'PP', color: 'linear-gradient(135deg,#F97316,#004F8E)' },
    { name: 'Alejandro Castillo', role: 'Tecnología', initials: 'AC', color: 'linear-gradient(135deg,#22C55E,#004F8E)' },
  ];

  ngOnInit(): void {
    this.title.setTitle('Quiénes Somos — ALAS Latin Tour');
    this.meta.updateTag({ name: 'description', content: 'ALAS Global Tour: asociación fundada el 30 de abril de 2001 en Lima, Perú. Conoce nuestra historia, misión, visión y el equipo directivo del circuito continental de surf.' });
    this.meta.updateTag({ property: 'og:title', content: 'Quiénes Somos — ALAS Latin Tour' });
  }
}
