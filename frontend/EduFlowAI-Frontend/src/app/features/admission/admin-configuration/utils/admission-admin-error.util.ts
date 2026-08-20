import { HttpErrorResponse } from '@angular/common/http';

import { isApiError } from '../../../../core/errors/api-problem';

interface ResultErrorBody {
  message?: unknown;
}

export function admissionAdminErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (isApiError(error)) {
    const original = error.original;

    if (original instanceof HttpErrorResponse) {
      const body = original.error as ResultErrorBody | null;
      const message = body?.message;

      if (typeof message === 'string' && message.trim()) {
        return message;
      }
    }

    return error.detail ?? error.title;
  }

  return error instanceof Error && error.message ? error.message : fallback;
}
