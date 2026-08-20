import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { RuntimeConfig } from '../config/runtime-config';

function createCorrelationId(): string {
  if (
    typeof crypto !== 'undefined' &&
    typeof crypto.randomUUID === 'function'
  ) {
    return crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random()
    .toString(36)
    .slice(2, 12)}`;
}

export const correlationIdInterceptor: HttpInterceptorFn = (request, next) => {
  const runtimeConfig = inject(RuntimeConfig);

  if (
    !runtimeConfig.isTrustedApiUrl(request.url) ||
    request.headers.has('X-Correlation-ID')
  ) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        'X-Correlation-ID': createCorrelationId(),
      },
    }),
  );
};
