# EduFlowAI

An AI-assisted admissions platform built for the Information Technology Institute (ITI), which runs
professional training programmes across Egypt and processes thousands of applications per intake.

The platform takes an applicant from registration through eligibility, track preferences, document
checks and allocation. It also provides a bilingual assistant that answers questions, reports
application status and recommends tracks.

---

## About this repository

EduFlowAI was built by a team of eight during the **ITI 9-Month Professional Training Program,
Intake 46**. This repository is a snapshot of the finished project, published so I can show the work
I did on it.

The team's original repositories are the source of truth. This snapshot has no commit history, so the
authorship of individual lines is not visible here. The section below sets out which parts are mine.
Everything else is my teammates' work.

| | |
|---|---|
| Team size | 8 |
| Programme | ITI 9-Month Professional Training, Intake 46 |
| My commits | 53 in the backend, 4 in the frontend |
| My area | `backend/EduFlowAI.AI`, the AI module |

---

## What I built

I worked on the **AI module**, covering what happens between an applicant sending a message and
receiving an answer they can rely on.

### The assistant

A message does not go straight to a language model. It is routed first.

An **intent router** classifies each message against a fixed set of intents using a
schema-constrained call at temperature zero, detects the language the applicant wrote in, and
produces a self-contained English search query. Routing first is a safety decision. Knowledge answers
may be generated from retrieved text, but a status answer has to come from the database, so the model
never gets the chance to invent one.

- Multi-intent support, so "what is my status, and which track suits me?" reaches two tools and the
  replies are merged
- A confidence gate. Below 0.6 the assistant asks a clarifying question instead of guessing
- Language is judged from the sentence the applicant actually wrote, so an Arabic message containing
  an English job title is answered in Arabic

### Retrieval that returns the right thing

Asked which tracks were offered in Alexandria, the assistant used to avoid the question. Vector
search was returning chunks about Alexandria that did not contain the track list.

I moved it to hybrid retrieval, combining vector similarity with reserved slots for keyword matches
so an exact term cannot be lost to semantic drift, and stamped every chunk with its source document
and section. It now answers with all six tracks and cites its sources.

### Status answers that cannot drift

Application and document status are narrated rather than generated. The database supplies the facts.
A narrow prompt instructs the model to report exactly that status and never to upgrade, soften or
invent one. If the model call fails, the plain fact is returned instead of nothing.

### Conversation quality

- Replies are plain text. No Markdown reaches the chat, and lists are numbered
- The assistant greets once and then continues the conversation instead of reintroducing itself on
  every topic change
- Greetings and small talk are answered naturally rather than refused
- National IDs and other long digit runs are masked before any user text reaches a model

### Cost

I costed the AI before assuming we could afford it. It runs at about **$0.024 per applicant**, which
is roughly 2.4 US cents, or $24 per thousand applicants. Token counts were measured from the actual
prompts rather than estimated. Knowledge answers and the router account for most of it because both
run on every message, which makes context caching the largest available saving.

### Configuration and reliability

Every model, API key and temperature is configuration rather than a literal, so features can be tuned
or moved onto separate quotas without a redeploy. Calls walk a list of models and keys, retrying on
rate limits and moving on when a model is unavailable.

### A bug worth mentioning

The assistant reported an application as withdrawn while the applicant had a live one. The lookup
took the first application it found, unordered and unfiltered. It now prefers the newest
non-withdrawn application and only falls back to a withdrawn one when nothing else exists. The same
flaw existed in a second place, so both were fixed together with tests covering each case.

**271 tests** in the AI module, of 392 across the solution.

---

## Architecture

A .NET modular monolith. One deployable unit, nine modules, each owning its entities, services and
controllers.

```
Api             HTTP surface, one controller set per module
Worker          background message handlers
Identity        authentication, roles, applicant profiles
Admission       cycles, programmes, tracks, offerings, applications, allocation
Documents       uploads, review workflow
AI              intent routing, retrieval, narration, recommendation
Communication   notifications and email
Persistence     EF Core, one DbContext, PostgreSQL
Messaging       Wolverine over RabbitMQ, durable outbox and inbox
```

Work that must not block a user request runs on queues, so a slow model call or an unavailable mail
service never holds up a response.

**Stack:** .NET, Angular with bilingual Arabic and English including right to left layout,
PostgreSQL with pgvector, RabbitMQ, Google Gemini, AWS ECS behind an ALB, S3 and CloudFront.

---

## Running it

Configuration lives in `appsettings.Development.json`, which is deliberately not in this repository
because it holds connection strings and API keys. A local run needs PostgreSQL with the pgvector
extension, a Gemini API key, and RabbitMQ, or `Messaging:Enabled: false` to stub the transports.
