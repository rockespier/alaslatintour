import { AngularNodeAppEngine, createNodeRequestHandler, isMainModule, writeResponseToNodeResponse } from '@angular/ssr/node';
import express from 'express';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { detectPreferredLang } from './app/core/i18n/lang-detection';
import { DEFAULT_LANG, langFromPath, localizeUrl } from './app/core/i18n/languages';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * First visit to a Spanish (unprefixed) page: send the visitor to their country's language
 * (geo header from the CDN/proxy if present, otherwise Accept-Language). The switcher's cookie
 * overrides it, and crawlers are never redirected.
 */
app.use((req, res, next) => {
  if ((req.method !== 'GET' && req.method !== 'HEAD') || /\.[a-z0-9]+$/i.test(req.path) || /^\/(api|v1)(\/|$)/.test(req.path)) {
    return next();
  }
  if (langFromPath(req.path) !== DEFAULT_LANG || !req.accepts('html')) return next();

  const lang = detectPreferredLang({
    cookie: req.headers.cookie,
    country: req.get('cf-ipcountry') ?? req.get('x-country-code'),
    acceptLanguage: req.get('accept-language'),
    userAgent: req.get('user-agent'),
  });
  res.vary('Accept-Language').vary('Cookie');
  if (!lang || lang === DEFAULT_LANG) return next();

  res.redirect(302, localizeUrl(req.originalUrl, lang));
});

app.use('/**', (req, res, next) => {
  angularApp
    .handle(req)
    .then((response) => (response ? writeResponseToNodeResponse(response, res) : next()))
    .catch(next);
});

if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

export const reqHandler = createNodeRequestHandler(app);
