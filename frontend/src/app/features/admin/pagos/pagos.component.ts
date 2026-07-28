import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type PagosTab = 'resumen' | 'inscripciones' | 'membresias' | 'multas';

type FineEstado = 'Pendiente' | 'Pagada' | 'Anulada';

interface CompetitorSearchResult {
  id: string;
  nombre: string;
  apellido: string;
  email: string;
}

interface Fine {
  id: string;
  motivo: string;
  montoUsd: number;
  estado: FineEstado;
  createdAt: string;
  notes?: string;
}

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

interface MonthlyRevenue {
  label: string;
  total: number;
  isCurrent: boolean;
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
function monthKey(d: Date): string {
  return `${d.getFullYear()}-${d.getMonth()}`;
}
function monthLabel(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return new Date(year, month, 1).toLocaleDateString('es', { month: 'short' }).replace('.', '');
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
          <button (click)="tab.set('multas')" [class]="tabClass('multas')">Multas</button>
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

          <!-- Revenue chart -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
            <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
              <div>
                <h2 class="font-heading text-xl text-white">Recaudación mensual</h2>
                <p class="text-sm text-text-muted">En USD · Total periodo: \${{ monthlyRevenueTotal() | number:'1.0-0' }}</p>
              </div>
              <div class="flex items-center gap-3 text-xs">
                <span class="flex items-center gap-1.5"><span class="h-3 w-3 rounded-sm bg-cyan-brand/60 border-t-2 border-cyan-brand"></span><span class="text-text-muted">Histórico</span></span>
                <span class="flex items-center gap-1.5"><span class="h-3 w-3 rounded-sm bg-success-brand/60 border-t-2 border-success-brand"></span><span class="text-text-muted">Mes actual</span></span>
              </div>
            </div>

            @if (monthlyRevenue().length === 0) {
              <p class="text-sm text-text-muted">Sin datos suficientes para mostrar el gráfico.</p>
            } @else {
              <div class="flex items-end justify-around gap-3 h-64 border-l border-b border-navy-mid pl-3 pb-2">
                @for (m of monthlyRevenue(); track m.label + $index) {
                  <div class="flex flex-col items-center flex-1 max-w-[70px] h-full justify-end">
                    <p [class]="m.isCurrent ? 'text-[11px] text-success-brand mb-1 font-medium' : 'text-[11px] text-text-muted mb-1'">
                      \${{ m.total | number:'1.0-0' }}
                    </p>
                    <div [class]="m.isCurrent
                        ? 'w-full rounded-t bg-gradient-to-t from-success-brand/20 to-success-brand/70 border-t-2 border-success-brand transition-all'
                        : 'w-full rounded-t bg-gradient-to-t from-cyan-brand/20 to-cyan-brand/60 border-t-2 border-cyan-brand transition-all'"
                        [style.height.%]="barHeightPercent(m.total)">
                    </div>
                  </div>
                }
              </div>
              <div class="flex justify-around gap-3 mt-2 pl-3">
                @for (m of monthlyRevenue(); track m.label + $index) {
                  <span [class]="m.isCurrent
                      ? 'flex-1 max-w-[70px] text-center text-[11px] font-accent uppercase tracking-wider text-success-brand'
                      : 'flex-1 max-w-[70px] text-center text-[11px] font-accent uppercase tracking-wider text-text-muted'">
                    {{ m.label }}
                  </span>
                }
              </div>
            }
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
            <select [class]="CLASS_INPUT + ' xl:max-w-[200px]'" [(ngModel)]="filterEvento">
              <option value="">Todos los eventos</option>
              @for (e of eventoOptions(); track e) {
                <option [value]="e">{{ e }}</option>
              }
            </select>
            <select [class]="CLASS_INPUT + ' xl:max-w-[180px]'" [(ngModel)]="filterCategoria">
              <option value="">Todas las categorías</option>
              @for (c of categoriaOptions(); track c) {
                <option [value]="c">{{ c }}</option>
              }
            </select>
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
            <input type="date" [class]="CLASS_INPUT + ' xl:max-w-[160px] [color-scheme:dark]'" [(ngModel)]="filterFromDate" (ngModelChange)="loadTransacciones()">
            <input type="date" [class]="CLASS_INPUT + ' xl:max-w-[160px] [color-scheme:dark]'" [(ngModel)]="filterToDate" (ngModelChange)="loadTransacciones()">
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
                        @if (t.estado === 'Pendiente' && canEdit()) {
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
            @if (canEdit()) {
            <button (click)="openCreateMembresia()"
                    class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Nueva Membresía
            </button>
            }
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
                        @if (m.plan === 'Mensual' && canEdit()) {
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

      <!-- ═══ TAB: MULTAS ═══ -->
      @if (tab() === 'multas') {
        <div>
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 mb-6">
            <h2 class="font-heading text-xl text-white mb-1">Buscar competidor</h2>
            <p class="text-sm text-text-muted mb-4">Busca por nombre o email para ver y registrar multas.</p>
            <div class="relative max-w-md">
              <input type="text" placeholder="Buscar competidor..." [(ngModel)]="finesSearchTerm"
                     (ngModelChange)="onFinesSearchChange($event)"
                     class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition">
            </div>
            @if (finesSearchResults().length > 0 && !finesSelectedCompetitor()) {
              <div class="mt-3 border border-navy-mid rounded-lg divide-y divide-navy-mid overflow-hidden max-w-md">
                @for (c of finesSearchResults(); track c.id) {
                  <button type="button" (click)="selectCompetitorForFines(c)"
                          class="w-full text-left px-4 py-2.5 hover:bg-navy-mid/30 transition">
                    <p class="text-sm text-text-light">{{ c.nombre }} {{ c.apellido }}</p>
                    <p class="text-xs text-text-muted">{{ c.email }}</p>
                  </button>
                }
              </div>
            }
          </div>

          @if (finesSelectedCompetitor(); as sel) {
            <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
              <div class="px-6 py-4 border-b border-navy-mid flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div>
                  <h2 class="font-heading text-xl text-white">{{ sel.nombre }} {{ sel.apellido }}</h2>
                  <p class="text-xs text-text-muted">{{ sel.email }}</p>
                </div>
                <div class="flex items-center gap-2">
                  @if (canEdit()) {
                    <button (click)="openCreateFine()"
                            class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition">
                      Nueva multa
                    </button>
                  }
                  <button (click)="clearFinesSelection()"
                          class="px-3 py-2 border border-navy-mid text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-xs rounded-md transition">
                    Cambiar competidor
                  </button>
                </div>
              </div>

              @if (loadingFines()) {
                <div class="p-6 text-sm text-text-muted">Cargando multas...</div>
              } @else if (fines().length === 0) {
                <div class="p-6 text-sm text-text-muted">Este competidor no tiene multas registradas.</div>
              } @else {
                <div class="overflow-x-auto">
                  <table class="w-full text-sm">
                    <thead class="border-b border-navy-mid">
                      <tr>
                        <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Fecha</th>
                        <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Motivo</th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Monto</th>
                        <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                        <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Acciones</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-navy-mid/50">
                      @for (f of fines(); track f.id) {
                        <tr class="hover:bg-cyan-brand/5 transition">
                          <td class="px-4 py-3 text-text-muted text-xs">{{ fmtDate(f.createdAt) }}</td>
                          <td class="px-4 py-3 text-text-light">{{ f.motivo }}</td>
                          <td class="px-4 py-3 text-right font-heading text-text-light">\${{ f.montoUsd | number:'1.0-2' }}</td>
                          <td class="px-4 py-3">
                            <span [class]="fineEstadoClass(f.estado)">{{ f.estado }}</span>
                          </td>
                          <td class="px-4 py-3 text-right whitespace-nowrap">
                            @if (f.estado === 'Pendiente' && canEdit()) {
                              <button (click)="updateFineEstado(f, 'Pagada')" class="text-xs font-accent uppercase tracking-wider text-success-brand hover:text-green-400 mr-3">Marcar pagada</button>
                              <button (click)="updateFineEstado(f, 'Anulada')" class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-error-brand">Anular</button>
                            }
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
            </div>
          }
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

    <!-- Modal: Nueva Multa -->
    @if (createFineOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="createFineOpen.set(false)">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl w-full max-w-md max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-navy-mid">
            <h3 class="font-heading text-xl text-white">Nueva multa</h3>
            <button (click)="createFineOpen.set(false)" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            @if (finesSelectedCompetitor(); as sel) {
              <p class="text-sm text-text-muted">Competidor: <span class="text-text-light">{{ sel.nombre }} {{ sel.apellido }}</span></p>
            }
            <div>
              <label [class]="LABEL_INPUT">Motivo *</label>
              <textarea [class]="CLASS_INPUT" rows="3" placeholder="Ej: Conducta antideportiva en Heat 3" [(ngModel)]="formFineMotivo"></textarea>
            </div>
            <div>
              <label [class]="LABEL_INPUT">Monto (USD) *</label>
              <input type="number" min="0" step="0.01" [class]="CLASS_INPUT" placeholder="50.00" [(ngModel)]="formFineMonto">
            </div>
          </div>
          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button (click)="createFineOpen.set(false)" class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition">Cancelar</button>
            <button (click)="confirmCreateFine()" [disabled]="savingFine()" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ savingFine() ? 'Registrando...' : 'Registrar multa' }}
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
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Pagos'));

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
  filterEvento = '';
  filterCategoria = '';
  filterFromDate = '';
  filterToDate = '';

  eventoOptions = computed(() => Array.from(new Set(this.transacciones().map(t => t.evento))).sort());
  categoriaOptions = computed(() => Array.from(new Set(this.transacciones().map(t => t.categoria))).sort());

  transaccionesFiltradas = computed(() => {
    return this.transacciones().filter(t => {
      if (this.filterMetodo && t.metodo !== this.filterMetodo) return false;
      if (this.filterEvento && t.evento !== this.filterEvento) return false;
      if (this.filterCategoria && t.categoria !== this.filterCategoria) return false;
      return true;
    });
  });

  monthlyRevenue = signal<MonthlyRevenue[]>([]);
  monthlyRevenueMax = computed(() => Math.max(1, ...this.monthlyRevenue().map(m => m.total)));
  monthlyRevenueTotal = computed(() => this.monthlyRevenue().reduce((acc, m) => acc + m.total, 0));

  barHeightPercent(total: number): number {
    if (total <= 0) return 0;
    return Math.max(4, Math.round((total / this.monthlyRevenueMax()) * 100));
  }

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
      await Promise.all([this.loadKpis(), this.loadTransacciones(), this.loadMembresias(), this.loadMonthlyRevenue()]);
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
    if (this.filterFromDate) params.set('fromDate', new Date(this.filterFromDate).toISOString());
    if (this.filterToDate) params.set('toDate', new Date(this.filterToDate).toISOString());
    const res = await this.api.get<any>(`/payments?${params.toString()}`);
    const data: any[] = res?.data ?? [];
    this.transacciones.set(data.map(p => this.mapPayment(p)));
  }

  private async loadMonthlyRevenue(): Promise<void> {
    const monthsBack = 5;
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth() - monthsBack, 1);
    const params = new URLSearchParams({ limit: '500', status: 'Confirmado', fromDate: from.toISOString() });
    const res = await this.api.get<any>(`/payments?${params.toString()}`);
    const data: any[] = res?.data ?? [];

    const buckets = new Map<string, number>();
    for (let i = monthsBack; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      buckets.set(monthKey(d), 0);
    }
    for (const p of data) {
      const key = monthKey(new Date(p.fecha));
      if (buckets.has(key)) buckets.set(key, (buckets.get(key) ?? 0) + Number(p.montoUsd ?? 0));
    }

    const currentKey = monthKey(now);
    this.monthlyRevenue.set(
      Array.from(buckets.entries()).map(([key, total]) => ({
        label: monthLabel(key),
        total,
        isCurrent: key === currentKey,
      })),
    );
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

  fmtDate(dt: string): string { return fmtDate(dt); }

  // ─── Multas ────────────────────────────────────────────────────

  finesSearchTerm = '';
  finesSearchResults = signal<CompetitorSearchResult[]>([]);
  finesSelectedCompetitor = signal<CompetitorSearchResult | null>(null);
  fines = signal<Fine[]>([]);
  loadingFines = signal(false);

  createFineOpen = signal(false);
  formFineMotivo = '';
  formFineMonto: number | null = null;
  savingFine = signal(false);

  private finesSearchDebounce?: ReturnType<typeof setTimeout>;

  onFinesSearchChange(term: string): void {
    clearTimeout(this.finesSearchDebounce);
    if (!term.trim()) {
      this.finesSearchResults.set([]);
      return;
    }
    this.finesSearchDebounce = setTimeout(() => void this.searchCompetitorsForFines(term), 300);
  }

  private async searchCompetitorsForFines(term: string): Promise<void> {
    try {
      const res = await this.api.get<any>(`/competitors?search=${encodeURIComponent(term)}&limit=10`);
      const data: any[] = res?.data ?? [];
      this.finesSearchResults.set(data.map(c => ({ id: c.id, nombre: c.nombre, apellido: c.apellido, email: c.email })));
    } catch {
      this.finesSearchResults.set([]);
    }
  }

  selectCompetitorForFines(c: CompetitorSearchResult): void {
    this.finesSelectedCompetitor.set(c);
    this.finesSearchResults.set([]);
    this.finesSearchTerm = `${c.nombre} ${c.apellido}`;
    void this.loadFines();
  }

  clearFinesSelection(): void {
    this.finesSelectedCompetitor.set(null);
    this.finesSearchTerm = '';
    this.fines.set([]);
  }

  private async loadFines(): Promise<void> {
    const competitor = this.finesSelectedCompetitor();
    if (!competitor) return;
    this.loadingFines.set(true);
    try {
      const res = await this.api.get<any>(`/competitors/${competitor.id}/fines`);
      const data: any[] = Array.isArray(res) ? res : (res?.data ?? []);
      this.fines.set(data.map(f => this.mapFine(f)));
    } catch {
      this.fines.set([]);
    } finally {
      this.loadingFines.set(false);
    }
  }

  private mapFine(f: any): Fine {
    return {
      id: f.id,
      motivo: f.reason ?? '',
      montoUsd: Number(f.amountUsd ?? 0),
      estado: f.status,
      createdAt: f.createdAt,
      notes: f.notes ?? undefined,
    };
  }

  fineEstadoClass(estado: FineEstado): string {
    const map: Record<FineEstado, string> = {
      Pendiente: 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-warning-brand/15 text-warning-brand',
      Pagada: 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-success-brand/15 text-success-brand',
      Anulada: 'px-2 py-0.5 rounded text-[10px] font-accent uppercase tracking-wider bg-navy-mid/50 text-text-muted',
    };
    return map[estado];
  }

  openCreateFine(): void {
    this.formFineMotivo = '';
    this.formFineMonto = null;
    this.createFineOpen.set(true);
  }

  async confirmCreateFine(): Promise<void> {
    const competitor = this.finesSelectedCompetitor();
    if (!competitor || !this.formFineMotivo.trim() || !this.formFineMonto) {
      this.showToast('Completa motivo y monto de la multa');
      return;
    }
    this.savingFine.set(true);
    try {
      await this.api.post<any>(`/competitors/${competitor.id}/fines`, {
        reason: this.formFineMotivo.trim(),
        amountUsd: Number(this.formFineMonto),
        notes: null,
      });
      await this.loadFines();
      this.createFineOpen.set(false);
      this.showToast('Multa registrada');
    } catch (err: any) {
      this.showToast(err?.body?.message ?? 'Error al registrar la multa');
    } finally {
      this.savingFine.set(false);
    }
  }

  async updateFineEstado(f: Fine, estado: FineEstado): Promise<void> {
    const competitor = this.finesSelectedCompetitor();
    if (!competitor) return;
    try {
      const updated = await this.api.put<any>(`/competitors/${competitor.id}/fines/${f.id}`, {
        amountUsd: f.montoUsd,
        reason: f.motivo,
        notes: f.notes ?? null,
        status: estado,
      });

      this.fines.update(list => list.map(x => x.id === f.id ? this.mapFine(updated) : x));
      this.showToast(estado === 'Pagada' ? 'Multa marcada como pagada' : 'Multa anulada');
    } catch {
      this.showToast('Error al actualizar la multa');
    }
  }
}
