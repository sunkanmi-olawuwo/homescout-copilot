import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';
import { AuthContext, anonymousAuth, type AuthState } from './auth/authContext';

const baseRateBody = { ratePercent: 4.25, effectiveDate: '2026-06-18', provenance: 'Live', source: 'Bank of England', note: 'context' };
const estimateBody = {
  loan: 372500, ltvPercent: 80.1, monthlyPayment: 2204.42, totalRepayment: 661326, totalInterest: 288826,
  stressTest: { ratePercent: 8.1, monthlyPayment: 2924.82 }, assumptions: ['a'], caveats: ['c'],
};
const meBody = { userId: 'u-1', subject: 'sub-1', email: 'dev@homescout.local', name: 'Dev User' };
const historyBody = {
  conversations: [{ sessionId: 'sess-1', createdAt: '2026-07-01T10:00:00Z', lastActiveAt: '2026-07-05T09:00:00Z' }],
};

function json(body: unknown) {
  return { ok: true, status: 200, statusText: 'OK', json: () => Promise.resolve(body) } as Response;
}

interface Call { url: string; init?: RequestInit }

function stubFetch(records: Call[]) {
  vi.stubGlobal(
    'fetch',
    vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      records.push({ url, init });
      if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(json(baseRateBody));
      if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(json(estimateBody));
      if (url.endsWith('/api/me')) return Promise.resolve(json(meBody));
      if (url.endsWith('/api/copilot/history')) return Promise.resolve(json(historyBody));
      if (url.includes('/api/copilot/session/resume/')) return Promise.resolve({ ok: true, status: 204, statusText: 'No Content', json: () => Promise.resolve({}) } as Response);
      if (url.endsWith('/api/copilot/ask')) return Promise.resolve(json({ text: 'ok', toolCalls: [], evidence: [], assumptions: [], caveats: [] }));
      return Promise.reject(new Error(`Unexpected request: ${url}`));
    }),
  );
}

const signedIn: AuthState = {
  authEnabled: true,
  isReady: true,
  isAuthenticated: true,
  userName: 'Dev User',
  accessToken: 'test-token',
  signIn: vi.fn(),
  signOut: vi.fn(),
};

function renderWithAuth(auth: AuthState) {
  return render(<AuthContext.Provider value={auth}><App /></AuthContext.Provider>);
}

function authHeaderOf(call: Call | undefined): string | undefined {
  return (call?.init?.headers as Record<string, string> | undefined)?.Authorization;
}

describe('auth + per-user history', () => {
  let records: Call[];
  beforeEach(() => { records = []; stubFetch(records); });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it('shows the signed-in identity and a sign out control', async () => {
    renderWithAuth(signedIn);

    expect(await screen.findByText('Dev User')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Sign in' })).toBeNull();
  });

  it('lists the user history and attaches the bearer token on authenticated calls', async () => {
    renderWithAuth(signedIn);

    expect(await screen.findByText('Your conversations')).toBeTruthy();
    await waitFor(() => expect(screen.getAllByText('Conversation').length).toBeGreaterThan(0));

    expect(authHeaderOf(records.find((c) => c.url.endsWith('/api/me')))).toBe('Bearer test-token');
    expect(authHeaderOf(records.find((c) => c.url.endsWith('/api/copilot/history')))).toBe('Bearer test-token');
  });

  it('reopens a conversation through the resume endpoint', async () => {
    renderWithAuth(signedIn);

    const item = await screen.findByRole('button', { name: /Conversation/ });
    fireEvent.click(item);

    await waitFor(() => expect(records.some((c) => c.url.includes('/api/copilot/session/resume/sess-1'))).toBe(true));
    expect(await screen.findByText(/Reopened your saved conversation/)).toBeTruthy();
  });

  it('shows a sign in button when auth is enabled but the user is anonymous', async () => {
    renderWithAuth({ ...anonymousAuth, authEnabled: true });

    expect(await screen.findByRole('button', { name: 'Sign in' })).toBeTruthy();
    expect(screen.queryByText('Your conversations')).toBeNull();
    // No token is fetched for /api/me while anonymous.
    expect(records.some((c) => c.url.endsWith('/api/me'))).toBe(false);
  });

  it('disables sign in until auth is ready, so the first click always redirects', async () => {
    // Auth enabled but not yet ready (OIDC metadata still warming).
    renderWithAuth({ ...anonymousAuth, authEnabled: true, isReady: false });

    const button = await screen.findByRole('button', { name: 'Loading…' });
    expect((button as HTMLButtonElement).disabled).toBe(true);
    expect(screen.queryByRole('button', { name: 'Sign in' })).toBeNull();
  });
});
