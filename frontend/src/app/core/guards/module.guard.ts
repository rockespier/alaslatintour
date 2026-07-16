import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionsService, AdminModule } from '../services/permissions.service';

export const moduleGuard: CanActivateFn = (route) => {
  const permissions = inject(PermissionsService);
  const router = inject(Router);
  const module = route.data?.['module'] as AdminModule | undefined;
  if (!module || permissions.canView(module)) return true;
  return router.createUrlTree(['/admin']);
};
