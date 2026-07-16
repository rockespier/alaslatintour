import { Injectable, inject, computed } from '@angular/core';
import { AuthService } from './auth.service';

export type AdminModule =
  | 'Dashboard'
  | 'Usuarios'
  | 'Circuitos'
  | 'Eventos'
  | 'Categorias'
  | 'Inscripciones'
  | 'Pagos'
  | 'Tokens'
  | 'Configuracion';

export type PermissionLevel = 'full' | 'read' | 'none';

type AdminRole = 'Super Admin' | 'Admin' | 'Árbitro' | 'Revisor';

// Matriz funcional documentada en api-postman.md ("Permisos por rol y rutas protegidas").
const ROLE_MATRIX: Record<AdminRole, Record<AdminModule, PermissionLevel>> = {
  'Super Admin': {
    Dashboard: 'full', Usuarios: 'full', Circuitos: 'full', Eventos: 'full',
    Categorias: 'full', Inscripciones: 'full', Pagos: 'full', Tokens: 'full', Configuracion: 'full',
  },
  Admin: {
    Dashboard: 'full', Usuarios: 'full', Circuitos: 'full', Eventos: 'full',
    Categorias: 'full', Inscripciones: 'full', Pagos: 'full', Tokens: 'full', Configuracion: 'read',
  },
  Árbitro: {
    Dashboard: 'read', Usuarios: 'none', Circuitos: 'none', Eventos: 'full',
    Categorias: 'read', Inscripciones: 'full', Pagos: 'none', Tokens: 'none', Configuracion: 'none',
  },
  Revisor: {
    Dashboard: 'read', Usuarios: 'read', Circuitos: 'read', Eventos: 'read',
    Categorias: 'read', Inscripciones: 'read', Pagos: 'read', Tokens: 'read', Configuracion: 'none',
  },
};

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private auth = inject(AuthService);

  private role = computed(() => this.auth.currentUser()?.adminRole as AdminRole | undefined);

  levelOf(module: AdminModule): PermissionLevel {
    const role = this.role();
    if (!role) return 'none';
    return ROLE_MATRIX[role]?.[module] ?? 'none';
  }

  canView(module: AdminModule): boolean {
    return this.levelOf(module) !== 'none';
  }

  canEdit(module: AdminModule): boolean {
    return this.levelOf(module) === 'full';
  }
}
