import {
  ApplicationConfig,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  inject,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { devUserInterceptor } from './core/auth/dev-user.interceptor';
import { IdentityService } from './core/auth/identity.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([devUserInterceptor])),
    provideAppInitializer(() => {
      const identityService = inject(IdentityService);
      return identityService.load();
    }),
  ],
};
