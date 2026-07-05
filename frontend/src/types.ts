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
