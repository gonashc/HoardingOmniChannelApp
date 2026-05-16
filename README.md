# Hoardly — Multi-Channel Advertising Marketplace (v2)

A unified India-market marketplace for **out-of-home advertising** (hoardings) **and influencer/creator marketing** in one media plan, one cart, one GST invoice — with the architecture to add radio, print, DOOH networks, podcasts, TV, and cinema later without rewriting the platform.

> **What changed from v1?** v1 was hoardings-only. v2 introduces a channel-agnostic core (`inventory_units` table + per-channel extensions) so the same booking, payment, creative, proof, payout, campaign, and review flows work across any channel.

---

## Why a unified platform

| | Hoardings | Influencers |
|---|---|---|
| Inventory unit | A physical hoarding | A creator profile |
| Capacity | Date range | Post slots / dates |
| Pricing | Monthly / weekly / daily | Per post / story / reel rate card |
| Booking | Date range × hoarding | Post slots × creator |
| Proof | Geo-tagged photo | Post URL + analytics |
| Performance | Daily impressions, traffic | Reach, engagement, CTR |
| Vendor | Media owner | Creator |

The booking → creative approval → go-live → proof → payout workflow is identical. Only the "inventory unit" changes. Hoardly captures both with shared infrastructure.

**Market context** — Indian influencer marketing was ~₹2,300 Cr in 2024 (+25% YoY) vs OOH ~₹4,500 Cr (+8% YoY). A brand planning "Bandra hoardings + 12 Mumbai food creators + 3 metro pillars" can do it in one cart on Hoardly. Neither merahoardings.com, gohoardings.com, nor any influencer platform offers this cleanly today.

---

## Tech stack

- **Frontend:** Angular 17 (standalone components, signals), Tailwind, Refined-fintech aesthetic (Source Serif 4 + Inter Tight, deep indigo accent #4436A8)
- **Backend:** .NET 8, Clean Architecture, CQRS + MediatR, FluentValidation, AutoMapper
- **Database:** PostgreSQL 16 + PostGIS (for hoarding geo-search) + pg_trgm (for text search)
- **Auth:** JWT + BCrypt, OAuth-ready (Google/Facebook)
- **Payments:** Razorpay-ready interface (sandbox)
- **Infra:** Docker Compose for local dev; Azure / AWS managed services for production

---

## Channel-agnostic architecture

```
inventory_units                    ← channel-agnostic core
├── hoarding_specs (1:1)           ← physical hoarding details
├── influencer_specs (1:1)         ← creator profile details
├── radio_specs (future)
├── print_specs (future)
└── ...

bookings        → references inventory_unit_id (works for any channel)
payments        → references booking_id (channel-agnostic)
creative_assets → references booking_id (channel-agnostic)
proof_of_delivery → polymorphic: geo_photo for hoardings, post_url for creators
vendor_payouts  → vendor_id is media_owner OR creator
campaigns       → target_channels[] for multi-channel briefs
```

About **70% of the schema is reused as-is** across channels. Adding a new channel means:

1. Add the channel to the `channel_type` enum
2. Create a `<channel>_specs` extension table with channel-specific fields
3. Implement an `IPricingStrategy` for the new channel's pricing model
4. Add channel-specific filters to `InventorySearchCriteria`
5. Add a frontend filter sidebar variant for that channel

That's it. Bookings, payments, creatives, proofs, payouts, campaigns, and reviews work unchanged.

---

## Project structure

```
hoarding-platform/
├── backend/
│   └── src/
│       ├── Hoarding.Domain/        (Entities, Enums)
│       │   └── Entities/
│       │       ├── InventoryUnit.cs   ← base + HoardingSpec + InfluencerSpec
│       │       ├── Booking.cs         ← references InventoryUnitId
│       │       └── User.cs
│       ├── Hoarding.Application/    (CQRS handlers, interfaces, pricing)
│       │   ├── Common/Interfaces/    ← IInventoryRepository, IPricingStrategy
│       │   ├── Pricing/              ← Hoarding + Influencer pricing strategies
│       │   └── Features/
│       │       ├── Auth/             (UC-33, UC-34)
│       │       ├── Inventory/        (channel-agnostic search, detail, quote)
│       │       └── Bookings/         (channel-agnostic create)
│       ├── Hoarding.Infrastructure/ (EF Core, DbContext, Repositories)
│       └── Hoarding.API/            (REST controllers)
├── frontend/
│   └── src/app/
│       ├── core/
│       │   ├── models/models.ts            ← InventoryListItem, channel ext types
│       │   └── services/inventory.service.ts ← + BookingService, CartService
│       ├── features/
│       │   ├── home/                       ← channel toggle on hero
│       │   ├── inventory/
│       │   │   ├── search.component.ts     ← channel-aware filters
│       │   │   └── detail.component.ts     ← polymorphic by channel
│       │   ├── booking/
│       │   │   ├── cart.component.ts       ← line items per channel
│       │   │   └── checkout.component.ts
│       │   ├── auth/
│       │   └── campaign/
│       └── app.routes.ts                   ← /inventory, /hoardings, /creators, /cart
├── database/
│   ├── migrations/001_initial_schema.sql   ← channel-agnostic schema
│   └── seeds/001_seed_india.sql            ← 3 hoardings + 3 influencers
├── docs/
│   └── Hoarding_Website_UseCases.xlsx      ← 50 use cases mapped
└── docker-compose.yml
```

---

## API endpoints

### Channel-agnostic (recommended)
| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/inventory` | Search any channel. Query params: `channel=Hoarding\|Influencer`, plus channel-specific filters |
| GET | `/api/v1/inventory/{id}` | Detail (response shape varies by channel) |
| GET | `/api/v1/inventory/trending?channel=&cityId=&limit=` | Trending |
| GET | `/api/v1/inventory/{id}/quote?startDate=&endDate=&deliverableSpec=` | Instant quote |

### Convenience routes
| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/hoardings` | Hoardings only (delegates to inventory with `channel=Hoarding`) |
| GET | `/api/v1/influencers` | Influencers only |

### Bookings & auth
| Method | Path | Purpose |
|---|---|---|
| POST | `/api/v1/auth/register` | Register user |
| POST | `/api/v1/auth/login` | Login |
| POST | `/api/v1/bookings` | Create booking. Body: `{inventoryUnitId, startDate, endDate, deliverableSpecJson?}` |

### Search params

**Hoarding-specific:** `cityId`, `areaId`, `pincode`, `latitude+longitude+radiusKm`, `hoardingTypes[]`, `minWidth`, `maxWidth`, `minTraffic`, `illuminationType`

**Influencer-specific:** `platforms[]` (instagram/youtube/twitter_x/...), `tiers[]` (nano/micro/mid/macro/mega/celebrity), `minFollowers`, `maxFollowers`, `minEngagementRate`, `categories[]` (food/tech/beauty/...), `languages[]` (en/hi/ta/...), `audienceCityId`, `platformVerifiedOnly`, `requiredDeliverables[]` (post/story/reel/...), `excludesCategory`

**Shared:** `query`, `availableFrom`, `availableTo`, `minPrice`, `maxPrice`, `sortBy` (popularity/price_asc/price_desc/newest/reach), `page`, `pageSize`

---

## Use cases mapped

50 use cases across 9 categories — most apply to all channels with channel-specific variations:

| Category | Examples |
|---|---|
| **Discovery & Search** | UC-01..09 (works for any channel; filters adapt) |
| **Pricing & Quote** | UC-12, UC-13 (per-channel `IPricingStrategy`) |
| **Cart & Booking** | UC-18..22 (channel-agnostic) |
| **Creative Workflow** | UC-16, UC-26 (artwork for hoardings, brief+caption for creators) |
| **Proof & Payout** | UC-28, UC-31 (geo-photo vs post URL+analytics) |
| **Campaign Management** | UC-24, UC-25 (`target_channels[]` for cross-channel briefs) |
| **User & Auth** | UC-33..35 (creator role added) |
| **Vendor Operations** | UC-30..32 (works for media_owner OR creator) |
| **Content & Marketing** | UC-44..50 (case studies tagged with `channels[]`) |

---

## Quick start

### Prerequisites
- Docker + Docker Compose
- .NET 8 SDK (for local backend dev)
- Node 18+ and pnpm/npm (for local frontend dev)

### Run with Docker
```bash
docker-compose up -d   # Starts Postgres+PostGIS, runs migrations, seeds data
cd backend/src/Hoarding.API && dotnet run
cd frontend && npm install && npm start
```

Open <http://localhost:4200>.

### Don't see seed inventory?

The schema and seed scripts only run on a **fresh** Postgres data volume. If you ran `docker compose up` before and the DB ended up empty, the named `postgres_data` volume is now persisted with an unseeded DB and Postgres skips the init scripts on subsequent starts. Reset with:

```bash
./reset-db.sh            # macOS / Linux / WSL
# or manually:
docker compose -f infrastructure/docker/docker-compose.yml down
docker volume rm $(docker volume ls --format '{{.Name}}' | grep postgres_data)
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

Tail `docker compose logs postgres` and you should see lines like `running /docker-entrypoint-initdb.d/01_schema.sql` and `02_seed.sql`. Once both finish, you'll have 3 hoardings + 3 creators in the DB.

### Demo accounts
| Role | Email | Password |
|---|---|---|
| Admin | admin@hoardly.in | (set on first login) |
| Advertiser | demo@advertiser.in | (set on first login) |
| Media Owner | owner@mediaco.in | (set on first login) |
| Creator | priya@creators.in | (set on first login) |
| Creator | rahul@creators.in | (set on first login) |

> **Note:** Seeded password hashes are placeholders. Re-register or update the hash via the admin tool.

---

## Adding a new channel (e.g. radio)

1. **Schema** — Add `'radio'` to `channel_type` enum, create `radio_specs` table:
   ```sql
   CREATE TABLE radio_specs (
       inventory_unit_id UUID PRIMARY KEY REFERENCES inventory_units(id),
       station_name VARCHAR(255),
       frequency_mhz DECIMAL(5,2),
       broadcast_areas TEXT[],
       slot_durations_seconds INT[],
       prime_time_windows JSONB
   );
   ```

2. **Domain** — Add `RadioSpec` entity, navigation on `InventoryUnit`.

3. **Pricing** — Implement `RadioPricingStrategy : IPricingStrategy` (e.g. by 10s/30s slot × time-of-day multiplier).

4. **Search** — Add radio-specific filters to `InventorySearchCriteria`.

5. **Frontend** — Add a "Radio" tab to channel switcher and a radio-specific filter sidebar.

That's it. Bookings, payments, proofs, etc. work unchanged.

---

## Observability

Hoardly ships with **OpenTelemetry** (traces, metrics, logs) and **Sentry** (errors) wired into both the API and the frontend. Both work with any OTLP-compatible backend (Grafana Cloud, Honeycomb, self-hosted Tempo, etc.) and any Sentry org. All SDKs are no-ops if not configured, so observability is opt-in.

See [`docs/observability.md`](docs/observability.md) for the 10-minute setup with Grafana Cloud free tier + Sentry free tier — gets you errors, traces, metrics, and logs all flowing for $0/month at small scale.

## Roadmap

| Phase | Focus |
|---|---|
| ✅ v1 (Phase 1) | Hoardings MVP — search, map, detail, cart, checkout, auth, dashboard |
| ✅ v2 (this) | **Channel-agnostic refactor + influencer marketplace** |
| Phase 3 | Razorpay live + GST invoice PDF + email/WhatsApp notifications |
| Phase 4 | Creator OAuth (Instagram Graph API, YouTube Data API) for verified metrics |
| Phase 5 | Add radio, print, DOOH network channels |
| Phase 6 | AI brief generator + creator-brand match recommendations |
| Phase 7 | Programmatic DOOH (DSP integration) + dynamic pricing |
s
---

## License

Proprietary — Vedanth Tech Solutions LLC.
