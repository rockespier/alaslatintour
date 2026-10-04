import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { LanguageService } from '../../core/i18n/language.service';

/**
 * Translates a backend enum value: `{{ circuit.status | enumLabel:'circuitStatus' }}`.
 * Keys in `enums.<table>` are the literal wire values (spaces/accents included).
 * Falls back to the raw value when the table lacks it, so a new backend value never breaks the UI.
 */
@Pipe({ name: 'enumLabel', standalone: true, pure: false })
export class EnumLabelPipe implements PipeTransform {
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  transform(value: string | null | undefined, table: string): string {
    if (value === null || value === undefined || value === '') return '';
    // Transloco stores root translations flattened (`enums.table.value`).
    const label = this.transloco.getTranslation(this.language.activeLang())[`enums.${table}.${value}`];
    return typeof label === 'string' && label ? label : value;
  }
}
