import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink],
  template: `
    <footer class="bg-[#001a40] border-t border-white/10 mt-16">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8">

          <!-- Brand -->
          <div class="md:col-span-2">
            <img src="/assets/logos/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-10 w-auto mb-4" />
            <p class="text-sm text-[#AAAAAA] max-w-xs leading-relaxed">
              Asociación Latinoamericana de Surfistas Profesionales. El circuito de surf profesional de Latinoamérica.
            </p>
            <p class="text-xs text-[#AAAAAA] mt-4">
              Results by <a href="https://surfscores.com" target="_blank" rel="noopener"
                class="text-[#0081C6] hover:underline">SurfScores.com</a>
            </p>
          </div>

          <!-- Links -->
          <div>
            <h4 class="text-sm font-semibold text-[#EEEEEE] mb-4 uppercase tracking-wider">Plataforma</h4>
            <ul class="space-y-2">
              <li><a routerLink="/eventos" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Eventos</a></li>
              <li><a routerLink="/ranking" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Ranking</a></li>
              <li><a routerLink="/noticias" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Noticias</a></li>
              <li><a routerLink="/quienes-somos" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Quiénes Somos</a></li>
            </ul>
          </div>

          <!-- Social -->
          <div>
            <h4 class="text-sm font-semibold text-[#EEEEEE] mb-4 uppercase tracking-wider">Redes Sociales</h4>
            <ul class="space-y-2">
              <li><a href="#" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Instagram</a></li>
              <li><a href="#" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">Facebook</a></li>
              <li><a href="#" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">YouTube</a></li>
            </ul>
          </div>
        </div>

        <div class="border-t border-white/10 mt-8 pt-6 flex flex-col sm:flex-row items-center justify-between gap-2">
          <p class="text-xs text-[#AAAAAA]">© {{ year }} ALAS Latin Tour. Todos los derechos reservados.</p>
          <p class="text-xs text-[#AAAAAA]">
            Results by <a href="https://surfscores.com" target="_blank" rel="noopener"
              class="text-[#0081C6] hover:underline">SurfScores.com</a>
          </p>
        </div>
      </div>
    </footer>
  `,
})
export class FooterComponent {
  year = new Date().getFullYear();
}
