import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-paypal-return',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent],
  template: `
    <section class="py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-2xl mx-auto bg-navy-dark border border-navy-mid rounded-2xl p-6 md:p-8">
        <p class="font-accent uppercase tracking-wider text-xs text-cyan-brand mb-3">PayPal</p>

        @if (status() === 'loading') {
          <h1 class="font-heading text-3xl mb-2">Confirmando pago</h1>
          <p class="text-text-muted mb-6">Estamos validando tu orden con PayPal y registrando la inscripción.</p>
          <app-loading-spinner label="Confirmando pago con PayPal..." />
        } @else if (status() === 'success') {
          <h1 class="font-heading text-3xl mb-2">Pago confirmado</h1>
          <p class="text-text-muted mb-6">Tu inscripción fue confirmada correctamente.</p>
          <div class="flex flex-col sm:flex-row gap-3">
            <a routerLink="/mi-panel/inscripciones" class="px-6 py-3 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition text-center">
              Ver mis inscripciones
            </a>
            <a routerLink="/eventos" class="px-6 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition text-center">
              Volver a eventos
            </a>
          </div>
        } @else {
          <h1 class="font-heading text-3xl mb-2">{{ status() === 'cancelled' ? 'Pago cancelado' : 'No se pudo confirmar el pago' }}</h1>
          <p class="text-text-muted mb-6">{{ message() }}</p>
          <div class="flex flex-col sm:flex-row gap-3">
            <a routerLink="/mi-panel/inscripciones" class="px-6 py-3 rounded-md bg-orange-brand hover:bg-orange-light text-white font-accent uppercase tracking-wider text-sm transition text-center">
              Ir a mis inscripciones
            </a>
            <button type="button" (click)="goBackToEvent()" class="px-6 py-3 rounded-md border border-navy-mid hover:border-cyan-brand text-text-light font-accent uppercase tracking-wider text-sm transition">
              Intentar nuevamente
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

  readonly status = signal<'loading' | 'success' | 'error' | 'cancelled'>('loading');
  readonly message = signal('Procesando respuesta de PayPal...');

  async ngOnInit(): Promise<void> {
    const path = this.route.snapshot.routeConfig?.path ?? '';
    const inscriptionId = this.route.snapshot.queryParamMap.get('inscriptionId') ?? '';

    if (path === 'paypal/cancelado') {
      this.status.set('cancelled');
      this.message.set('Cancelaste el pago en PayPal. La inscripción quedó creada pero pendiente de completar el pago.');
      return;
    }

    const orderId = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!inscriptionId || !orderId) {
      this.status.set('error');
      this.message.set('No recibimos los datos necesarios para confirmar el pago con PayPal.');
      return;
    }

    try {
      await this.api.post(`/paypal/orders/${encodeURIComponent(orderId)}/capture`, { inscriptionId });
      this.status.set('success');
      this.message.set('Tu inscripción fue confirmada correctamente.');
    } catch (err: any) {
      this.status.set('error');
      this.message.set(err?.body?.message ?? err?.message ?? 'Ocurrió un error al confirmar el pago con PayPal.');
    }
  }

  goBackToEvent(): void {
    this.router.navigate(['/mi-panel/inscripciones']);
  }
}
