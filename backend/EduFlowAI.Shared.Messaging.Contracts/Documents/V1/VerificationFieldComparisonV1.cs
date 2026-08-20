using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    public sealed record VerificationFieldComparisonV1(
        string FieldName,
        string? ExtractedValue,
        string? ExpectedValue,
        bool IsMatch,
        string? Notes
    );

    public sealed record DocumentVerificationDetailsV1(
        IReadOnlyCollection<VerificationFieldComparisonV1> Fields,
        IReadOnlyCollection<string> MissingFields,
        IReadOnlyCollection<string> Warnings,
        decimal? ConfidenceScore,
        string ModelName
    );
}
