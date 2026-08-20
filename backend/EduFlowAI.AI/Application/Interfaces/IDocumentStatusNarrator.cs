using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Documents.Application.DTOs;

namespace EduFlowAI.AI.Application.Interfaces;

// Turns an applicant's document records into a warm, natural reply. The statuses are authoritative
// facts the model phrases - it never decides, changes or invents them.
public interface IDocumentStatusNarrator
{
    // isContinuingConversation suppresses the greeting when the applicant is already mid-chat.
    Task<string> NarrateAsync(
        IEnumerable<ApplicantDocumentDto> documents,
        string language,
        bool isContinuingConversation = false,
        CancellationToken cancellationToken = default);
}
