import { Component, inject, signal, computed, input, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslocoModule, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ApiService } from '../../../core/services/api.service';
import { LocaleFormatService } from '../../../core/i18n/locale-format.service';
import { LocalizePathPipe } from '../../../shared/pipes/localize-path.pipe';

type BeachPaymentState = 'request' | 'pending' | 'enter_token' | 'confirmed' | 'expired';

interface InscriptionSummary {
  eventoNombre: string;
  eventoPais: string;
  categoria: string;
  baseAmountUsd: number;
  administrativeFeeUsd: number | null;
  montoUsd: number;
}

const TOTAL_SECONDS = 24 * 3600;

@Component({
  selector: 'app-pago-playa',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslocoModule, LocalizePathPipe],
  providers: [provideTranslocoScope('competitor')],
  template: `
    <section class="py-10 px-4 sm:px-6 lg:px-8">
      <div class="max-w-2xl mx-auto">

        <nav class="flex items-center gap-2 text-xs font-accent uppercase tracking-wider text-text-muted mb-6">
          <a [routerLink]="'/eventos' | localizePath" class="hover:text-cyan-brand">{{ 'competitor.inscription.breadcrumbEvents' | transloco }}</a>
          <span>/</span>
          <span class="text-cyan-brand">{{ 'competitor.beach.breadcrumb' | transloco }}</span>
        </nav>

        <h1 class="font-heading text-4xl md:text-5xl mb-2">{{ 'competitor.beach.title' | transloco }}</h1>
        <p class="text-text-muted mb-8">{{ 'competitor.beach.intro' | transloco }}</p>

        <!-- Inscription info banner -->
        @if (inscription()) {
          <div class="bg-navy-dark border border-navy-mid rounded-xl p-5 mb-8">
            <div class="flex items-center gap-4">
              <div class="text-3xl">{{ inscription()!.eventoPais }}</div>
              <div>
                <p class="font-accent uppercase text-cyan-brand text-xs tracking-wider">{{ 'competitor.beach.inscriptionNumber' | transloco: { id: inscriptionId() } }}</p>
                <h2 class="font-heading text-xl leading-tight">{{ inscription()!.eventoNombre }}</h2>
                <p class="text-sm text-text-muted">{{ inscription()!.categoria }}</p>
              </div>
            </div>
            <div class="mt-4 pt-4 border-t border-navy-mid space-y-1.5 text-sm">
              <div class="flex justify-between"><span class="text-text-muted">{{ 'competitor.beach.categoryFee' | transloco }}</span><span>{{ formatUSD(inscription()!.baseAmountUsd) }}</span></div>
              @if (inscription()!.administrativeFeeUsd) {
                <div class="flex justify-between"><span class="text-text-muted">{{ 'competitor.beach.adminFee' | transloco }}</span><span>{{ formatUSD(inscription()!.administrativeFeeUsd!) }}</span></div>
              }
              <div class="flex justify-between items-baseline pt-1.5 border-t border-navy-mid/60">
                <span class="font-heading">{{ 'competitor.inscription.total' | transloco }}</span>
                <span class="font-heading text-xl text-cyan-brand">{{ formatUSD(inscription()!.montoUsd) }}<span class="text-xs ml-1">USD</span></span>
              </div>
            </div>
          </div>
        }

        <!-- State machine -->
        <div class="bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">

          <!-- STATE: request -->
          @if (state() === 'request') {
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-orange-brand/15 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-orange-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                    d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"/>
                </svg>
              </div>
              <h3 class="font-heading text-2xl mb-2">{{ 'competitor.beach.requestTitle' | transloco }}</h3>
              <p class="text-text-muted text-sm mb-6 leading-relaxed max-w-md mx-auto"
                 [innerHTML]="'competitor.beach.requestText' | transloco"></p>

              <div class="bg-navy-deepest border border-navy-mid/60 rounded-xl p-5 text-left mb-6 space-y-2 text-sm">
                <div class="flex items-center gap-2 text-text-muted"><span class="text-warning-brand">1.</span> {{ 'competitor.beach.steps.s1' | transloco }}</div>
                <div class="flex items-center gap-2 text-text-muted"><span class="text-warning-brand">2.</span> {{ 'competitor.beach.steps.s2' | transloco }}</div>
                <div class="flex items-center gap-2 text-text-muted"><span class="text-warning-brand">3.</span> {{ 'competitor.beach.steps.s3' | transloco }}</div>
                <div class="flex items-center gap-2 text-text-muted"><span class="text-warning-brand">4.</span> {{ 'competitor.beach.steps.s4' | transloco }}</div>
                <div class="flex items-center gap-2 text-text-muted"><span class="text-warning-brand">5.</span> {{ 'competitor.beach.steps.s5' | transloco }}</div>
              </div>

              @if (errorMsg()) {
                <div class="mb-5 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm text-left">
                  {{ errorMsg() }}
                </div>
              }

              <div class="flex flex-col sm:flex-row items-center justify-center gap-3">
                <button (click)="requestToken()" [disabled]="submitting()"
                  class="px-8 py-3 rounded-md bg-orange-brand hover:bg-orange-light disabled:opacity-60 text-white font-accent uppercase tracking-wider text-sm transition shadow-lg shadow-orange-brand/20">
                  {{ (submitting() ? 'competitor.beach.sendingRequest' : 'competitor.beach.requestCode') | transloco }}
                </button>

                <button (click)="openTokenEntry()"
                  class="px-8 py-3 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-sm transition">
                  {{ 'competitor.beach.haveToken' | transloco }}
                </button>
              </div>

              <p class="mt-4 text-xs text-text-muted">
                {{ 'competitor.beach.haveTokenHint' | transloco }}
              </p>
            </div>
          }

          <!-- STATE: pending -->
          @if (state() === 'pending') {
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-warning-brand/15 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-warning-brand animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                </svg>
              </div>
              <h3 class="font-heading text-2xl mb-2">{{ 'competitor.beach.pendingTitle' | transloco }}</h3>
              <p class="text-text-muted text-sm mb-6 leading-relaxed">
                {{ 'competitor.beach.pendingText' | transloco }}
              </p>
              <p class="text-xs text-text-muted mb-8">{{ 'competitor.beach.pendingHint' | transloco }}</p>

              <div class="pt-5 border-t border-navy-mid">
                <p class="text-sm text-text-muted mb-3">{{ 'competitor.beach.receivedToken' | transloco }}</p>
                <button (click)="state.set('enter_token')"
                  class="px-6 py-2.5 rounded-md border border-cyan-brand text-cyan-brand hover:bg-cyan-brand hover:text-navy-deepest font-accent uppercase tracking-wider text-xs transition">
                  {{ 'competitor.beach.enterMyToken' | transloco }}
                </button>
              </div>
            </div>
          }

          <!-- STATE: enter_token -->
          @if (state() === 'enter_token') {
            <div>
              <h3 class="font-heading text-2xl mb-1">{{ 'competitor.beach.enterTitle' | transloco }}</h3>
              <p class="text-text-muted text-sm mb-6">{{ 'competitor.beach.enterText' | transloco }}</p>

              <!-- Countdown -->
              <div class="mb-6 p-4 rounded-xl flex items-center justify-between"
                   [class]="countdownUrgent() ? 'bg-error-brand/10 border border-error-brand/40' : 'bg-navy-deepest border border-navy-mid'">
                <div>
                  <p class="font-accent uppercase text-xs tracking-wider"
                     [class]="countdownUrgent() ? 'text-error-brand' : 'text-text-muted'">
                    {{ 'competitor.beach.timeLeft' | transloco }}
                  </p>
                  <p class="font-heading text-3xl mt-1"
                     [class]="countdownUrgent() ? 'text-error-brand animate-pulse' : 'text-text-light'">
                    {{ countdownDisplay() }}
                  </p>
                </div>
                @if (countdownUrgent()) {
                  <svg class="w-8 h-8 text-error-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                  </svg>
                }
              </div>

              <div class="mb-5">
                <label class="block font-accent uppercase text-xs tracking-wider text-text-muted mb-2">{{ 'competitor.beach.tokenLabel' | transloco }}</label>
                <input [(ngModel)]="tokenValue" type="text" maxlength="9"
                       placeholder="ABCD-1234"
                       class="input-field text-center font-heading text-2xl tracking-[0.3em] uppercase"
                       (input)="tokenValue = tokenValue.toUpperCase()" />
              </div>

              @if (errorMsg()) {
                <div class="mb-5 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
                  {{ errorMsg() }}
                </div>
              }

              <button (click)="redeemToken()" [disabled]="tokenValue.length < 9 || submitting()"
                class="w-full py-3 rounded-md font-accent uppercase tracking-wider text-sm transition"
                [class]="tokenValue.length >= 6 && !submitting() ? 'bg-orange-brand hover:bg-orange-light text-white shadow-lg shadow-orange-brand/20' : 'bg-navy-mid text-text-muted cursor-not-allowed'">
                {{ (submitting() ? 'competitor.beach.verifying' : 'competitor.beach.confirm') | transloco }}
              </button>
            </div>
          }

          <!-- STATE: expired -->
          @if (state() === 'expired') {
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-error-brand/15 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-error-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <h3 class="font-heading text-2xl text-error-brand mb-2">{{ 'competitor.beach.expiredTitle' | transloco }}</h3>
              <p class="text-text-muted text-sm mb-8 leading-relaxed">
                {{ 'competitor.beach.expiredText' | transloco }}
              </p>

              @if (errorMsg()) {
                <div class="mb-5 px-4 py-3 rounded-lg bg-error-brand/10 border border-error-brand/30 text-error-brand text-sm">
                  {{ errorMsg() }}
                </div>
              }

              <button (click)="requestToken()" [disabled]="submitting()"
                class="px-8 py-3 rounded-md bg-orange-brand hover:bg-orange-light disabled:opacity-60 text-white font-accent uppercase tracking-wider text-sm transition">
                {{ (submitting() ? 'competitor.beach.sending' : 'competitor.beach.rerequest') | transloco }}
              </button>
            </div>
          }

          <!-- STATE: confirmed -->
          @if (state() === 'confirmed') {
            <div class="text-center py-4">
              <div class="w-16 h-16 rounded-full bg-success-brand/20 flex items-center justify-center mx-auto mb-5">
                <svg class="w-8 h-8 text-success-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                </svg>
              </div>
              <h3 class="font-heading text-2xl text-success-brand mb-2">{{ 'competitor.beach.confirmedTitle' | transloco }}</h3>
              <p class="text-text-muted text-sm mb-2 leading-relaxed">
                {{ 'competitor.beach.confirmedText' | transloco }}
              </p>
              <p class="text-xs text-text-muted mb-8" [innerHTML]="'competitor.beach.confirmedStatus' | transloco"></p>
              <a [routerLink]="'/mi-panel/inscripciones' | localizePath"
                 class="px-8 py-3 rounded-md bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm transition font-bold inline-block">
                {{ 'competitor.beach.myInscriptions' | transloco }}
              </a>
            </div>
          }

        </div>

      </div>
    </section>
  `,
})
export class PagoPlayaComponent implements OnInit, OnDestroy {
  readonly inscriptionId = input<string>('');

  private api = inject(ApiService);
  private platformId = inject(PLATFORM_ID);
  private transloco = inject(TranslocoService);
  private localeFormat = inject(LocaleFormatService);

  state = signal<BeachPaymentState>('request');
  inscription = signal<InscriptionSummary | null>(null);
  submitting = signal(false);
  errorMsg = signal('');
  tokenValue = '';
  countdown = signal(TOTAL_SECONDS);

  countdownUrgent = computed(() => this.countdown() < 180);
  countdownDisplay = computed(() => {
    const s = this.countdown();
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    const sec = s % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
  });

  private countdownRef?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.loadInscription();
  }

  ngOnDestroy(): void {
    clearInterval(this.countdownRef);
  }

  private async loadInscription(): Promise<void> {
    const id = this.inscriptionId();
    if (!id) return;
    try {
      const res = await this.api.get<any>(`/inscriptions/${id}`);
      const d = res?.data ?? res;
      this.inscription.set({
        eventoNombre: d?.eventoNombre ?? d?.event?.nombre ?? '',
        eventoPais: d?.eventoPais ?? d?.event?.pais ?? '',
        categoria: d?.categoriaNombre ?? d?.categoria?.nombre ?? '',
        baseAmountUsd: Number(d?.baseAmountUsd ?? d?.montoUsd ?? 0),
        administrativeFeeUsd: d?.administrativeFeeUsd != null ? Number(d.administrativeFeeUsd) : null,
        montoUsd: Number(d?.montoUsd ?? 0),
      });
    } catch { /* optional */ }
  }

  async requestToken(): Promise<void> {
    this.submitting.set(true);
    this.errorMsg.set('');
    try {
      await this.api.post('/payments/beach/request', { inscriptionId: this.inscriptionId() });
      this.state.set('pending');
    } catch (err: any) {
      this.errorMsg.set(err?.body?.message ?? this.transloco.translate('competitor.beach.requestError'));
    } finally {
      this.submitting.set(false);
    }
  }

  openTokenEntry(): void {
    this.errorMsg.set('');
    this.startCountdown();
    this.state.set('enter_token');
  }

  async redeemToken(): Promise<void> {
    if (this.tokenValue.length < 9) return;
    this.submitting.set(true);
    this.errorMsg.set('');
    try {
      await this.api.post('/payments/beach/redeem', {
        inscriptionId: this.inscriptionId(),
        tokenCode: this.tokenValue,
      });
      clearInterval(this.countdownRef);
      this.state.set('confirmed');
    } catch (err: any) {
      const status = err?.status ?? err?.statusCode;
      if (status === 400) {
        clearInterval(this.countdownRef);
        this.state.set('expired');
        this.errorMsg.set(this.transloco.translate('competitor.beach.invalidToken'));
      } else {
        this.errorMsg.set(err?.body?.message ?? this.transloco.translate('competitor.beach.verifyError'));
      }
    } finally {
      this.submitting.set(false);
    }
  }

  formatUSD(n: number): string { return this.localeFormat.usd(n); }

  private startCountdown(seconds = TOTAL_SECONDS): void {
    if (!isPlatformBrowser(this.platformId)) return;
    clearInterval(this.countdownRef);
    this.countdown.set(seconds);
    this.countdownRef = setInterval(() => {
      this.countdown.update(n => {
        if (n <= 1) {
          clearInterval(this.countdownRef);
          this.state.set('expired');
          return 0;
        }
        return n - 1;
      });
    }, 1000);
  }

}
