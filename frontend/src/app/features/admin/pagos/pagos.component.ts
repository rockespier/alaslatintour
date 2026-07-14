import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type PagosTab = 'resumen' | 'inscripciones' | 'membresias';

interface Transaccion {
  id: string;
  fecha: string;
  competidor: string;
  evento: string;
  categoria: string;
  monto: string;
  metodo: 'PayPal' | 'Efectivo';
  transaccionId: string;
  estado: 'Confirmado' | 'Pendiente';
}

interface Membresia {
  id: string;
  nombre: string;
  iniciales: string;
  gradient: string;
  pais: string;
  plan: 'Mensual' | 'Por evento';
  competidores: number;
  vencimiento: string;
  vencimientoIso: string;
  vencimientoWarning: boolean;
  estado: 'Activo' | 'Vence pronto';
  monto: string;
}

const CLASS_INPUT = 'w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition';
const LABEL_INPUT = 'block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5';

const GRADIENTS = ['from-cyan-brand to-navy-mid', 'from-orange-brand to-navy-mid', 'from-success-brand to-cyan-brand', 'from-warning-brand to-orange-brand'];
function gradientFor(id: string): string {
  let hash = 0;
  for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
  return GRADIENTS[hash % GRADIENTS.length];
}
function initialsOf(name: string): string {
  return name.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase() ?? '').join('');
}
function fmtDate(dt: string): string {
  return new Date(dt).toLocaleDateString('es', { day: '2-digit', month: 'short', year: 'numeric' });
}

@Component({
  selector: 'app-pagos',
  standalone: true,
  imports: [FormsModule, DecimalPipe, LoadingSpinnerComponent],
  template: `
    <div class="py-8">
      <div class="mb-6">
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Pagos</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Pagos y Recaudación</h1>
      </div>

      <!-- Tabs -->
      <div class="border-b border-navy-mid mb-6 overflow-x-auto">
        <nav class="flex gap-2 min-w-max">
          <button (click)="tab.set('resumen')" [class]="tabClass('resumen')">Resumen</button>
          <button (click)="tab.set('inscripciones')" [class]="tabClass('inscripciones')">Inscripciones</button>
          <button (click)="tab.set('membresias')" [class]="tabClass('membresias')">Membresías</button>
        </nav>
      </div>

      @if (loading()) {
        <app-loading-spinner />
      } @else {

      <!-- ═══ TAB: RESUMEN ═══ -->
      @if (tab() === 'resumen') {
        <div>
          <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-6">
            <div class="bg-navy-dark rounded-xl border border-cyan-brand/30 p-5">
              <div class="flex items-center justify-between mb-2">
                <p class="font-accent uppercase tracking-wider text-xs text-text-muted">Total recaudado (mes)</p>
                <span [class]="kpis().tendenciaPercent >= 0 ? 'text-xs font-accent uppercase tracking-wider text-success-brand flex items-center gap-1' : 'text-xs font-accent uppercase tracking-wider text-error-brand flex items-center gap-1'">
                  {{ kpis().tendenciaPercent >= 0 ? '+' : '' }}{{ kpis().tendenciaPercent }}%
                </span>
              </div>
              <p class="font-heading text-3xl text-cyan-brand">\${{ kpis().totalRecaudadoMes | number:'1.0-0' }}<span class="text-base text-text-muted ml-1">USD</span></p>
            </div>
            <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Pagos PayPal confirmados</p>
              <p class="font-heading text-3xl text-success-brand">\${{ kpis().pagoPaypalConfirmados.amountUsd | number:'1.0-0' }}<span class="text-base text-text-muted ml-1">USD</span></p>
              <p class="text-xs text-text-muted mt-1">{{ kpis().pagoPaypalConfirmados.count }} transacciones</p>
            </div>
            <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Pagos en playa validados</p>
              <p class="font-heading text-3xl text-orange-brand">\${{ kpis().pagosPlayaValidados.amountUsd | number:'1.0-0' }}<span class="text-base text-text-muted ml-1">USD</span></p>
              <p class="text-xs text-text-muted mt-1">{{ kpis().pagosPlayaValidados.count }} validaciones · {{ kpis().pagosPlayaValidados.pendingCount }} pendientes</p>
            </div>
            <div class="bg-navy-dark rounded-xl border border-navy-mid p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Membresías activas</p>
              <p class="font-heading text-3xl text-cyan-dark">\${{ kpis().membresiasActivas.amountUsd | number:'1.0-0' }}<span class="text-base text-text-muted ml-1">USD</span></p>
              <p class="text-xs text-text-muted mt-1">{{ kpis().membresiasActivas.count }} membresías</p>
            </div>
          </div>

          <!-- Recent transactions -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
            <div class="px-6 py-4 border-b border-navy-mid">
              <h2 class="font-heading text-xl text-white">Transacciones recientes</h2>
              <p class="text-sm text-text-muted">Últimas {{ transacciones().length }} transacciones registradas</p>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="border-b border-navy-mid">
                  <tr>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Fecha</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Competidor</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Evento</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Categoría</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Monto</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Método</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">ID Transacción</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (t of transacciones(); track t.id) {
                    <tr class="hover:bg-cyan-brand/5 transition">
                      <td class="px-4 py-3 text-xs text-text-muted">{{ t.fecha }}</td>
                      <td class="px-4 py-3 font-medium text-text-light">{{ t.competidor }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ t.evento }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ t.categoria }}</td>
                      <td class="px-4 py-3 text-right font-medium text-text-light">{{ t.monto }}</td>
                      <td class="px-4 py-3">
                        <span [class]="t.metodo === 'PayPal' ? 'text-success-brand text-xs font-accent uppercase tracking-wider' : 'text-orange-brand text-xs font-accent uppercase tracking-wider'">{{ t.metodo }}</span>
                      </td>
                      <td class="px-4 py-3 font-mono text-xs text-cyan-brand">{{ t.transaccionId }}</td>
                      <td class="px-4 py-3">
                        <span [class]="t.estado === 'Confirmado'
                          ? 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-brand/15 text-success-brand text-xs font-accent uppercase tracking-wider'
                          : 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-brand/15 text-warning-brand text-xs font-accent uppercase tracking-wider'">
                          {{ t.estado }}
                        </span>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      }

      <!-- ═══ TAB: INSCRIPCIONES ═══ -->
      @if (tab() === 'inscripciones') {
        <div>
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-4 mb-6 flex flex-col xl:flex-row gap-3">
            <select [class]="CLASS_INPUT + ' xl:max-w-[180px]'" [(ngModel)]="filterMetodo">
              <option value="">Todos los métodos</option>
              <option value="PayPal">PayPal</option>
              <option value="Efectivo">Efectivo</option>
            </select>
            <select [class]="CLASS_INPUT + ' xl:max-w-[180px]'" [(ngModel)]="filterEstado" (ngModelChange)="loadTransacciones()">
              <option value="">Todos los estados</option>
              <option value="Confirmado">Confirmado</option>
              <option value="Pendiente">Pendiente</option>
            </select>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
            <div class="bg-navy-dark rounded-xl border border-success-brand/30 p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Inscripciones PayPal</p>
              <p class="font-heading text-2xl text-success-brand">{{ resumenInscripciones().paypalCount }} inscripciones</p>
              <p class="text-sm text-text-muted mt-1">Total: \${{ resumenInscripciones().paypalTotal | number:'1.0-2' }}</p>
            </div>
            <div class="bg-navy-dark rounded-xl border border-orange-brand/30 p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Efectivo validado</p>
              <p class="font-heading text-2xl text-orange-brand">{{ resumenInscripciones().efectivoCount }} inscripciones</p>
              <p class="text-sm text-text-muted mt-1">Total: \${{ resumenInscripciones().efectivoTotal | number:'1.0-2' }}</p>
            </div>
            <div class="bg-navy-dark rounded-xl border border-warning-brand/30 p-5">
              <p class="font-accent uppercase tracking-wider text-xs text-text-muted mb-2">Pendiente</p>
              <p class="font-heading text-2xl text-warning-brand">{{ resumenInscripciones().pendienteCount }} inscripciones</p>
              <p class="text-sm text-text-muted mt-1">Total: \${{ resumenInscripciones().pendienteTotal | number:'1.0-2' }}</p>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
            <div class="px-4 py-3 border-b border-navy-mid flex justify-between items-center">
              <p class="text-sm text-text-muted">Mostrando {{ transaccionesFiltradas().length }} transacciones de inscripción</p>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="border-b border-navy-mid">
                  <tr>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Fecha</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Competidor</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Evento</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Categoría</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Monto</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Método</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">ID</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (t of transaccionesFiltradas(); track t.id) {
                    <tr class="hover:bg-cyan-brand/5 transition">
                      <td class="px-4 py-3 text-xs text-text-muted">{{ t.fecha }}</td>
                      <td class="px-4 py-3 font-medium text-text-light">{{ t.competidor }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ t.evento }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ t.categoria }}</td>
                      <td class="px-4 py-3 text-right text-text-light">{{ t.monto }}</td>
                      <td class="px-4 py-3">
                        <span [class]="t.metodo === 'PayPal' ? 'text-success-brand text-xs' : 'text-orange-brand text-xs'">{{ t.metodo }}</span>
                      </td>
                      <td class="px-4 py-3 font-mono text-xs" [class]="t.estado === 'Pendiente' ? 'text-warning-brand' : 'text-cyan-brand'">{{ t.transaccionId }}</td>
                      <td class="px-4 py-3">
                        <span [class]="t.estado === 'Confirmado'
                          ? 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-brand/15 text-success-brand text-xs font-accent uppercase tracking-wider'
                          : 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-brand/15 text-warning-brand text-xs font-accent uppercase tracking-wider'">
                          {{ t.estado }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-right">
                        @if (t.estado === 'Pendiente') {
                          <button (click)="openValidateModal(t)"
                                  class="px-2 py-1 bg-success-brand/10 hover:bg-success-brand/20 text-success-brand border border-success-brand/30 font-accent uppercase tracking-wider text-[10px] rounded transition">
                            Validar Pago
                          </button>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      }

      <!-- ═══ TAB: MEMBRESÍAS ═══ -->
      @if (tab() === 'membresias') {
        <div>
          <div class="bg-cyan-brand/10 border border-cyan-brand/30 rounded-xl p-5 mb-6 flex gap-4 items-start">
            <div class="w-10 h-10 rounded-lg bg-cyan-brand/20 flex items-center justify-center flex-shrink-0">
              <svg class="h-5 w-5 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            </div>
            <div class="flex-1">
              <h3 class="font-heading text-base text-white mb-1">¿Qué es una membresía?</h3>
              <p class="text-sm text-text-muted">Las membresías permiten a clubes y federaciones afiliar a múltiples competidores bajo una tarifa especial. Pueden ser mensuales (acceso completo) o por evento (descuento específico).</p>
            </div>
          </div>

          <div class="flex flex-col sm:flex-row justify-between sm:items-center gap-3 mb-4">
            <h2 class="font-heading text-xl text-white">Membresías activas</h2>
            <button (click)="openCreateMembresia()"
                    class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Nueva Membresía
            </button>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="border-b border-navy-mid">
                  <tr>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Club/Federación</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">País</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Plan</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Competidores Afiliados</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Vencimiento</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Monto</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (m of membresias(); track m.id) {
                    <tr class="hover:bg-cyan-brand/5 transition">
                      <td class="px-4 py-3">
                        <div class="flex items-center gap-3">
                          <div [class]="'w-9 h-9 rounded-lg flex items-center justify-center font-heading text-white text-xs bg-gradient-to-br ' + m.gradient">{{ m.iniciales }}</div>
                          <span class="font-medium text-text-light">{{ m.nombre }}</span>
                        </div>
                      </td>
                      <td class="px-4 py-3 text-text-muted">{{ m.pais }}</td>
                      <td class="px-4 py-3">
                        <span [class]="m.plan === 'Mensual'
                          ? 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-cyan-brand/15 text-cyan-brand text-xs font-accent uppercase tracking-wider'
                          : 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-orange-brand/15 text-orange-brand text-xs font-accent uppercase tracking-wider'">
                          {{ m.plan }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-text-light">{{ m.competidores }} competidores</td>
                      <td [class]="m.vencimientoWarning ? 'px-4 py-3 text-warning-brand text-xs' : 'px-4 py-3 text-text-muted'">{{ m.vencimiento }}</td>
                      <td class="px-4 py-3">
                        <span [class]="m.estado === 'Activo'
                          ? 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-brand/15 text-success-brand text-xs font-accent uppercase tracking-wider'
                          : 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-brand/15 text-warning-brand text-xs font-accent uppercase tracking-wider'">
                          {{ m.estado === 'Vence pronto' ? '⚠ Vence pronto' : m.estado }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-right font-medium text-text-light">{{ m.monto }}</td>
                      <td class="px-4 py-3 text-right whitespace-nowrap">
                        @if (m.plan === 'Mensual') {
                          <button (click)="renovarMembresia(m)"
                                  [class]="m.vencimientoWarning
                                    ? 'text-xs font-accent uppercase tracking-wider text-warning-brand hover:text-yellow-400 mr-2'
                                    : 'text-xs font-accent uppercase tracking-wider text-cyan-brand hover:text-cyan-dark mr-2'">
                            Renovar
                          </button>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        </div>
      }
      }
    </div>

    <!-- Modal: Validar Pago en Playa -->
    @if (validateModalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="validateModalOpen.set(false)">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl w-full max-w-md max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-navy-mid">
            <h3 class="font-heading text-xl text-white">Validar Pago en Playa</h3>
            <button (click)="validateModalOpen.set(false)" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            <p class="text-sm text-text-muted">Confirma que recibiste el monto correcto en efectivo para la siguiente inscripción:</p>
            @if (validatingTransaction(); as t) {
              <div class="bg-navy-deepest rounded-lg border border-navy-mid p-4 space-y-2 text-sm">
                <div class="flex justify-between"><span class="text-text-muted">Competidor:</span><span class="font-medium text-text-light">{{ t.competidor }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Evento:</span><span class="text-text-light">{{ t.evento }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Categoría:</span><span class="text-text-light">{{ t.categoria }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Monto esperado:</span><span class="font-heading text-cyan-brand">USD {{ t.monto }}</span></div>
                <div class="flex justify-between"><span class="text-text-muted">Token:</span><code class="text-warning-brand font-mono">{{ t.transaccionId }}</code></div>
              </div>
            }
            <div>
              <label [class]="LABEL_INPUT">Notas (opcional)</label>
              <textarea [class]="CLASS_INPUT" rows="2" placeholder="Recibido por..." [(ngModel)]="validateNotes"></textarea>
            </div>
          </div>
          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button (click)="validateModalOpen.set(false)" class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition">Cancelar</button>
            <button (click)="confirmValidatePago()" [disabled]="validating()" class="px-4 py-2 bg-success-brand hover:bg-green-600 text-white font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ validating() ? 'Confirmando...' : 'Confirmar Pago' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Modal: Nueva Membresía -->
    @if (createMembresiaOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="createMembresiaOpen.set(false)">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-navy-mid">
            <h3 class="font-heading text-xl text-white">Nueva Membresía</h3>
            <button (click)="createMembresiaOpen.set(false)" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            <div>
              <label [class]="LABEL_INPUT">Club / Federación</label>
              <input type="text" [class]="CLASS_INPUT" placeholder="Nombre del club" [(ngModel)]="formClub">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">País</label>
                <select [class]="CLASS_INPUT" [(ngModel)]="formPais">
                  <option>Perú</option><option>Chile</option><option>Brasil</option><option>Argentina</option><option>México</option>
                </select>
              </div>
              <div>
                <label [class]="LABEL_INPUT">Plan</label>
                <select [class]="CLASS_INPUT" [(ngModel)]="formPlan">
                  <option value="Mensual">Mensual ($50/mes)</option>
                  <option value="Por evento">Por evento ($80/evento)</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">Inicio vigencia</label>
                <input type="date" [class]="CLASS_INPUT + ' [color-scheme:dark]'" [(ngModel)]="formInicio">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Vencimiento</label>
                <input type="date" [class]="CLASS_INPUT + ' [color-scheme:dark]'" [(ngModel)]="formVencimiento">
              </div>
            </div>
            <div>
              <label [class]="LABEL_INPUT">Email de contacto</label>
              <input type="email" [class]="CLASS_INPUT" placeholder="contacto@club.com" [(ngModel)]="formEmail">
            </div>
          </div>
          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button (click)="createMembresiaOpen.set(false)" class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition">Cancelar</button>
            <button (click)="confirmCreateMembresia()" [disabled]="savingMembresia()" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ savingMembresia() ? 'Creando...' : 'Crear Membresía' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Toast -->
    @if (toast().show) {
      <div class="fixed bottom-6 right-6 z-50 bg-navy-dark border border-success-brand/50 rounded-lg shadow-2xl px-5 py-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-success-brand" fill="currentColor" viewBox="0 0 20 20"><path d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"/></svg>
        <p class="text-sm text-text-light">{{ toast().message }}</p>
      </div>
    }
  `,
})
export class PagosComponent implements OnInit {
  private api = inject(ApiService);

  CLASS_INPUT = CLASS_INPUT;
  LABEL_INPUT = LABEL_INPUT;

  tab = signal<PagosTab>('resumen');
  tabClass(t: PagosTab): string {
    return this.tab() === t
      ? 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-cyan-brand border-b-2 border-cyan-brand whitespace-nowrap'
      : 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-text-muted border-b-2 border-transparent hover:text-text-light transition whitespace-nowrap';
  }

  loading = signal(true);
  validating = signal(false);
  savingMembresia = signal(false);

  kpis = signal({
    totalRecaudadoMes: 0,
    tendenciaPercent: 0,
    pagoPaypalConfirmados: { amountUsd: 0, count: 0 },
    pagosPlayaValidados: { amountUsd: 0, count: 0, pendingCount: 0 },
    membresiasActivas: { amountUsd: 0, count: 0 },
  });

  transacciones = signal<Transaccion[]>([]);
  filterMetodo = '';
  filterEstado = '';

  transaccionesFiltradas = computed(() => {
    return this.transacciones().filter(t => {
      if (this.filterMetodo && t.metodo !== this.filterMetodo) return false;
      return true;
    });
  });

  resumenInscripciones = computed(() => {
    const list = this.transacciones();
    const paypal = list.filter(t => t.metodo === 'PayPal');
    const efectivo = list.filter(t => t.metodo === 'Efectivo' && t.estado === 'Confirmado');
    const pendiente = list.filter(t => t.estado === 'Pendiente');
    const sum = (arr: Transaccion[]) => arr.reduce((a, t) => a + parseFloat(t.monto.replace(/[^0-9.]/g, '')), 0);
    return {
      paypalCount: paypal.length,
      paypalTotal: sum(paypal),
      efectivoCount: efectivo.length,
      efectivoTotal: sum(efectivo),
      pendienteCount: pendiente.length,
      pendienteTotal: sum(pendiente),
    };
  });

  membresias = signal<Membresia[]>([]);

  async ngOnInit(): Promise<void> {
    await this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      await Promise.all([this.loadKpis(), this.loadTransacciones(), this.loadMembresias()]);
    } catch {
      this.showToast('Error al cargar los datos de pagos');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadKpis(): Promise<void> {
    const res = await this.api.get<any>('/payments/kpis');
    this.kpis.set({
      totalRecaudadoMes: res?.totalRecaudadoMes ?? 0,
      tendenciaPercent: res?.tendenciaPercent ?? 0,
      pagoPaypalConfirmados: res?.pagoPaypalConfirmados ?? { amountUsd: 0, count: 0 },
      pagosPlayaValidados: res?.pagosPlayaValidados ?? { amountUsd: 0, count: 0, pendingCount: 0 },
      membresiasActivas: res?.membresiasActivas ?? { amountUsd: 0, count: 0 },
    });
  }

  async loadTransacciones(): Promise<void> {
    const params = new URLSearchParams({ limit: '50' });
    if (this.filterEstado) params.set('status', this.filterEstado);
    const res = await this.api.get<any>(`/payments?${params.toString()}`);
    const data: any[] = res?.data ?? [];
    this.transacciones.set(data.map(p => this.mapPayment(p)));
  }

  private mapPayment(p: any): Transaccion {
    return {
      id: p.id,
      fecha: fmtDate(p.fecha),
      competidor: p.competidor,
      evento: p.evento,
      categoria: p.categoria,
      monto: `$${p.montoUsd.toFixed(2)}`,
      metodo: p.metodo === 'paypal' ? 'PayPal' : 'Efectivo',
      transaccionId: p.transaccionId,
      estado: p.estado,
    };
  }

  private async loadMembresias(): Promise<void> {
    const res = await this.api.get<any>('/memberships?limit=50');
    const data: any[] = res?.data ?? [];
    this.membresias.set(data.map(m => this.mapMembresia(m)));
  }

  private mapMembresia(m: any): Membresia {
    const vencimiento = new Date(m.vencimiento);
    return {
      id: m.id,
      nombre: m.clubFederacion,
      iniciales: initialsOf(m.clubFederacion),
      gradient: gradientFor(m.id),
      pais: m.pais,
      plan: m.plan,
      competidores: m.competidoresAfiliados,
      vencimiento: m.plan === 'Por evento' ? '—' : fmtDate(m.vencimiento),
      vencimientoIso: vencimiento.toISOString().slice(0, 10),
      vencimientoWarning: m.estado === 'Vence pronto',
      estado: m.estado,
      monto: m.plan === 'Mensual' ? '$50/mes' : '$80/evento',
    };
  }

  renovarMembresia(m: Membresia): void {
    (async () => {
      try {
        const nuevoVencimiento = new Date(m.vencimientoIso);
        nuevoVencimiento.setMonth(nuevoVencimiento.getMonth() + 1);
        const membresiaApi = await this.api.get<any>(`/memberships/${m.id}`);
        await this.api.put<any>(`/memberships/${m.id}`, {
          clubFederacion: membresiaApi.clubFederacion,
          pais: membresiaApi.pais,
          plan: membresiaApi.plan,
          inicioVigencia: membresiaApi.inicioVigencia,
          vencimiento: nuevoVencimiento.toISOString(),
          emailContacto: membresiaApi.emailContacto,
        });
        await this.loadMembresias();
        this.showToast('Membresía renovada');
      } catch {
        this.showToast('Error al renovar la membresía');
      }
    })();
  }

  validateModalOpen = signal(false);
  validatingTransaction = signal<Transaccion | null>(null);
  validateNotes = '';
  openValidateModal(t: Transaccion): void {
    this.validatingTransaction.set(t);
    this.validateNotes = '';
    this.validateModalOpen.set(true);
  }
  async confirmValidatePago(): Promise<void> {
    const t = this.validatingTransaction();
    if (!t) return;
    this.validating.set(true);
    try {
      await this.api.put<any>(`/payments/${t.id}`, { status: 'Confirmado', notes: this.validateNotes });
      this.transacciones.update(list => list.map(x => x.id === t.id ? { ...x, estado: 'Confirmado' as const } : x));
      this.validateModalOpen.set(false);
      this.showToast('Pago validado correctamente');
    } catch {
      this.showToast('Error al validar el pago');
    } finally {
      this.validating.set(false);
    }
  }

  createMembresiaOpen = signal(false);
  formClub = '';
  formPais = 'Perú';
  formPlan: 'Mensual' | 'Por evento' = 'Mensual';
  formInicio = new Date().toISOString().slice(0, 10);
  formVencimiento = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
  formEmail = '';

  openCreateMembresia(): void {
    this.formClub = '';
    this.formPais = 'Perú';
    this.formPlan = 'Mensual';
    this.formInicio = new Date().toISOString().slice(0, 10);
    this.formVencimiento = new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10);
    this.formEmail = '';
    this.createMembresiaOpen.set(true);
  }

  async confirmCreateMembresia(): Promise<void> {
    if (!this.formClub.trim() || !this.formEmail.trim()) {
      this.showToast('Completa club y email de contacto');
      return;
    }
    this.savingMembresia.set(true);
    try {
      await this.api.post<any>('/memberships', {
        clubFederacion: this.formClub,
        pais: this.formPais,
        plan: this.formPlan,
        inicioVigencia: new Date(this.formInicio).toISOString(),
        vencimiento: new Date(this.formVencimiento).toISOString(),
        emailContacto: this.formEmail,
      });
      await this.loadMembresias();
      this.createMembresiaOpen.set(false);
      this.showToast('Membresía creada');
    } catch {
      this.showToast('Error al crear la membresía');
    } finally {
      this.savingMembresia.set(false);
    }
  }

  toast = signal<{ show: boolean; message: string }>({ show: false, message: '' });
  showToast(message: string): void {
    this.toast.set({ show: true, message });
    setTimeout(() => this.toast.set({ show: false, message: '' }), 3000);
  }
}
