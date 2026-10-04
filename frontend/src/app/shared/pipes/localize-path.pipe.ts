import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';

/** `[routerLink]="'/noticias' | localizePath"` → `/en/noticias` when English is active. */
@Pipe({ name: 'localizePath', standalone: true, pure: false })
export class LocalizePathPipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(path: string): string {
    return this.language.localize(path);
  }
}
