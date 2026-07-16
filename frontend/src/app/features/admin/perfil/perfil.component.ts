import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-perfil',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="py-8 max-w-lg">
      <div class="mb-6">
        <h1 class="text-3xl font-heading text-white">Mi Perfil</h1>
        <p class="text-text-muted text-sm mt-1">{{ auth.currentUser()?.fullName }} · {{ auth.currentUser()?.email }}</p>
      </div>

      <div class="bg-navy-dark rounded-xl border border-navy-mid p-6">
        <h2 class="font-heading text-xl text-white mb-1">Cambiar contraseña</h2>
        <p class="text-sm text-text-muted mb-5">Actualiza la contraseña de tu propia cuenta.</p>

        <div class="space-y-4">
          <div>
            <label class="block text-xs font-accent uppercase tracking-wider text-text-muted mb-1.5">Nueva contraseña</label>
            <input type="password" [(ngModel)]="newPasswordValue" autocomplete="new-password"
                   class="w-full bg-navy-mid/40 border border-navy-mid rounded-md px-3 py-2 text-sm text-text-light focus:outline-none focus:border-cyan-brand transition">
            <p class="text-[11px] text-text-muted mt-1">Mínimo 8 caracteres, 1 mayúscula y 1 dígito.</p>
          </div>

          @if (error()) {
            <p class="text-error-brand text-xs">{{ error() }}</p>
          }
          @if (success()) {
            <p class="text-success-brand text-xs">Contraseña actualizada correctamente.</p>
          }

          <div class="flex justify-end">
            <button (click)="save()" [disabled]="saving()"
                    class="px-5 py-2 rounded-md bg-cyan-brand text-navy-deepest font-accent uppercase tracking-wider text-sm hover:bg-cyan-dark transition disabled:opacity-50">
              {{ saving() ? 'Guardando...' : 'Guardar contraseña' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class AdminPerfilComponent {
  private api = inject(ApiService);
  auth = inject(AuthService);

  newPasswordValue = '';
  saving = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  async save(): Promise<void> {
    this.error.set(null);
    this.success.set(false);
    if (!/^(?=.*[A-Z])(?=.*\d).{8,}$/.test(this.newPasswordValue)) {
      this.error.set('La contraseña debe tener al menos 8 caracteres, 1 mayúscula y 1 dígito.');
      return;
    }
    this.saving.set(true);
    try {
      await this.api.post('/admin/users/me/password', { newPassword: this.newPasswordValue });
      this.newPasswordValue = '';
      this.success.set(true);
    } catch (err: any) {
      this.error.set(err?.body?.message ?? 'No se pudo actualizar la contraseña.');
    } finally {
      this.saving.set(false);
    }
  }
}
