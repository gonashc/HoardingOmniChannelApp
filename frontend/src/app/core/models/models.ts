// ============================================================
// Channel-aware inventory models
// ============================================================

export type ChannelType =
  | 'Hoarding' | 'Influencer' | 'Radio' | 'Print'
  | 'DoohNetwork' | 'Podcast' | 'Tv' | 'Cinema';

export interface HoardingListExt {
  type: string;
  illumination: string;
  cityName?: string;
  areaName?: string;
  latitude: number;
  longitude: number;
  widthFt: number;
  heightFt: number;
  visibilityScore?: number;
}

export interface InfluencerListExt {
  platform: string;          // 'Instagram', 'Youtube', ...
  handle: string;
  isPlatformVerified: boolean;
  followerCount: number;
  engagementRate?: number;
  tier: string;              // 'Nano' | 'Micro' | 'Mid' | 'Macro' | 'Mega' | 'Celebrity'
  categories: string[];
  languages: string[];
  audienceCityName?: string;
}

export interface InventoryListItem {
  id: string;
  code: string;
  channel: ChannelType;
  name: string;
  description?: string;
  basePriceMonthly?: number;
  basePricePerUnit?: number;
  unitLabel?: string;
  currency: string;
  estimatedReachDaily?: number;
  estimatedImpressionsDaily?: number;
  avgRating?: number;
  ratingCount: number;
  coverImageUrl?: string;
  isAvailable: boolean;
  hoarding?: HoardingListExt;
  influencer?: InfluencerListExt;
}

export interface HoardingDetailExt {
  type: string;
  illumination: string;
  location: {
    areaId?: number;
    areaName?: string;
    cityId?: number;
    cityName?: string;
    stateName?: string;
    address?: string;
    landmark?: string;
    latitude: number;
    longitude: number;
    pincode?: string;
  };
  widthFt: number;
  heightFt: number;
  facingDirection?: string;
  visibilityScore?: number;
  panorama360Url?: string;
  streetViewUrl?: string;
  printingCost: number;
  installationCost: number;
}

export interface InfluencerDetailExt {
  platform: string;
  handle: string;
  profileUrl?: string;
  isPlatformVerified: boolean;
  followerCount: number;
  avgViewsPerPost?: number;
  avgLikesPerPost?: number;
  engagementRate?: number;
  metricsVerifiedAt?: string;
  tier: string;
  contentCategories: string[];
  languages: string[];
  audienceGeoSplitJson?: string;
  audienceAgeSplitJson?: string;
  audienceGenderSplitJson?: string;
  deliverablePricingJson?: string;
  availableDeliverables: string[];
  excludesCategories: string[];
  requiresPaidPartnershipTag: boolean;
  typicalTurnaroundDays: number;
  sampleWorkUrlsJson?: string;
}

export interface InventoryDetail {
  id: string;
  code: string;
  channel: ChannelType;
  name: string;
  description?: string;
  status: string;
  pricing: {
    monthlyRate?: number;
    weeklyRate?: number;
    dailyRate?: number;
    pricePerUnit?: number;
    unitLabel?: string;
    setupCost: number;
    gstPercentage: number;
    currency: string;
  };
  estimatedReachDaily?: number;
  estimatedImpressionsDaily?: number;
  audienceProfileJson?: string;
  avgRating?: number;
  ratingCount: number;
  bookingCount: number;
  images: { url: string; caption?: string; isPrimary: boolean; order: number }[];
  coverImageUrl?: string;
  hoarding?: HoardingDetailExt;
  influencer?: InfluencerDetailExt;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface QuoteResponse {
  inventoryUnitId: string;
  channel: string;
  startDate: string;
  endDate: string;
  durationDays: number;
  baseAmount: number;
  setupCost: number;
  subtotal: number;
  gstAmount: number;
  totalAmount: number;
  currency: string;
  breakdown: string;
}

// ============================================================
// Search criteria
// ============================================================
export interface InventorySearchCriteria {
  channel?: string;
  query?: string;
  // hoarding
  cityId?: number;
  areaId?: number;
  pincode?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
  hoardingTypes?: string[];
  minWidth?: number;
  maxWidth?: number;
  minTraffic?: number;
  illuminationType?: string;
  // influencer
  platforms?: string[];
  tiers?: string[];
  minFollowers?: number;
  maxFollowers?: number;
  minEngagementRate?: number;
  categories?: string[];
  languages?: string[];
  audienceCityId?: number;
  platformVerifiedOnly?: boolean;
  requiredDeliverables?: string[];
  excludesCategory?: string;
  // shared
  availableFrom?: string;
  availableTo?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

// ============================================================
// Auth, booking, cart
// ============================================================
export type UserRoleType = 'Admin' | 'Advertiser' | 'MediaPlanner' | 'MediaOwner' | 'Creator' | 'FieldAgent';

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: UserRoleType;
  phone?: string;
}

/**
 * Mirrors the backend AuthResult record:
 * (Guid UserId, string Email, string FullName, string Role, string Token)
 * Serialized as camelCase by the API.
 */
export interface AuthResult {
  userId: string;
  email: string;
  fullName: string;
  role: UserRoleType;
  token: string;
}

export interface RegisterPayload {
  email: string;
  phone?: string;
  password: string;
  fullName: string;
  role: UserRoleType;
  companyName?: string;
  gstin?: string;
}

export interface CartLineItem {
  inventoryUnit: InventoryListItem;
  startDate: string;
  endDate: string;
  deliverableSpec?: { deliverable: string; quantity: number };
  quotedPrice?: number;
}

export interface CreateBookingRequest {
  inventoryUnitId: string;
  campaignId?: string;
  startDate: string;
  endDate: string;
  deliverableSpecJson?: string;
  discountPct?: number;
  includeSetupCost?: boolean;
}

export interface CreateBookingResult {
  bookingId: string;
  bookingRef: string;
  channel: string;
  totalAmount: number;
  currency: string;
  status: string;
}
