using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace EduFlowAI.Persistence.Messaging
{
    public sealed class DocumentVerificationOutbox
    : IDocumentVerificationOutbox
    {
        private readonly IDbContextOutbox<EduFlowAIDbContext> _outbox;

        public DocumentVerificationOutbox(
            IDbContextOutbox<EduFlowAIDbContext> outbox)
        {
            _outbox = outbox;
        }

        public IDocumentDbContext DbContext => _outbox.DbContext;

        public ValueTask PublishAsync(VerifyApplicantDocumentV1 message, CancellationToken cancellationToken = default)
        {
            return _outbox.PublishAsync(message);
        }

        public Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken = default)
        {
            return _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        }
    }
}
