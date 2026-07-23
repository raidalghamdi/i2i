import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  /**
   * Production deploys serve both locale bundles from one origin under
   * /en/ and /ar/ path prefixes (Angular's --localize build convention),
   * so swapping the first path segment is enough there. Local dev instead
   * runs one `ng serve` instance per locale on its own port (no path
   * prefix in either) — DEV_LOCALE_PORTS documents that convention so the
   * same toggle works in both setups without an environment-specific flag.
   * Arabic is the default `ng serve` locale (angular.json serve.defaultConfiguration),
   * so it owns the default port 4200; English is the explicit
   * `--configuration=development --port=4201` alternative.
   */
  private static readonly DEV_LOCALE_PORTS: Record<string, string> = { ar: '4200', en: '4201' };

  alternateLocaleHref(): string {
    const { pathname, port, protocol, hostname } = window.location;
    const segments = pathname.split('/');
    const firstSegment = segments[1];

    if (firstSegment === 'en' || firstSegment === 'ar') {
      const targetLocale = firstSegment === 'ar' ? 'en' : 'ar';
      segments[1] = targetLocale;
      return segments.join('/') || `/${targetLocale}/`;
    }

    const currentLocale = Object.entries(LocaleService.DEV_LOCALE_PORTS).find(([, p]) => p === port)?.[0] ?? 'en';
    const targetLocale = currentLocale === 'ar' ? 'en' : 'ar';
    const targetPort = LocaleService.DEV_LOCALE_PORTS[targetLocale];
    return `${protocol}//${hostname}:${targetPort}${pathname}`;
  }
}
