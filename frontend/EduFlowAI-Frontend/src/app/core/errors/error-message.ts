import { Injectable, inject } from '@angular/core';

import { LocaleStore } from '../i18n/locale.store';
import {
  ApiError,
  generalErrors,
  isApiError,
  toApiError,
} from './api-problem';

@Injectable({ providedIn: 'root' })
export class ErrorMessage {
  private readonly locale = inject(LocaleStore);

  normalize(error: unknown): ApiError {
    return isApiError(error) ? error : toApiError(error);
  }

  summary(error: unknown, knownFields: readonly string[] = []): string {
    const normalized = this.normalize(error);
    return (
      generalErrors(normalized, knownFields)[0] ??
      this.locale.t('common.unexpected')
    );
  }
}
