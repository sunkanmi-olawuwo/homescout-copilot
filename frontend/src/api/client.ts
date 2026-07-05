import type { BaseRate, CopilotAnswer, MortgageEstimateRequest, MortgageEstimateResult } from '../types';

// Thin HomeScout API client. Endpoints are same-origin (Vite proxies to the API in dev, and the API
// serves the built SPA in prod). Callers own UI state; this centralises the fetch mechanics.

export async function readJson<T>(url: string, options?: RequestInit, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { ...options, signal });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }
  return (await response.json()) as T;
}

export function fetchBaseRate(signal?: AbortSignal): Promise<BaseRate> {
  return readJson<BaseRate>('/api/mortgage/base-rate', undefined, signal);
}

export function fetchMortgageEstimate(
  request: MortgageEstimateRequest,
  signal?: AbortSignal,
): Promise<MortgageEstimateResult> {
  return readJson<MortgageEstimateResult>(
    '/api/mortgage/estimate',
    { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request) },
    signal,
  );
}

/**
 * Asks the copilot. Returns the HTTP status alongside the parsed answer (when 200) so the caller can
 * map 503 / other failures to the right notice. Network failures reject (the caller catches).
 */
export async function askCopilot(message: string): Promise<{ status: number; answer: CopilotAnswer | null }> {
  const response = await fetch('/api/copilot/ask', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message }),
  });
  if (!response.ok) {
    return { status: response.status, answer: null };
  }
  const answer = (await response.json()) as CopilotAnswer;
  return { status: response.status, answer };
}

export async function resetSession(): Promise<void> {
  const response = await fetch('/api/copilot/session/reset', { method: 'POST', credentials: 'same-origin' });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }
}
