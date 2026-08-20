# EduFlowAI.AI — Knowledge & Intent Service

Owner: **Abdelrhman Saleem** (RAG, Knowledge Indexing, Intent Router)

This is the AI engine behind the assistant. It has **no endpoints of its own** — per the task
distribution, Kamel owns the presentation layer and calls into the services below.

Given a free-text message it works out what the applicant wants (`knowledge` / `status` /
`recommendation`) and, for knowledge questions, produces an answer grounded in the indexed ITI
documents. It also owns the knowledge base: storing uploads, transcribing, chunking, embedding and
searching the vectors.

Work in progress — this branch is not ready to merge, see [What's left](#whats-left).

---

## Services provided to Kamel

These five interfaces are the module's public surface. All live in `Application/Interfaces/` and are
registered by `services.AddAIModule()`.

| Interface | What it does |
|---|---|
| `IIntentRouter` | `ClassifyAsync(question, context)` → intent, confidence, `requiresClarification` |
| `IRagAnswerService` | `AnswerWithContextAsync(question, language)` → grounded answer + `sources` |
| `IKnowledgeIndexingService` | Upload / paste text / list / status / delete / full re-sync. Uploads are queued and indexed in the background; poll `GetDocumentStatusAsync`. |
| `IKnowledgeRetrievalService` | `RetrieveContextAsync(query, limit)` → nearest chunks with their source |
| `IEmbeddingService` | `GenerateEmbeddingAsync(text)` → vector of `Gemini:EmbeddingDimensions` |

`ISessionManager` is also registered — conversation context is on my task list — so Kamel's assistant
endpoint can keep per-session history without building its own store.

Ingestion failures are typed so the endpoint can map them without inspecting messages:

| Exception | Suggested status |
|---|---|
| `KnowledgeBaseValidationException` | 400 |
| `KnowledgeBaseTooLargeException` | 413 |
| `KnowledgeBaseBusyException` | 409 (a re-sync is running) |

Integration notes for Kamel, Halim and Karim are shared directly rather than through the repo — ask
me for them.

## What works today

Verified end to end against a local PostgreSQL + pgvector database using a real ITI track PDF.
101 unit tests pass.

- A **PDF, .txt or .md** file — or pasted text — is validated and stored, then transcribed, chunked
  and embedded on a background worker. The call returns in well under a second with `Pending`, and
  the caller polls `GetDocumentStatusAsync` through `Indexing` to `Indexed` or `Failed`.
- Questions in **English or Arabic** get an answer grounded in the uploaded documents, in the same
  language, with the source documents returned alongside.
- Intent routing is deterministic — the same question always routes the same way — and asks the user
  to clarify instead of guessing when confidence is below the configured threshold.
- **Multi-intent:** *"What is my application status, and which track should I choose?"* returns both
  intents, ranked most likely first, so the caller can dispatch each and combine the replies.
  `PrimaryIntent` stays the top-ranked entry, so single-intent callers are unaffected.
- Knowledge base management: list, delete, and a full re-sync that rebuilds every embedding from the
  stored originals.
- Long digit runs (National ID) are masked before any text leaves for the model.

## Layout

```
Application/
  DbContextAbstraction/IAIDbContext.cs   module's slice of the shared DbContext
  Exceptions/                            typed ingestion failures for the caller to map
  Interfaces/                            the five services above + repository, storage, extractor
  Services/
    AiChatService.cs                     IRagAnswerService — grounded answer + the ITI assistant prompt
    IntentClassifierService.cs           IIntentRouter — routing prompt, schema and parsing
    KnowledgeBaseService.cs              IKnowledgeIndexingService — validate, ingest, re-sync, delete
    KnowledgeBaseRetrievalService.cs     IKnowledgeRetrievalService — embeds the query, cosine search
    LanguageDetectionService.cs          ar / en
    InputSanitizerService.cs             masks long digit runs
Infrastructure/
  ExternalServices/GeminiChatClient.cs   single generateContent client (text, JSON mode, documents)
  ExternalServices/GeminiEmbeddingService.cs
  Processing/DocumentTextExtractor.cs    AI transcription, falls back to iText
  Processing/TextChunker.cs              sentence-aware chunking
  Indexing/IndexingQueue.cs              in-memory queue behind background indexing
  Indexing/KnowledgeIndexingWorker.cs    drains it, one scope per document
  Persistence/KnowledgeRepository.cs
  Storage/LocalFileStorageService.cs
  Services/InMemorySessionManager.cs
  Configurations/                        EF mappings for the two KB tables
```

Tables owned here: `ai.KnowledgeBaseDocument`, `ai.KnowledgeBaseChunk` (`vector(1536)`, exact cosine
search, no ANN index).

## Running it

1. PostgreSQL with the `vector` extension, database `EduFlowAIDb`.
2. Put the connection string and the Gemini key in `EduFlowAI.Api/appsettings.Development.json`
   (git-ignored — never commit keys).
3. `dotnet ef database update --project EduFlowAI.Persistence --startup-project EduFlowAI.Api`
4. `dotnet test EduFlowAI.AI.Tests`

Config worth knowing: `Gemini:ChatModels` (fallback chain), `Gemini:ApiKeys`, `Gemini:EmbeddingModel`,
`Gemini:EmbeddingDimensions`, `IntentClassification:MinConfidence`, `ChatSession:IdleTimeout`,
`Ingestion:UseAiExtraction`, `Ingestion:MaxUploadBytes`. `ChatModel`/`ApiKey` (singular) still work
as a fallback when the lists are empty.

---

## What's left

**Durable indexing queue.** The indexing queue is in-memory, so a restart mid-index would strand
documents — the worker re-queues anything left `Pending`/`Indexing` on startup to cover that. The
proper answer is RabbitMQ through the Worker host once Ali's messaging contracts exist. Note the
`Ingestion:RequeueStrandedOnStartup` flag: both hosts call `AddAIModule()`, so if the Worker is ever
run alongside the API it must be `false` there or both would index the same documents.

**Evaluation datasets.** RAG and router evaluation sets are on my task list and don't exist yet.
Routing has been checked against a hand-written golden set, but retrieval quality has not been
measured.

**Models.** Everything runs on Gemini (`gemini-3.1-flash-lite` for chat and transcription,
`gemini-embedding-001` @ 1536 for embeddings). Chat and extraction share a **fallback chain**
(`Gemini:ChatModels`): a model that is rate-limited, quota-exhausted or 404 (as `gemini-2.5-flash` is
for new keys) is skipped for the next, and each model has its own free-tier quota bucket. `ApiKeys`
rotate on quota/auth failures — one key today, extensible via config. **Embeddings never fall back**
to another model, because a different model is a different vector space; they stay on the one model
with the cache and backoff.

**Hosted database.** Only the local database holds embeddings. The embedding model and 1536 width are
now fixed, so the hosted (Neon) database just needs the `vector(1536)` schema applied (no alter) and
then the corpus ingested/re-synced. To be done with whoever owns the database once the documents are
supplied.

**RabbitMQ.** The in-memory indexing queue is the interim; message-based indexing through the Worker
host is the future path once the messaging contracts exist.

**Endpoints — Kamel.** Nothing in this module is reachable over HTTP until the assistant and
knowledge-base endpoints exist. Integration notes sent separately.

**Uploader identity — Karim.** `UploadedByUserId` is the placeholder `"system-seed"` until there is
a JWT scheme to read a real user id from.

**Content.** Only one track brochure is indexed, so questions about other tracks correctly return
"I don't have that". Arabic answers could also use tuning for Egyptian Arabic.
