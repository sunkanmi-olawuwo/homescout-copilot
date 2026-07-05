import type { MortgageEstimateRequest } from './types';

// Static content for the workspace shell (design placeholders until wired to real data).

export interface SavedComparison {
  name: string;
  meta: string;
  age: string;
  active: boolean;
}

export interface StartPrompt {
  title: string;
  body: string;
  opensEstimator?: boolean;
  upload?: boolean;
}

export const savedComparisons: SavedComparison[] = [
  { name: 'Greenwich vs Croydon', meta: 'Commute · schools · monthly cost', age: 'edited just now · 2 areas', active: true },
  { name: 'Reading family homes', meta: 'Parks · rail · price context', age: '3 days ago · 3 areas', active: false },
  { name: 'Canary Wharf flats', meta: 'Service charge · commute', age: '1 week ago · 2 areas', active: false },
];

export const startPrompts: StartPrompt[] = [
  { title: 'Compare SE10 vs CR0', body: '2-bed flat, on commute, schools, parks, crime context & monthly cost' },
  { title: 'What would this cost me monthly?', body: 'Ownership cost on your own rate, with a +3% stress test', opensEstimator: true },
  { title: 'What should I ask at the viewing?', body: 'Questions worth asking for each area, grounded in the data' },
  { title: 'Upload a listing, EPC or survey', body: 'Extract facts to feed the comparison', upload: true },
];

export const initialRequest: MortgageEstimateRequest = {
  propertyPrice: 465_000,
  deposit: 92_500,
  annualInterestRatePercent: 5.1,
  termYears: 25,
  repaymentType: 'Repayment',
};
