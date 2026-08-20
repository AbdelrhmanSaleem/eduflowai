using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Messaging.Contracts
{
    public static class MessagingQueueNames
    {
        public const string DocumentVerification = "eduflow.documents.verification.v1";
        public const string ApplyDocumentVerificationResult = "eduflow.documents.apply-verification-result.v1";
        public const string ApplyDocumentVerificationFailure = "eduflow.documents.apply-verification-failure.v1";
        public const string DocumentStatusNotifications = "eduflow.communication.document-status.v1";
        public const string DocumentReplacementFulfilled = "eduflow.documents.replacement-fulfilled.v1";
        public const string KnowledgeIndexing = "eduflow.knowledge.indexing.v1";
        public const string KnowledgeIndexingResults = "eduflow.communication.knowledge-results.v1";
        public const string SendEmail = "eduflow.communication.send-email.v1";
        public const string CreateNotification = "eduflow.communication.create-notification.v1";
        public const string RealtimeNotification = "eduflow.communication.realtime.v1";
    }
}
