import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

interface TokenRequest {
  id: string;
  time: string;
  name: string;
  initials: string;
  email: string;
  event: string;
  category: string;
  amount: number;
}

type HistEstado = 'Usado' | 'Expirado' | 'Rechazado' | 'Pendiente';

interface TokenHistoryRow {
  competidor: string;
  evento: string;
  estado: HistEstado;
  token: string;
  generado: string;
  expiracion: string;
  usadoEn: string;
}

const CLASS_INPUT = 'bg-navy-dark border border-navy-mid px-3 py-2 rounded-md text-sm text-text-light focus:outline-none focus:border-cyan-brand';

function initialsOf(name: string): string {
  return name.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase() ?? '').join('');
}

function fmt(dt: string | null | undefined): string {
  if (!dt) return '—';
  return new Date(dt).toLocaleString('es', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
}

@Component({
  selector: 'app-admin-tokens',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent],
  template: `
    <div class="space-y-10">
      <div>
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Pagos / Tokens</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Autorización de tokens de pago en playa</h1>
        <p class="text-sm text-text-muted mt-2">Solicitudes de pago en efectivo que requieren tu aprobación. Cada token aprobado se envía automáticamente al correo del competidor y tiene una validez de 24 horas.</p>
      </div>

      @if (loading()) {
        <app-loading-spinner />
      } @else {

      <!-- Stats bar -->
      <section class="flex flex-wrap gap-3">
        <div class="px-5 py-3 rounded-full bg-orange-brand/15 border border-orange-brand/40 flex items-center gap-3">
          <span class="w-2.5 h-2.5 rounded-full bg-orange-brand"></span>
          <span class="font-heading text-2xl text-orange-brand">{{ pendingRequests().length }}</span>
          <span class="font-accent uppercase tracking-wider text-sm text-orange-brand">Solicitudes pendientes</span>
        </div>
        <div class="px-5 py-3 rounded-full bg-success-brand/15 border border-success-brand/40 flex items-center gap-3">
          <svg class="h-4 w-4 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
          <span class="font-heading text-2xl text-success-brand">{{ approvedToday() }}</span>
          <span class="font-accent uppercase tracking-wider text-sm text-success-brand">Aprobados hoy</span>
        </div>
        <div class="px-5 py-3 rounded-full bg-error-brand/15 border border-error-brand/40 flex items-center gap-3">
          <svg class="h-4 w-4 text-error-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/></svg>
          <span class="font-heading text-2xl text-error-brand">{{ rejectedToday() }}</span>
          <span class="font-accent uppercase tracking-wider text-sm text-error-brand">Rechazados hoy</span>
        </div>
      </section>

      <!-- Pending requests -->
      <section>
        <div class="flex items-end justify-between gap-3 mb-5">
          <div>
            <p class="font-accent uppercase tracking-[0.3em] text-orange-brand text-xs mb-1">Cola de aprobación</p>
            <h2 class="font-heading text-2xl text-white">Solicitudes pendientes</h2>
          </div>
          <button (click)="refresh()" class="text-sm text-cyan-brand hover:text-cyan-dark font-accent uppercase tracking-wider flex items-center gap-1">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>
            Actualizar
          </button>
        </div>

        <div class="bg-navy-dark border border-orange-brand/30 rounded-xl overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-orange-brand/10 text-orange-brand font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="text-left px-5 py-3">Hora solicitud</th>
                  <th class="text-left px-3 py-3">Competidor</th>
                  <th class="text-left px-3 py-3">Email</th>
                  <th class="text-left px-3 py-3">Evento</th>
                  <th class="text-left px-3 py-3">Categoría</th>
                  <th class="text-right px-3 py-3">Monto</th>
                  <th class="text-right px-5 py-3">Acciones</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (req of pendingRequests(); track req.id) {
                  <tr class="hover:bg-cyan-brand/5 transition">
                    <td class="px-5 py-4 font-mono text-cyan-brand text-xs whitespace-nowrap">{{ req.time }}</td>
                    <td class="px-3 py-4">
                      <div class="flex items-center gap-2">
                        <div class="w-8 h-8 rounded-full bg-gradient-to-br from-cyan-brand to-orange-brand flex items-center justify-center font-heading font-bold text-navy-deepest text-xs">{{ req.initials }}</div>
                        <p class="font-medium text-text-light">{{ req.name }}</p>
                      </div>
                    </td>
                    <td class="px-3 py-4 text-text-muted text-xs">{{ req.email }}</td>
                    <td class="px-3 py-4 text-text-light">{{ req.event }}</td>
                    <td class="px-3 py-4 text-text-light">{{ req.category }}</td>
                    <td class="px-3 py-4 text-right text-cyan-brand font-heading text-base">\${{ req.amount }}</td>
                    <td class="px-5 py-4 text-right">
                      <div class="inline-flex gap-2">
                        <button (click)="openApprove(req)" class="px-4 py-2 rounded-md bg-success-brand hover:bg-green-600 text-white font-accent uppercase tracking-wider text-xs transition flex items-center gap-1 whitespace-nowrap">
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                          Aprobar y generar token
                        </button>
                        <button (click)="openReject(req)" class="px-3 py-2 rounded-md border border-error-brand text-error-brand hover:bg-error-brand hover:text-white font-accent uppercase tracking-wider text-xs transition">
                          Rechazar
                        </button>
                      </div>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7" class="px-5 py-10 text-center text-text-muted">
                      <svg class="h-12 w-12 mx-auto mb-3 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                      <p class="font-heading text-lg text-text-light">No hay solicitudes pendientes</p>
                      <p class="text-sm mt-1">Todas las solicitudes han sido procesadas.</p>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <!-- History -->
      <section>
        <div class="flex items-end justify-between gap-3 mb-5">
          <div>
            <p class="font-accent uppercase tracking-[0.3em] text-cyan-brand text-xs mb-1">Historial</p>
            <h2 class="font-heading text-2xl text-white">Últimos tokens procesados</h2>
          </div>
          <div class="flex items-center gap-3">
            <select [class]="CLASS_INPUT" [(ngModel)]="historyFilter" (ngModelChange)="onFilterChange()">
              <option value="">Todos los estados</option>
              <option value="Usado">Usados</option>
              <option value="Expirado">Expirados</option>
              <option value="Rechazado">Rechazados</option>
            </select>
          </div>
        </div>

        <div class="bg-navy-dark border border-navy-mid rounded-xl overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-navy-mid/30 text-text-muted font-accent uppercase tracking-wider text-xs">
                <tr>
                  <th class="text-left px-5 py-3">Competidor</th>
                  <th class="text-left px-3 py-3">Evento</th>
                  <th class="text-left px-3 py-3">Estado</th>
                  <th class="text-left px-3 py-3">Token</th>
                  <th class="text-left px-3 py-3">Generado</th>
                  <th class="text-left px-3 py-3">Expiración</th>
                  <th class="text-left px-5 py-3">Usado en</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid">
                @for (h of history(); track h.token + h.competidor) {
                  <tr class="hover:bg-cyan-brand/5 transition">
                    <td class="px-5 py-3 font-medium text-text-light">{{ h.competidor }}</td>
                    <td class="px-3 py-3 text-text-muted">{{ h.evento }}</td>
                    <td class="px-3 py-3"><span [class]="historyEstadoClass(h.estado)">{{ historyEstadoLabel(h.estado) }}</span></td>
                    <td class="px-3 py-3 font-mono text-xs" [class]="h.estado === 'Expirado' ? 'text-text-muted line-through' : (h.estado === 'Rechazado' ? 'text-text-muted' : 'text-cyan-brand')">{{ h.token }}</td>
                    <td class="px-3 py-3 text-text-muted text-xs">{{ h.generado }}</td>
                    <td class="px-3 py-3 text-text-muted text-xs">{{ h.expiracion }}</td>
                    <td class="px-5 py-3 text-xs" [class]="h.estado === 'Usado' ? 'text-success-brand' : 'text-text-muted'">{{ h.usadoEn }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-navy-mid flex items-center justify-between text-xs text-text-muted">
            <p>Mostrando {{ history().length }} de {{ totalHistory() }} registros</p>
          </div>
        </div>
      </section>

      <!-- Help callout -->
      <div class="bg-gradient-to-r from-cyan-brand/8 to-orange-brand/5 border border-cyan-brand/20 rounded-xl p-5 flex flex-col md:flex-row items-start md:items-center gap-4">
        <div class="w-12 h-12 rounded-lg bg-cyan-brand/15 border border-cyan-brand/30 flex items-center justify-center flex-shrink-0">
          <svg class="h-6 w-6 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
        </div>
        <div class="flex-1">
          <h3 class="font-heading text-lg text-white mb-1">Política de pagos en playa</h3>
          <p class="text-sm text-text-muted leading-relaxed">Cada token aprobado genera un código alfanumérico válido por 24 horas. La inscripción se reserva con estado "pago pendiente" y debe regularizarse con efectivo al llegar al evento.</p>
        </div>
      </div>

      <footer class="pt-6 border-t border-navy-mid flex flex-col md:flex-row items-center justify-between gap-3 text-xs text-text-muted">
        <p>© 2026 ALAS Latin Tour — Panel administrativo</p>
      </footer>
      }
    </div>

    <!-- Approve modal -->
    @if (modal() === 'approve') {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="closeModals()">
        <div class="bg-navy-dark border border-success-brand/40 rounded-2xl max-w-lg w-full p-6 md:p-8" (click)="$event.stopPropagation()">
          <div class="flex items-center gap-3 mb-5">
            <div class="w-12 h-12 rounded-full bg-success-brand/15 border border-success-brand/40 flex items-center justify-center">
              <svg class="h-6 w-6 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            </div>
            <div>
              <h3 class="font-heading text-xl text-white">Generar token de pago</h3>
              <p class="text-xs text-text-muted">Confirma los datos antes de enviar</p>
            </div>
          </div>

          @if (selectedReq(); as req) {
            <div class="bg-navy-deepest border border-navy-mid rounded-lg p-4 mb-5">
              <dl class="space-y-2 text-sm">
                <div class="flex justify-between"><dt class="text-text-muted">Competidor</dt><dd class="text-text-light">{{ req.name }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Email destino</dt><dd class="text-cyan-brand">{{ req.email }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Evento</dt><dd class="text-text-light">{{ req.event }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Categoría</dt><dd class="text-text-light">{{ req.category }}</dd></div>
                <div class="flex justify-between"><dt class="text-text-muted">Monto</dt><dd class="text-cyan-brand font-heading">\${{ req.amount }} USD</dd></div>
              </dl>
            </div>
          }

          <div class="bg-warning-brand/8 border border-warning-brand/30 rounded-lg p-4 mb-5 flex items-start gap-3">
            <svg class="h-5 w-5 text-warning-brand flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            <div class="text-sm">
              <p class="text-warning-brand font-semibold">Validez: 24 horas</p>
              <p class="text-text-muted text-xs mt-1">El token será enviado automáticamente al correo del competidor y expirará tras 24 horas.</p>
            </div>
          </div>

          <div class="flex flex-col-reverse sm:flex-row gap-3 sm:justify-end">
            <button (click)="closeModals()" class="px-5 py-2.5 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-sm transition">Cancelar</button>
            <button (click)="confirmApprove()" [disabled]="approving()" class="px-5 py-2.5 rounded-md bg-success-brand hover:bg-green-600 text-white font-accent uppercase tracking-wider text-sm transition flex items-center justify-center gap-2 disabled:opacity-50">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>
              {{ approving() ? 'Enviando...' : 'Confirmar y enviar token' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Reject modal -->
    @if (modal() === 'reject') {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="closeModals()">
        <div class="bg-navy-dark border border-error-brand/40 rounded-2xl max-w-lg w-full p-6 md:p-8" (click)="$event.stopPropagation()">
          <div class="flex items-center gap-3 mb-5">
            <div class="w-12 h-12 rounded-full bg-error-brand/15 border border-error-brand/40 flex items-center justify-center">
              <svg class="h-6 w-6 text-error-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </div>
            <div>
              <h3 class="font-heading text-xl text-white">Rechazar solicitud</h3>
              <p class="text-xs text-text-muted">Notifica al competidor del motivo</p>
            </div>
          </div>

          @if (selectedReq(); as req) {
            <div class="bg-navy-deepest border border-navy-mid rounded-lg p-4 mb-5">
              <p class="text-sm text-text-light"><span class="text-text-muted">Competidor:</span> {{ req.name }}</p>
              <p class="text-sm text-text-light mt-1"><span class="text-text-muted">Evento:</span> {{ req.event }}</p>
            </div>
          }

          <label class="block">
            <span class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2 block">Motivo del rechazo (mínimo 10 caracteres)</span>
            <textarea [(ngModel)]="rejectReason" rows="4" placeholder="Ej: Cupo agotado en la categoría seleccionada. Te invitamos a inscribirte en otro evento del circuito."
                      class="w-full bg-navy-deepest border border-navy-mid rounded-md px-4 py-3 text-sm text-text-light focus:outline-none focus:border-error-brand"></textarea>
          </label>
          <p class="text-xs text-text-muted mt-2">Este mensaje se enviará al correo del competidor junto con la notificación de rechazo.</p>

          <div class="flex flex-col-reverse sm:flex-row gap-3 sm:justify-end mt-6">
            <button (click)="closeModals()" class="px-5 py-2.5 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase tracking-wider text-sm transition">Cancelar</button>
            <button (click)="confirmReject()" [disabled]="rejectReason.trim().length < 10 || rejecting()"
                    [class]="rejectReason.trim().length >= 10 ? 'bg-error-brand hover:bg-red-700 text-white' : 'bg-navy-mid text-text-muted cursor-not-allowed'"
                    class="px-5 py-2.5 rounded-md font-accent uppercase tracking-wider text-sm transition">
              {{ rejecting() ? 'Enviando...' : 'Rechazar y notificar' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Toast -->
    @if (toast().visible) {
      <div class="fixed top-6 right-6 z-[60]">
        <div [class]="toast().type === 'success' ? 'bg-navy-dark border border-success-brand/40 rounded-lg shadow-2xl px-5 py-4 flex items-center gap-3 max-w-sm' : 'bg-navy-dark border border-error-brand/40 rounded-lg shadow-2xl px-5 py-4 flex items-center gap-3 max-w-sm'">
          <span [class]="toast().type === 'success' ? 'flex-shrink-0 h-8 w-8 rounded-full flex items-center justify-center bg-success-brand/15' : 'flex-shrink-0 h-8 w-8 rounded-full flex items-center justify-center bg-error-brand/15'">
            @if (toast().type === 'success') {
              <svg class="h-5 w-5 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
            } @else {
              <svg class="h-5 w-5 text-error-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/></svg>
            }
          </span>
          <p class="text-sm text-text-light">{{ toast().message }}</p>
        </div>
      </div>
    }
  `,
})
export class AdminTokensComponent implements OnInit {
  private api = inject(ApiService);

  CLASS_INPUT = CLASS_INPUT;

  loading = signal(true);
  approving = signal(false);
  rejecting = signal(false);

  approvedToday = signal(0);
  rejectedToday = signal(0);
  totalHistory = signal(0);

  pendingRequests = signal<TokenRequest[]>([]);
  history = signal<TokenHistoryRow[]>([]);

  historyFilter = '';

  async ngOnInit(): Promise<void> {
    await this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      const status = this.historyFilter || undefined;
      const query = status ? `?status=${encodeURIComponent(status)}&limit=50` : '?limit=50';
      const res = await this.api.get<any>(`/payments/beach/tokens${query}`);
      this.pendingRequests.set((res?.pendingRequests ?? []).map((r: any) => this.mapRequest(r)));
      this.history.set((res?.history ?? []).map((h: any) => this.mapHistory(h)));
      this.approvedToday.set(res?.dailyStats?.approvedToday ?? 0);
      this.rejectedToday.set(res?.dailyStats?.rejectedToday ?? 0);
      this.totalHistory.set(res?.pagination?.totalItems ?? (res?.history ?? []).length);
    } catch {
      this.showToast('error', 'Error al cargar los tokens');
    } finally {
      this.loading.set(false);
    }
  }

  private mapRequest(r: any): TokenRequest {
    return {
      id: r.id,
      time: r.generadoAt ? new Date(r.generadoAt).toLocaleTimeString('es', { hour: '2-digit', minute: '2-digit' }) : '—',
      name: r.competitorName,
      initials: initialsOf(r.competitorName),
      email: r.competitorEmail,
      event: r.event,
      category: r.category,
      amount: r.amountUsd,
    };
  }

  private mapHistory(h: any): TokenHistoryRow {
    return {
      competidor: h.competitorName,
      evento: h.event,
      estado: h.status,
      token: h.tokenCode ?? '—',
      generado: fmt(h.generadoAt),
      expiracion: fmt(h.expiracionAt),
      usadoEn: fmt(h.usadoEn),
    };
  }

  async onFilterChange(): Promise<void> {
    await this.loadAll();
  }

  async refresh(): Promise<void> {
    await this.loadAll();
    this.showToast('success', 'Lista actualizada');
  }

  historyEstadoClass(estado: HistEstado): string {
    const map: Record<HistEstado, string> = {
      Usado: 'px-2 py-1 rounded-full text-xs bg-success-brand/15 text-success-brand border border-success-brand/30 font-accent uppercase tracking-wider',
      Expirado: 'px-2 py-1 rounded-full text-xs bg-error-brand/15 text-error-brand border border-error-brand/30 font-accent uppercase tracking-wider',
      Rechazado: 'px-2 py-1 rounded-full text-xs bg-navy-mid text-text-muted border border-navy-mid font-accent uppercase tracking-wider',
      Pendiente: 'px-2 py-1 rounded-full text-xs bg-orange-brand/15 text-orange-brand border border-orange-brand/30 font-accent uppercase tracking-wider',
    };
    return map[estado];
  }

  historyEstadoLabel(estado: HistEstado): string {
    return estado === 'Usado' ? 'Usado ✓' : estado;
  }

  modal = signal<'approve' | 'reject' | null>(null);
  selectedReq = signal<TokenRequest | null>(null);
  rejectReason = '';

  openApprove(req: TokenRequest): void {
    this.selectedReq.set(req);
    this.modal.set('approve');
  }

  openReject(req: TokenRequest): void {
    this.selectedReq.set(req);
    this.rejectReason = '';
    this.modal.set('reject');
  }

  closeModals(): void {
    this.modal.set(null);
    this.selectedReq.set(null);
  }

  async confirmApprove(): Promise<void> {
    const req = this.selectedReq();
    if (!req) return;
    this.approving.set(true);
    try {
      await this.api.post<any>(`/payments/beach/tokens/${req.id}/approve`, {});
      this.pendingRequests.update(list => list.filter(r => r.id !== req.id));
      this.showToast('success', `✓ Token enviado a ${req.email}`);
      this.closeModals();
      await this.loadAll();
    } catch {
      this.showToast('error', 'Error al aprobar la solicitud');
    } finally {
      this.approving.set(false);
    }
  }

  async confirmReject(): Promise<void> {
    const req = this.selectedReq();
    if (!req || this.rejectReason.trim().length < 10) return;
    this.rejecting.set(true);
    try {
      await this.api.post<any>(`/payments/beach/tokens/${req.id}/reject`, { reason: this.rejectReason.trim() });
      this.pendingRequests.update(list => list.filter(r => r.id !== req.id));
      this.showToast('error', `✕ Solicitud rechazada y notificada a ${req.email}`);
      this.closeModals();
      await this.loadAll();
    } catch {
      this.showToast('error', 'Error al rechazar la solicitud');
    } finally {
      this.rejecting.set(false);
    }
  }

  toast = signal<{ visible: boolean; type: 'success' | 'error'; message: string }>({ visible: false, type: 'success', message: '' });
  showToast(type: 'success' | 'error', message: string): void {
    this.toast.set({ visible: true, type, message });
    setTimeout(() => this.toast.set({ visible: false, type, message: '' }), 4000);
  }
}
