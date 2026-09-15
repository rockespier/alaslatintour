import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminSidebarComponent } from '../../shared/components/admin-sidebar/admin-sidebar.component';
import { AdminThemeService } from '../../core/services/admin-theme.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, AdminSidebarComponent],
  template: `
    <app-admin-sidebar />
    <div class="lg:ml-64 min-h-screen bg-navy-deepest" [attr.data-theme]="theme.theme()">
      <main class="p-6 lg:p-8">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminLayoutComponent {
  theme = inject(AdminThemeService);
}
