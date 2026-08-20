using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Interfaces
{
    public interface IDocumentVerificationOutbox
    {
        IDocumentDbContext DbContext { get; }

        ValueTask PublishAsync(VerifyApplicantDocumentV1 message, CancellationToken cancellationToken = default);

        Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken = default);
    }
}
