using System;
using System.Collections.Generic;

namespace EduFlowAI.Documents.Application.DTOs;

public sealed record DocumentVerificationFieldDto(
    string FieldName,
    string? ExtractedValue,
    string? ExpectedValue,
    bool IsMatch,
    string? Notes);

// Read-only projection of ApplicantDocument's verification state. There is no separate
// attempt-history table today - see SEIF_IMPLEMENTATION_GUIDE.md section 1.2 for why.
public sealed record DocumentVerificationResponseDto(
    Guid DocumentId,
    string Status,
    string? OverallResult,
    IReadOnlyCollection<DocumentVerificationFieldDto> Fields,
    IReadOnlyCollection<string> MissingFields,
    IReadOnlyCollection<string> Warnings,
    decimal? ConfidenceScore,
    string? ModelName,
    string? TechnicalErrorCode,
    string? TechnicalErrorMessage,
    DateTimeOffset? ProcessedAt);
