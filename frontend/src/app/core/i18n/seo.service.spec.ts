import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { SeoService } from './seo.service';

describe('SeoService', () => {
  let seo: SeoService;
  let doc: Document;

  const hreflangs = () =>
    Array.from(doc.head.querySelectorAll('link[rel="alternate"][hreflang]')).map(
      l => `${l.getAttribute('hreflang')} ${new URL(l.getAttribute('href')!).pathname}`,
    );

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TranslocoTestingModule.forRoot({ langs: { es: {} } })],
    });
    seo = TestBed.inject(SeoService);
    doc = TestBed.inject(DOCUMENT);
  });

  afterEach(() => doc.head.querySelectorAll('link[rel="alternate"]').forEach(l => l.remove()));

  it('emits es/en/pt and x-default for the same page', () => {
    seo.updateAlternateLinks('/en/eventos');
    expect(hreflangs()).toEqual([
      'es /eventos',
      'en /en/eventos',
      'pt /pt/eventos',
      'x-default /eventos',
    ]);
  });

  it('replaces previous links instead of accumulating', () => {
    seo.updateAlternateLinks('/ranking');
    seo.updateAlternateLinks('/eventos');
    expect(hreflangs().length).toBe(4);
  });

  it('skips article and gallery detail pages (slugs differ per language)', () => {
    seo.updateAlternateLinks('/ranking');
    seo.updateAlternateLinks('/en/noticias/final-day');
    expect(hreflangs()).toEqual([]);
    seo.updateAlternateLinks('/galerias/roca-bruja');
    expect(hreflangs()).toEqual([]);
  });

  it('advertises only the translations WordPress reported for a detail page', () => {
    seo.updateAlternateLinks('/en/noticias/final-stretch');
    seo.setContentAlternates('/en/noticias/final-stretch', {
      es: '/noticias/recta-final',
      en: '/noticias/final-stretch',
    });
    expect(hreflangs()).toEqual([
      'es /noticias/recta-final',
      'en /en/noticias/final-stretch',
      'x-default /noticias/recta-final',
    ]);
    expect(seo.contentAlternateFor('/en/noticias/final-stretch', 'es')).toBe('/noticias/recta-final');
    expect(seo.contentAlternateFor('/en/noticias/final-stretch', 'pt')).toBeNull();

    // Navigating to another article drops the previous article's alternates.
    seo.updateAlternateLinks('/noticias/otra-nota');
    expect(hreflangs()).toEqual([]);
    expect(seo.contentAlternateFor('/noticias/otra-nota', 'en')).toBeNull();
  });

  it('skips admin pages', () => {
    seo.updateAlternateLinks('/admin/pagos');
    expect(hreflangs()).toEqual([]);
  });
});
