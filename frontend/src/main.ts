import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import { initObservability } from './app/observability';

// Init Sentry + OTel before the app bootstraps so even startup errors are captured.
initObservability();

bootstrapApplication(AppComponent, appConfig).catch(err => console.error(err));
