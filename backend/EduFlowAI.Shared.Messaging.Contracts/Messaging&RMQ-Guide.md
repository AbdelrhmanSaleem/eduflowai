# Messaging and RabbitMQ in EduFlowAI

## 1. Purpose

EduFlowAI uses RabbitMQ for background operations that should not block the API.

Current messaging scope:

- Document verification
- Communication and email notifications

---

## 2. Main Components

```text
API
  -> saves business changes and outgoing message in PostgreSQL Outbox
  -> Wolverine publishes the message to RabbitMQ

RabbitMQ
  -> stores the message in the correct queue
  -> waits until a Worker consumer is available

Worker
  -> consumes the message
  -> runs the handler from the owning module
  -> saves the result
  -> acknowledges the message
```

The real handlers remain inside their owning modules:

```text
EduFlowAI.AI             -> document verification handler
EduFlowAI.Documents      -> result and failure handlers
EduFlowAI.Communication  -> notification and email handlers
EduFlowAI.Worker         -> hosts and runs all consumers
```

---

## 3. Outbox and Inbox

### Outbox

The Outbox saves the business update and outgoing message in the same PostgreSQL transaction.

Example:

```text
ApplicantDocument.Status = Verifying
+
VerifyApplicantDocumentV1
```

This prevents a document from being marked as verifying without its message being published.

### Inbox

The Inbox stores incoming messages and protects handlers from duplicate processing.

This is important because RabbitMQ provides at-least-once delivery, so a message may be delivered again after a crash or lost acknowledgement.

Wolverine manages the durable Outbox and Inbox.

---

## 4. Document Verification Flow

```text
1. Applicant submits documents.

2. Documents module:
   - changes ApplicantDocument.Status to Verifying;
   - publishes VerifyApplicantDocumentV1 through the Outbox.

3. Wolverine sends the message to:
   eduflow.documents.verification.v1

4. RabbitMQ delivers it to EduFlowAI.Worker.

5. Seif's handler:
   - loads the file using SourceStorageKey;
   - verifies it using the AI service;
   - publishes either:
     - ApplicantDocumentVerificationCompletedV1; or
     - ApplicantDocumentVerificationFailedV1.

6. Mansy's result handler:
   - loads ApplicantDocument;
   - compares the current StorageKey with SourceStorageKey;
   - ignores stale results;
   - updates VerificationDetailsJson and VerifiedAt;
   - sets the status to Approved or NeedsHumanReview.

7. Communication integration can later create in-app or email notifications.
```

`DocumentId + SourceStorageKey` represents the exact file being verified because the current database does not have a document-version table.

---

## 5. Important Queues

```text
eduflow.documents.verification.v1
    Receives VerifyApplicantDocumentV1.

eduflow.documents.apply-verification-result.v1
    Receives ApplicantDocumentVerificationCompletedV1.

eduflow.documents.apply-verification-failure.v1
    Receives ApplicantDocumentVerificationFailedV1.

eduflow.communication.document-status.v1
    Receives final document-status events.

eduflow.communication.send-email.v1
    Receives email notification commands.
```

Each queue should have a Worker consumer and an error/DLQ path.

---

## 6. Required Configuration

### Connection strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=EduFlowAI;Username=postgres;Password=...",
    "RabbitMq": "amqp://eduflow:password@localhost:5672/eduflow"
  },
  "Messaging": {
    "Enabled": true
  }
}
```

When testing unrelated features without RabbitMQ:

```json
{
  "Messaging": {
    "Enabled": false
  }
}
```

When disabled, Wolverine can use:

```csharp
options.StubAllExternalTransports();
```

This keeps Wolverine services available but prevents external RabbitMQ connections. Do not test real messaging while transports are stubbed.

---

## 7. Starting the System Locally

Start RabbitMQ:

```bash
docker compose up -d rabbitmq
```

Open the management UI:

```text
http://localhost:15672
```

Then start the Worker:

```bash
dotnet run --project EduFlowAI.Worker
```

Then start the API:

```bash
dotnet run --project EduFlowAI.Api
```

Recommended startup order:

```text
PostgreSQL
-> RabbitMQ
-> Worker
-> API
```

---

## 8. Verifying That Messaging Works

In RabbitMQ Management UI, open **Queues and Streams**.

For each Worker queue, verify:

```text
Consumers > 0
```

Useful queue values:

```text
Ready
    Messages waiting for a consumer.

Unacked
    Messages delivered to a Worker but not finished yet.

Consumers
    Number of active Worker consumers.
```

A reliable test:

```text
1. Stop the Worker.
2. Publish a verification request.
3. Confirm Ready = 1.
4. Start the Worker.
5. Confirm Ready returns to 0.
6. Check Worker logs.
7. Confirm ApplicantDocument was updated.
```

Expected Worker logs:

```text
VerifyApplicantDocumentV1 received
Verification completed or failed event published
Verification result received
ApplicantDocument updated
```

---

## 9. Handler Discovery

Every assembly containing Wolverine handlers should include:

```csharp
using Wolverine.Attributes;

[assembly: WolverineModule]
```

Examples:

```text
EduFlowAI.AI
EduFlowAI.Documents
EduFlowAI.Communication
```

Handler requirements:

```text
- public concrete class;
- class name usually ends with Handler;
- public Handle or HandleAsync method;
- message contract is a handler parameter.
```

The Worker loads the modules and executes their handlers. The handlers do not need to live inside the Worker project.

---

## 10. Failure Behavior

```text
RabbitMQ unavailable
    -> outgoing message remains in PostgreSQL Outbox;
    -> Wolverine sends it after RabbitMQ returns.

Worker unavailable
    -> message waits in RabbitMQ.

Worker crashes before acknowledgement
    -> RabbitMQ can redeliver the message;
    -> Inbox and business idempotency prevent duplicate effects.

Temporary exception
    -> Wolverine retries.

Permanent or poison message
    -> message moves to the configured error/DLQ queue.
```

Handlers should still apply business-level idempotency checks, such as comparing `SourceStorageKey` and checking the current document status.

---

## 11. Development Rules

- Keep RabbitMQ and messaging disabled when testing unrelated features if needed.
- Start `EduFlowAI.Worker` when testing real messaging.
- Do not call Gemini or SMTP inside a long database transaction.
- Do not send files, secrets, EF entities, or unmasked sensitive data in contracts.
- Use contracts from `EduFlowAI.Shared.Messaging.Contracts`.
- Use the Outbox when publishing messages alongside database changes.
- Keep business handlers inside their owning modules.
- Keep Worker as the consumer host.
