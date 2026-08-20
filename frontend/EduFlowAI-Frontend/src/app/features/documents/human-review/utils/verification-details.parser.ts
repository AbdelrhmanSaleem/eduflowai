import { VerificationDetails, VerificationField } from '../models/human-review.model';

type JsonRecord = Record<string, unknown>;

export function parseVerificationDetails(value?: string | null): VerificationDetails | null {
  if (!value?.trim()) {
    return null;
  }

  try {
    const parsed: unknown = JSON.parse(value);
    if (!isRecord(parsed)) {
      return null;
    }

    const rawFields = read(parsed, 'Fields', 'fields');
    const confidence = read(parsed, 'ConfidenceScore', 'confidenceScore');

    return {
      fields: Array.isArray(rawFields) ? rawFields.map(parseField).filter(isPresent) : [],
      warnings: stringArray(read(parsed, 'Warnings', 'warnings')),
      modelName: stringValue(read(parsed, 'ModelName', 'modelName')),
      missingFields: stringArray(read(parsed, 'MissingFields', 'missingFields')),
      confidenceScore:
        typeof confidence === 'number' && Number.isFinite(confidence)
          ? Math.min(1, Math.max(0, confidence))
          : 0,
    };
  } catch {
    return null;
  }
}

function parseField(value: unknown): VerificationField | null {
  if (!isRecord(value)) {
    return null;
  }

  const fieldName = stringValue(read(value, 'FieldName', 'fieldName'));
  if (!fieldName) {
    return null;
  }

  return {
    fieldName,
    isMatch: read(value, 'IsMatch', 'isMatch') === true,
    notes: nullableString(read(value, 'Notes', 'notes')),
    expectedValue: nullableString(read(value, 'ExpectedValue', 'expectedValue')),
    extractedValue: nullableString(read(value, 'ExtractedValue', 'extractedValue')),
  };
}

function read(record: JsonRecord, pascalCase: string, camelCase: string): unknown {
  return record[pascalCase] ?? record[camelCase];
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function stringValue(value: unknown): string {
  return typeof value === 'string' ? value : '';
}

function nullableString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value : null;
}

function stringArray(value: unknown): string[] {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === 'string' && Boolean(item.trim()))
    : [];
}

function isPresent<T>(value: T | null): value is T {
  return value !== null;
}
