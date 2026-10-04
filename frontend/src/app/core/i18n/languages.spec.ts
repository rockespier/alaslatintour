import { langFromPath, localizeUrl, stripLangPrefix, translatedContentFallback, translationPaths } from './languages';

describe('languages', () => {
  it('reads the language from the first segment, defaulting to es', () => {
    expect(langFromPath('/en/noticias')).toBe('en');
    expect(langFromPath('/pt')).toBe('pt');
    expect(langFromPath('/noticias')).toBe('es');
    expect(langFromPath('/english')).toBe('es');
    expect(langFromPath('/xx/noticias')).toBe('es');
  });

  it('strips the prefix keeping path, query and fragment', () => {
    expect(stripLangPrefix('/en/noticias/slug?x=1#top')).toBe('/noticias/slug?x=1#top');
    expect(stripLangPrefix('/pt')).toBe('/');
    expect(stripLangPrefix('/en?x=1')).toBe('/?x=1');
    expect(stripLangPrefix('/english')).toBe('/english');
  });

  it('localizes URLs and preserves params, query and fragment', () => {
    expect(localizeUrl('/noticias/mi-slug?page=2#c', 'en')).toBe('/en/noticias/mi-slug?page=2#c');
    expect(localizeUrl('/en/noticias/mi-slug', 'pt')).toBe('/pt/noticias/mi-slug');
    expect(localizeUrl('/pt/noticias', 'es')).toBe('/noticias');
    expect(localizeUrl('/', 'en')).toBe('/en');
    expect(localizeUrl('/en', 'es')).toBe('/');
    expect(localizeUrl('/?x=1', 'pt')).toBe('/pt?x=1');
  });

  it('sends article/gallery detail pages to the news listing when switching language', () => {
    expect(translatedContentFallback('/en/noticias/final-day?x=1')).toBe('/noticias');
    expect(translatedContentFallback('/galerias/roca-bruja')).toBe('/noticias');
    expect(translatedContentFallback('/noticias')).toBeNull();
    expect(translatedContentFallback('/pt/eventos')).toBeNull();
  });

  it('builds per-language paths from Polylang slugs, ignoring unknown languages', () => {
    expect(translationPaths('/noticias', { es: 'recta-final', en: 'final-stretch', fr: 'x' })).toEqual({
      es: '/noticias/recta-final',
      en: '/noticias/final-stretch',
    });
    expect(translationPaths('/galerias', undefined)).toEqual({});
  });
});
