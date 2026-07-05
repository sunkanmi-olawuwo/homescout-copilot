// Shared UI + API-contract types for the HomeScout frontend. The interfaces mirror the
// HomeScoutCopilot.Shared DTOs the API returns.

export type Theme = 'light' | 'dark';
export type RepaymentType = 'Repayment' | 'InterestOnly';
export type Provenance = 'Live' | 'Cache' | 'Fallback';
export type FigureKind = 'fact' | 'estimate' | 'assumption' | 'missing';
export type MainTab = 'conversation' | 'comparison';
export type RightTab = 'evidence' | 'estimator';

export interface MortgageEstimateRequest {
  propertyPrice: number;
  deposit: number;
  annualInterestRatePercent: number;
  termYears: number;
  repaymentType: RepaymentType;
}

export interface MortgageStressTest {
  ratePercent: number;
  monthlyPayment: number;
}

export interface MortgageEstimateResult {
  loan: number;
  ltvPercent: number;
  monthlyPayment: number;
  totalRepayment: number | null;
  totalInterest: number;
  stressTest: MortgageStressTest;
  assumptions: string[];
  caveats: string[];
}

export interface BaseRate {
  ratePercent: number;
  effectiveDate: string;
  provenance: Provenance;
  source: string;
  note: string;
}

export interface CopilotToolCall {
  name: string;
  summary: string;
}

export interface EvidenceItem {
  label: string;
  value: string;
  kind: FigureKind;
  source: string;
  provenance: Provenance | null;
}

export interface CopilotAnswer {
  text: string;
  toolCalls: CopilotToolCall[];
  evidence: EvidenceItem[];
  assumptions: string[];
  caveats: string[];
}

// --- Listings: capture + comparison (mirror HomeScoutCopilot.Shared.Contracts) ---

export type ListingMode = 'Buy' | 'Rent';
export type PropertyTenure = 'Freehold' | 'Leasehold' | 'ShareOfFreehold';
export type FloorAreaUnit = 'SquareFeet' | 'SquareMetres';
export type FurnishingState = 'Furnished' | 'PartFurnished' | 'Unfurnished' | 'AtTenantChoice';
export type CouncilTaxBand = 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G' | 'H';
export type PriceQualifier = 'Guide' | 'OffersOver' | 'OffersInRegionOf' | 'FixedPrice' | 'Poa';
export type FieldProvenance = 'Text' | 'Vision' | 'Register' | 'None';
export type FieldConfidence = 'High' | 'Medium' | 'Low';

export interface Listing {
  label: string;
  mode: ListingMode;
  postcode: string;
  price?: number | null;
  monthlyRent?: number | null;
  bedrooms?: number | null;
  floorArea?: number | null;
  areaUnit?: FloorAreaUnit | null;
  tenure?: PropertyTenure | null;
  epcRating?: string | null;
  monthlyCouncilTax?: number | null;
  annualServiceCharge?: number | null;
  furnishing?: FurnishingState | null;
  estimatedMonthlyBills?: number | null;
  sourceUrl?: string | null;
  notes?: string | null;
  councilTaxBand?: CouncilTaxBand | null;
  propertyType?: string | null;
  bathrooms?: number | null;
  receptions?: number | null;
  priceQualifier?: PriceQualifier | null;
  addressLine?: string | null;
}

export interface ListingComparison {
  listing: Listing;
  pricePerSquareFoot: number | null;
  pricePerSquareMetre: number | null;
  indicativeMonthlyCost: number | null;
  completenessPercent: number;
  missingInformation: string[];
  notes: string[];
}

export interface ComparisonResult {
  listings: ListingComparison[];
  highlights: string[];
  assumptions: string[];
  caveats: string[];
}

export interface FieldExtraction {
  field: string;
  source: FieldProvenance;
  confidence: FieldConfidence;
}

export interface ListingExtractionResult {
  draft: Listing;
  fields: FieldExtraction[];
  notes: string[];
}

// --- Auth + per-user history (Keycloak) ---

export interface AuthConfigResponse {
  authEnabled: boolean;
  authority: string | null;
  clientId: string;
  audience: string;
}

export interface MeResponse {
  userId: string | null;
  subject: string;
  email: string | null;
  name: string | null;
}

export interface ConversationSummary {
  sessionId: string;
  createdAt: string;
  lastActiveAt: string;
}

export interface ConversationHistoryResponse {
  conversations: ConversationSummary[];
}
