import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

function normalizeApiBaseUrl(configuredUrl: string): string {
  const normalized = configuredUrl.trim().replace(/\/+$/, '');

  return /\/api$/i.test(normalized) ? normalized : `${normalized}/api`;
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfig {
  readonly apiBaseUrl = normalizeApiBaseUrl(environment.apiUrl);

  isTrustedApiUrl(url: string): boolean {
    if (url.startsWith('/api/')) {
      return true;
    }

    if (!/^https?:\/\//i.test(url)) {
      return false;
    }

    try {
      const parsedUrl = new URL(url);
      const configuredApiUrl = new URL(`${this.apiBaseUrl}/`);

      return (
        parsedUrl.origin === configuredApiUrl.origin &&
        parsedUrl.pathname.startsWith(configuredApiUrl.pathname)
      );
    } catch {
      return false;
    }
  }
}
