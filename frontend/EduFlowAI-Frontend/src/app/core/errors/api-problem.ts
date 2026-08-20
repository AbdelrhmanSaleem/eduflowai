import { HttpErrorResponse } from '@angular/common/http';

export type ApiValidationErrors = Record<string, string[]>;

export interface ApiError {
  status: number;
  title: string;
  detail?: string;
  errors: ApiValidationErrors;
  traceId?: string;
  original?: unknown;
}

type ProblemPayload = {
  title?: unknown;
  detail?: unknown;
  traceId?: unknown;
  errors?: unknown;
};

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}

function normalizeMessages(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === 'string');
  }

  return typeof value === 'string' ? [value] : [];
}

export function normalizeValidationKey(key: string): string {
  return key
    .replace(/^\$\./, '')
    .replace(/\[(\d+)\]/g, '.$1')
    .trim()
    .toLowerCase();
}

function normalizeErrors(errors: unknown): ApiValidationErrors {
  if (!errors || typeof errors !== 'object' || Array.isArray(errors)) {
    return {};
  }

  return Object.entries(errors).reduce<ApiValidationErrors>(
    (result, [key, value]) => {
      const messages = normalizeMessages(value);

      if (messages.length) {
        result[normalizeValidationKey(key)] = messages;
      }

      return result;
    },
    {},
  );
}

function fallbackTitle(status: number): string {
  switch (status) {
    case 0:
      return 'Unable to reach the server';
    case 400:
      return 'Invalid request';
    case 401:
      return 'Authentication required';
    case 403:
      return 'Forbidden';
    case 404:
      return 'Not found';
    case 409:
      return 'Update conflict';
    case 423:
      return 'Account locked';
    case 429:
      return 'Too many requests';
    default:
      return status >= 500 ? 'Unexpected server error' : 'Request failed';
  }
}

export function toApiError(error: unknown): ApiError {
  if (!isHttpErrorResponse(error)) {
    return {
      status: 0,
      title: 'Unexpected error',
      detail: error instanceof Error ? error.message : undefined,
      errors: {},
      original: error,
    };
  }

  const body =
    error.error && typeof error.error === 'object'
      ? (error.error as ProblemPayload)
      : undefined;

  return {
    status: error.status,
    title: stringValue(body?.title) ?? fallbackTitle(error.status),
    detail:
      stringValue(body?.detail) ??
      (typeof error.error === 'string' ? error.error : undefined),
    errors: normalizeErrors(body?.errors),
    traceId: stringValue(body?.traceId),
    original: error,
  };
}

export function isApiError(error: unknown): error is ApiError {
  return (
    !!error &&
    typeof error === 'object' &&
    typeof (error as ApiError).status === 'number' &&
    typeof (error as ApiError).title === 'string' &&
    !!(error as ApiError).errors
  );
}

export function isHttpErrorResponse(
  error: unknown,
): error is HttpErrorResponse {
  return error instanceof HttpErrorResponse;
}

export function fieldErrors(
  error: ApiError | null | undefined,
  fieldName: string,
): string[] {
  if (!error) {
    return [];
  }

  return error.errors[normalizeValidationKey(fieldName)] ?? [];
}

export function generalErrors(
  error: ApiError | null | undefined,
  knownFields: readonly string[] = [],
): string[] {
  if (!error) {
    return [];
  }

  const normalizedKnownFields = new Set(
    knownFields.map((field) => normalizeValidationKey(field)),
  );
  const messages = Object.entries(error.errors)
    .filter(([key]) => !normalizedKnownFields.has(key))
    .flatMap(([, values]) => values);

  if (messages.length) {
    return messages;
  }

  return [error.detail ?? error.title];
}
