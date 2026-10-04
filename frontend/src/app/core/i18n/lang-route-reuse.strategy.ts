import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from '@angular/router';

/**
 * `/en/...` and `/pt/...` share one route config (`langPrefixMatcher`), so switching between
 * them only changes the `lang` param and Angular would reuse the mounted components: their
 * `ngOnInit` never reruns and content fetched with `?lang=` (WordPress news, galleries) stays
 * in the previous language. Treating a language change as a new route re-creates the page.
 */
@Injectable()
export class LangRouteReuseStrategy extends BaseRouteReuseStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    return super.shouldReuseRoute(future, curr) && future.params['lang'] === curr.params['lang'];
  }
}
