import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface SocialLinks {
  instagram: string;
  facebook: string;
  x: string;
  youTube: string;
}

@Injectable({ providedIn: 'root' })
export class SiteSettingsService {
  private api = inject(ApiService);

  async getSocialLinks(): Promise<SocialLinks> {
    const res = await this.api.get<any>('/site-settings');
    const social = res?.socialLinks ?? {};
    return {
      instagram: social.instagram ?? '',
      facebook: social.facebook ?? '',
      x: social.x ?? '',
      youTube: social.youTube ?? '',
    };
  }
}
