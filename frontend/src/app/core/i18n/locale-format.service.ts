import { formatDate, formatNumber } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { LanguageService } from './language.service';
import { LOCALE_BY_LANG } from './languages';

/**
 * Date/number formatting that follows the active language on every call.
 * LOCALE_ID is resolved once per injector, so it can't follow a runtime switch.
 */
@Injectable({ providedIn: 'root' })
export class LocaleFormatService {
  private readonly language = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  get locale(): string {
    return LOCALE_BY_LANG[this.language.activeLang()];
  }

  date(value: string | number | Date | null | undefined, format = 'mediumDate', timezone?: string): string {
    if (value === null || value === undefined || value === '') return '';
    return formatDate(value, format, this.locale, timezone);
  }

  number(value: number | null | undefined, digitsInfo?: string): string {
    if (value === null || value === undefined) return '';
    return formatNumber(value, this.locale, digitsInfo);
  }

  /**
   * "Del 3 al 7 de mar" / "Mar 3 – 7". Event dates are date-only values stored at UTC
   * midnight, so they're read in UTC to keep the day independent of the browser's timezone.
   */
  dateRange(start: string | null | undefined, end: string | null | undefined): string {
    if (!start) return '';
    const s = new Date(start);
    const e = new Date(end || start);
    const month = formatDate(s, 'MMM', this.locale, 'UTC').replace(/\.$/, '');
    return this.transloco.translate('common.dateRange', {
      start: s.getUTCDate(),
      end: e.getUTCDate(),
      month,
    });
  }

  /** Date-only values (UTC midnight) formatted without timezone drift. */
  utcDate(value: string | null | undefined, format = 'mediumDate'): string {
    return this.date(value, format, 'UTC');
  }

  /**
   * The organization operates in USD in every language: always `$` as prefix (as the UI
   * showed before), only the thousands/decimal separators follow the language.
   */
  usd(value: number | null | undefined, digitsInfo = '1.0-2'): string {
    if (value === null || value === undefined) return '';
    return '$' + formatNumber(value, this.locale, digitsInfo);
  }
}
