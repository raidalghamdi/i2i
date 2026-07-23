import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export const devUserInterceptor: HttpInterceptorFn = (req, next) => {
  if (environment.production) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { 'X-Dev-User': environment.devUser } }));
};
