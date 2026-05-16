-- ============================================================
-- Hoardly Platform - Channel-Agnostic Schema v2
-- Supports: hoardings, influencers, and future channels
-- (radio, print, DOOH networks, podcasts, TV, cinema)
-- ============================================================

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ============================================================
-- ENUMS
-- ============================================================
CREATE TYPE channel_type AS ENUM (
    'hoarding', 'influencer', 'radio', 'print',
    'dooh_network', 'podcast', 'tv', 'cinema'
);

CREATE TYPE user_role AS ENUM (
    'admin', 'advertiser', 'media_planner',
    'media_owner', 'creator', 'field_agent'
);

CREATE TYPE hoarding_type AS ENUM ('billboard', 'unipole', 'gantry', 'bus_shelter', 'metro_pillar', 'dooh', 'wall_mount', 'other');
CREATE TYPE illumination_type AS ENUM ('frontlit', 'backlit', 'digital', 'non_lit');

CREATE TYPE social_platform AS ENUM ('instagram', 'youtube', 'twitter_x', 'linkedin', 'tiktok', 'facebook', 'threads');
CREATE TYPE creator_tier AS ENUM ('nano', 'micro', 'mid', 'macro', 'mega', 'celebrity');
CREATE TYPE deliverable_type AS ENUM ('post', 'story', 'reel', 'short', 'video', 'live', 'tweet', 'thread', 'integration');

CREATE TYPE listing_status AS ENUM ('draft', 'pending_approval', 'active', 'inactive', 'rejected');
CREATE TYPE booking_status AS ENUM ('cart', 'pending_payment', 'pending_approval', 'confirmed', 'active', 'completed', 'cancelled');
CREATE TYPE campaign_status AS ENUM ('draft', 'scheduled', 'active', 'paused', 'completed', 'cancelled');
CREATE TYPE creative_status AS ENUM ('uploaded', 'under_review', 'approved', 'rejected', 'print_ready', 'published');
CREATE TYPE payment_status AS ENUM ('pending', 'partial', 'paid', 'refunded', 'failed');
CREATE TYPE payout_status AS ENUM ('pending', 'processing', 'paid', 'on_hold');
CREATE TYPE enquiry_status AS ENUM ('new', 'in_progress', 'quoted', 'won', 'lost');

-- ============================================================
-- USERS & PROFILES (UC-33, UC-34, UC-35)
-- ============================================================
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email           VARCHAR(255) UNIQUE NOT NULL,
    phone           VARCHAR(20) UNIQUE,
    password_hash   VARCHAR(255),
    full_name       VARCHAR(255) NOT NULL,
    role            user_role NOT NULL DEFAULT 'advertiser',
    auth_provider   VARCHAR(50) DEFAULT 'local',
    auth_provider_id VARCHAR(255),
    email_verified  BOOLEAN DEFAULT FALSE,
    phone_verified  BOOLEAN DEFAULT FALSE,
    is_active       BOOLEAN DEFAULT TRUE,
    last_login_at   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);

CREATE TABLE user_profiles (
    user_id             UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    company_name        VARCHAR(255),
    gstin               VARCHAR(20),
    pan_number          VARCHAR(10),
    billing_address     TEXT,
    billing_city        VARCHAR(100),
    billing_state       VARCHAR(100),
    billing_pincode     VARCHAR(10),
    profile_image_url   TEXT,
    bio                 TEXT,
    website             VARCHAR(255),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- LOCATIONS
-- ============================================================
CREATE TABLE states (
    id      SERIAL PRIMARY KEY,
    name    VARCHAR(100) NOT NULL,
    code    VARCHAR(5) UNIQUE NOT NULL
);

CREATE TABLE cities (
    id          SERIAL PRIMARY KEY,
    state_id    INT REFERENCES states(id),
    name        VARCHAR(100) NOT NULL,
    tier        SMALLINT DEFAULT 1,
    centroid    GEOGRAPHY(POINT, 4326)
);
CREATE INDEX idx_cities_name_trgm ON cities USING GIN(name gin_trgm_ops);
CREATE INDEX idx_cities_state ON cities(state_id);

CREATE TABLE areas (
    id          SERIAL PRIMARY KEY,
    city_id     INT REFERENCES cities(id),
    name        VARCHAR(150) NOT NULL,
    pincode     VARCHAR(10),
    centroid    GEOGRAPHY(POINT, 4326)
);
CREATE INDEX idx_areas_city ON areas(city_id);
CREATE INDEX idx_areas_pincode ON areas(pincode);

-- ============================================================
-- INVENTORY UNITS (CHANNEL-AGNOSTIC CORE)
-- ============================================================
CREATE TABLE inventory_units (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code                VARCHAR(50) UNIQUE NOT NULL,
    channel             channel_type NOT NULL,
    name                VARCHAR(255) NOT NULL,
    description         TEXT,
    status              listing_status DEFAULT 'pending_approval',

    owner_id            UUID REFERENCES users(id),

    -- Pricing
    base_price_monthly  DECIMAL(12,2),
    base_price_weekly   DECIMAL(12,2),
    base_price_daily    DECIMAL(12,2),
    base_price_per_unit DECIMAL(12,2),
    unit_label          VARCHAR(50),
    setup_cost          DECIMAL(10,2) DEFAULT 0,
    gst_percentage      DECIMAL(5,2) DEFAULT 18.00,
    currency            VARCHAR(3) DEFAULT 'INR',

    -- Audience signals
    estimated_reach_daily       BIGINT,
    estimated_impressions_daily BIGINT,
    audience_profile            JSONB DEFAULT '{}',

    booking_count       INT DEFAULT 0,
    view_count          INT DEFAULT 0,
    avg_rating          DECIMAL(3,2),
    rating_count        INT DEFAULT 0,

    images              JSONB DEFAULT '[]',
    cover_image_url     TEXT,

    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_inv_channel ON inventory_units(channel);
CREATE INDEX idx_inv_status ON inventory_units(status);
CREATE INDEX idx_inv_owner ON inventory_units(owner_id);
CREATE INDEX idx_inv_price ON inventory_units(base_price_monthly);
CREATE INDEX idx_inv_trending ON inventory_units(booking_count DESC, view_count DESC);
CREATE INDEX idx_inv_name_trgm ON inventory_units USING GIN(name gin_trgm_ops);

-- ============================================================
-- HOARDING SPECS (extension table)
-- ============================================================
CREATE TABLE hoarding_specs (
    inventory_unit_id   UUID PRIMARY KEY REFERENCES inventory_units(id) ON DELETE CASCADE,
    type                hoarding_type NOT NULL,
    illumination        illumination_type DEFAULT 'frontlit',

    area_id             INT REFERENCES areas(id),
    address             TEXT,
    landmark            VARCHAR(255),
    location            GEOGRAPHY(POINT, 4326) NOT NULL,
    google_place_id     VARCHAR(255),
    street_view_url     TEXT,

    width_ft            DECIMAL(8,2) NOT NULL,
    height_ft           DECIMAL(8,2) NOT NULL,
    facing_direction    VARCHAR(50),

    visibility_score    SMALLINT CHECK (visibility_score BETWEEN 1 AND 10),
    panorama_360_url    TEXT,
    past_brand_logos    JSONB DEFAULT '[]',
    printing_cost       DECIMAL(10,2) DEFAULT 0,
    installation_cost   DECIMAL(10,2) DEFAULT 0
);
CREATE INDEX idx_hoarding_location ON hoarding_specs USING GIST(location);
CREATE INDEX idx_hoarding_area ON hoarding_specs(area_id);
CREATE INDEX idx_hoarding_type ON hoarding_specs(type);
CREATE INDEX idx_hoarding_size ON hoarding_specs(width_ft, height_ft);

-- ============================================================
-- INFLUENCER SPECS (extension table)
-- ============================================================
CREATE TABLE influencer_specs (
    inventory_unit_id           UUID PRIMARY KEY REFERENCES inventory_units(id) ON DELETE CASCADE,

    primary_platform            social_platform NOT NULL,
    handle                      VARCHAR(255) NOT NULL,
    profile_url                 TEXT,
    is_platform_verified        BOOLEAN DEFAULT FALSE,
    platform_user_id            VARCHAR(255),

    follower_count              BIGINT NOT NULL,
    avg_views_per_post          BIGINT,
    avg_likes_per_post          BIGINT,
    avg_comments_per_post       BIGINT,
    engagement_rate             DECIMAL(5,2),
    metrics_verified_at         TIMESTAMPTZ,

    tier                        creator_tier NOT NULL,
    content_categories          TEXT[],
    languages                   TEXT[],

    primary_audience_city_id    INT REFERENCES cities(id),
    audience_geo_split          JSONB DEFAULT '{}',
    audience_age_split          JSONB DEFAULT '{}',
    audience_gender_split       JSONB DEFAULT '{}',

    deliverable_pricing         JSONB DEFAULT '{}',
    available_deliverables      deliverable_type[],

    accepts_categories          TEXT[],
    excludes_categories         TEXT[],
    requires_paid_partnership_tag BOOLEAN DEFAULT TRUE,
    typical_turnaround_days     INT DEFAULT 7,

    sample_work_urls            JSONB DEFAULT '[]'
);
CREATE INDEX idx_inf_platform ON influencer_specs(primary_platform);
CREATE INDEX idx_inf_tier ON influencer_specs(tier);
CREATE INDEX idx_inf_followers ON influencer_specs(follower_count);
CREATE INDEX idx_inf_engagement ON influencer_specs(engagement_rate);
CREATE INDEX idx_inf_city ON influencer_specs(primary_audience_city_id);
CREATE INDEX idx_inf_categories ON influencer_specs USING GIN(content_categories);
CREATE INDEX idx_inf_handle ON influencer_specs(handle);

-- ============================================================
-- AVAILABILITY (channel-agnostic)
-- ============================================================
CREATE TABLE availability_blocks (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    inventory_unit_id   UUID REFERENCES inventory_units(id) ON DELETE CASCADE,
    start_date          DATE NOT NULL,
    end_date            DATE NOT NULL,
    block_type          VARCHAR(50) NOT NULL,
    reason              VARCHAR(500),
    blocked_by          UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT no_block_overlap EXCLUDE USING GIST (
        inventory_unit_id WITH =,
        daterange(start_date, end_date, '[]') WITH &&
    )
);
CREATE INDEX idx_avail_unit_date ON availability_blocks(inventory_unit_id, start_date, end_date);

CREATE TABLE seasonal_pricing (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    inventory_unit_id   UUID REFERENCES inventory_units(id) ON DELETE CASCADE,
    season_name         VARCHAR(100),
    start_date          DATE NOT NULL,
    end_date            DATE NOT NULL,
    price_multiplier    DECIMAL(4,2) DEFAULT 1.00,
    flat_price          DECIMAL(12,2),
    created_at          TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_season_unit ON seasonal_pricing(inventory_unit_id, start_date, end_date);

-- ============================================================
-- WISHLIST / COMPARE / MEDIA PLAN (channel-agnostic)
-- ============================================================
CREATE TABLE wishlists (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID REFERENCES users(id) ON DELETE CASCADE,
    inventory_unit_id   UUID REFERENCES inventory_units(id) ON DELETE CASCADE,
    notes               TEXT,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, inventory_unit_id)
);
CREATE INDEX idx_wishlist_user ON wishlists(user_id);

CREATE TABLE compare_lists (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                 UUID REFERENCES users(id) ON DELETE CASCADE,
    inventory_unit_ids      UUID[] NOT NULL,
    created_at              TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE media_plans (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID REFERENCES users(id) ON DELETE CASCADE,
    name            VARCHAR(255) DEFAULT 'My Media Plan',
    is_active_cart  BOOLEAN DEFAULT TRUE,
    share_token     VARCHAR(64) UNIQUE,
    is_shared       BOOLEAN DEFAULT FALSE,
    notes           TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_media_plans_user ON media_plans(user_id);

CREATE TABLE media_plan_items (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    media_plan_id       UUID REFERENCES media_plans(id) ON DELETE CASCADE,
    inventory_unit_id   UUID REFERENCES inventory_units(id),
    start_date          DATE,
    end_date            DATE,
    deliverable_spec    JSONB,
    quoted_price        DECIMAL(12,2),
    quoted_at           TIMESTAMPTZ DEFAULT NOW(),
    notes               TEXT,
    UNIQUE(media_plan_id, inventory_unit_id)
);
CREATE INDEX idx_plan_items_plan ON media_plan_items(media_plan_id);

-- ============================================================
-- CAMPAIGNS & BOOKINGS (channel-agnostic)
-- ============================================================
CREATE TABLE campaigns (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code            VARCHAR(50) UNIQUE NOT NULL,
    name            VARCHAR(255) NOT NULL,
    advertiser_id   UUID REFERENCES users(id),
    planner_id      UUID REFERENCES users(id),
    status          campaign_status DEFAULT 'draft',
    objective       VARCHAR(255),
    brief           TEXT,
    target_channels channel_type[],
    budget          DECIMAL(14,2),
    currency        VARCHAR(3) DEFAULT 'INR',
    start_date      DATE,
    end_date        DATE,
    notes           TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_campaigns_advertiser ON campaigns(advertiser_id);
CREATE INDEX idx_campaigns_status ON campaigns(status);

CREATE TABLE bookings (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_ref         VARCHAR(20) UNIQUE NOT NULL,
    campaign_id         UUID REFERENCES campaigns(id),
    inventory_unit_id   UUID REFERENCES inventory_units(id),
    channel             channel_type NOT NULL,
    booked_by           UUID REFERENCES users(id),
    status              booking_status DEFAULT 'cart',

    start_date          DATE NOT NULL,
    end_date            DATE NOT NULL,

    deliverable_spec    JSONB,

    base_price          DECIMAL(12,2) NOT NULL,
    setup_cost          DECIMAL(10,2) DEFAULT 0,
    discount_pct        DECIMAL(5,2) DEFAULT 0,
    discount_amount     DECIMAL(12,2) DEFAULT 0,
    subtotal            DECIMAL(12,2) NOT NULL,
    gst_percentage      DECIMAL(5,2) DEFAULT 18.00,
    gst_amount          DECIMAL(12,2),
    total_amount        DECIMAL(12,2) NOT NULL,
    currency            VARCHAR(3) DEFAULT 'INR',

    approved_by         UUID REFERENCES users(id),
    approved_at         TIMESTAMPTZ,
    rejection_reason    TEXT,
    cancelled_at        TIMESTAMPTZ,
    cancellation_reason TEXT,

    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT no_booking_overlap EXCLUDE USING GIST (
        inventory_unit_id WITH =,
        daterange(start_date, end_date, '[]') WITH &&
    ) WHERE (status IN ('confirmed', 'active'))
);
CREATE INDEX idx_bookings_unit ON bookings(inventory_unit_id, start_date, end_date);
CREATE INDEX idx_bookings_channel ON bookings(channel);
CREATE INDEX idx_bookings_campaign ON bookings(campaign_id);
CREATE INDEX idx_bookings_user ON bookings(booked_by);
CREATE INDEX idx_bookings_status ON bookings(status);

-- ============================================================
-- CREATIVE ASSETS, PAYMENTS, PROOFS, PAYOUTS, REVIEWS
-- (channel-agnostic with polymorphic fields where needed)
-- ============================================================
CREATE TABLE creative_assets (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id          UUID REFERENCES bookings(id) ON DELETE CASCADE,
    campaign_id         UUID REFERENCES campaigns(id),
    uploaded_by         UUID REFERENCES users(id),
    status              creative_status DEFAULT 'uploaded',
    asset_kind          VARCHAR(50),

    file_name           VARCHAR(500),
    file_url            TEXT,
    file_size_kb        INT,
    mime_type           VARCHAR(100),
    width_px            INT,
    height_px           INT,

    mockup_preview_url  TEXT,
    text_content        TEXT,

    reviewed_by         UUID REFERENCES users(id),
    reviewed_at         TIMESTAMPTZ,
    review_notes        TEXT,

    version             INT DEFAULT 1,
    is_current          BOOLEAN DEFAULT TRUE,
    created_at          TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_creative_booking ON creative_assets(booking_id);
CREATE INDEX idx_creative_status ON creative_assets(status);

CREATE TABLE payments (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id          UUID REFERENCES bookings(id),
    amount              DECIMAL(12,2) NOT NULL,
    currency            VARCHAR(3) DEFAULT 'INR',
    status              payment_status DEFAULT 'pending',
    gateway             VARCHAR(50),
    gateway_order_id    VARCHAR(255),
    gateway_payment_id  VARCHAR(255),
    gateway_signature   VARCHAR(500),
    payment_method      VARCHAR(50),
    payment_date        TIMESTAMPTZ,
    invoice_number      VARCHAR(50),
    invoice_url         TEXT,
    refund_amount       DECIMAL(12,2),
    refund_date         TIMESTAMPTZ,
    notes               TEXT,
    created_at          TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_payments_booking ON payments(booking_id);
CREATE INDEX idx_payments_status ON payments(status);

CREATE TABLE proof_of_delivery (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id          UUID REFERENCES bookings(id),
    channel             channel_type NOT NULL,
    submitted_by        UUID REFERENCES users(id),
    proof_kind          VARCHAR(50) NOT NULL,           -- geo_photo, post_url, analytics_export, broadcast_log

    photo_urls          JSONB DEFAULT '[]',
    geo_location        GEOGRAPHY(POINT, 4326),
    location_accuracy_m DECIMAL(8,2),

    post_url            TEXT,
    post_id_external    VARCHAR(255),
    posted_at           TIMESTAMPTZ,
    metrics_snapshot    JSONB,

    captured_at         TIMESTAMPTZ,
    notes               TEXT,
    advertiser_acknowledged     BOOLEAN DEFAULT FALSE,
    advertiser_acknowledged_at  TIMESTAMPTZ,
    created_at          TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_pod_booking ON proof_of_delivery(booking_id);
CREATE INDEX idx_pod_channel ON proof_of_delivery(channel);

CREATE TABLE vendor_payouts (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    vendor_id       UUID REFERENCES users(id),
    booking_id      UUID REFERENCES bookings(id),
    channel         channel_type NOT NULL,
    gross_amount    DECIMAL(12,2) NOT NULL,
    platform_fee    DECIMAL(12,2) DEFAULT 0,
    tds_amount      DECIMAL(12,2) DEFAULT 0,
    net_payout      DECIMAL(12,2) NOT NULL,
    status          payout_status DEFAULT 'pending',
    payout_date     TIMESTAMPTZ,
    transaction_ref VARCHAR(255),
    notes           TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_payout_vendor ON vendor_payouts(vendor_id);

CREATE TABLE reviews (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id          UUID REFERENCES bookings(id),
    inventory_unit_id   UUID REFERENCES inventory_units(id),
    vendor_id           UUID REFERENCES users(id),
    reviewer_id         UUID REFERENCES users(id),
    rating              SMALLINT CHECK (rating BETWEEN 1 AND 5),
    title               VARCHAR(255),
    comment             TEXT,
    is_verified         BOOLEAN DEFAULT FALSE,
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(booking_id, reviewer_id)
);
CREATE INDEX idx_reviews_unit ON reviews(inventory_unit_id);

-- ============================================================
-- ENQUIRIES, NOTIFICATIONS, METRICS, CONTENT
-- ============================================================
CREATE TABLE enquiries (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name                VARCHAR(255) NOT NULL,
    email               VARCHAR(255),
    phone               VARCHAR(20),
    company             VARCHAR(255),
    inventory_unit_id   UUID REFERENCES inventory_units(id),
    channel             channel_type,
    city_id             INT REFERENCES cities(id),
    budget              DECIMAL(12,2),
    duration_days       INT,
    message             TEXT,
    source              VARCHAR(50),
    status              enquiry_status DEFAULT 'new',
    assigned_to         UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_enquiries_status ON enquiries(status);

CREATE TABLE notifications (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID REFERENCES users(id) ON DELETE CASCADE,
    type            VARCHAR(100) NOT NULL,
    title           VARCHAR(255),
    body            TEXT,
    data            JSONB DEFAULT '{}',
    channel         VARCHAR(50),
    delivered       BOOLEAN DEFAULT FALSE,
    delivered_at    TIMESTAMPTZ,
    read            BOOLEAN DEFAULT FALSE,
    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_notif_user_unread ON notifications(user_id, read, created_at DESC);

CREATE TABLE campaign_metrics (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id              UUID REFERENCES bookings(id),
    metric_date             DATE NOT NULL,
    estimated_impressions   BIGINT,
    estimated_reach         BIGINT,
    actual_impressions      BIGINT,
    actual_reach            BIGINT,
    actual_engagement       BIGINT,
    spend                   DECIMAL(12,2),
    notes                   TEXT,
    created_at              TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(booking_id, metric_date)
);
CREATE INDEX idx_metrics_booking ON campaign_metrics(booking_id);

CREATE TABLE case_studies (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title           VARCHAR(255) NOT NULL,
    brand_name      VARCHAR(255),
    industry        VARCHAR(100),
    campaign_id     UUID REFERENCES campaigns(id),
    channels        channel_type[],
    summary         TEXT,
    full_content    TEXT,
    cover_image_url TEXT,
    metrics         JSONB DEFAULT '{}',
    is_published    BOOLEAN DEFAULT FALSE,
    published_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE blog_posts (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    slug            VARCHAR(255) UNIQUE NOT NULL,
    title           VARCHAR(255) NOT NULL,
    excerpt         TEXT,
    content_html    TEXT,
    cover_image_url TEXT,
    author_id       UUID REFERENCES users(id),
    tags            TEXT[],
    is_published    BOOLEAN DEFAULT FALSE,
    published_at    TIMESTAMPTZ,
    view_count      INT DEFAULT 0,
    seo_title       VARCHAR(255),
    seo_description VARCHAR(500),
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_blog_slug ON blog_posts(slug);

CREATE TABLE promotions (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code                VARCHAR(50) UNIQUE NOT NULL,
    name                VARCHAR(255) NOT NULL,
    description         TEXT,
    applicable_channels channel_type[],
    discount_type       VARCHAR(20),
    discount_value      DECIMAL(10,2),
    min_order_amount    DECIMAL(12,2),
    max_discount        DECIMAL(10,2),
    valid_from          TIMESTAMPTZ,
    valid_until         TIMESTAMPTZ,
    usage_limit         INT,
    usage_count         INT DEFAULT 0,
    is_active           BOOLEAN DEFAULT TRUE,
    created_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE referrals (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    referrer_id         UUID REFERENCES users(id),
    referee_id          UUID REFERENCES users(id),
    referral_code       VARCHAR(50) NOT NULL,
    status              VARCHAR(50) DEFAULT 'pending',
    reward_amount       DECIMAL(10,2),
    rewarded_at         TIMESTAMPTZ,
    created_at          TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE newsletter_subscriptions (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email               VARCHAR(255) UNIQUE NOT NULL,
    city_ids            INT[],
    channel_interests   channel_type[],
    preferences         JSONB DEFAULT '{}',
    is_active           BOOLEAN DEFAULT TRUE,
    subscribed_at       TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE audit_log (
    id              BIGSERIAL PRIMARY KEY,
    entity_type     VARCHAR(50),
    entity_id       UUID,
    action          VARCHAR(50),
    changed_by      UUID REFERENCES users(id),
    old_values      JSONB,
    new_values      JSONB,
    ip_address      INET,
    user_agent      TEXT,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_audit_entity ON audit_log(entity_type, entity_id);

-- ============================================================
-- VIEWS for convenience and backwards compatibility
-- ============================================================
CREATE VIEW v_hoardings AS
SELECT
    iu.id, iu.code, iu.name, iu.description, iu.status, iu.owner_id,
    iu.base_price_monthly, iu.base_price_weekly, iu.base_price_daily,
    iu.gst_percentage, iu.currency,
    iu.estimated_reach_daily as daily_traffic_count,
    iu.estimated_impressions_daily, iu.audience_profile,
    iu.booking_count, iu.view_count, iu.avg_rating, iu.rating_count,
    iu.images, iu.cover_image_url, iu.created_at, iu.updated_at,
    h.type, h.illumination, h.area_id, h.address, h.landmark,
    h.location, h.google_place_id, h.street_view_url,
    h.width_ft, h.height_ft, h.facing_direction,
    h.visibility_score, h.panorama_360_url, h.past_brand_logos,
    h.printing_cost, h.installation_cost
FROM inventory_units iu
JOIN hoarding_specs h ON h.inventory_unit_id = iu.id
WHERE iu.channel = 'hoarding';

CREATE VIEW v_influencers AS
SELECT
    iu.id, iu.code, iu.name, iu.description, iu.status, iu.owner_id,
    iu.base_price_per_unit, iu.unit_label,
    iu.gst_percentage, iu.currency,
    iu.estimated_reach_daily, iu.estimated_impressions_daily,
    iu.audience_profile,
    iu.booking_count, iu.view_count, iu.avg_rating, iu.rating_count,
    iu.cover_image_url, iu.created_at, iu.updated_at,
    i.primary_platform, i.handle, i.profile_url, i.is_platform_verified,
    i.follower_count, i.avg_views_per_post, i.engagement_rate,
    i.metrics_verified_at, i.tier, i.content_categories, i.languages,
    i.primary_audience_city_id, i.audience_geo_split,
    i.audience_age_split, i.audience_gender_split,
    i.deliverable_pricing, i.available_deliverables,
    i.accepts_categories, i.excludes_categories,
    i.requires_paid_partnership_tag, i.typical_turnaround_days,
    i.sample_work_urls
FROM inventory_units iu
JOIN influencer_specs i ON i.inventory_unit_id = iu.id
WHERE iu.channel = 'influencer';

CREATE OR REPLACE FUNCTION get_available_inventory(
    p_start_date DATE,
    p_end_date DATE,
    p_channel channel_type DEFAULT NULL
) RETURNS SETOF inventory_units AS $$
BEGIN
    RETURN QUERY
    SELECT iu.* FROM inventory_units iu
    WHERE iu.status = 'active'
      AND (p_channel IS NULL OR iu.channel = p_channel)
      AND NOT EXISTS (
          SELECT 1 FROM bookings b
          WHERE b.inventory_unit_id = iu.id
            AND b.status IN ('confirmed', 'active')
            AND daterange(b.start_date, b.end_date, '[]') && daterange(p_start_date, p_end_date, '[]')
      )
      AND NOT EXISTS (
          SELECT 1 FROM availability_blocks ab
          WHERE ab.inventory_unit_id = iu.id
            AND daterange(ab.start_date, ab.end_date, '[]') && daterange(p_start_date, p_end_date, '[]')
      );
END;
$$ LANGUAGE plpgsql;
