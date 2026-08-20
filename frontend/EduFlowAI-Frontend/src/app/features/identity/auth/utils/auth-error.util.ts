interface ErrorLike {
  status?: unknown;
  title?: unknown;
  detail?: unknown;
  message?: unknown;
  errors?: unknown;
  error?: unknown;
}

export function errorStatus(error: unknown): number {
  if (!error || typeof error !== 'object') {
    return 0;
  }

  const status = (error as ErrorLike).status;
  return typeof status === 'number' ? status : 0;
}

export function errorContains(error: unknown, search: string): boolean {
  if (!error || typeof error !== 'object') {
    return false;
  }

  const value = error as ErrorLike;

  if (value.error && value.error !== error && errorContains(value.error, search)) {
    return true;
  }

  const parts = [value.title, value.detail, value.message];

  if (value.errors && typeof value.errors === 'object') {
    for (const [key, messages] of Object.entries(
      value.errors as Record<string, unknown>,
    )) {
      parts.push(key);
      if (Array.isArray(messages)) {
        parts.push(...messages);
      } else {
        parts.push(messages);
      }
    }
  }

  const haystack = parts
    .filter((part): part is string => typeof part === 'string')
    .join(' ')
    .toLowerCase();

  return haystack.includes(search.toLowerCase());
}

export function safeReturnUrl(value: string | null): string | null {
  if (
    !value ||
    !value.startsWith('/') ||
    value.startsWith('//') ||
    value.startsWith('/auth')
  ) {
    return null;
  }

  return value;
}
