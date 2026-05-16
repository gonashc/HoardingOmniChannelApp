-- ============================================================
-- Seed Data - India Market (multi-channel)
-- ============================================================

-- States & cities
INSERT INTO states (name, code) VALUES
('Maharashtra', 'MH'), ('Delhi', 'DL'), ('Karnataka', 'KA'),
('Tamil Nadu', 'TN'), ('Gujarat', 'GJ'), ('Telangana', 'TS'),
('West Bengal', 'WB'), ('Rajasthan', 'RJ'), ('Uttar Pradesh', 'UP'),
('Punjab', 'PB'), ('Kerala', 'KL'), ('Haryana', 'HR');

INSERT INTO cities (state_id, name, tier, centroid) VALUES
(1, 'Mumbai', 1, ST_MakePoint(72.8777, 19.0760)::geography),
(1, 'Pune', 1, ST_MakePoint(73.8567, 18.5204)::geography),
(2, 'New Delhi', 1, ST_MakePoint(77.2090, 28.6139)::geography),
(12, 'Gurugram', 1, ST_MakePoint(77.0266, 28.4595)::geography),
(3, 'Bengaluru', 1, ST_MakePoint(77.5946, 12.9716)::geography),
(4, 'Chennai', 1, ST_MakePoint(80.2707, 13.0827)::geography),
(5, 'Ahmedabad', 1, ST_MakePoint(72.5714, 23.0225)::geography),
(6, 'Hyderabad', 1, ST_MakePoint(78.4867, 17.3850)::geography),
(7, 'Kolkata', 1, ST_MakePoint(88.3639, 22.5726)::geography),
(8, 'Jaipur', 1, ST_MakePoint(75.7873, 26.9124)::geography),
(11, 'Kochi', 2, ST_MakePoint(76.2673, 9.9312)::geography);

-- Areas (Mumbai + Bengaluru)
INSERT INTO areas (city_id, name, pincode, centroid) VALUES
(1, 'Bandra West', '400050', ST_MakePoint(72.8295, 19.0596)::geography),
(1, 'Andheri East', '400069', ST_MakePoint(72.8697, 19.1136)::geography),
(1, 'Worli', '400018', ST_MakePoint(72.8147, 19.0176)::geography),
(1, 'Powai', '400076', ST_MakePoint(72.9089, 19.1197)::geography),
(5, 'Koramangala', '560034', ST_MakePoint(77.6309, 12.9352)::geography),
(5, 'Indiranagar', '560038', ST_MakePoint(77.6408, 12.9784)::geography);

-- Demo users
INSERT INTO users (email, full_name, role, password_hash, email_verified) VALUES
('admin@hoardly.in',     'Platform Admin',      'admin',         '$2a$11$placeholder', TRUE),
('demo@advertiser.in',   'Demo Advertiser',     'advertiser',    '$2a$11$placeholder', TRUE),
('owner@mediaco.in',     'Demo Media Owner',    'media_owner',   '$2a$11$placeholder', TRUE),
('priya@creators.in',    'Priya Krishnan',      'creator',       '$2a$11$placeholder', TRUE),
('rahul@creators.in',    'Rahul Verma',         'creator',       '$2a$11$placeholder', TRUE);

-- ============================================================
-- INVENTORY: HOARDINGS (3 samples)
-- ============================================================
DO $$
DECLARE
    v_owner UUID;
    v_unit UUID;
BEGIN
    SELECT id INTO v_owner FROM users WHERE email = 'owner@mediaco.in';

    -- Hoarding 1: Bandra Linking Road
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_monthly, base_price_weekly, base_price_daily, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, images)
    VALUES ('MUM-BIL-001', 'hoarding', 'Bandra Linking Road Premium Billboard',
        'High-visibility billboard on Mumbai''s busiest shopping street.', 'active', v_owner,
        450000.00, 120000.00, 18000.00, 40000.00, 18.00,
        85000, 250000, 12, 1240,
        '[{"url":"https://picsum.photos/seed/MUM-BIL-001/800/600","is_primary":true,"order":1}]'::jsonb)
    RETURNING id INTO v_unit;

    INSERT INTO hoarding_specs (inventory_unit_id, type, illumination, area_id,
        address, landmark, location, width_ft, height_ft, facing_direction,
        visibility_score, printing_cost, installation_cost)
    VALUES (v_unit, 'billboard', 'backlit', 1,
        'Linking Road, Near National College', 'Opposite Mehboob Studios',
        ST_MakePoint(72.8295, 19.0596)::geography, 40, 20, 'Towards Khar Station',
        9, 25000.00, 15000.00);

    -- Hoarding 2: Andheri Metro
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_monthly, base_price_weekly, base_price_daily, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, images)
    VALUES ('MUM-UNI-002', 'hoarding', 'Andheri East Metro Junction Unipole',
        'Strategic unipole at the WEH metro interchange.', 'active', v_owner,
        380000.00, 100000.00, 15000.00, 32000.00, 18.00,
        120000, 350000, 8, 980,
        '[{"url":"https://picsum.photos/seed/MUM-UNI-002/800/600","is_primary":true,"order":1}]'::jsonb)
    RETURNING id INTO v_unit;

    INSERT INTO hoarding_specs (inventory_unit_id, type, illumination, area_id,
        address, landmark, location, width_ft, height_ft, facing_direction,
        visibility_score, printing_cost, installation_cost)
    VALUES (v_unit, 'unipole', 'frontlit', 2,
        'WEH Metro Station, Andheri East', 'Near Andheri Metro Station',
        ST_MakePoint(72.8697, 19.1136)::geography, 30, 20, 'Towards Western Express Highway',
        9, 20000.00, 12000.00);

    -- Hoarding 3: Bengaluru DOOH
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_monthly, base_price_weekly, base_price_daily, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, images)
    VALUES ('BLR-DOH-003', 'hoarding', 'Indiranagar 100ft Road Digital Hoarding',
        'Premium digital OOH on Bengaluru''s tech corridor.', 'active', v_owner,
        520000.00, 140000.00, 22000.00, 8000.00, 18.00,
        65000, 195000, 15, 1820,
        '[{"url":"https://picsum.photos/seed/BLR-DOH-003/800/600","is_primary":true,"order":1}]'::jsonb)
    RETURNING id INTO v_unit;

    INSERT INTO hoarding_specs (inventory_unit_id, type, illumination, area_id,
        address, landmark, location, width_ft, height_ft, facing_direction,
        visibility_score, printing_cost, installation_cost)
    VALUES (v_unit, 'dooh', 'digital', 6,
        '100ft Road, Indiranagar', 'Near CMH Hospital',
        ST_MakePoint(77.6408, 12.9784)::geography, 20, 10, 'Towards Domlur',
        8, 0, 8000.00);
END $$;

-- ============================================================
-- INVENTORY: INFLUENCERS (3 samples)
-- ============================================================
DO $$
DECLARE
    v_priya UUID;
    v_rahul UUID;
    v_unit  UUID;
BEGIN
    SELECT id INTO v_priya FROM users WHERE email = 'priya@creators.in';
    SELECT id INTO v_rahul FROM users WHERE email = 'rahul@creators.in';

    -- Influencer 1: Priya - food creator (Mumbai)
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_per_unit, unit_label, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, cover_image_url)
    VALUES ('IG-FOOD-001', 'influencer', 'Priya Krishnan · Food & Travel',
        'Mumbai-based food creator. Known for restaurant reviews, recipes, and travel vlogs.',
        'active', v_priya,
        50000.00, 'reel', 0, 18.00,
        180000, 420000, 24, 3200,
        'https://picsum.photos/seed/priyaeats/600/600')
    RETURNING id INTO v_unit;

    INSERT INTO influencer_specs (inventory_unit_id, primary_platform, handle, profile_url,
        is_platform_verified, follower_count, avg_views_per_post, avg_likes_per_post, avg_comments_per_post,
        engagement_rate, tier, content_categories, languages, primary_audience_city_id,
        audience_geo_split, audience_age_split, audience_gender_split,
        deliverable_pricing, available_deliverables, excludes_categories, typical_turnaround_days)
    VALUES (v_unit, 'instagram', '@priyaeats', 'https://instagram.com/priyaeats',
        TRUE, 180000, 420000, 18500, 240,
        4.85, 'mid', ARRAY['food','travel','lifestyle'], ARRAY['en','hi'], 1,
        '{"IN":0.85,"AE":0.06,"US":0.04,"GB":0.03,"OTHER":0.02}'::jsonb,
        '{"18-24":0.32,"25-34":0.45,"35-44":0.18,"45+":0.05}'::jsonb,
        '{"f":0.62,"m":0.37,"other":0.01}'::jsonb,
        '{"post":35000,"story":12000,"reel":50000,"integration":85000}'::jsonb,
        ARRAY['post','story','reel','integration']::deliverable_type[],
        ARRAY['alcohol','tobacco','gambling','crypto'], 5);

    -- Influencer 2: Rahul - tech creator (Bengaluru)
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_per_unit, unit_label, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, cover_image_url)
    VALUES ('YT-TECH-002', 'influencer', 'Rahul Verma · Tech Reviews',
        'Bengaluru-based tech reviewer. In-depth gadget reviews, comparisons, and tutorials.',
        'active', v_rahul,
        180000.00, 'video', 0, 18.00,
        850000, 1200000, 18, 4150,
        'https://picsum.photos/seed/rahultech/600/600')
    RETURNING id INTO v_unit;

    INSERT INTO influencer_specs (inventory_unit_id, primary_platform, handle, profile_url,
        is_platform_verified, follower_count, avg_views_per_post, avg_likes_per_post, avg_comments_per_post,
        engagement_rate, tier, content_categories, languages, primary_audience_city_id,
        audience_geo_split, audience_age_split, audience_gender_split,
        deliverable_pricing, available_deliverables, excludes_categories, typical_turnaround_days)
    VALUES (v_unit, 'youtube', '@rahultech', 'https://youtube.com/@rahultech',
        TRUE, 850000, 1200000, 45000, 1800,
        3.95, 'macro', ARRAY['technology','gadgets','reviews'], ARRAY['en','hi'], 5,
        '{"IN":0.78,"US":0.08,"GB":0.04,"AE":0.03,"OTHER":0.07}'::jsonb,
        '{"18-24":0.41,"25-34":0.38,"35-44":0.15,"45+":0.06}'::jsonb,
        '{"m":0.81,"f":0.18,"other":0.01}'::jsonb,
        '{"video":180000,"short":45000,"integration":120000}'::jsonb,
        ARRAY['video','short','integration']::deliverable_type[],
        ARRAY['gambling','crypto'], 10);

    -- Influencer 3: Priya - secondary lifestyle profile
    INSERT INTO inventory_units (code, channel, name, description, status, owner_id,
        base_price_per_unit, unit_label, setup_cost, gst_percentage,
        estimated_reach_daily, estimated_impressions_daily, booking_count, view_count, cover_image_url)
    VALUES ('IG-LIFE-003', 'influencer', 'Aarya Iyer · Beauty & Wellness',
        'Bengaluru-based beauty creator. Skincare, makeup tutorials, and wellness routines.',
        'active', v_priya,                                                          -- shared owner for demo
        25000.00, 'reel', 0, 18.00,
        65000, 145000, 8, 920, 'https://picsum.photos/seed/aaryaglow/600/600')
    RETURNING id INTO v_unit;

    INSERT INTO influencer_specs (inventory_unit_id, primary_platform, handle, profile_url,
        is_platform_verified, follower_count, avg_views_per_post, avg_likes_per_post, avg_comments_per_post,
        engagement_rate, tier, content_categories, languages, primary_audience_city_id,
        audience_geo_split, audience_age_split, audience_gender_split,
        deliverable_pricing, available_deliverables, excludes_categories, typical_turnaround_days)
    VALUES (v_unit, 'instagram', '@aaryaglow', 'https://instagram.com/aaryaglow',
        FALSE, 65000, 145000, 8200, 180,
        6.40, 'micro', ARRAY['beauty','skincare','wellness'], ARRAY['en'], 5,
        '{"IN":0.92,"OTHER":0.08}'::jsonb,
        '{"18-24":0.55,"25-34":0.32,"35-44":0.10,"45+":0.03}'::jsonb,
        '{"f":0.84,"m":0.15,"other":0.01}'::jsonb,
        '{"post":15000,"story":5000,"reel":25000}'::jsonb,
        ARRAY['post','story','reel']::deliverable_type[],
        ARRAY['alcohol','tobacco','gambling'], 7);
END $$;
