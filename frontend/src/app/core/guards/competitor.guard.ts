import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { langFromPath, localizeUrl } from '../i18n/languages';

export const competitorGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    return router.createUrlTree([localizeUrl('/login', langFromPath(state.url))], { queryParams: { returnUrl: state.url } });
  }
  if (auth.isCompetitor()) return true;
  return router.createUrlTree([localizeUrl('/', langFromPath(state.url))]);
};
