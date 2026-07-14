import { Component, inject, signal, computed, input, OnInit, effect } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { StarRatingComponent } from '../../../shared/components/star-rating/star-rating.component';

interface EventDetail {
  id: string;
  nombre: string;
  ciudad: string;
  pais: string;
  stars: number;
  fechaInicio: string;
  fechaFin: string;
  statusPublic: string;
}

interface EventCategory {
  id: string;
  nombre: string;
  tipo: string;
  gender: 'Masculino' | 'Femenino' | 'Ambos' | '';
  tarifa: number;
  inscritos: number;
  capacidad: number | null;
  descripcion?: string;
}

const FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽', CR: '🇨🇷',
};

@Component({
  selector: 'app-inscripcion',
  standalone: true,
  imports: [RouterLink, FormsModule, StarRatingComponent],
  template: `
    <section class="py-10 px-4 sm:px-6 lg:px-8">
      <div class="max-w-4xl mx-auto">

        <nav class="flex items-center gap-2 text-xs font-accent uppercase tracking-wider text-text-muted mb-6">
          <a routerLink="/eventos" class="hover:text-cyan-brand">Eventos</a>
          <span>/</span>
          @if (event()) { <span>{{ event()!.nombre }}</span><span>/</span> }
          <span class="text-cyan-brand">Inscripción</span>
        </nav>

        <h1 class="font-heading text-4xl md:text-5xl mb-2">Inscripción al evento</h1>
        <p class="text-text-muted mb-10">Completa los 3 pasos para asegurar tu cupo en el circuito ALAS Latin Tour 2026.</p>

        <!-- STEP INDICATOR -->
        <div class="flex items-start justify-center mb-12 max-w-2xl mx-auto">
          @for (s of stepDefs; track s.num; let last = $last) {
            <div class="flex flex-col items-center">
              <div class="step-circle"
                   [class.active]="step() === s.num"
                   [class.done]="step() > s.num">
                @if (step() > s.num) {
                  <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                } @else {
                  {{ s.num }}
                }
              </div>
              <p class="mt-3 font-accent uppercase tracking-wider text-xs"
                 [class]="step() >= s.num ? 'text-cyan-brand' : 'text-text-muted'">
                {{ s.label }}
              </p>
            </div>
            @if (!last) {
              <div class="step-line" [class.done]="step() > s.num"></div>
            }
          }
        </div>

        <!-- EVENT SUMMARY -->
        @if (event()) {
          <div class="bg-gradient-to-r from-navy-dark via-navy-dark to-navy-mid border border-navy-mid rounded-xl p-5 mb-8 flex flex-col md:flex-row md:items-center gap-4">
            <div class="flex items-center gap-3">
              <span class="text-3xl">{{ flagOf(event()!.pais) }}</span>
              <div>
                <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs">Evento</p>
                <h2 class="font-heading text-xl leading-tight">{{ event()!.nombre }}</h2>
              </div>
            </div>
            <div class="hidden md:block w-px h-10 bg-navy-mid"></div>
            <div class="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm">
              <div><span class="text-text-muted">Sede:</span> <span>{{ event()!.ciudad }}, {{ event()!.pais }}</span></div>
              <div><span class="text-text-muted">Fechas:</span> <span>{{ dateRange(event()!.fechaInicio, event()!.fechaFin) }}</span></div>
              <app-star-rating [value]="event()!.stars" />
            </div>
          </div>
        } @else if (loadingEvent()) {
          <div class="skeleton h-24 rounded-xl mb-8"></div>
        }

        <!-- STEP 1 — DATOS -->
        @if (step() === 1) {
          <div class="bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">
            <h3 class="font-heading text-2xl mb-1">Datos del competidor</h3>
            <p class="text-sm text-text-muted mb-6">Verifica que tus datos sean correctos.</p>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div>
                <label class="block font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Nombre completo</label>
                <input type="text" [value]="auth.currentUser()?.fullName ?? ''" readonly class="input-field w-full px-4 py-3 rounded-md opacity-70" />
              </div>
              <div>
                <label class="block font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Email registrado</label>
                <input type="email" [value]="auth.currentUser()?.email ?? ''" readonly class="input-field w-full px-4 py-3 rounded-md opacity-70" />
              </div>
              <div>
                <label class="flex items-center justify-between font-accent uppercase tracking-wider text-xs mb-2">
                  <span class="text-cyan-brand">Número de camiseta</span>
                  <span class="text-text-muted normal-case">(editable)</span>
                </label>
                <input type="number" min="0" max="99" [(ngModel)]="shirtNumber"
                       class="input-field w-full px-4 py-3 rounded-md font-heading text-lg" placeholder="Ej: 23" />
              </div>
            </div>

            <div class="mt-8 pt-6 border-t border-navy-mid">
              <label class="flex items-start gap-3 cursor-pointer group">
                <input type="checkbox" [(ngModel)]="reglamentoAccepted"
                       class="mt-1 w-5 h-5 rounded border-navy-mid bg-navy-deepest accent-cyan-brand" />
                <span class="text-sm text-text-light group-hover:text-cyan-brand transition">
                  Confirmo que he leído y acepto el
                  <a href="#" class="text-cyan-brand hover:underline">reglamento del evento</a>
                  y las <a href="#" class="text-cyan-brand hover:underline">políticas del circuito ALAS</a>.
                </span>
              </label>
            </div>

            <div class="mt-8 flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3">
              <a routerLink="/eventos" class="text-sm text-text-muted hover:text-cyan-brand font-accent uppercase tracking-wider">← Volver a eventos</a>
              <button (click)="goToStep2()"
                      [disabled]="!reglamentoAccepted"
                      class="px-7 py-3 rounded-md font-accent uppercase tracking-wider text-sm transition shadow-lg"
                      [class]="reglamentoAccepted ? 'bg-orange-brand hover:bg-orange-light text-white shadow-orange-brand/20' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
                Siguiente →
              </button>
            </div>
          </div>
        }

        <!-- STEP 2 — CATEGORÍA -->
        @if (step() === 2) {
          <div class="bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">
            <h3 class="font-heading text-2xl mb-1">Selecciona tu categoría</h3>
            <p class="text-sm text-text-muted mb-6">Elige la categoría en la que quieres competir.</p>

            @if (loadingCategories()) {
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                @for (sk of skeletons; track sk) {
                  <div class="skeleton h-28 rounded-xl"></div>
                }
              </div>
            } @else {
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                @for (cat of categories(); track cat.id) {
                  <div class="category-card rounded-xl p-5"
                       [class.selected]="selectedCategoryId() === cat.id"
                       [class.opacity-50]="isFull(cat)"
                       (click)="selectCategory(cat)">
                    <div class="flex items-start justify-between gap-3">
                      <div class="flex items-center gap-3">
                        <div class="w-5 h-5 rounded-full border-2 border-cyan-brand flex items-center justify-center flex-shrink-0">
                          @if (selectedCategoryId() === cat.id) {
                            <div class="w-2.5 h-2.5 rounded-full bg-cyan-brand"></div>
                          }
                        </div>
                        <div>
                          <h4 class="font-heading text-lg leading-tight">{{ cat.nombre }}</h4>
                          <p class="text-xs font-accent uppercase tracking-wider mt-0.5"
                             [class]="cat.tipo === 'Principal' ? 'text-cyan-brand' : 'text-text-muted'">
                            {{ cat.tipo }}
                          </p>
                        </div>
                      </div>
                      <span class="font-heading text-xl text-cyan-brand">{{ formatUSD(cat.tarifa) }}</span>
                    </div>
                    @if (cat.descripcion) {
                      <p class="text-xs text-text-muted mt-3 leading-relaxed">{{ cat.descripcion }}</p>
                    }
                    @if (isFull(cat)) {
                      <p class="text-xs text-error-brand mt-2 font-accent uppercase tracking-wider">Categoría llena</p>
                    }
                  </div>
                }
              </div>
            }

            @if (selectedCategory()) {
              <div class="mt-6 p-5 rounded-xl bg-navy-deepest border border-navy-mid flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div>
                  <p class="font-accent uppercase tracking-wider text-xs text-text-muted">Categoría seleccionada</p>
                  <p class="font-heading text-xl mt-1">{{ selectedCategory()!.nombre }}</p>
                </div>
                <div class="text-right">
                  <p class="font-accent uppercase tracking-wider text-xs text-text-muted">Tarifa</p>
                  <p class="font-heading text-3xl text-cyan-brand mt-1">{{ formatUSD(selectedCategory()!.tarifa) }}<span class="text-base text-text-muted ml-1">USD</span></p>
                </div>
              </div>
            }

            <div class="mt-8 flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3">
              <button (click)="step.set(1)" class="px-5 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition">← Anterior</button>
              <button (click)="goToStep3()"
                      [disabled]="!selectedCategoryId()"
                      class="px-7 py-3 rounded-md font-accent uppercase tracking-wider text-sm transition shadow-lg"
                      [class]="selectedCategoryId() ? 'bg-orange-brand hover:bg-orange-light text-white shadow-orange-brand/20' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
                Siguiente →
              </button>
            </div>
          </div>
        }

        <!-- STEP 3 — PAGO -->
        @if (step() === 3) {
          <div class="bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">
            <h3 class="font-heading text-2xl mb-1">Método de pago</h3>
            <p class="text-sm text-text-muted mb-6">Elige cómo deseas pagar tu inscripción.</p>

            <div class="bg-navy-deepest border border-navy-mid rounded-xl p-5 mb-6">
              <p class="font-accent uppercase tracking-wider text-cyan-brand text-xs mb-4">Resumen de inscripción</p>
              <div class="space-y-3 text-sm">
                <div class="flex justify-between"><span class="text-text-muted">Evento</span><span>{{ event()?.nombre }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Categoría</span><span>{{ selectedCategory()?.nombre }}</span></div>
                @if (shirtNumber) {
                  <div class="flex justify-between"><span class="text-text-muted">Camiseta solicitada</span><span>#{{ shirtNumber }}</span></div>
                }
                <div class="flex justify-between"><span class="text-text-muted">Tarifa de categoría</span><span>{{ formatUSD(selectedCategory()?.tarifa ?? 0) }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Cuota administrativa</span><span>{{ formatUSD(5) }}</span></div>
                <div class="pt-3 border-t border-navy-mid flex justify-between items-baseline">
                  <span class="font-heading text-lg">Total</span>
                  <span class="font-heading text-3xl text-cyan-brand">{{ formatUSD(totalAmount()) }}<span class="text-base ml-1">USD</span></span>
                </div>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
              <div class="payment-card rounded-xl p-5" [class.selected]="paymentMethod() === 'paypal'" (click)="paymentMethod.set('paypal')">
                <div class="flex items-start gap-3 mb-3">
                  <div class="w-5 h-5 rounded-full border-2 border-cyan-brand flex items-center justify-center flex-shrink-0 mt-1">
                    @if (paymentMethod() === 'paypal') { <div class="w-2.5 h-2.5 rounded-full bg-cyan-brand"></div> }
                  </div>
                  <div class="flex-1">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="font-heading text-lg">PayPal</span>
                      <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand border border-success-brand/30">Recomendado</span>
                    </div>
                    <p class="text-xs text-text-muted leading-relaxed">Pago seguro online. Confirmación inmediata.</p>
                  </div>
                </div>
                @if (paymentMethod() === 'paypal') {
                  <div class="mt-3 pt-3 border-t border-navy-mid">
                    <button class="w-full py-3 rounded-md bg-yellow-400 hover:bg-yellow-300 transition font-heading italic text-navy-deepest text-lg">
                      Pagar con PayPal
                    </button>
                    <p class="text-[10px] text-text-muted text-center mt-2">Serás redirigido al checkout seguro de PayPal.</p>
                  </div>
                }
              </div>

              <div class="payment-card rounded-xl p-5" [class.selected]="paymentMethod() === 'beach'" (click)="paymentMethod.set('beach')">
                <div class="flex items-start gap-3">
                  <div class="w-5 h-5 rounded-full border-2 border-cyan-brand flex items-center justify-center flex-shrink-0 mt-1">
                    @if (paymentMethod() === 'beach') { <div class="w-2.5 h-2.5 rounded-full bg-cyan-brand"></div> }
                  </div>
                  <div class="flex-1">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="font-heading text-lg">Pago en Playa</span>
                      <span class="px-2 py-0.5 rounded-full text-[10px] font-accent uppercase tracking-wider bg-orange-brand/15 text-orange-brand border border-orange-brand/30">Efectivo</span>
                    </div>
                    <p class="text-xs text-text-muted leading-relaxed">Paga al llegar al evento. Requiere aprobación administrativa y token de 24 h.</p>
                  </div>
                  <svg class="h-8 w-8 text-orange-brand flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                      d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"/>
                  </svg>
                </div>
              </div>
            </div>

            @if (paymentMethod() === 'beach') {
              <div class="mb-6 p-5 rounded-xl bg-orange-brand/8 border border-orange-brand/30 flex items-start gap-3">
                <svg class="h-6 w-6 text-orange-brand flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                </svg>
                <div>
                  <p class="font-heading text-base text-orange-brand mb-1">Importante: solicitud de token</p>
                  <p class="text-sm text-text-muted leading-relaxed">
                    Notificaremos al administrador. Cuando apruebe, recibirás un
                    <strong class="text-text-light">token por correo</strong> con validez de
                    <strong class="text-orange-brand">24 horas</strong>. Si expira, deberás solicitar uno nuevo.
                  </p>
                </div>
              </div>
            }

            @if (errorMessage()) {
              <div class="mb-4 p-4 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
                {{ errorMessage() }}
              </div>
            }

            <div class="flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3">
              <button (click)="step.set(2)" class="px-5 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition">← Anterior</button>
              <button (click)="confirm()"
                      [disabled]="!paymentMethod() || submitting()"
                      class="px-8 py-3 rounded-md font-accent uppercase tracking-wider text-sm transition shadow-lg"
                      [class]="paymentMethod() && !submitting() ? 'bg-orange-brand hover:bg-orange-light text-white shadow-orange-brand/20' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
                {{ submitting() ? 'Procesando...' : 'Confirmar y pagar' }}
              </button>
            </div>

            <p class="text-xs text-text-muted text-center mt-6 leading-relaxed">
              Al confirmar aceptas el reglamento del circuito. Para pagos en playa, tu cupo queda reservado pero pendiente hasta el pago presencial.
            </p>
          </div>
        }
      </div>
    </section>
  `,
})
export class InscripcionComponent implements OnInit {
  readonly eventId = input<string>('');

  private api = inject(ApiService);
  auth = inject(AuthService);
  private router = inject(Router);

  step = signal(1);
  event = signal<EventDetail | null>(null);
  categories = signal<EventCategory[]>([]);
  competitorGender = signal<'Masculino' | 'Femenino' | 'PrefieroNoIndicar' | null>(null);
  loadingEvent = signal(true);
  loadingCategories = signal(false);
  submitting = signal(false);
  errorMessage = signal('');
  selectedCategoryId = signal('');
  paymentMethod = signal<'paypal' | 'beach' | ''>('');
  reglamentoAccepted = false;
  shirtNumber: number | null = null;
  readonly skeletons = [1, 2, 3, 4];

  stepDefs = [
    { num: 1, label: 'Datos' },
    { num: 2, label: 'Categoría' },
    { num: 3, label: 'Pago' },
  ];

  selectedCategory = computed(() =>
    this.categories().find(c => c.id === this.selectedCategoryId()) ?? null
  );

  totalAmount = computed(() => (this.selectedCategory()?.tarifa ?? 0) + 5);

  constructor() {
    effect(() => {
      const id = this.eventId();
      if (id) this.loadEvent(id);
    });
  }

  ngOnInit(): void {
    const id = this.eventId();
    if (id) this.loadEvent(id);
  }

  private async loadEvent(id: string): Promise<void> {
    this.loadingEvent.set(true);
    try {
      const res = await this.api.get<any>(`/events/${id}`);
      this.event.set(res?.data ?? res);
    } catch {
      this.event.set(null);
    } finally {
      this.loadingEvent.set(false);
    }
  }

  private async loadCategories(): Promise<void> {
    this.loadingCategories.set(true);
    try {
      await this.loadCompetitorGender();
      const res = await this.api.get<any>(`/events/${this.eventId()}/categories`);
      const raw: any[] = res?.data ?? [];
      const mapped = raw.map(c => ({
        id: c.categoryId,
        nombre: c.categoryName,
        tipo: c.tipo ?? '',
        gender: this.normalizeCategoryGender(c.gender),
        tarifa: c.effectiveTariffUsd ?? c.customTariffUsd ?? 0,
        inscritos: c.enrolledCount ?? 0,
        capacidad: c.capacidad ?? null,
        descripcion: c.descripcion,
      }));

      this.categories.set(mapped.filter(c => this.isCategoryGenderCompatible(c)));
    } catch {
      this.categories.set([]);
    } finally {
      this.loadingCategories.set(false);
    }
  }

  private async loadCompetitorGender(): Promise<void> {
    if (this.competitorGender()) return;

    const competitorId = this.auth.currentUser()?.competitorId;
    if (!competitorId) return;

    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}`);
      const data = res?.data ?? res;
      this.competitorGender.set(this.normalizeCompetitorGender(data?.genero ?? data?.gender));
    } catch {
      this.competitorGender.set(null);
    }
  }

  private isCategoryGenderCompatible(cat: EventCategory): boolean {
    const gender = this.competitorGender();
    if (!gender || !cat.gender || cat.gender === 'Ambos') {
      return true;
    }

    if (gender === 'PrefieroNoIndicar') {
      return false;
    }

    return cat.gender === gender;
  }

  private normalizeCategoryGender(value: unknown): EventCategory['gender'] {
    const text = String(value ?? '').toLowerCase();
    if (text.includes('masculino')) return 'Masculino';
    if (text.includes('femenino')) return 'Femenino';
    if (text.includes('ambos')) return 'Ambos';
    return '';
  }

  private normalizeCompetitorGender(value: unknown): 'Masculino' | 'Femenino' | 'PrefieroNoIndicar' | null {
    const text = String(value ?? '').toLowerCase();
    if (text.includes('masculino')) return 'Masculino';
    if (text.includes('femenino')) return 'Femenino';
    if (text.includes('prefiero')) return 'PrefieroNoIndicar';
    return null;
  }

  selectCategory(cat: EventCategory): void {
    if (!this.isFull(cat)) {
      this.selectedCategoryId.set(cat.id);
    }
  }

  isFull(cat: EventCategory): boolean {
    return cat.capacidad !== null && cat.inscritos >= cat.capacidad;
  }

  goToStep2(): void {
    if (this.reglamentoAccepted) {
      this.step.set(2);
      if (this.categories().length === 0) this.loadCategories();
    }
  }

  goToStep3(): void {
    if (this.selectedCategoryId()) this.step.set(3);
  }

  async confirm(): Promise<void> {
    if (!this.paymentMethod() || this.submitting()) return;
    this.submitting.set(true);
    this.errorMessage.set('');
    try {
      const inscRes = await this.api.post<any>('/inscriptions', {
        competitorId: this.auth.currentUser()?.competitorId,
        eventId: this.eventId(),
        categoryId: this.selectedCategoryId(),
        paymentMethod: this.paymentMethod() === 'beach' ? 'beach' : 'Paypal',
        shirtNumber: this.shirtNumber != null ? String(this.shirtNumber) : undefined,
        reglamento: true,
      });
      const inscriptionId: string = inscRes?.data?.id ?? inscRes?.id;

      if (this.paymentMethod() === 'beach') {
        this.router.navigate(['/pago-playa', inscriptionId]);
      } else {
        // PayPal: call /payments to create payment session and get checkout URL
        const payRes = await this.api.post<any>('/payments', {
          inscriptionId,
          method: 'paypal',
        });
        const paypalUrl: string | undefined = payRes?.data?.paypalUrl ?? payRes?.paypalUrl ?? payRes?.checkoutUrl;
        if (paypalUrl) {
          window.location.href = paypalUrl;
        } else {
          this.router.navigate(['/mi-panel/inscripciones']);
        }
      }
    } catch (err: any) {
      this.errorMessage.set(err?.body?.message ?? err?.message ?? 'Error al procesar la inscripción. Inténtalo de nuevo.');
    } finally {
      this.submitting.set(false);
    }
  }

  flagOf(code: string): string { return FLAGS[code] ?? '🏄'; }
  formatUSD(n: number): string { return '$' + n.toLocaleString('en-US'); }

  dateRange(start: string, end: string): string {
    if (!start || !end) return '';
    const s = new Date(start), e = new Date(end);
    const months = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    return `${s.getDate()} - ${e.getDate()} de ${months[s.getMonth()]}`;
  }
}
