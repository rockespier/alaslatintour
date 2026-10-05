import { detectPreferredLang, langForCountry, langFromAcceptLanguage, readLangCookie } from './lang-detection';

describe('lang-detection', () => {
  it('maps countries to languages', () => {
    expect(langForCountry('BR')).toBe('pt');
    expect(langForCountry('pt')).toBe('pt');
    expect(langForCountry('PE')).toBe('es');
    expect(langForCountry('ES')).toBe('es');
    expect(langForCountry('US')).toBe('en');
    expect(langForCountry('FR')).toBe('en');
  });

  it('reads the country/language from Accept-Language', () => {
    expect(langFromAcceptLanguage('pt-BR,pt;q=0.9')).toBe('pt');
    expect(langFromAcceptLanguage('es-MX,es;q=0.9')).toBe('es');
    expect(langFromAcceptLanguage('en-US')).toBe('en');
    expect(langFromAcceptLanguage('de-DE,de;q=0.9')).toBe('en');
    expect(langFromAcceptLanguage('es')).toBe('es');
    expect(langFromAcceptLanguage('fr')).toBe('en');
    expect(langFromAcceptLanguage('')).toBeNull();
    expect(langFromAcceptLanguage(undefined)).toBeNull();
  });

  it('reads the language cookie', () => {
    expect(readLangCookie('a=1; alas_lang=pt; b=2')).toBe('pt');
    expect(readLangCookie('alas_lang=xx')).toBeNull();
    expect(readLangCookie(undefined)).toBeNull();
  });

  it('prefers cookie, then geo header, then Accept-Language', () => {
    expect(detectPreferredLang({ cookie: 'alas_lang=es', country: 'BR' })).toBe('es');
    expect(detectPreferredLang({ country: 'BR', acceptLanguage: 'en-US' })).toBe('pt');
    expect(detectPreferredLang({ country: 'XX', acceptLanguage: 'en-US' })).toBe('en');
    expect(detectPreferredLang({ acceptLanguage: 'pt-BR' })).toBe('pt');
    expect(detectPreferredLang({})).toBe('es');
  });

  it('never redirects crawlers', () => {
    expect(detectPreferredLang({ country: 'US', userAgent: 'Mozilla/5.0 (compatible; Googlebot/2.1)' })).toBeNull();
  });
});
