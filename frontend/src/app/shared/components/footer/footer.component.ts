import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LocalizePathPipe } from '../../pipes/localize-path.pipe';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';
import { SiteSettingsService } from '../../../core/services/site-settings.service';

interface SocialLink {
  name: string;
  url: string;
}

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, TranslocoModule, LocalizePathPipe, LanguageSwitcherComponent],
  template: `
    <footer class="bg-[#001a40] border-t border-white/10 mt-16">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-8">

          <!-- Brand -->
          <div class="md:col-span-2">
            <img src="assets/images/brand/logo-pro-tour-white-2x.png" alt="ALAS Latin Tour" class="h-14 w-auto mb-4" />
            <p class="text-sm text-[#AAAAAA] max-w-xs leading-relaxed">
              {{ 'footer.tagline' | transloco }}
            </p>
            <div class="mt-4"><app-language-switcher /></div>
          </div>

          <!-- Links -->
          <div>
            <h4 class="text-sm font-semibold text-[#EEEEEE] mb-4 uppercase tracking-wider">{{ 'footer.platform' | transloco }}</h4>
            <ul class="space-y-2">
              <li><a [routerLink]="'/eventos' | localizePath" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">{{ 'nav.events' | transloco }}</a></li>
              <li><a [routerLink]="'/ranking' | localizePath" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">{{ 'nav.ranking' | transloco }}</a></li>
              <li><a [routerLink]="'/noticias' | localizePath" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">{{ 'nav.news' | transloco }}</a></li>
              <li><a [routerLink]="'/quienes-somos' | localizePath" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">{{ 'nav.about' | transloco }}</a></li>
            </ul>
          </div>

          <!-- Social -->
          @if (socialLinks().length) {
            <div>
              <h4 class="text-sm font-semibold text-[#EEEEEE] mb-4 uppercase tracking-wider">{{ 'footer.social' | transloco }}</h4>
              <ul class="space-y-2">
                @for (link of socialLinks(); track link.name) {
                  <li><a [href]="link.url" target="_blank" rel="noopener" class="text-sm text-[#AAAAAA] hover:text-[#0081C6]">{{ link.name }}</a></li>
                }
              </ul>
            </div>
          }
        </div>

        <div class="border-t border-white/10 mt-8 pt-6 flex flex-col sm:flex-row items-center justify-between gap-2">
          <p class="text-xs text-[#AAAAAA]">{{ 'footer.copyright' | transloco: { year } }}</p>
          <p class="text-xs text-[#AAAAAA]">
            Results by <a href="https://surfscores.com" target="_blank" rel="noopener"
              class="text-[#0081C6] hover:underline">SurfScores.com</a>
          </p>
        </div>
      </div>
    </footer>
  `,
})
export class FooterComponent implements OnInit {
  private siteSettings = inject(SiteSettingsService);

  year = new Date().getFullYear();
  socialLinks = signal<SocialLink[]>([]);

  async ngOnInit(): Promise<void> {
    try {
      const social = await this.siteSettings.getSocialLinks();
      const links: SocialLink[] = [];
      if (social.instagram) links.push({ name: 'Instagram', url: this.buildSocialUrl('instagram', social.instagram) });
      if (social.facebook) links.push({ name: 'Facebook', url: this.buildSocialUrl('facebook', social.facebook) });
      if (social.x) links.push({ name: 'X (Twitter)', url: this.buildSocialUrl('x', social.x) });
      if (social.youTube) links.push({ name: 'YouTube', url: this.buildSocialUrl('youtube', social.youTube) });
      this.socialLinks.set(links);
    } catch {
      this.socialLinks.set([]);
    }
  }

  private buildSocialUrl(platform: 'instagram' | 'facebook' | 'x' | 'youtube', value: string): string {
    const trimmed = value.trim();
    if (!trimmed) return '';
    if (/^https?:\/\//i.test(trimmed)) return trimmed;
    if (trimmed.startsWith('@')) {
      const host = platform === 'x' ? 'x.com' : `${platform}.com`;
      return `https://${host}/${trimmed.slice(1)}`;
    }
    return `https://${trimmed}`;
  }
}
