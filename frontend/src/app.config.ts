import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeuix/themes/aura';
import { authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import { API_BASE, IDP_AUTHORITY, SPA_CLIENT_ID, SPA_SCOPE } from './app/core/config';

// A factory (not a constant) so it reads API_BASE / IDP_AUTHORITY after loadRuntimeConfig() ran.
export const createAppConfig = (): ApplicationConfig => ({
    providers: [
        provideRouter(appRoutes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' }), withEnabledBlockingInitialNavigation()),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor()])),
        provideAnimationsAsync(),
        providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } } }),
        provideAuth({
            config: {
                authority: IDP_AUTHORITY,
                // relative to <base href>, so it works at / (dev) and /FleetIQ/ (GitHub Pages)
                redirectUrl: new URL('callback', document.baseURI).href,
                postLogoutRedirectUri: new URL('.', document.baseURI).href,
                // behind a tunnel the IdP's public host differs from its pinned issuer (https://localhost:5001)
                strictIssuerValidationOnWellKnownRetrievalOff: true,
                clientId: SPA_CLIENT_ID,
                scope: SPA_SCOPE,
                responseType: 'code',
                silentRenew: false,
                useRefreshToken: false,
                postLoginRoute: '/app',
                // the interceptor attaches the access token only to calls to these origins
                secureRoutes: [API_BASE + '/']
            }
        })
    ]
});
