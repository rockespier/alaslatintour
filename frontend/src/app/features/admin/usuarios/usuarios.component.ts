import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

type UsuariosTab = 'usuarios' | 'roles' | 'permisos';
type RolUsuario = 'Super Admin' | 'Admin' | 'Árbitro' | 'Revisor';
type PermLevel = 'full' | 'read-only' | 'none';

interface UsuarioAdmin {
  id: string;
  iniciales: string;
  gradient: string;
  nombreCompleto: string;
  email: string;
  rol: RolUsuario;
  ultimaSesion: string;
  estado: 'Activo' | 'Inactivo' | 'Bloqueado';
  bloqueadoHasta: string | null;
}

interface ApiRolePermission {
  module: string;
  level: PermLevel;
}

interface ApiRole {
  name: RolUsuario;
  permissions: ApiRolePermission[];
}

const ROL_DESCRIPCIONES: Record<RolUsuario, string> = {
  'Super Admin': 'Acceso completo al sistema, incluyendo configuración y gestión de usuarios.',
  'Admin': 'Gestión de circuitos, eventos, inscritos y pagos. Sin acceso a configuración de sistema.',
  'Árbitro': 'Acceso de solo lectura a inscritos y resultados. Validación de pagos en playa.',
  'Revisor': 'Solo lectura. Puede exportar reportes.',
};

const ROL_ESTILOS: Record<RolUsuario, { colorClass: string; bgClass: string }> = {
  'Super Admin': { colorClass: 'text-purple-400', bgClass: 'bg-purple-500/15' },
  'Admin': { colorClass: 'text-cyan-brand', bgClass: 'bg-cyan-brand/15' },
  'Árbitro': { colorClass: 'text-orange-brand', bgClass: 'bg-orange-brand/15' },
  'Revisor': { colorClass: 'text-text-muted', bgClass: 'bg-navy-mid/50' },
};

const GRADIENTS = [
  'from-cyan-brand to-navy-mid',
  'from-orange-brand to-cyan-brand',
  'from-success-brand to-navy-mid',
  'from-warning-brand to-orange-brand',
  'from-navy-mid to-navy-deepest',
  'from-pink-500 to-purple-500',
  'from-blue-500 to-teal-500',
  'from-yellow-500 to-orange-brand',
];

function gradientFor(id: string): string {
  let hash = 0;
  for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
  return GRADIENTS[hash % GRADIENTS.length];
}

const LABEL_INPUT = 'block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5';
const CLASS_INPUT = 'w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light placeholder-text-muted/50 focus:outline-none focus:border-cyan-brand transition';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent],
  template: `
    <div class="py-8">
      <div class="mb-6">
        <p class="text-xs text-text-muted font-accent uppercase tracking-wider">Admin / Usuarios</p>
        <h1 class="font-heading text-2xl text-white leading-tight">Gestión de Usuarios</h1>
      </div>

      <!-- Tabs -->
      <div class="border-b border-navy-mid mb-6 overflow-x-auto">
        <nav class="flex gap-2 min-w-max">
          <button (click)="tab.set('usuarios')" [class]="tabClass('usuarios')">Usuarios</button>
          <button (click)="tab.set('roles')" [class]="tabClass('roles')">Roles y Perfiles</button>
          <button (click)="tab.set('permisos')" [class]="tabClass('permisos')">Permisos</button>
        </nav>
      </div>

      @if (loading()) {
        <app-loading-spinner />
      } @else {

      <!-- ═══ TAB: USUARIOS ═══ -->
      @if (tab() === 'usuarios') {
        <div>
          <!-- Filter bar -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid p-4 mb-6 flex flex-col lg:flex-row gap-3 lg:items-center lg:justify-between">
            <div class="flex flex-col sm:flex-row gap-3 flex-1">
              <div class="relative flex-1 max-w-md">
                <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
                <input type="text" placeholder="Buscar por nombre o email..." [ngModel]="searchTerm()" (ngModelChange)="searchTerm.set($event)" [class]="CLASS_INPUT + ' pl-9'">
              </div>
              <select [class]="CLASS_INPUT + ' sm:max-w-[180px]'" [ngModel]="filterRol()" (ngModelChange)="filterRol.set($event)">
                <option value="">Todos los roles</option>
                @for (r of roles; track r) { <option [value]="r">{{ r }}</option> }
              </select>
              <select [class]="CLASS_INPUT + ' sm:max-w-[180px]'" [ngModel]="filterEstado()" (ngModelChange)="filterEstado.set($event)">
                <option value="">Todos los estados</option>
                <option value="Activo">Activo</option>
                <option value="Bloqueado">Bloqueado</option>
                <option value="Inactivo">Inactivo</option>
              </select>
            </div>
            @if (canEdit()) {
              <button (click)="openCreate()"
                      class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition flex items-center gap-2 justify-center whitespace-nowrap">
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
                Nuevo Usuario
              </button>
            }
          </div>

          <!-- Users table -->
          <div class="bg-navy-dark rounded-xl border border-navy-mid overflow-hidden">
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="border-b border-navy-mid">
                  <tr>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Avatar</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Nombre Completo</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Email</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Rol</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Última sesión</th>
                    <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted">Estado</th>
                    <th class="px-4 py-3 text-right font-accent uppercase text-xs tracking-wider text-text-muted">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-navy-mid/50">
                  @for (u of filteredUsuarios(); track u.id) {
                    <tr [class]="u.estado === 'Inactivo' ? 'hover:bg-cyan-brand/5 transition opacity-60' : 'hover:bg-cyan-brand/5 transition'">
                      <td class="px-4 py-3">
                        <div [class]="'w-9 h-9 rounded-full flex items-center justify-center font-heading text-white text-xs bg-gradient-to-br ' + u.gradient">{{ u.iniciales }}</div>
                      </td>
                      <td class="px-4 py-3 font-medium text-text-light">{{ u.nombreCompleto }}</td>
                      <td class="px-4 py-3 text-text-muted">{{ u.email }}</td>
                      <td class="px-4 py-3"><span [class]="rolClass(u.rol)">{{ u.rol }}</span></td>
                      <td class="px-4 py-3 text-text-muted">
                        {{ u.ultimaSesion }}
                        @if (u.bloqueadoHasta) {
                          <div class="text-[11px] text-warning-brand mt-1">Bloqueado hasta {{ u.bloqueadoHasta }}</div>
                        }
                      </td>
                      <td class="px-4 py-3">
                        <span [class]="estadoClass(u.estado)">
                          {{ u.estado }}
                        </span>
                      </td>
                      <td class="px-4 py-3 text-right whitespace-nowrap">
                        @if (canEdit()) {
                          <button (click)="openEdit(u)" class="text-xs font-accent uppercase tracking-wider text-cyan-brand hover:text-cyan-dark mr-2">Editar</button>
                          <button (click)="openPassword(u)" class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-text-light mr-2">Contraseña</button>
                          @if (u.estado !== 'Inactivo') {
                            <button (click)="confirmDeactivate(u)" class="text-xs font-accent uppercase tracking-wider text-text-muted hover:text-error-brand">Desactivar</button>
                          } @else {
                            <button (click)="activate(u)" class="text-xs font-accent uppercase tracking-wider text-success-brand hover:text-green-400">Activar</button>
                          }
                        } @else {
                          <span class="text-xs text-text-muted">—</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <div class="border-t border-navy-mid px-4 py-3 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-text-muted">
              <p>Mostrando {{ filteredUsuarios().length }} de {{ usuarios().length }} usuarios</p>
            </div>
          </div>
        </div>
      }

      <!-- ═══ TAB: ROLES Y PERFILES ═══ -->
      @if (tab() === 'roles') {
        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5">
          @for (r of rolCards(); track r.nombre) {
            <div class="bg-navy-dark rounded-xl border border-navy-mid p-6 hover:border-cyan-brand/40 transition">
              <div class="flex items-start justify-between mb-4">
                <div [class]="'w-12 h-12 rounded-lg flex items-center justify-center ' + r.bgClass">
                  <svg [class]="'h-6 w-6 ' + r.colorClass" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/></svg>
                </div>
                <span [class]="'text-xs font-accent uppercase tracking-wider px-2 py-1 rounded-full ' + r.colorClass + ' ' + r.bgClass">{{ r.usuarios }} usuario{{ r.usuarios === 1 ? '' : 's' }}</span>
              </div>
              <h3 class="font-heading text-xl text-white mb-2">{{ r.nombre }}</h3>
              <p class="text-sm text-text-muted leading-relaxed mb-4">{{ r.descripcion }}</p>
              <button (click)="showToast('Edición de rol no disponible todavía')"
                      class="w-full px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-cyan-brand font-accent uppercase tracking-wider text-sm rounded-md transition">
                Editar Rol
              </button>
            </div>
          }
        </div>
      }

      <!-- ═══ TAB: PERMISOS ═══ -->
      @if (tab() === 'permisos') {
        <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
          <div class="flex items-start justify-between mb-6 flex-col sm:flex-row gap-3">
            <div>
              <h2 class="font-heading text-xl text-white mb-1">Matriz de Permisos</h2>
              <p class="text-sm text-text-muted">Accesos por rol y módulo del sistema (definidos en el backend).</p>
            </div>
            <div class="flex items-center gap-4 text-xs">
              <span class="flex items-center gap-1.5"><svg class="h-4 w-4 text-success-brand" fill="currentColor" viewBox="0 0 20 20"><path d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"/></svg><span class="text-text-muted">Acceso total</span></span>
              <span class="flex items-center gap-1.5"><span class="h-3 w-3 rounded-full bg-cyan-brand/40 border border-cyan-brand"></span><span class="text-text-muted">Solo lectura</span></span>
              <span class="flex items-center gap-1.5"><span class="text-text-muted text-lg leading-none">—</span><span class="text-text-muted">Sin acceso</span></span>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="border-b border-navy-mid">
                <tr>
                  <th class="px-4 py-3 text-left font-accent uppercase text-xs tracking-wider text-text-muted sticky left-0 bg-navy-dark">Rol</th>
                  @for (mod of modulos(); track mod) {
                    <th class="px-4 py-3 text-center font-accent uppercase text-xs tracking-wider text-text-muted">{{ mod }}</th>
                  }
                </tr>
              </thead>
              <tbody class="divide-y divide-navy-mid/50">
                @for (fila of matrizPermisos(); track fila.rol) {
                  <tr class="hover:bg-cyan-brand/5 transition">
                    <td class="px-4 py-4 sticky left-0 bg-navy-dark"><span [class]="rolClass(fila.rol)">{{ fila.rol }}</span></td>
                    @for (nivel of fila.niveles; track $index) {
                      <td class="px-4 py-4 text-center">
                        @if (nivel === 'full') {
                          <svg class="h-5 w-5 text-success-brand inline-block" fill="currentColor" viewBox="0 0 20 20"><path d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"/></svg>
                        } @else if (nivel === 'read-only') {
                          <span class="inline-block h-4 w-4 rounded-full bg-cyan-brand/40 border-2 border-cyan-brand"></span>
                        } @else {
                          <span class="text-text-muted text-lg leading-none">—</span>
                        }
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
      }
    </div>

    <!-- Modal Crear/Editar Usuario -->
    @if (modalOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="closeModal()">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-navy-mid">
            <h3 class="font-heading text-xl text-white">{{ editingUser() ? 'Editar Usuario' : 'Nuevo Usuario' }}</h3>
            <button (click)="closeModal()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label [class]="LABEL_INPUT">Nombre</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="formNombre" placeholder="María" [readonly]="!!editingUser()" [class.opacity-50]="!!editingUser()">
              </div>
              <div>
                <label [class]="LABEL_INPUT">Apellido</label>
                <input type="text" [class]="CLASS_INPUT" [(ngModel)]="formApellido" placeholder="González" [readonly]="!!editingUser()" [class.opacity-50]="!!editingUser()">
              </div>
            </div>
            <div>
              <label [class]="LABEL_INPUT">Email</label>
              <input type="email" [class]="CLASS_INPUT" [(ngModel)]="formEmail" placeholder="usuario@alasglobaltour.com" [readonly]="!!editingUser()" [class.opacity-50]="!!editingUser()">
            </div>
            <div>
              <label [class]="LABEL_INPUT">Rol</label>
              <select [class]="CLASS_INPUT" [(ngModel)]="formRol">
                <option value="">Selecciona un rol</option>
                @for (r of roles; track r) { <option [value]="r">{{ r }}</option> }
              </select>
            </div>
            @if (editingUser()) {
              <div>
                <label [class]="LABEL_INPUT">Estado</label>
                <select [class]="CLASS_INPUT" [(ngModel)]="formEstado">
                  <option value="Activo">Activo</option>
                  <option value="Inactivo">Inactivo</option>
                </select>
              </div>
            }
            @if (!editingUser()) {
              <label class="flex items-center gap-3 cursor-pointer">
                <input type="checkbox" [(ngModel)]="sendInvite" class="h-4 w-4 rounded border-navy-mid bg-navy-dark text-cyan-brand focus:ring-cyan-brand">
                <span class="text-sm text-text-light">Enviar invitación por correo electrónico</span>
              </label>
            }
          </div>
          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button (click)="closeModal()" class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition">Cancelar</button>
            <button (click)="saveUser()" [disabled]="saving()" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ saving() ? 'Guardando...' : (editingUser() ? 'Guardar cambios' : 'Crear Usuario') }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Modal Cambiar Contraseña -->
    @if (passwordTarget()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4" style="background:rgba(0,35,89,0.8)" (click)="closePassword()">
        <div class="bg-navy-dark border border-navy-mid rounded-2xl w-full max-w-sm max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-navy-mid">
            <h3 class="font-heading text-xl text-white">Cambiar contraseña</h3>
            <button (click)="closePassword()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
          <div class="p-6 space-y-4">
            <p class="text-sm text-text-muted">{{ passwordTarget()!.nombreCompleto }}</p>
            <div>
              <label [class]="LABEL_INPUT">Nueva contraseña</label>
              <input type="password" [class]="CLASS_INPUT" [(ngModel)]="newPasswordValue" autocomplete="new-password">
              <p class="text-[11px] text-text-muted mt-1">Mínimo 8 caracteres, 1 mayúscula y 1 dígito.</p>
            </div>
            @if (passwordError()) {
              <p class="text-error-brand text-xs">{{ passwordError() }}</p>
            }
          </div>
          <div class="px-6 py-4 border-t border-navy-mid flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button (click)="closePassword()" class="px-4 py-2 border border-navy-mid hover:border-cyan-brand text-text-muted hover:text-text-light font-accent uppercase tracking-wider text-sm rounded-md transition">Cancelar</button>
            <button (click)="confirmPasswordChange()" [disabled]="savingPassword()" class="px-4 py-2 bg-cyan-brand hover:bg-cyan-dark text-navy-deepest font-accent uppercase tracking-wider text-sm rounded-md transition disabled:opacity-50">
              {{ savingPassword() ? 'Guardando...' : 'Guardar contraseña' }}
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
export class UsuariosComponent implements OnInit {
  private api = inject(ApiService);
  private permissions = inject(PermissionsService);

  canEdit = computed(() => this.permissions.canEdit('Usuarios'));

  readonly LABEL_INPUT = LABEL_INPUT;
  readonly CLASS_INPUT = CLASS_INPUT;

  tab = signal<UsuariosTab>('usuarios');
  tabClass(t: UsuariosTab): string {
    return this.tab() === t
      ? 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-cyan-brand border-b-2 border-cyan-brand whitespace-nowrap'
      : 'px-4 py-3 font-accent uppercase tracking-wider text-sm text-text-muted border-b-2 border-transparent hover:text-text-light transition whitespace-nowrap';
  }

  roles: RolUsuario[] = ['Super Admin', 'Admin', 'Árbitro', 'Revisor'];

  loading = signal(true);
  saving = signal(false);

  searchTerm = signal('');
  filterRol = signal('');
  filterEstado = signal('');

  usuarios = signal<UsuarioAdmin[]>([]);
  apiRoles = signal<ApiRole[]>([]);

  modulos = computed(() => {
    const roles = this.apiRoles();
    return roles.length ? roles[0].permissions.map(p => p.module) : [];
  });

  filteredUsuarios = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const rol = this.filterRol();
    const estado = this.filterEstado();
    return this.usuarios().filter(u => {
      if (term && !u.nombreCompleto.toLowerCase().includes(term) && !u.email.toLowerCase().includes(term)) return false;
      if (rol && u.rol !== rol) return false;
      if (estado && u.estado !== estado) return false;
      return true;
    });
  });

  rolCards = computed(() => {
    const conteo = new Map<RolUsuario, number>();
    for (const u of this.usuarios()) conteo.set(u.rol, (conteo.get(u.rol) ?? 0) + 1);
    return this.apiRoles().map(r => ({
      nombre: r.name,
      descripcion: ROL_DESCRIPCIONES[r.name] ?? '',
      usuarios: conteo.get(r.name) ?? 0,
      ...ROL_ESTILOS[r.name],
    }));
  });

  matrizPermisos = computed(() =>
    this.apiRoles().map(r => ({ rol: r.name, niveles: r.permissions.map(p => p.level) }))
  );

  rolClass(rol: RolUsuario): string {
    const map: Record<RolUsuario, string> = {
      'Super Admin': 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-purple-500/15 text-purple-400 text-xs font-accent uppercase tracking-wider',
      'Admin': 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-cyan-brand/15 text-cyan-brand text-xs font-accent uppercase tracking-wider',
      'Árbitro': 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-orange-brand/15 text-orange-brand text-xs font-accent uppercase tracking-wider',
      'Revisor': 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-navy-mid/50 text-text-muted text-xs font-accent uppercase tracking-wider',
    };
    return map[rol];
  }

  estadoClass(estado: UsuarioAdmin['estado']): string {
    switch (estado) {
      case 'Activo':
        return 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-brand/15 text-success-brand text-xs font-accent uppercase tracking-wider';
      case 'Bloqueado':
        return 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-brand/15 text-warning-brand text-xs font-accent uppercase tracking-wider';
      default:
        return 'inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-error-brand/15 text-error-brand text-xs font-accent uppercase tracking-wider';
    }
  }

  toast = signal<{ show: boolean; message: string }>({ show: false, message: '' });
  showToast(message: string): void {
    this.toast.set({ show: true, message });
    setTimeout(() => this.toast.set({ show: false, message: '' }), 3000);
  }

  modalOpen = signal(false);
  editingUser = signal<UsuarioAdmin | null>(null);
  formNombre = '';
  formApellido = '';
  formEmail = '';
  formRol = '';
  formEstado: 'Activo' | 'Inactivo' = 'Activo';
  sendInvite = true;

  async ngOnInit(): Promise<void> {
    await this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      const [usersRes, rolesRes] = await Promise.all([
        this.api.get<any>('/admin/users'),
        this.api.get<any>('/admin/roles'),
      ]);
      const usersData: any[] = usersRes?.data ?? [];
      this.usuarios.set(usersData.map(u => this.mapUser(u)));
      this.apiRoles.set((rolesRes?.data ?? []) as ApiRole[]);
    } catch {
      this.showToast('Error al cargar usuarios');
    } finally {
      this.loading.set(false);
    }
  }

  private mapUser(u: any): UsuarioAdmin {
    const isLocked = Boolean(u.isLocked);
    const lockedUntil = u.lockedUntilUtc ? new Date(u.lockedUntilUtc) : null;

    return {
      id: u.id,
      iniciales: u.initials,
      gradient: gradientFor(u.id),
      nombreCompleto: u.fullName,
      email: u.email,
      rol: u.role,
      ultimaSesion: u.lastSession ? new Date(u.lastSession).toLocaleString('es', { dateStyle: 'medium', timeStyle: 'short' }) : 'Nunca',
      estado: isLocked ? 'Bloqueado' : u.status,
      bloqueadoHasta: lockedUntil ? lockedUntil.toLocaleString('es', { dateStyle: 'medium', timeStyle: 'short' }) : null,
    };
  }

  openCreate(): void {
    this.editingUser.set(null);
    this.formNombre = '';
    this.formApellido = '';
    this.formEmail = '';
    this.formRol = '';
    this.sendInvite = true;
    this.modalOpen.set(true);
  }

  openEdit(u: UsuarioAdmin): void {
    this.editingUser.set(u);
    const [nombre, ...resto] = u.nombreCompleto.split(' ');
    this.formNombre = nombre;
    this.formApellido = resto.join(' ');
    this.formEmail = u.email;
    this.formRol = u.rol;
    this.formEstado = u.estado === 'Inactivo' ? 'Inactivo' : 'Activo';
    this.modalOpen.set(true);
  }

  closeModal(): void {
    this.modalOpen.set(false);
  }

  async saveUser(): Promise<void> {
    if (!this.formRol) {
      this.showToast('Selecciona un rol');
      return;
    }
    this.saving.set(true);
    try {
      const editing = this.editingUser();
      if (editing) {
        const res = await this.api.put<any>(`/admin/users/${editing.id}`, {
          rol: this.formRol,
          status: this.formEstado,
        });
        this.usuarios.update(list => list.map(x => x.id === editing.id ? this.mapUser(res) : x));
        this.showToast('Usuario actualizado');
      } else {
        const res = await this.api.post<any>('/admin/users', {
          nombre: this.formNombre,
          apellido: this.formApellido,
          email: this.formEmail,
          rol: this.formRol,
          sendInvitationEmail: this.sendInvite,
        });
        this.usuarios.update(list => [...list, this.mapUser(res)]);
        this.showToast('Usuario creado');
      }
      this.modalOpen.set(false);
    } catch (e: any) {
      this.showToast(e?.body?.message || 'Error al guardar el usuario');
    } finally {
      this.saving.set(false);
    }
  }

  async confirmDeactivate(u: UsuarioAdmin): Promise<void> {
    try {
      const res = await this.api.put<any>(`/admin/users/${u.id}`, { rol: u.rol, status: 'Inactivo' });
      this.usuarios.update(list => list.map(x => x.id === u.id ? this.mapUser(res) : x));
      this.showToast(`Usuario ${u.nombreCompleto} desactivado`);
    } catch {
      this.showToast('Error al desactivar usuario');
    }
  }

  async activate(u: UsuarioAdmin): Promise<void> {
    try {
      const res = await this.api.put<any>(`/admin/users/${u.id}`, { rol: u.rol, status: 'Activo' });
      this.usuarios.update(list => list.map(x => x.id === u.id ? this.mapUser(res) : x));
      this.showToast('Usuario activado');
    } catch {
      this.showToast('Error al activar usuario');
    }
  }

  // ─── Cambiar contraseña ─────────────────────────────────────────

  passwordTarget = signal<UsuarioAdmin | null>(null);
  newPasswordValue = '';
  savingPassword = signal(false);
  passwordError = signal<string | null>(null);

  openPassword(u: UsuarioAdmin): void {
    this.passwordTarget.set(u);
    this.newPasswordValue = '';
    this.passwordError.set(null);
  }

  closePassword(): void {
    this.passwordTarget.set(null);
  }

  async confirmPasswordChange(): Promise<void> {
    const u = this.passwordTarget();
    if (!u) return;
    if (!/^(?=.*[A-Z])(?=.*\d).{8,}$/.test(this.newPasswordValue)) {
      this.passwordError.set('La contraseña debe tener al menos 8 caracteres, 1 mayúscula y 1 dígito.');
      return;
    }
    this.savingPassword.set(true);
    this.passwordError.set(null);
    try {
      await this.api.post(`/admin/users/${u.id}/password`, { newPassword: this.newPasswordValue });
      this.closePassword();
      this.showToast(`Contraseña actualizada para ${u.nombreCompleto}`);
    } catch (err: any) {
      this.passwordError.set(err?.body?.message ?? 'No se pudo actualizar la contraseña.');
    } finally {
      this.savingPassword.set(false);
    }
  }
}
