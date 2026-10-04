import rootEs from '../../../i18n/es.json';
import rootEn from '../../../i18n/en.json';
import rootPt from '../../../i18n/pt.json';
import publicEs from '../../../i18n/public/es.json';
import publicEn from '../../../i18n/public/en.json';
import publicPt from '../../../i18n/public/pt.json';
import aboutEs from '../../../i18n/quienes-somos/es.json';
import aboutEn from '../../../i18n/quienes-somos/en.json';
import aboutPt from '../../../i18n/quienes-somos/pt.json';
import authEs from '../../../i18n/auth/es.json';
import authEn from '../../../i18n/auth/en.json';
import authPt from '../../../i18n/auth/pt.json';
import competitorEs from '../../../i18n/competitor/es.json';
import competitorEn from '../../../i18n/competitor/en.json';
import competitorPt from '../../../i18n/competitor/pt.json';

type Json = Record<string, unknown>;

function flatKeys(obj: Json, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([key, value]) =>
    value && typeof value === 'object' ? flatKeys(value as Json, `${prefix}${key}.`) : [`${prefix}${key}`],
  );
}

function params(text: unknown): string[] {
  return typeof text === 'string' ? (text.match(/\{\{\s*\w+\s*\}\}/g) ?? []).map(p => p.replace(/\s/g, '')).sort() : [];
}

function valueAt(obj: Json, path: string): unknown {
  return path.split('.').reduce<unknown>((acc, k) => (acc as Json | undefined)?.[k], obj);
}

// Runtime fallback to Spanish is disabled (see i18n.providers.ts), so every
// language must define exactly the same keys and interpolation params.
const FILES: Record<string, { es: Json; en: Json; pt: Json }> = {
  root: { es: rootEs, en: rootEn, pt: rootPt },
  public: { es: publicEs, en: publicEn, pt: publicPt },
  'quienes-somos': { es: aboutEs, en: aboutEn, pt: aboutPt },
  auth: { es: authEs, en: authEn, pt: authPt },
  competitor: { es: competitorEs, en: competitorEn, pt: competitorPt },
};

describe('translation files', () => {
  for (const [name, { es, en, pt }] of Object.entries(FILES)) {
    describe(name, () => {
      const esKeys = flatKeys(es).sort();

      for (const [lang, file] of Object.entries({ en, pt })) {
        it(`${lang} has the same keys as es`, () => {
          expect(flatKeys(file).sort()).toEqual(esKeys);
        });

        it(`${lang} keeps the same interpolation params as es`, () => {
          const mismatched = esKeys.filter(k => params(valueAt(es, k)).join() !== params(valueAt(file, k)).join());
          expect(mismatched).toEqual([]);
        });
      }

      it('has no empty values', () => {
        const empty = Object.entries({ es, en, pt }).flatMap(([lang, file]) =>
          flatKeys(file).filter(k => valueAt(file, k) === '').map(k => `${lang}:${k}`),
        );
        expect(empty).toEqual([]);
      });
    });
  }
});
