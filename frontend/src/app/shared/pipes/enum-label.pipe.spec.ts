import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LanguageService } from '../../core/i18n/language.service';
import { EnumLabelPipe } from './enum-label.pipe';

describe('EnumLabelPipe', () => {
  let pipe: EnumLabelPipe;
  let language: LanguageService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        TranslocoTestingModule.forRoot({
          langs: {
            es: { enums: { circuitRegion: { 'América del Sur': 'América del Sur' } } },
            en: { enums: { circuitRegion: { 'América del Sur': 'South America' } } },
          },
          translocoConfig: { availableLangs: ['es', 'en'], defaultLang: 'es' },
          preloadLangs: true,
        }),
      ],
      providers: [EnumLabelPipe],
    });
    pipe = TestBed.inject(EnumLabelPipe);
    language = TestBed.inject(LanguageService);
  });

  it('translates wire values with spaces and accents', () => {
    language.setActiveLang('en');
    expect(pipe.transform('América del Sur', 'circuitRegion')).toBe('South America');
  });

  it('falls back to the raw value when the key is missing', () => {
    language.setActiveLang('en');
    expect(pipe.transform('Antártida', 'circuitRegion')).toBe('Antártida');
    expect(pipe.transform('Activo', 'unknownTable')).toBe('Activo');
  });

  it('returns an empty string for empty values', () => {
    expect(pipe.transform(null, 'circuitRegion')).toBe('');
  });
});
