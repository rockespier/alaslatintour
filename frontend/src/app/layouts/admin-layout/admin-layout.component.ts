import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminSidebarComponent } from '../../shared/components/admin-sidebar/admin-sidebar.component';
import { AdminThemeService } from '../../core/services/admin-theme.service';
import { AdminSidebarService } from '../../core/services/admin-sidebar.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, AdminSidebarComponent],
  template: `
    <app-admin-sidebar />
    <div [class]="sidebar.collapsed() ? 'min-h-screen bg-navy-deepest lg:ml-20' : 'min-h-screen bg-navy-deepest lg:ml-64'" [attr.data-theme]="theme.theme()">
      <main class="p-6 lg:p-8">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminLayoutComponent {
  theme = inject(AdminThemeService);
  sidebar = inject(AdminSidebarService);
}
