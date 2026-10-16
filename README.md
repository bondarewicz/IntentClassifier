# IntentMatcher

Classifies banking customer queries into the 77 [Banking77](data/banking77/README.md) intents using OpenAI
embeddings and cosine similarity computed in Postgres with [pgvector](https://github.com/pgvector/pgvector).

On the held-out Banking77 test split (3,079 queries):

| Intent represented by | Top-1 | Top-3 | Ambiguous* | Top-1 when ambiguous / confident |
|---|---|---|---|---|
| **Centroid** of its train query embeddings (default) | **88.9%** | **97.2%** | 9.3% | 49.8% / 92.9% |
| Embedding of its name only | 62.2% | 79.1% | 21.6% | 28.7% / 71.4% |

\* best intent less than 0.02 cosine similarity ahead of the runner-up (`MatchConfidence.AmbiguousMargin`).
Ambiguous matches are where a second opinion (e.g. an LLM choosing among the top candidates) would pay off.

## Solution layout (`IntentMatcher.slnx`)

Dependencies point inwards: Domain ← Application ← Infrastructure, with Api and Cli as composition roots.

| Project | Responsibility |
|---|---|
| `src/IntentMatcher.Domain` | Model and rules, no dependencies: `QueryText` (normalization), `IntentId` (label canonicalization), `Intent`, `LabelledQuery`, `Embedding`, `IntentMatch`, `MatchConfidence`; repository interfaces |
| `src/IntentMatcher.Application` | Use cases: `ClassifyQueryHandler`, `EmbedDatasetHandler`, `TrainIntentsHandler`, `RecomputeMatchesHandler`, `EvaluateHandler`; ports (`IEmbeddingGenerator`, `IDatasetImporter`, `IDatabaseMigrator`, report/evaluation queries) |
| `src/IntentMatcher.Infrastructure` | Adapters: Dapper repositories over Npgsql + pgvector, FluentMigrator migrations, Banking77 COPY importer, OpenAI embedding generator |
| `src/IntentMatcher.Api` | Minimal API: `POST /queries/classify` |
| `src/IntentMatcher.Cli` | Commands: `migrate`, `seed`, `embed`, `train`, `match`, `evaluate`, `classify` |
| `tests/IntentMatcher.UnitTests` | Domain rules and use cases with in-memory fakes |
| `tests/IntentMatcher.IntegrationTests` | Whole pipeline against a throwaway pgvector Postgres (Testcontainers) with a deterministic fake embedder |

Persistence follows the Schema/Row/Repository pattern (Fowler's *Metadata Mapping* + *Data Mapper*): per table a
`*Row` persistence model, a static `*Schema` (`Table`, `Columns` via `nameof(Row.X).ToSnakeCase()`, `Parameters`,
`Sql`) and a thin Dapper repository that maps rows to domain objects.

## Data model (FluentMigrator, `Infrastructure/Persistence/Migrations`)

| # | Table / view | What |
|---|---|---|
| 1 | extension `vector` | pgvector |
| 2 | `intents` | 77 intents: `name_embedding` and `centroid_embedding` (`vector(1536)`), `example_count` |
| 3 | `labelled_queries` | Banking77 queries: `split` (train/test), normalized `query_text`, labelled `intent_id`, `embedding` |
| 4 | `query_intent_matches` | top-K intents per query from the last `match` run: `(query_id, rank) -> intent_id, similarity` |
| 5 | `query_match_report` (view) | per query: labelled intent, best + runner-up with scores, `margin`, `is_correct` |

Similarity = `1 - (a <=> b)` (pgvector cosine distance). Centroids are `avg(embedding)` of an intent's train queries.

## Run

Requires the .NET 10 SDK and Docker.

```sh
docker compose up -d --wait                 # pgvector/pgvector:pg17 as banking77-pg on localhost:5435
dotnet user-secrets --project src/IntentMatcher.Cli set "OpenAi:ApiKey" "<key>"   # shared with the API

CLI="dotnet run --project src/IntentMatcher.Cli --"
$CLI migrate        # schema (idempotent)
$CLI seed           # data/banking77/{train,test}.csv (re-runnable, keeps embeddings)
$CLI embed          # 77 intent names + ~13k queries, ~0.2M tokens (< 1 cent), resumable
$CLI train          # intent centroids from train embeddings
$CLI match          # store top-5 intents for every query, print accuracy per split
$CLI evaluate       # test-split accuracy for centroid vs name representation
$CLI classify "I was charged twice for the same coffee" [--top 3] [--by centroid|name]
```

## API

```sh
dotnet run --project src/IntentMatcher.Api --launch-profile http     # http://localhost:5235
```

```http
POST /queries/classify
{ "query": "I was charged twice for the same coffee this morning" }
```

```json
{ "query": "I was charged twice for the same coffee this morning",
  "normalizedQuery": "i was charged twice for the same coffee this morning",
  "embeddingSource": "openai", "ambiguous": false,
  "matches": [
    { "rank": 1, "intentId": "transaction_charged_twice", "intentName": "Transaction charged twice", "similarity": 0.757 },
    { "rank": 2, "intentId": "extra_charge_on_statement", "intentName": "Extra charge on statement", "similarity": 0.604 },
    { "rank": 3, "intentId": "card_payment_fee_charged", "intentName": "Card payment fee charged", "similarity": 0.593 } ] }
```

Known dataset queries reuse their stored embedding (`"embeddingSource": "stored"`, a few ms); new ones are embedded
with OpenAI. 400 for an empty query, 503 for a new query when no OpenAI key is configured. OpenAPI document at
`/openapi/v1.json` (Development); sample requests in `src/IntentMatcher.Api/IntentMatcher.Api.http`.
Set `Database:AutoMigrate=true` to apply migrations on startup.

## Settings

`appsettings.json` in each entry point: `ConnectionStrings:Postgres`, `OpenAi:ApiKey` (leave empty), `OpenAi:Model`.
Precedence: appsettings < user-secrets < environment variables (`OpenAi__ApiKey`, `ConnectionStrings__Postgres`);
plain `OPENAI_API_KEY` is a fallback for the key. Never commit a key.

## Tests

```sh
dotnet test IntentMatcher.slnx     # integration tests need Docker running
```
