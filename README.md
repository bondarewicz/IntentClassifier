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
| `src/IntentMatcher.Domain` | Model and rules, no dependencies: `QueryText` (normalization), `IntentId` (label canonicalization), `Intent`, `LabelledQuery`, `Embedding`, `IntentMatch`, `MatchConfidence`; aggregate repository interfaces (`IIntentRepository`, `ILabelledQueryRepository`) |
| `src/IntentMatcher.Application` | Use cases: `ClassifyQueryHandler`, `EmbedDatasetHandler`, `TrainIntentsHandler`, `RecomputeMatchesHandler`, `EvaluateHandler`; ports (`IEmbeddingGenerator`, `IDatasetImporter`, `IDatabaseMigrator`, `IIntentMatchProjection`, report/evaluation queries) |
| `src/IntentMatcher.Infrastructure` | Adapters: Dapper repositories over Npgsql + pgvector, FluentMigrator migrations, Banking77 COPY importer, OpenAI embedding generator |
| `src/IntentMatcher.Api` | Minimal API: `POST /queries/classify`, errors mapped to `ProblemDetails` by one `IExceptionHandler` |
| `src/IntentMatcher.Cli` | Commands: `migrate`, `seed`, `embed`, `train`, `match`, `evaluate`, `classify` |
| `tests/IntentMatcher.UnitTests` | Domain rules and use cases with in-memory fakes |
| `tests/IntentMatcher.IntegrationTests` | Whole pipeline (migrations, COPY, Dapper, pgvector SQL, the API via `WebApplicationFactory`) against a throwaway pgvector Postgres (Testcontainers) with a deterministic fake embedder |

Persistence follows the Schema/Row/Repository pattern (Fowler's *Metadata Mapping* + *Data Mapper*): per table a
`*Row` persistence model, a static `*Schema` (`Table`, `Columns` via `nameof(Row.X).ToSnakeCase()`, `Sql`) and a
thin Dapper repository that maps rows to domain objects.

## Data model (FluentMigrator, `Infrastructure/Persistence/Migrations`)

| # | Table / view | What |
|---|---|---|
| 1 | extension `vector` | pgvector |
| 2 | `intents` | 77 intents: `name_embedding` and `centroid_embedding` (`vector(1536)`), `example_count` |
| 3 | `labelled_queries` | Banking77 queries: `split` (train/test), normalized `query_text`, labelled `intent_id`, `embedding` |
| 4 | `query_intent_matches` | top-K intents per query from the last `match` run: `(query_id, rank) -> intent_id, similarity` |
| 5 | `query_match_report` (view) | per query: labelled intent, best + runner-up with scores, `margin`, `is_correct` |
| 6 | `query_intent_matches.similarity` | widened `float4` → `double precision`, matching the view, `evaluate` and the C# comparisons |
| 7 | `classified_queries` | log of every `classify` call, keyed by normalized `query_text`: `embedding` (written once), `times_classified`, last best match + `ambiguous`, `confirmed_intent_id`/`confirmed_at` once a human runs `label` |

Similarity = `1 - (a <=> b)` (pgvector cosine distance). Centroids are `avg(embedding)` of an intent's train queries.

Migration classes use FluentMigrator's recommended timestamp versions (`[Migration(202610161000, ...)]`,
not sequential integers), with a descriptive class name and a `{version}_{ClassName}.cs` file name
(e.g. `202610161000_EnableVectorExtension.cs`). The table above numbers them 1-6 for readability only -
that is not their `[Migration]` version. These were originally numbered 1-6; the one existing database's
`VersionInfo` rows were remapped to the new timestamps in a single transaction, so do not renumber them back.

## Run

Requires the .NET 10 SDK and Docker.

```sh
# pgvector/pgvector:pg17 as banking77-pg on localhost:5435
docker compose up -d --wait

# shared with the API
dotnet user-secrets --project src/IntentMatcher.Cli set "OpenAi:ApiKey" "<key>"

CLI="dotnet run --project src/IntentMatcher.Cli --"
$CLI migrate    # schema (idempotent)
$CLI seed       # data/banking77/{train,test}.csv (keeps existing embeddings)
$CLI embed      # 77 intent names + ~13k queries, ~0.2M tokens (< 1 cent)
$CLI train      # intent centroids from train embeddings
$CLI match      # store top-5 intents for every query, print accuracy per split
$CLI evaluate   # test-split accuracy for centroid vs name representation
```

`classify` prints the normalized query, where the embedding came from, and the
ranked matches. A known dataset query reuses its stored embedding (fast, free);
`--top` controls how many ranks print and `--by` switches representation.

Exit codes: `0` ok, `1` usage error (no/unknown command, bad flag), `2` a
runtime or configuration error (bad input, database unreachable, missing
config) - always with a one-line message on stderr, never a stack trace.

```sh
$CLI classify "where is the virtual card?"
```
```
Query:      where is the virtual card?
Normalized: where is the virtual card?
Embedding:  Stored, by Centroid
   1. 0.9143  Getting virtual card (getting_virtual_card)
   2. 0.7809  Virtual card not working (virtual_card_not_working)
   3. 0.7242  Get disposable virtual card (get_disposable_virtual_card)
```

A margin under `MatchConfidence.AmbiguousMargin` (0.02) between rank 1 and 2
prints `AMBIGUOUS` - here "fee" plausibly means two different things:

```sh
$CLI classify "why did my cash get charged a fee that should not be there."
```
```
Query:      why did my cash get charged a fee that should not be there.
Normalized: why did my cash get charged a fee that should not be there.
Embedding:  Stored, by Centroid, AMBIGUOUS
   1. 0.8156  Cash withdrawal charge (cash_withdrawal_charge)
   2. 0.8034  Card payment fee charged (card_payment_fee_charged)
   3. 0.7340  Transfer fee charged (transfer_fee_charged)
```

The same query by intent *name* only (no train queries) is far less sure, and
`--top 1` still computes the runner-up internally - only the printed ranks change:

```sh
$CLI classify "where is the virtual card?" --by name --top 1
```
```
Query:      where is the virtual card?
Normalized: where is the virtual card?
Embedding:  Stored, by Name
   1. 0.8233  Getting virtual card (getting_virtual_card)
```

A query that is not in the dataset is embedded on the fly (`Generated`, a real
OpenAI call):

```sh
$CLI classify "I was charged twice for the same coffee this morning"
```
```
Query:      I was charged twice for the same coffee this morning
Normalized: i was charged twice for the same coffee this morning
Embedding:  Generated, by Centroid
   1. 0.7574  Transaction charged twice (transaction_charged_twice)
   2. 0.6044  Extra charge on statement (extra_charge_on_statement)
   3. 0.5929  Card payment fee charged (card_payment_fee_charged)
```

## Query log and confirming labels

Every `classify` call (API or CLI) is upserted into `classified_queries`,
keyed by the normalized text: the embedding is written once and reused by
later calls, `times_classified` counts repeats, and the last best match /
`ambiguous` are kept current. A query generated on the fly is served from
this log on its next repeat - `Stored`, no second OpenAI call - exactly like
a known dataset query. Rejected (400) queries are never logged. `train`,
`match` and `evaluate` never read the log.

```sh
$CLI classify "the barista charged me twice for one iced latte"
$CLI classify "the barista charged me twice for one iced latte"
```
```
Embedding:  Generated, by Centroid
   ...
Embedding:  Stored, by Centroid
   ...
```

`label "<query>" <intent_id>` promotes a logged query into
`labelled_queries` (train split) once a human confirms its intent:

```sh
$CLI label "the barista charged me twice for one iced latte" \
  transaction_charged_twice
```
```
Labelled "the barista charged me twice for one iced latte" as
transaction_charged_twice.
Run `train` to fold it into the centroids.
```

It never recomputes centroids itself - run `train` afterwards. Rejected
(exit 2, nothing written) when:

- the query was never classified (not in the log yet)
- the intent id does not exist
- the query is in the test split (would leak test data into training):

```sh
$CLI label "where is the virtual card?" getting_virtual_card
```
```
'where is the virtual card?' is in the test split - labelling it
would leak test data into training.
```

- the query is already labelled in the dataset (an original train row,
  not an earlier promotion)

Relabelling a previously promoted query with a different intent updates
that same train row (latest human wins); labelling it again with the
same intent is a no-op.

## API

```sh
# http://localhost:5235
dotnet run --project src/IntentMatcher.Api --launch-profile http
```

A confident match:

```sh
curl -s -X POST http://localhost:5235/queries/classify \
  -H "Content-Type: application/json" \
  -d '{"query": "where is the virtual card?"}'
```
```json
{
  "query": "where is the virtual card?",
  "normalizedQuery": "where is the virtual card?",
  "embeddingSource": "stored",
  "ambiguous": false,
  "matches": [
    {
      "rank": 1,
      "intentId": "getting_virtual_card",
      "intentName": "Getting virtual card",
      "similarity": 0.9143095506540406
    },
    {
      "rank": 2,
      "intentId": "virtual_card_not_working",
      "intentName": "Virtual card not working",
      "similarity": 0.7808828053695734
    }
  ]
}
```

An ambiguous match (`"ambiguous": true`, same query as the CLI example above;
`query`/`normalizedQuery`/rank 3 omitted below, same shape as the first example):

```sh
curl -s -X POST http://localhost:5235/queries/classify \
  -H "Content-Type: application/json" \
  -d '{"query": "why did my cash get charged a fee that should not be there."}'
```
```json
{
  "ambiguous": true,
  "matches": [
    {
      "rank": 1,
      "intentId": "cash_withdrawal_charge",
      "similarity": 0.8155662705736807
    },
    {
      "rank": 2,
      "intentId": "card_payment_fee_charged",
      "similarity": 0.8034169081821354
    }
  ]
}
```

An empty query - `InvalidQueryException` maps to a 400 `ValidationProblem`:

```sh
curl -s -i -X POST http://localhost:5235/queries/classify \
  -H "Content-Type: application/json" \
  -d '{"query": ""}'
```
```
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "query": ["Query is empty."] }
}
```

A new query with no OpenAI key configured - `EmbeddingUnavailableException`
maps to a 503, with `detail` explaining why ("Query is not in the dataset and
no OpenAI API key is configured to embed it."):

```sh
curl -s -i -X POST http://localhost:5235/queries/classify \
  -H "Content-Type: application/json" \
  -d '{"query": "a brand new question never seen before"}'
```
```
HTTP/1.1 503 Service Unavailable
Content-Type: application/problem+json

{ "title": "Service Unavailable", "status": 503, "detail": "..." }
```

Both `ProblemDetails` responses above (and any unexpected failure, e.g. Postgres
unreachable) go through one `app.UseExceptionHandler()` + `IExceptionHandler`
(`ClassifyQueryExceptionHandler`), not a per-endpoint try/catch - see
`tests/IntentMatcher.IntegrationTests/ClassifyEndpointTests.cs` for all four
cases run against a real pipeline. OpenAPI document at `/openapi/v1.json`
(Development); sample requests in `src/IntentMatcher.Api/IntentMatcher.Api.http`.
Set `Database:AutoMigrate=true` to apply migrations on startup.

## Settings

`appsettings.json` in each entry point: `ConnectionStrings:Postgres`, `OpenAi:ApiKey` (leave empty), `OpenAi:Model`.
Precedence: appsettings < user-secrets < environment variables (`OpenAi__ApiKey`, `ConnectionStrings__Postgres`);
plain `OPENAI_API_KEY` is a fallback for the key. Never commit a key.
`IntentMatcher.Api` and `IntentMatcher.Cli` share the same `UserSecretsId`, so setting the key
once via either project's user-secrets (as in `Run` above) covers both.

## Tests

```sh
dotnet test IntentMatcher.slnx     # integration tests need Docker running
```

## Formatting

```sh
dotnet tool restore          # installs CSharpier (.config/dotnet-tools.json)
dotnet csharpier format .    # formats the solution
dotnet csharpier check .     # verifies formatting without changing files
```
