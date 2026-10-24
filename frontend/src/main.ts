import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app.config';
import { AppComponent } from './app.component';
import { loadRuntimeConfig } from './app/core/config';

loadRuntimeConfig()
    .then(() => bootstrapApplication(AppComponent, createAppConfig()))
    .catch((err) => console.error(err));
