import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type ConfigTab = 'general' | 'ranking' | 'integraciones' | 'notificaciones' | 'envivo';

interface RankingPointsRow {
  pos: string;
  s1: number;
  s2: number;
  s3: number;
  s4: number;
  s5: number;
  s6: number;
  s7: number;
}

interface PrizeDistRow {
  label: string;
  p1: number;
  p2: number;
  p3: number;
  p4: number;
  p5: number;
  p6: number;
  p7: number;
}

interface DemoEvento { id: string; label: string; }

const LABEL_INPUT = 'block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5';
const CLASS_INPUT = 'w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent],
  template: `
    <div class="py-8">
      <div class="mb-6">
        <h1 class="text-3xl font-heading text-white">Configuración del Sistema</h1>
        <p class="text-text-muted text-sm mt-1">Parámetros generales de la plataforma ALAS Latin Tour.</p>
        @if (!canEdit()) {
          <p class="text-warning-brand text-xs mt-2 font-accent uppercase tracking-wider">Tu rol tiene acceso de solo lectura a esta sección.</p>
        }
      </div>

      <!-- Tabs -->
      <div class="border-b border-navy-mid mb-6 overflow-x-auto">
        <nav class="flex gap-2 min-w-max">
          <button (click)="tab.set('general')" [class]="tabClass('general')">General</button>
          <button (click)="tab.set('ranking')" [class]="tabClass('ranking')">Ranking</button>
          <button (click)="tab.set('integraciones')" [class]="tabClass('integraciones')">Integraciones</button>
          <button (click)="tab.set('notificaciones')" [class]="tabClass('notificaciones')">Notificaciones</button>
          <button (click)="tab.set('envivo')" [class]="tabClass('envivo')">
            <span class="inline-flex items-center gap-1.5">
              <span class="relative flex h-2 w-2">
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-error-brand opacity-75"></span>
                <span class="relative inline-flex rounded-full h-2 w-2 bg-error-brand"></span>
              </span>
              En Vivo
            </span>
          </button>
        </nav>
      </div>

      @if (loading()) {
        <app-loading-spinner />
      } @else {

      <!-- ═══ TAB: GENERAL ═══ -->
      @if (tab() === 'general') {
        <div class="space-y-6 max-w-4xl">
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Información de la Organización</h2>
            <p class="text-sm text-text-muted mb-6">Datos generales que aparecen en el sitio público y comunicaciones.</p>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">Nombre de la Organización</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="orgName">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Nombre corto / Acrónimo</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="orgShortName">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Email de contacto</label>
                <input type="email" [class]="CLASS_INPUT" [(ngModel)]="orgEmail">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Teléfono</label>
                <input type="tel" [class]="CLASS_INPUT" [(ngModel)]="orgPhone">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Sitio web</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="orgWebsite">
              </div>
              <div>
                <label [class]="LABEL_INPUT">País sede</label>
                <select [class]="CLASS_INPUT" [(ngModel)]="orgCountry">
                  @for (c of countries; track c) { <option [value]="c">{{ c }}</option> }
                </select>
              </div>
              <div>
                <label [class]="LABEL_INPUT">Cuota administrativa (USD)</label>
                <input type="number" min="0" step="0.01" [class]="CLASS_INPUT" [(ngModel)]="administrativeFeeUsd">
                <p class="text-[11px] text-text-muted mt-1">Se suma a la tarifa de categoría en cada inscripción. Si se guarda en 0, no se muestra en el resumen de inscripción.</p>
              </div>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Redes Sociales</h2>
            <p class="text-sm text-text-muted mb-6">Enlaces que se muestran en el footer del sitio público.</p>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">Instagram</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="igHandle">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Facebook</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="fbHandle">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Twitter / X</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="twHandle">
              </div>
              <div>
                <label [class]="LABEL_INPUT">YouTube</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="ytHandle">
              </div>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Temporada activa</h2>
            <p class="text-sm text-text-muted mb-6">Define el período actual del circuito. Afecta cálculos de ranking y reportes.</p>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label [class]="LABEL_INPUT">Temporada actual</label>
                <input type="number" [class]="CLASS_INPUT" [(ngModel)]="temporadaActual">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Fecha inicio temporada</label>
                <input type="date" [class]="CLASS_INPUT + ' [color-scheme:dark]'" [(ngModel)]="temporadaInicio">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Fecha fin temporada</label>
                <input type="date" [class]="CLASS_INPUT + ' [color-scheme:dark]'" [(ngModel)]="temporadaFin">
              </div>
            </div>
          </div>

          <div class="flex justify-end">
            <button (click)="saveSettings('Configuración guardada correctamente')" [disabled]="saving() || !canEdit()"
                    class="px-6 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ saving() ? 'Guardando...' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
      }

      <!-- ═══ TAB: RANKING ═══ -->
      @if (tab() === 'ranking') {
        <div class="space-y-6 max-w-5xl">
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Parámetros de distribución de puntos de ranking</h2>
            <p class="text-sm text-text-muted mb-6">Define los puntos otorgados por puesto final según el nivel de estrellas del evento. Estos valores se sincronizan con el motor de ranking de SurfScores.</p>
            <h3 class="font-heading text-lg text-white mb-3">Puntos por posición final (por nivel de evento)</h3>
            <div class="bg-navy-deepest rounded-lg border border-navy-mid overflow-hidden">
              <div class="overflow-x-auto">
                <table class="w-full text-sm">
                  <thead class="border-b border-navy-mid bg-navy-dark/50">
                    <tr>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Puesto</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★★</span></th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-navy-mid/40">
                    @for (row of rankingRows; track row.pos) {
                      <tr class="hover:bg-cyan-brand/5 transition">
                        <td class="px-4 py-3 font-heading text-text-light">{{ row.pos }}</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s1"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s2"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s3"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s4"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s5"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s6"></td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.s7"></td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Distribución de premios (% del pozo por evento)</h2>
            <p class="text-sm text-text-muted mb-6">Porcentaje del premio total del evento (según sus estrellas) que recibe cada puesto. Se usa para calcular el monto en USD mostrado en Inscritos → Puntajes de Premios de cada evento.</p>
            <div class="bg-navy-deepest rounded-lg border border-navy-mid overflow-hidden">
              <div class="overflow-x-auto">
                <table class="w-full text-sm">
                  <thead class="border-b border-navy-mid bg-navy-dark/50">
                    <tr>
                      <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Puesto</th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★</span></th>
                      <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted"><span class="text-warning-brand">★★★★★★★</span></th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-navy-mid/40">
                    @for (row of prizeDistRows; track row.label) {
                      <tr class="hover:bg-cyan-brand/5 transition">
                        <td class="px-4 py-3 font-heading text-text-light">{{ row.label }}</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p1">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p2">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p3">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p4">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p5">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p6">%</td>
                        <td class="px-4 py-3 text-right"><input type="number" class="rank-cell" [(ngModel)]="row.p7">%</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Configuración de ranking</h2>
            <p class="text-sm text-text-muted mb-6">Reglas adicionales para el cálculo del ranking general. La cantidad de mejores resultados ahora se define por categoría desde el CRUD de Categorías.</p>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">% del puntaje si no compite (DNS)</label>
                <input type="number" [class]="CLASS_INPUT" [(ngModel)]="dnsPercent">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Penalización por retiro (DSQ, puntos)</label>
                <input type="number" [class]="CLASS_INPUT" [(ngModel)]="dsqPenalty">
              </div>
            </div>
          </div>

          <div class="flex justify-end">
            <button (click)="saveSettings('Parámetros de ranking guardados')" [disabled]="saving() || !canEdit()"
                    class="px-6 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ saving() ? 'Guardando...' : 'Guardar parámetros' }}
            </button>
          </div>
        </div>
      }

      <!-- ═══ TAB: INTEGRACIONES ═══ -->
      @if (tab() === 'integraciones') {
        <div class="space-y-6 max-w-4xl">
          <!-- SurfScores -->
          <div class="bg-navy-dark rounded-xl border border-cyan-brand/30 p-6">
            <div class="flex items-start justify-between mb-4 flex-col sm:flex-row gap-3">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-lg bg-cyan-brand/15 flex items-center justify-center">
                  <svg class="h-6 w-6 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>
                </div>
                <div>
                  <h2 class="font-heading text-xl text-white">SurfScores API</h2>
                  <p class="text-xs text-text-muted">Integración para extracción de rankings, heats y olas.</p>
                </div>
              </div>
            </div>

            <div class="space-y-4">
              <div>
                <label [class]="LABEL_INPUT">Endpoint</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="surfScoresEndpoint">
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label [class]="LABEL_INPUT">Usuario API (correo)</label>
                  <input type="text" [class]="CLASS_INPUT + ' font-mono'" [(ngModel)]="surfScoresUsername" autocomplete="off">
                </div>
                <div>
                  <label [class]="LABEL_INPUT">Contraseña API</label>
                  <input type="password" [class]="CLASS_INPUT + ' font-mono'" [(ngModel)]="surfScoresPassword" autocomplete="off">
                </div>
              </div>

              <div>
                <label [class]="LABEL_INPUT">ID de Organización</label>
                <input type="text" [class]="CLASS_INPUT + ' font-mono'" [(ngModel)]="surfScoresOrgId" placeholder="ej: 142">
                <p class="text-[11px] text-text-muted mt-1">Identificador numérico de la org. en SurfScores. Se usa en las consultas de ranking y circuitos.</p>
              </div>
              <p class="text-[11px] text-text-muted -mt-2">El BFF inicia sesión en SurfScores con estas credenciales y reutiliza el token de sesión obtenido en cada llamada mientras siga vigente; el token nunca se ingresa manualmente aquí.</p>

              <label class="flex items-center gap-3 cursor-pointer p-3 rounded-lg bg-navy-deepest border border-navy-mid">
                <input type="checkbox" [(ngModel)]="surfScoresTermsAccepted" class="h-4 w-4 rounded border-navy-mid bg-navy-dark text-cyan-brand focus:ring-cyan-brand">
                <span class="text-sm text-text-light">Confirmo que acepto los <span class="text-cyan-brand hover:underline cursor-pointer">términos de uso de la API SurfScores</span></span>
              </label>

              <div>
                <label [class]="LABEL_INPUT">Caché (minutos)</label>
                <input type="number" min="1" [class]="CLASS_INPUT" [(ngModel)]="surfScoresCacheMinutes">
              </div>

              <div class="bg-warning-brand/10 border border-warning-brand/30 rounded-lg p-4 flex gap-3 items-start">
                <svg class="h-5 w-5 text-warning-brand flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8.485 3.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 3.495zM10 6a1 1 0 011 1v3a1 1 0 11-2 0V7a1 1 0 011-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/></svg>
                <div>
                  <p class="text-sm text-warning-brand font-medium mb-1">Advertencia</p>
                  <p class="text-xs text-text-light leading-relaxed">La API SurfScores prohíbe el uso como marcador en tiempo real (Live Heatboard). El sistema respetará el límite de caché configurado. Polling excesivo puede causar bloqueo de IP.</p>
                </div>
              </div>

              <div class="flex flex-col sm:flex-row gap-3">
                <button type="button" (click)="testIntegration('surfscores')" [disabled]="testing() || !canEdit()"
                        class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                  Probar conexión
                </button>
                <button type="button" (click)="saveSettings('Integración SurfScores guardada')" [disabled]="saving() || !canEdit()"
                        class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                  Guardar
                </button>
              </div>
            </div>
          </div>

          <!-- WordPress -->
          <div class="bg-navy-dark rounded-xl border border-success-brand/30 p-6">
            <div class="flex items-start justify-between mb-4 flex-col sm:flex-row gap-3">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-lg bg-success-brand/15 flex items-center justify-center">
                  <svg class="h-6 w-6 text-success-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/></svg>
                </div>
                <div>
                  <h2 class="font-heading text-xl text-white">WordPress (Headless CMS)</h2>
                  <p class="text-xs text-text-muted">Origen de noticias y galerías de fotos.</p>
                </div>
              </div>
            </div>
            <div class="space-y-4">
              <div>
                <label [class]="LABEL_INPUT">Endpoint</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="wordPressEndpoint">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Usuario</label>
                <input type="text" [class]="CLASS_INPUT + ' font-mono'" [(ngModel)]="wordPressUsername">
              </div>
              <div class="flex gap-3">
                <button type="button" (click)="testIntegration('wordpress')" [disabled]="testing() || !canEdit()"
                        class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                  Probar conexión
                </button>
                <button type="button" (click)="saveSettings('Integración WordPress guardada')" [disabled]="saving() || !canEdit()"
                        class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                  Guardar
                </button>
              </div>
            </div>
          </div>
        </div>
      }

      <!-- ═══ TAB: NOTIFICACIONES ═══ -->
      @if (tab() === 'notificaciones') {
        <div class="space-y-6 max-w-4xl">
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Tokens de pago</h2>
            <p class="text-sm text-text-muted mb-6">Configuración del flujo de pago en playa con códigos temporales.</p>
            <div class="space-y-4">
              <div>
                <label [class]="LABEL_INPUT">Tiempo de validez del token</label>
                <div class="flex gap-2 items-center">
                  <input type="number" [class]="CLASS_INPUT + ' max-w-[140px]'" [(ngModel)]="tokenValidityHours">
                  <span class="text-sm text-text-muted">horas</span>
                </div>
                <div class="bg-error-brand/10 border border-error-brand/30 rounded-lg p-3 mt-3 flex gap-2 items-start">
                  <svg class="h-4 w-4 text-error-brand flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/></svg>
                  <p class="text-xs text-error-brand leading-relaxed">Este valor es crítico para el flujo de pago en playa. Cambios aquí afectan a todos los tokens activos.</p>
                </div>
              </div>

              <div>
                <label [class]="LABEL_INPUT">Email de notificación al administrador</label>
                <input type="email" [class]="CLASS_INPUT" [(ngModel)]="adminNotifyEmail">
              </div>

              <div>
                <label [class]="LABEL_INPUT">Notificar también a (correos adicionales)</label>
                <div class="space-y-2">
                  @for (email of extraEmails(); track email) {
                    <div class="flex gap-2">
                      <input type="email" [class]="CLASS_INPUT" [value]="email" readonly>
                      <button type="button" (click)="removeExtraEmail(email)"
                              class="px-3 py-2 border border-error-brand/30 hover:border-error-brand text-error-brand font-accent uppercase tracking-wider text-xs rounded-md transition whitespace-nowrap">
                        Quitar
                      </button>
                    </div>
                  }
                  <div class="flex gap-2">
                    <input type="email" placeholder="nuevo@alasglobaltour.com" [class]="CLASS_INPUT" [(ngModel)]="newExtraEmail" (keydown.enter)="addExtraEmail()">
                    <button type="button" (click)="addExtraEmail()"
                            class="px-3 py-2 border border-cyan-brand/40 hover:border-cyan-brand text-cyan-brand font-accent uppercase tracking-wider text-xs rounded-md transition flex items-center gap-1.5 whitespace-nowrap">
                      <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
                      Añadir email
                    </button>
                  </div>
                </div>
              </div>

              <div>
                <label [class]="LABEL_INPUT">Plantilla del email al competidor</label>
                <textarea [class]="CLASS_INPUT + ' font-mono text-xs'" rows="4" [(ngModel)]="emailTemplate"></textarea>
                <p class="text-[11px] text-text-muted mt-1">Variables disponibles: [EVENTO], [TOKEN], [COMPETIDOR], [MONTO]</p>
              </div>
            </div>
          </div>

          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <h2 class="font-heading text-xl text-white mb-1">Notificaciones generales</h2>
            <p class="text-sm text-text-muted mb-6">Controla qué eventos del sistema disparan notificaciones por email.</p>
            <div class="space-y-4">
              <div>
                <label [class]="LABEL_INPUT">Email de sistema (remitente)</label>
                <input type="email" [class]="CLASS_INPUT" [(ngModel)]="systemSenderEmail">
              </div>

              <div class="space-y-3">
                <label class="flex items-center justify-between gap-4 cursor-pointer p-4 rounded-lg bg-navy-deepest border border-navy-mid hover:border-cyan-brand/40 transition">
                  <div>
                    <p class="font-medium text-sm text-text-light">Notificar nuevas inscripciones al admin</p>
                    <p class="text-xs text-text-muted">Envía email cada vez que un competidor se inscribe a un evento.</p>
                  </div>
                  <button type="button" role="switch" [attr.aria-checked]="notifNewInscription()"
                          aria-label="Notificar nuevas inscripciones al admin"
                          (click)="notifNewInscription.set(!notifNewInscription())" [disabled]="!canEdit()"
                          [class]="notifNewInscription() ? 'bg-cyan-brand' : 'bg-navy-mid'"
                          class="relative w-12 h-6 rounded-full transition flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-brand focus-visible:ring-offset-2 focus-visible:ring-offset-navy-deepest">
                    <span [class]="notifNewInscription() ? 'translate-x-6' : 'translate-x-0.5'"
                          class="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform"></span>
                  </button>
                </label>

                <label class="flex items-center justify-between gap-4 cursor-pointer p-4 rounded-lg bg-navy-deepest border border-navy-mid hover:border-cyan-brand/40 transition">
                  <div>
                    <p class="font-medium text-sm text-text-light">Notificar pagos confirmados</p>
                    <p class="text-xs text-text-muted">Email al competidor cuando su pago es validado.</p>
                  </div>
                  <button type="button" role="switch" [attr.aria-checked]="notifPaymentConfirmed()"
                          aria-label="Notificar pagos confirmados"
                          (click)="notifPaymentConfirmed.set(!notifPaymentConfirmed())" [disabled]="!canEdit()"
                          [class]="notifPaymentConfirmed() ? 'bg-cyan-brand' : 'bg-navy-mid'"
                          class="relative w-12 h-6 rounded-full transition flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-brand focus-visible:ring-offset-2 focus-visible:ring-offset-navy-deepest">
                    <span [class]="notifPaymentConfirmed() ? 'translate-x-6' : 'translate-x-0.5'"
                          class="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform"></span>
                  </button>
                </label>

                <label class="flex items-center justify-between gap-4 cursor-pointer p-4 rounded-lg bg-navy-deepest border border-navy-mid hover:border-cyan-brand/40 transition">
                  <div>
                    <p class="font-medium text-sm text-text-light">Notificar tokens expirados</p>
                    <p class="text-xs text-text-muted">Avisa al competidor y admin cuando un token caduca sin ser canjeado.</p>
                  </div>
                  <button type="button" role="switch" [attr.aria-checked]="notifTokenExpired()"
                          aria-label="Notificar tokens expirados"
                          (click)="notifTokenExpired.set(!notifTokenExpired())" [disabled]="!canEdit()"
                          [class]="notifTokenExpired() ? 'bg-cyan-brand' : 'bg-navy-mid'"
                          class="relative w-12 h-6 rounded-full transition flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-brand focus-visible:ring-offset-2 focus-visible:ring-offset-navy-deepest">
                    <span [class]="notifTokenExpired() ? 'translate-x-6' : 'translate-x-0.5'"
                          class="absolute top-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform"></span>
                  </button>
                </label>
              </div>
            </div>
          </div>

          <div class="flex justify-end">
            <button (click)="saveSettings('Configuración de notificaciones guardada')" [disabled]="saving() || !canEdit()"
                    class="px-6 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ saving() ? 'Guardando...' : 'Guardar configuración' }}
            </button>
          </div>
        </div>
      }

      <!-- ═══ TAB: EN VIVO ═══ -->
      @if (tab() === 'envivo') {
        <div class="space-y-6 max-w-6xl">

          <!-- YouTube Live -->
          <div class="bg-navy-dark rounded-xl border border-error-brand/40 p-6">
            <div class="flex items-start justify-between mb-5 flex-col sm:flex-row gap-3">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-lg bg-error-brand/15 flex items-center justify-center flex-shrink-0">
                  <svg class="h-6 w-6 text-error-brand" fill="currentColor" viewBox="0 0 24 24"><path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>
                </div>
                <div>
                  <h2 class="font-heading text-xl text-white">Streaming en Vivo — YouTube</h2>
                  <p class="text-xs text-text-muted">Configura la transmisión que se incrusta en la página pública del evento.</p>
                </div>
              </div>
              <div class="flex items-center gap-3">
                @if (ytActive()) {
                  <div class="flex items-center gap-2 text-xs">
                    <span class="relative flex h-2 w-2"><span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-error-brand opacity-75"></span><span class="relative inline-flex rounded-full h-2 w-2 bg-error-brand"></span></span>
                    <span class="text-error-brand font-accent uppercase tracking-wider">En Vivo</span>
                  </div>
                } @else {
                  <div class="flex items-center gap-2 text-xs">
                    <span class="w-2 h-2 rounded-full bg-text-muted"></span>
                    <span class="text-text-muted font-accent uppercase tracking-wider">Inactivo</span>
                  </div>
                }
                <button type="button" (click)="ytActive.set(!ytActive())" [disabled]="!canEdit()"
                        [class]="ytActive() ? 'bg-error-brand' : 'bg-navy-mid'"
                        class="relative w-14 h-7 rounded-full transition flex-shrink-0 disabled:opacity-50">
                  <span [class]="ytActive() ? 'translate-x-7' : 'translate-x-0.5'"
                        class="absolute top-1 w-5 h-5 rounded-full bg-white transition-transform shadow"></span>
                </button>
              </div>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <div class="space-y-4">
                <div>
                  <label [class]="LABEL_INPUT">Evento en emisión</label>
                  <select [class]="CLASS_INPUT" [(ngModel)]="ytEvento">
                    <option value="">— Seleccionar evento —</option>
                    @for (e of eventos(); track e.id) { <option [value]="e.id">{{ e.label }}</option> }
                  </select>
                </div>
                <div>
                  <label [class]="LABEL_INPUT + ' flex items-center gap-2'">
                    URL o ID del video / stream de YouTube
                  </label>
                  <input type="text" [class]="CLASS_INPUT + ' font-mono text-sm'" [(ngModel)]="ytVideoId" placeholder="ej: dQw4w9WgXcQ  ó  https://youtu.be/dQw4w9WgXcQ">
                  <p class="text-[11px] text-text-muted mt-1">Acepta el ID corto o la URL completa de YouTube.</p>
                </div>
                <div>
                  <label [class]="LABEL_INPUT">Privacidad del embed</label>
                  <div class="flex gap-3">
                    <label class="flex items-center gap-2 cursor-pointer text-sm text-text-light">
                      <input type="radio" name="yt-privacy" value="public" [(ngModel)]="ytPrivacy" class="text-cyan-brand">
                      Público
                    </label>
                    <label class="flex items-center gap-2 cursor-pointer text-sm text-text-light">
                      <input type="radio" name="yt-privacy" value="unlisted" [(ngModel)]="ytPrivacy" class="text-cyan-brand">
                      No listado
                    </label>
                  </div>
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label [class]="LABEL_INPUT">Ancho del embed (%)</label>
                    <input type="number" [class]="CLASS_INPUT" [(ngModel)]="ytWidth">
                  </div>
                  <div>
                    <label [class]="LABEL_INPUT">Alto del embed (px)</label>
                    <input type="number" [class]="CLASS_INPUT" [(ngModel)]="ytHeight">
                  </div>
                </div>
              </div>

              <div class="space-y-3">
                <p [class]="LABEL_INPUT">Previsualización en el sitio público</p>
                <div class="rounded-xl overflow-hidden border border-navy-mid bg-navy-deepest">
                  <div class="relative bg-black" style="padding-top: 56.25%">
                    <div class="absolute inset-0 flex flex-col items-center justify-center gap-3">
                      @if (!ytVideoId) {
                        <div class="text-center text-text-muted text-sm px-4">
                          <svg class="h-10 w-10 mx-auto mb-2 text-navy-mid" fill="currentColor" viewBox="0 0 24 24"><path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>
                          <p>Ingresa un ID de video para ver la previsualización</p>
                        </div>
                      } @else {
                        <div class="absolute inset-0 flex items-center justify-center bg-black">
                          <div class="text-center">
                            <div class="w-16 h-16 rounded-full bg-error-brand flex items-center justify-center mx-auto mb-3 shadow-lg shadow-error-brand/40">
                              <svg class="h-8 w-8 text-white ml-1" fill="currentColor" viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>
                            </div>
                            <p class="text-white font-heading text-lg">{{ ytEventoLabel() || 'Evento en vivo' }}</p>
                            <p class="text-text-muted text-xs mt-1">ALAS Latin Tour · En Vivo</p>
                          </div>
                        </div>
                      }
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="flex flex-col sm:flex-row gap-3 mt-6 pt-5 border-t border-navy-mid">
              <button type="button" (click)="saveSettings('Configuración de streaming guardada')" [disabled]="saving() || !canEdit()"
                      class="px-5 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                Guardar streaming
              </button>
              <button type="button" (click)="ytActive.set(false); saveSettings('Streaming desactivado del sitio público')" [disabled]="!canEdit()"
                      class="px-5 py-2.5 border border-error-brand/40 hover:border-error-brand text-error-brand font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                Desactivar y ocultar
              </button>
            </div>
          </div>

          <!-- Programación del evento (PDF) -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
            <div class="flex items-center gap-3 mb-5">
              <div class="w-12 h-12 rounded-lg bg-orange-brand/15 flex items-center justify-center flex-shrink-0">
                <svg class="h-6 w-6 text-orange-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
              </div>
              <div>
                <h2 class="font-heading text-xl text-white">Programación del Evento — PDF</h2>
                <p class="text-xs text-text-muted">Documento con el itinerario de heats que verán los espectadores en la página del evento en vivo.</p>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <input #scheduleInput type="file" accept="application/pdf" class="hidden" (change)="onScheduleFileSelected($event)">
              <button type="button" (click)="scheduleInput.click()" [disabled]="uploadingSchedule() || !canEdit()"
                      class="px-3 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition disabled:opacity-50">
                {{ uploadingSchedule() ? 'Subiendo...' : 'Subir PDF' }}
              </button>
              <input type="url" [class]="CLASS_INPUT" [(ngModel)]="schedulePdfUrl" placeholder="https://cdn.ejemplo.com/programacion-evento.pdf">
            </div>
            <p class="text-text-muted/70 text-[11px] mt-1.5">Formato permitido: PDF.</p>
            @if (scheduleUploadError()) {
              <p class="text-error-brand text-xs mt-1">{{ scheduleUploadError() }}</p>
            }
            @if (schedulePdfUrl) {
              <div class="mt-3 flex items-center justify-between bg-navy-deepest border border-navy-mid rounded-lg px-4 py-2.5">
                <a [href]="schedulePdfUrl" target="_blank" class="text-cyan-brand hover:underline text-sm truncate">{{ schedulePdfUrl }}</a>
                <button type="button" (click)="schedulePdfUrl = ''" [disabled]="!canEdit()"
                        class="text-text-muted hover:text-error-brand ml-3 flex-shrink-0" title="Quitar PDF">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
                </button>
              </div>
            }

            <div class="flex mt-6 pt-5 border-t border-navy-mid">
              <button type="button" (click)="saveSettings('Programación del evento guardada')" [disabled]="saving() || !canEdit()"
                      class="px-5 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                Guardar programación
              </button>
            </div>
          </div>

          <!-- SurfScores Live -->
          <div class="bg-navy-dark rounded-xl border border-cyan-brand/30 p-6">
            <div class="flex items-start justify-between mb-5 flex-col sm:flex-row gap-3">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-lg bg-cyan-brand/15 flex items-center justify-center flex-shrink-0">
                  <svg class="h-6 w-6 text-cyan-brand" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>
                </div>
                <div>
                  <h2 class="font-heading text-xl text-white">Puntajes en Vivo — SurfScores</h2>
                  <p class="text-xs text-text-muted">Iframe de resultados de heat embebido en la página del evento.</p>
                </div>
              </div>
              <div class="flex items-center gap-3">
                @if (ssActive()) {
                  <div class="flex items-center gap-2 text-xs">
                    <span class="relative flex h-2 w-2"><span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-cyan-brand opacity-75"></span><span class="relative inline-flex rounded-full h-2 w-2 bg-cyan-brand"></span></span>
                    <span class="text-cyan-brand font-accent uppercase tracking-wider">Activo</span>
                  </div>
                } @else {
                  <div class="flex items-center gap-2 text-xs">
                    <span class="w-2 h-2 rounded-full bg-text-muted"></span>
                    <span class="text-text-muted font-accent uppercase tracking-wider">Inactivo</span>
                  </div>
                }
                <button type="button" (click)="ssActive.set(!ssActive())" [disabled]="!canEdit()"
                        [class]="ssActive() ? 'bg-cyan-brand' : 'bg-navy-mid'"
                        class="relative w-14 h-7 rounded-full transition flex-shrink-0 disabled:opacity-50">
                  <span [class]="ssActive() ? 'translate-x-7' : 'translate-x-0.5'"
                        class="absolute top-1 w-5 h-5 rounded-full bg-white transition-transform shadow"></span>
                </button>
              </div>
            </div>

            <div class="bg-warning-brand/10 border border-warning-brand/40 rounded-xl p-4 flex gap-3 items-start mb-6">
              <svg class="h-5 w-5 text-warning-brand flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8.485 3.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 3.495zM10 6a1 1 0 011 1v3a1 1 0 11-2 0V7a1 1 0 011-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/></svg>
              <div>
                <p class="text-sm text-warning-brand font-semibold mb-1">Restricción contractual — Live Heatboard</p>
                <p class="text-xs text-text-light leading-relaxed">SurfScores prohíbe explícitamente usar su API como marcador en tiempo real. Este iframe sirve para pantallas de exhibición locales del evento (TVs en la playa, backstage), <strong>no para el sitio público</strong>. El BFF .NET aplica un caché mínimo configurado.</p>
              </div>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <div class="space-y-4">
                <div>
                  <label [class]="LABEL_INPUT">Evento a mostrar</label>
                  <select [class]="CLASS_INPUT" [(ngModel)]="ssEvento">
                    <option value="">— Seleccionar evento —</option>
                    @for (e of eventos(); track e.id) { <option [value]="e.id">{{ e.label }}</option> }
                  </select>
                </div>
                <div>
                  <label [class]="LABEL_INPUT">URL del iframe de SurfScores</label>
                  <input type="text" [class]="CLASS_INPUT + ' font-mono text-sm'" [(ngModel)]="ssUrl" placeholder="https://surfscores.com/embed/event/CODIGO-EVENTO">
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label [class]="LABEL_INPUT">Ancho (%)</label>
                    <input type="number" [class]="CLASS_INPUT" [(ngModel)]="ssWidth">
                  </div>
                  <div>
                    <label [class]="LABEL_INPUT">Alto (px)</label>
                    <input type="number" [class]="CLASS_INPUT" [(ngModel)]="ssHeight">
                  </div>
                </div>
                <div>
                  <label [class]="LABEL_INPUT + ' flex justify-between items-center'">
                    <span>Actualización automática</span>
                    <span class="text-cyan-brand text-[11px] normal-case tracking-normal">{{ ssRefresh }} min (mínimo recomendado: 5)</span>
                  </label>
                  <input type="range" min="5" max="60" step="5" [(ngModel)]="ssRefresh" class="w-full accent-cyan-brand cursor-pointer">
                  <div class="flex justify-between text-[10px] text-text-muted mt-1">
                    <span>5 min</span><span>30 min</span><span>60 min</span>
                  </div>
                  <p class="text-[11px] text-warning-brand mt-2">⚠ Valores menores a 5 minutos infringen los términos de la API SurfScores.</p>
                </div>
                <div>
                  <label class="flex items-center gap-2 cursor-pointer text-sm text-text-light p-2 rounded-lg border border-navy-mid hover:border-cyan-brand/40 transition">
                    <input type="checkbox" [(ngModel)]="ssUsoLocal" class="text-cyan-brand">
                    <span>Solo pantallas locales del evento (TVs en la playa, backstage)</span>
                  </label>
                </div>
              </div>
            </div>

            <div class="flex flex-col sm:flex-row gap-3 mt-6 pt-5 border-t border-navy-mid">
              <button type="button" (click)="saveSettings('Configuración de puntajes guardada')" [disabled]="saving() || !canEdit()"
                      class="px-5 py-2.5 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                Guardar configuración
              </button>
              <button type="button" (click)="ssActive.set(false); saveSettings('Iframe de SurfScores desactivado')" [disabled]="!canEdit()"
                      class="px-5 py-2.5 border border-navy-mid hover:border-error-brand text-text-muted hover:text-error-brand font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
                Desactivar iframe
              </button>
            </div>
          </div>
        </div>
      }
      }
    </div>

    <!-- Toast -->
    @if (toast().show) {
      <div class="fixed bottom-6 right-6 z-50 bg-navy-dark border border-success-brand/50 rounded-lg shadow-2xl px-5 py-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-success-brand" fill="currentColor" viewBox="0 0 20 20"><path d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"/></svg>
        <p class="text-sm text-text-light">{{ toast().message }}</p>
      </div>
    }
  `,
  styles: [`
    .rank-cell {
      background: rgba(0, 79, 142, 0.4);
      border: 1px solid #004F8E;
      border-radius: 4px;
      padding: 6px 10px;
      color: #EEEEEE;
      font-size: 13px;
      width: 80px;
      text-align: right;
      font-family: monospace;
    }
    .rank-cell:focus {
      outline: none;
      border-color: #0081C6;
      box-shadow: 0 0 0 3px rgba(0,129,198,0.15);
    }
  `],
})
export class ConfiguracionComponent implements OnInit {
  private api = inject(ApiService);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Configuracion'));

  readonly LABEL_INPUT = LABEL_INPUT;
  readonly CLASS_INPUT = CLASS_INPUT;

  tab = signal<ConfigTab>('general');

  tabClass(t: ConfigTab): string {
    return this.tab() === t
      ? 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-cyan-brand border-b-2 border-cyan-brand whitespace-nowrap'
      : 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-text-muted border-b-2 border-transparent hover:text-text-light transition whitespace-nowrap';
  }

  loading = signal(true);
  saving = signal(false);
  testing = signal(false);

  toast = signal<{ show: boolean; message: string }>({ show: false, message: '' });
  showToast(message: string): void {
    this.toast.set({ show: true, message });
    setTimeout(() => this.toast.set({ show: false, message: '' }), 3000);
  }

  // ─── Tab: General ────────────────────────────────────────────
  orgName = '';
  orgShortName = '';
  orgEmail = '';
  orgPhone = '';
  orgWebsite = '';
  orgCountry = '';
  administrativeFeeUsd: number | null = 0;
  countries = ['Colombia', 'Perú', 'Chile', 'Brasil', 'Argentina', 'México', 'Costa Rica', 'Ecuador'];

  igHandle = '';
  fbHandle = '';
  twHandle = '';
  ytHandle = '';

  temporadaActual = new Date().getFullYear();
  temporadaInicio = '';
  temporadaFin = '';

  // ─── Tab: Ranking ────────────────────────────────────────────
  rankingRows: RankingPointsRow[] = [];
  prizeDistRows: PrizeDistRow[] = [];
  bestResultsCount = 5;
  dnsPercent = 0;
  dsqPenalty = 0;

  // ─── Tab: Integraciones ──────────────────────────────────────
  surfScoresEndpoint = '';
  surfScoresUsername = '';
  surfScoresPassword = '';
  surfScoresOrgId = '';
  surfScoresTermsAccepted = true;
  surfScoresCacheMinutes = 5;
  wordPressEndpoint = '';
  wordPressUsername = '';

  // ─── Tab: Notificaciones ─────────────────────────────────────
  tokenValidityHours = 24;
  adminNotifyEmail = '';
  extraEmails = signal<string[]>([]);
  newExtraEmail = '';
  emailTemplate = '';
  systemSenderEmail = '';
  notifNewInscription = signal(true);
  notifPaymentConfirmed = signal(true);
  notifTokenExpired = signal(true);

  addExtraEmail(): void {
    const email = this.newExtraEmail.trim();
    if (!email) return;
    this.extraEmails.update(list => list.includes(email) ? list : [...list, email]);
    this.newExtraEmail = '';
  }

  removeExtraEmail(email: string): void {
    this.extraEmails.update(list => list.filter(e => e !== email));
  }

  // ─── Tab: En Vivo ────────────────────────────────────────────
  eventos = signal<DemoEvento[]>([]);

  ytActive = signal(false);
  ytEvento = '';
  ytVideoId = '';
  ytPrivacy: 'public' | 'unlisted' = 'public';
  ytWidth = 100;
  ytHeight = 480;

  ssActive = signal(false);
  ssEvento = '';
  ssUrl = '';
  ssWidth = 100;
  ssHeight = 600;
  ssRefresh = 5;
  ssUsoLocal = true;

  schedulePdfUrl = '';
  uploadingSchedule = signal(false);
  scheduleUploadError = signal<string | null>(null);

  ytEventoLabel(): string {
    return this.eventos().find(e => e.id === this.ytEvento)?.label ?? '';
  }

  async onScheduleFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.scheduleUploadError.set(null);
    this.uploadingSchedule.set(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const uploaded = await this.api.upload<{ url: string }>('/uploads/live-schedule', formData);
      this.schedulePdfUrl = uploaded.url;
    } catch (err: any) {
      this.scheduleUploadError.set(err?.message ?? 'No se pudo subir el PDF.');
    } finally {
      this.uploadingSchedule.set(false);
      input.value = '';
    }
  }

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      const [settings, eventsRes] = await Promise.all([
        this.api.get<any>('/admin/settings'),
        this.api.get<any>('/events?limit=100'),
      ]);
      this.applySettings(settings);
      this.eventos.set((eventsRes?.data ?? []).map((e: any) => ({ id: e.id, label: `${e.nombre} · ${e.pais}` })));
    } catch {
      this.showToast('Error al cargar la configuración');
    } finally {
      this.loading.set(false);
    }
  }

  private applySettings(s: any): void {
    const g = s.general ?? {};
    this.orgName = g.organizationName ?? '';
    this.orgShortName = g.shortName ?? '';
    this.orgEmail = g.contactEmail ?? '';
    this.orgPhone = g.phone ?? '';
    this.orgWebsite = g.website ?? '';
    this.orgCountry = g.headquartersCountry ?? '';
    this.administrativeFeeUsd = g.administrativeFeeUsd ?? 0;
    this.igHandle = g.socialLinks?.instagram ?? '';
    this.fbHandle = g.socialLinks?.facebook ?? '';
    this.twHandle = g.socialLinks?.x ?? '';
    this.ytHandle = g.socialLinks?.youTube ?? '';
    this.temporadaActual = g.season?.currentYear ?? new Date().getFullYear();
    this.temporadaInicio = (g.season?.startDate ?? '').slice(0, 10);
    this.temporadaFin = (g.season?.endDate ?? '').slice(0, 10);

    const r = s.ranking ?? {};
    this.bestResultsCount = r.bestResultsCount ?? 5;
    this.dnsPercent = r.dnsScorePercentage ?? 0;
    this.dsqPenalty = r.dsqPenaltyPoints ?? 0;
    this.rankingRows = (r.pointsMatrix ?? []).map((row: any) => ({
      pos: row.position, s1: row.star1, s2: row.star2, s3: row.star3, s4: row.star4, s5: row.star5, s6: row.star6, s7: row.star7,
    }));
    this.prizeDistRows = (r.prizeDistribution ?? []).map((row: any) => ({
      label: row.placeLabel, p1: row.star1Percent, p2: row.star2Percent, p3: row.star3Percent, p4: row.star4Percent, p5: row.star5Percent, p6: row.star6Percent, p7: row.star7Percent,
    }));

    const i = s.integrations ?? {};
    this.surfScoresEndpoint = i.surfScores?.endpoint ?? '';
    this.surfScoresUsername = i.surfScores?.username ?? '';
    this.surfScoresPassword = i.surfScores?.password ?? '';
    this.surfScoresOrgId = i.surfScores?.organizacionId ?? '';
    this.surfScoresTermsAccepted = i.surfScores?.termsAccepted ?? false;
    this.surfScoresCacheMinutes = i.surfScores?.cacheMinutes ?? 5;
    this.wordPressEndpoint = i.wordPress?.endpoint ?? '';
    this.wordPressUsername = i.wordPress?.username ?? '';

    const n = s.notifications ?? {};
    this.tokenValidityHours = n.tokenValidityHours ?? 24;
    this.adminNotifyEmail = n.adminEmail ?? '';
    this.extraEmails.set(n.additionalAdminEmails ?? []);
    this.emailTemplate = n.competitorTokenEmailTemplate ?? '';
    this.systemSenderEmail = n.senderEmail ?? '';
    this.notifNewInscription.set(n.notifyNewInscriptions ?? true);
    this.notifPaymentConfirmed.set(n.notifyConfirmedPayments ?? true);
    this.notifTokenExpired.set(n.notifyExpiredTokens ?? true);

    const l = s.live ?? {};
    this.ytActive.set(l.youTube?.active ?? false);
    this.ytEvento = l.youTube?.eventId ?? '';
    this.ytVideoId = l.youTube?.videoIdOrUrl ?? '';
    this.ytPrivacy = l.youTube?.privacy ?? 'public';
    this.ytWidth = l.youTube?.width ?? 100;
    this.ytHeight = l.youTube?.height ?? 480;
    this.ssActive.set(l.surfScores?.active ?? false);
    this.ssEvento = l.surfScores?.eventId ?? '';
    this.ssUrl = l.surfScores?.embedUrl ?? '';
    this.ssWidth = l.surfScores?.width ?? 100;
    this.ssHeight = l.surfScores?.height ?? 600;
    this.ssRefresh = l.surfScores?.refreshMinutes ?? 5;
    this.ssUsoLocal = l.surfScores?.localDisplaysOnly ?? true;
    this.schedulePdfUrl = l.schedulePdfUrl ?? '';
  }

  private buildPayload(): any {
    return {
      general: {
        organizationName: this.orgName,
        shortName: this.orgShortName,
        contactEmail: this.orgEmail,
        phone: this.orgPhone,
        website: this.orgWebsite,
        headquartersCountry: this.orgCountry,
        administrativeFeeUsd: this.administrativeFeeUsd ?? 0,
        socialLinks: { instagram: this.igHandle, facebook: this.fbHandle, x: this.twHandle, youTube: this.ytHandle },
        season: { currentYear: this.temporadaActual, startDate: this.temporadaInicio, endDate: this.temporadaFin },
      },
      ranking: {
        bestResultsCount: this.bestResultsCount,
        dnsScorePercentage: this.dnsPercent,
        dsqPenaltyPoints: this.dsqPenalty,
        pointsMatrix: this.rankingRows.map(row => ({
          position: row.pos, star1: row.s1, star2: row.s2, star3: row.s3, star4: row.s4, star5: row.s5, star6: row.s6, star7: row.s7,
        })),
        prizeDistribution: this.prizeDistRows.map(row => ({
          placeLabel: row.label, star1Percent: row.p1, star2Percent: row.p2, star3Percent: row.p3, star4Percent: row.p4, star5Percent: row.p5, star6Percent: row.p6, star7Percent: row.p7,
        })),
      },
      integrations: {
        surfScores: {
          endpoint: this.surfScoresEndpoint,
          username: this.surfScoresUsername,
          password: this.surfScoresPassword,
          organizacionId: this.surfScoresOrgId,
          termsAccepted: this.surfScoresTermsAccepted,
          cacheMinutes: this.surfScoresCacheMinutes,
        },
        wordPress: { endpoint: this.wordPressEndpoint, username: this.wordPressUsername },
      },
      notifications: {
        tokenValidityHours: this.tokenValidityHours,
        adminEmail: this.adminNotifyEmail,
        additionalAdminEmails: this.extraEmails(),
        competitorTokenEmailTemplate: this.emailTemplate,
        senderEmail: this.systemSenderEmail,
        notifyNewInscriptions: this.notifNewInscription(),
        notifyConfirmedPayments: this.notifPaymentConfirmed(),
        notifyExpiredTokens: this.notifTokenExpired(),
      },
      live: {
        youTube: {
          active: this.ytActive(), eventId: this.ytEvento || null, videoIdOrUrl: this.ytVideoId,
          privacy: this.ytPrivacy, width: this.ytWidth, height: this.ytHeight,
        },
        surfScores: {
          active: this.ssActive(), eventId: this.ssEvento || null, embedUrl: this.ssUrl,
          width: this.ssWidth, height: this.ssHeight, refreshMinutes: this.ssRefresh, localDisplaysOnly: this.ssUsoLocal,
        },
        schedulePdfUrl: this.schedulePdfUrl,
      },
    };
  }

  async saveSettings(successMessage: string): Promise<void> {
    this.saving.set(true);
    try {
      const res = await this.api.put<any>('/admin/settings', this.buildPayload());
      this.applySettings(res);
      this.showToast(successMessage);
    } catch {
      this.showToast('Error al guardar la configuración');
    } finally {
      this.saving.set(false);
    }
  }

  async testIntegration(provider: 'surfscores' | 'wordpress'): Promise<void> {
    this.testing.set(true);
    try {
      const res = await this.api.post<any>(`/admin/settings/integrations/${provider}/test`, {});
      this.showToast(res?.message ?? 'Prueba completada');
    } catch {
      this.showToast('Error al probar la conexión');
    } finally {
      this.testing.set(false);
    }
  }
}
