import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule, provideTranslocoScope } from '@jsverse/transloco';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { LocalizePathPipe } from '../../../shared/pipes/localize-path.pipe';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-paypal-return',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, TranslocoModule, LocalizePathPipe],
  providers: [provideTranslocoScope('competitor')],
  template: `
    <section class="py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-2xl mx-auto bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">
        <p class="font-accent uppercase tracking-wider text-xs text-cyan-brand mb-3">PayPal</p>

        @if (status() === 'loading') {
          <h1 class="font-heading text-3xl mb-2">{{ 'competitor.paypal.confirming' | transloco }}</h1>
          <p class="text-text-muted mb-6">{{ 'competitor.paypal.confirmingText' | transloco }}</p>
          <app-loading-spinner [label]="'competitor.paypal.confirmingSpinner' | transloco" />
        } @else if (status() === 'success') {
          <h1 class="font-heading text-3xl mb-2">{{ 'competitor.paypal.successTitle' | transloco }}</h1>
          <p class="text-text-muted mb-6">{{ 'competitor.paypal.successText' | transloco }}</p>
          <div class="flex flex-col sm:flex-row gap-3">
            <a [routerLink]="'/mi-panel/inscripciones' | localizePath" class="px-6 py-3 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition text-center">
              {{ 'competitor.paypal.myInscriptions' | transloco }}
            </a>
            <a [routerLink]="'/eventos' | localizePath" class="px-6 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition text-center">
              {{ 'competitor.paypal.backToEvents' | transloco }}
            </a>
          </div>
        } @else {
          <h1 class="font-heading text-3xl mb-2">{{ (status() === 'cancelled' ? 'competitor.paypal.cancelledTitle' : 'competitor.paypal.errorTitle') | transloco }}</h1>
          <p class="text-text-muted mb-6">{{ message() || (messageKey() | transloco) }}</p>
          <div class="flex flex-col sm:flex-row gap-3">
            <a [routerLink]="'/mi-panel/inscripciones' | localizePath" class="px-6 py-3 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition text-center">
              {{ 'competitor.paypal.goToInscriptions' | transloco }}
            </a>
            <button type="button" (click)="goBackToEvent()" class="px-6 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition">
              {{ 'competitor.paypal.retry' | transloco }}
            </button>
          </div>
        }
      </div>
    </section>
  `,
})
export class PaypalReturnComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly language = inject(LanguageService);

  readonly status = signal<'loading' | 'success' | 'error' | 'cancelled'>('loading');
  /** Server-provided message; when empty the template shows the translated `messageKey`. */
  readonly message = signal('');
  readonly messageKey = signal('competitor.paypal.processing');

  async ngOnInit(): Promise<void> {
    const path = this.route.snapshot.routeConfig?.path ?? '';
    const inscriptionId = this.route.snapshot.queryParamMap.get('inscriptionId') ?? '';

    if (path === 'paypal/cancelado') {
      this.status.set('cancelled');
      this.messageKey.set('competitor.paypal.cancelledText');
      return;
    }

    const orderId = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!inscriptionId || !orderId) {
      this.status.set('error');
      this.messageKey.set('competitor.paypal.missingData');
      return;
    }

    try {
      await this.api.post(`/paypal/orders/${encodeURIComponent(orderId)}/capture`, { inscriptionId });
      this.status.set('success');
      this.messageKey.set('competitor.paypal.successText');
    } catch (err: any) {
      this.status.set('error');
      this.messageKey.set('competitor.paypal.captureError');
      this.message.set(err?.body?.message ?? err?.message ?? '');
    }
  }

  goBackToEvent(): void {
    this.router.navigateByUrl(this.language.localize('/mi-panel/inscripciones'));
  }
}
