import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, inject } from '@angular/core';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import * as Sentry from '@sentry/angular';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),

    // ---------- Sentry ----------
    // Captures uncaught errors thrown anywhere in the Angular app, including
    // template binding errors, lifecycle errors, and effect errors. If Sentry
    // wasn't initialised in main.ts (no DSN configured), this is a no-op.
    {
      provide: ErrorHandler,
      useValue: Sentry.createErrorHandler({ showDialog: false }),
    },
    // TraceService starts a Sentry performance transaction for every route
    // change, so navigation timings and the API calls inside each route get
    // grouped together.
    { provide: Sentry.TraceService, deps: [Router] },
    {
      provide: APP_INITIALIZER,
      useFactory: () => () => inject(Sentry.TraceService),
      multi: true,
    },
  ],
};
