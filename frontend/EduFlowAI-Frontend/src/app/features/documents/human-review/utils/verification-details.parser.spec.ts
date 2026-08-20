import { parseVerificationDetails } from './verification-details.parser';

describe('parseVerificationDetails', () => {
  it('parses the PascalCase backend response', () => {
    const details = parseVerificationDetails(
      JSON.stringify({
        Fields: [
          {
            Notes: 'Different name',
            IsMatch: false,
            FieldName: 'FullNameAr',
            ExpectedValue: 'محمد علي',
            ExtractedValue: 'محمد أحمد',
          },
        ],
        Warnings: [],
        ModelName: 'gemini-document-verification',
        MissingFields: ['DateOfBirth'],
        ConfidenceScore: 0.95,
      }),
    );

    expect(details?.modelName).toBe('gemini-document-verification');
    expect(details?.confidenceScore).toBe(0.95);
    expect(details?.fields[0].isMatch).toBe(false);
    expect(details?.missingFields).toEqual(['DateOfBirth']);
  });

  it('returns null for null or malformed JSON', () => {
    expect(parseVerificationDetails(null)).toBeNull();
    expect(parseVerificationDetails('{not-json')).toBeNull();
  });
});
