import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../../core/services/api.service';
import { AuthService } from '../../../../core/services/auth.service';

type InscriptionStatus = 'activa' | 'completada' | 'cancelada';
type PaymentStatus = 'confirmado' | 'pendiente' | 'rechazado';
type FilterTab = 'todas' | 'activas' | 'pendiente_pago' | 'completadas';

interface Inscription {
  id: string;
  eventoNombre: string;
  eventoPais: string;
  eventoFechaInicio: string;
  eventoFechaFin: string;
  categoria: string;
  status: InscriptionStatus;
  statusPago: PaymentStatus;
  monto: number;
  metodoPago?: string;
}

const PAGO_CLASS: Record<PaymentStatus, string> = {
  confirmado: 'bg-success-brand/15 text-success-brand border-success-brand/30',
  pendiente:  'bg-warning-brand/15 text-warning-brand border-warning-brand/30',
  rechazado:  'bg-error-brand/15 text-error-brand border-error-brand/30',
};

@Component({
  selector: 'app-mis-inscripciones',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
      <h2 class="font-heading text-3xl">Mis Inscripciones</h2>
    </div>

    <!-- Filter tabs -->
    <div class="flex gap-1 border-b border-navy-mid mb-6 overflow-x-auto">
      @for (tab of filterTabs; track tab.key) {
        <button (click)="filter.set(tab.key)"
                class="px-4 py-2.5 border-b-2 font-accent uppercase text-xs tracking-wider whitespace-nowrap transition"
                [class]="filter() === tab.key ? 'border-cyan-brand text-cyan-brand' : 'border-transparent text-text-muted hover:text-text-light'">
          {{ tab.label }}
          @if (tab.count() > 0) {
            <span class="ml-1.5 px-1.5 py-0.5 rounded-full text-[10px]"
                  [class]="filter() === tab.key ? 'bg-cyan-brand/20' : 'bg-navy-mid'">{{ tab.count() }}</span>
          }
        </button>
      }
    </div>

    @if (loading()) {
      <div class="space-y-3">
        @for (sk of skeletons; track sk) { <div class="skeleton h-20 rounded-xl"></div> }
      </div>
    } @else if (filtered().length === 0) {
      <div class="text-center py-16">
        <p class="text-text-muted text-sm">No hay inscripciones en esta categoría.</p>
        <a routerLink="/eventos" class="mt-4 inline-block text-cyan-brand hover:text-cyan-dark font-accent uppercase text-xs tracking-wider">Ver eventos disponibles →</a>
      </div>
    } @else {
      <div class="overflow-x-auto rounded-2xl border border-navy-mid">
        <table class="w-full text-sm">
          <thead class="bg-navy-mid/40 font-accent uppercase tracking-wider text-text-muted text-xs">
            <tr>
              <th class="px-5 py-3 text-left">Evento</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">Categoría</th>
              <th class="px-4 py-3 text-left hidden md:table-cell">Fechas</th>
              <th class="px-4 py-3 text-center">Pago</th>
              <th class="px-4 py-3 text-right hidden sm:table-cell">Monto</th>
              <th class="px-4 py-3 text-right">Acción</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-navy-mid/50">
            @for (ins of filtered(); track ins.id) {
              <tr class="hover:bg-navy-mid/20 transition">
                <td class="px-5 py-4">
                  <div class="flex items-center gap-2">
                    <span class="text-xl">{{ ins.eventoPais }}</span>
                    <div>
                      <p class="font-medium leading-tight">{{ ins.eventoNombre }}</p>
                      <p class="text-xs text-text-muted sm:hidden">{{ ins.categoria }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-4 py-4 text-text-muted hidden sm:table-cell">{{ ins.categoria }}</td>
                <td class="px-4 py-4 text-text-muted hidden md:table-cell text-xs">
                  {{ dateRange(ins.eventoFechaInicio, ins.eventoFechaFin) }}
                </td>
                <td class="px-4 py-4 text-center">
                  <span class="px-2 py-1 rounded-full text-[10px] font-accent uppercase tracking-wider border whitespace-nowrap"
                        [class]="pagoClass(ins.statusPago)">
                    {{ ins.statusPago === 'confirmado' ? 'Confirmado' : ins.statusPago === 'pendiente' ? 'Pendiente' : 'Rechazado' }}
                  </span>
                </td>
                <td class="px-4 py-4 text-right text-cyan-brand hidden sm:table-cell">{{ '$' + ins.monto }}</td>
                <td class="px-4 py-4 text-right">
                  @if (ins.statusPago === 'pendiente') {
                    <a [routerLink]="['/pago-playa', ins.id]"
                       class="text-xs font-accent uppercase tracking-wider text-orange-brand hover:text-orange-light">
                      Ver token →
                    </a>
                  } @else {
                    <span class="text-xs text-text-muted">—</span>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (filtered().length > 0) {
        <p class="text-xs text-text-muted mt-3 text-right">{{ filtered().length }} inscripción{{ filtered().length !== 1 ? 'es' : '' }}</p>
      }
    }
  `,
})
export class MisInscripcionesComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  loading = signal(true);
  inscriptions = signal<Inscription[]>([]);
  filter = signal<FilterTab>('todas');
  readonly skeletons = [1, 2, 3, 4];

  filtered = computed(() => {
    const f = this.filter();
    const all = this.inscriptions();
    if (f === 'activas') return all.filter(i => i.status === 'activa');
    if (f === 'pendiente_pago') return all.filter(i => i.statusPago === 'pendiente');
    if (f === 'completadas') return all.filter(i => i.status === 'completada');
    return all;
  });

  filterTabs = [
    { key: 'todas' as FilterTab,         label: 'Todas',             count: computed(() => this.inscriptions().length) },
    { key: 'activas' as FilterTab,        label: 'Activas',           count: computed(() => this.inscriptions().filter(i => i.status === 'activa').length) },
    { key: 'pendiente_pago' as FilterTab, label: 'Pendiente de pago', count: computed(() => this.inscriptions().filter(i => i.statusPago === 'pendiente').length) },
    { key: 'completadas' as FilterTab,    label: 'Completadas',       count: computed(() => this.inscriptions().filter(i => i.status === 'completada').length) },
  ];

  ngOnInit(): void {
    this.load();
  }

  private async load(): Promise<void> {
    const competitorId = this.auth.currentUser()?.competitorId;
    try {
      const res = await this.api.get<any>(`/competitors/${competitorId}/inscriptions?limit=50`);
      this.inscriptions.set(res?.data ?? []);
    } catch {
      this.inscriptions.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  pagoClass(status: PaymentStatus): string { return PAGO_CLASS[status] ?? ''; }

  dateRange(start: string, end: string): string {
    if (!start) return '';
    const s = new Date(start), e = new Date(end);
    const months = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    return `${s.getDate()} - ${e.getDate()} ${months[s.getMonth()]} ${s.getFullYear()}`;
  }
}
