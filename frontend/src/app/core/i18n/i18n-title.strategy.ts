import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { take } from 'rxjs/operators';

/** Route titles starting with `titles.` are Transloco keys; anything else (admin) is literal. */
@Injectable({ providedIn: 'root' })
export class I18nTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly transloco = inject(TranslocoService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const value = this.buildTitle(snapshot);
    if (!value) return;
    if (!value.startsWith('titles.')) {
      this.title.setTitle(value);
      return;
    }
    this.transloco
      .selectTranslate(value)
      .pipe(take(1))
      .subscribe(text => this.title.setTitle(text));
  }
}
