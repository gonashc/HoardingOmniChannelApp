# Observability setup

Hoardly ships with **OpenTelemetry** (traces + metrics + logs) and **Sentry** (errors) wired into both the .NET API and the Angular frontend. Both work with any OTLP-compatible backend and any Sentry org. Recommended free-tier stack:

| Pillar | Backend | Free quota |
|---|---|---|
| Errors | Sentry.io | 5k events/month |
| Traces | Grafana Cloud Tempo | 50 GB/month |
| Metrics | Grafana Cloud Mimir | 10k series |
| Logs | Grafana Cloud Loki | 50 GB/month |

All five SDKs are no-ops if their respective env vars are unset, so you can ignore observability entirely until you're ready.

---

## 1. Sentry (errors)

Sign up at <https://sentry.io>, create a project (.NET for the API, Angular for the frontend). Each project gives you a **DSN** — a URL containing a public key.

### Backend (.NET)

Set the DSN as an env var before starting the API container, or paste it into `appsettings.json` under `Sentry:Dsn`.

```powershell
# Windows PowerShell
$env:SENTRY_DSN = "https://abc123@o0.ingest.sentry.io/123456"
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

```bash
# macOS / Linux
export SENTRY_DSN="https://abc123@o0.ingest.sentry.io/123456"
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

To verify, hit any endpoint that throws (e.g. force a 500 via an invalid booking). Within 30 seconds the event will appear in Sentry with the full stack trace and the last 20 log lines as breadcrumbs.

### Frontend (Angular)

Edit `frontend/src/environments/environment.ts` (and `environment.prod.ts` for production builds):

```typescript
export const environment = {
  // ...
  sentryDsn: 'https://abc123@o0.ingest.sentry.io/789012',
};
```

Sentry will capture uncaught JS errors, unhandled promise rejections, and route navigation timings. Session replay is enabled only for errored sessions (free-tier-friendly).

---

## 2. OpenTelemetry (traces + metrics)

Sign up at <https://grafana.com/products/cloud/>. Free tier requires no credit card. From the Cloud portal, get:

1. Your **OTLP endpoint URL** (looks like `https://otlp-gateway-prod-us-east-0.grafana.net/otlp`)
2. Your **instance ID** and **API token**
3. A **base64 string** of `<instance-id>:<token>` — that's your auth header value

### Backend (.NET)

Set these env vars before bringing the API container up:

```powershell
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "https://otlp-gateway-prod-us-east-0.grafana.net/otlp"
$env:OTEL_EXPORTER_OTLP_HEADERS  = "Authorization=Basic <your-base64-string>"
$env:OTEL_EXPORTER_OTLP_PROTOCOL = "http/protobuf"   # gRPC is the default; Grafana Cloud needs HTTP
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

The API instruments:

- Every HTTP request (path, status, duration)
- Every outgoing HttpClient call
- Every EF Core query (with the actual SQL — sensitive data is masked by EF by default)
- Every Npgsql command (via the `Npgsql` ActivitySource, subscribed by name)
- Runtime metrics (GC, thread pool, exceptions/sec)

Within a minute of starting traffic, head to **Grafana Cloud → Explore → Tempo** and search for service `hoardly-api`.

### Frontend (Angular)

Edit `environment.ts`:

```typescript
export const environment = {
  // ...
  otelCollectorUrl: 'https://otlp-gateway-prod-us-east-0.grafana.net/otlp/v1/traces',
  otelHeaders: { Authorization: 'Basic <your-base64-string>' },
};
```

The frontend instruments document load, route changes, and every `fetch`/`XHR` call. Because the browser propagates the W3C `traceparent` header on outgoing API calls and the .NET API picks it up automatically, **a single trace covers the user's click → frontend route change → API request → DB query → response**.

> ⚠️ CORS note: Grafana Cloud's OTLP endpoint supports CORS, but if you front it with your own collector make sure to add `Access-Control-Allow-Origin` for your web origin.

---

## 3. What you should build first

Once data is flowing, build two dashboards in Grafana:

**Dashboard 1 — Platform health**

- Request rate by endpoint (top 10)
- p50 / p95 / p99 latency
- Error rate %
- DB connection pool wait time
- .NET GC pause time
- Active bookings created per hour

**Dashboard 2 — Marketplace metrics**

- Inventory views per channel (hoarding vs influencer)
- Quote requests per channel
- Booking conversion rate (quotes → confirmed bookings)
- Median time from cart → checkout
- Search latency by filter complexity

The first dashboard is "is the platform up". The second is "is the business growing". You'll need the second one to make pricing and feature decisions; both are trivial to build once OTel metrics are flowing.

---

## 4. Alerts worth setting up

In Sentry: alert on any new issue type and any spike > 10/min.

In Grafana: alert on p95 latency > 1s for 5 min, on 5xx rate > 1% for 2 min, on DB connection pool saturation > 80%, and on no-data conditions (which usually mean the API is down). Slack or email destinations both work for free.

---

## 5. Future: opening the trace data to other backends

Because everything goes through OTLP, swapping Grafana Cloud for Honeycomb, Datadog, New Relic, or a self-hosted Tempo/Jaeger is just an env var change. The code does not change. The same applies if you ever decide to run a [local OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) for log scrubbing, sampling, or routing to multiple destinations.
