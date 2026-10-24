import { CanActivateFn } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';

// The library guard validates tokens straight from storage (no async checkAuth
// pipeline), saves the attempted URL and redirects to the IdP when logged out.
export const authGuard: CanActivateFn = (route, state) => autoLoginPartialRoutesGuard(route, state);
