import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminSidebarComponent } from '../../shared/components/admin-sidebar/admin-sidebar.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, AdminSidebarComponent],
  template: `
    <app-admin-sidebar />
    <div class="lg:ml-64 min-h-screen bg-[#002359]">
      <main class="p-6 lg:p-8">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminLayoutComponent {}
