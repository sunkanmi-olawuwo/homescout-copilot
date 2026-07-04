import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';

const estimateBody = {
  loan: 372500,
  ltvPercent: 80.1,
  monthlyPayment: 2204.42,
  totalRepayment: 661326,
  totalInterest: 288826,
  stressTest: {
    ratePercent: 8.1,
    monthlyPayment: 2924.82,
  },
  assumptions: [
    'Repayment mortgage at 5.1% over 25 years.',
    'Payments are monthly and on time.',
  ],
  caveats: [
    'This is an estimate, not mortgage advice - speak to a qualified mortgage adviser.',
  ],
};

const baseRateBody = {
  ratePercent: 4.25,
  effectiveDate: '2026-06-18',
  provenance: 'Live',
  source: 'Bank of England',
  note: 'Bank Rate is provided for orientation only.',
};

function jsonResponse(body: unknown) {
  return {
    ok: true,
    status: 200,
    statusText: 'OK',
    json: () => Promise.resolve(body),
  } as Response;
}

describe('App mortgage workspace', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input);

        if (url.endsWith('/api/mortgage/base-rate')) {
          return Promise.resolve(jsonResponse(baseRateBody));
        }

        if (url.endsWith('/api/mortgage/estimate')) {
          return Promise.resolve(jsonResponse(estimateBody));
        }

        return Promise.reject(new Error(`Unexpected request: ${url}`));
      }),
    );
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('renders the app shell and API-backed mortgage result', async () => {
    render(<App />);

    expect(screen.getByLabelText('HomeScout Copilot')).toBeTruthy();
    expect(
      screen.getByRole('button', { name: 'Mortgage cost estimator' }),
    ).toBeTruthy();
    expect(
      screen.getByRole('heading', { name: 'Mortgage cost estimator' }),
    ).toBeTruthy();

    expect((await screen.findAllByText('£2,204.42')).length).toBeGreaterThan(0);
    expect(screen.getByText('£372,500')).toBeTruthy();
    expect(screen.getAllByText('80.1%').length).toBeGreaterThan(0);
    expect(screen.getByText('BoE base rate 4.25%')).toBeTruthy();
    expect(screen.getAllByText('Live').length).toBeGreaterThan(0);
    expect(screen.getByText(/not mortgage advice/i)).toBeTruthy();
  });

  it('posts the typed mortgage estimate request to the API seam', async () => {
    const fetchMock = vi.mocked(fetch);
    render(<App />);

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/mortgage/estimate' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).repaymentType === 'Repayment' &&
            JSON.parse(String(init.body)).propertyPrice === 465000,
        ),
      ).toBe(true);
    });

    const interestOnly = screen.getByRole('button', { name: 'Interest-only' });
    fireEvent.click(interestOnly);

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/mortgage/estimate' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).repaymentType === 'InterestOnly',
        ),
      ).toBe(true);
    });
  });

  it('updates running-cost assumptions without changing the API contract', async () => {
    render(<App />);

    await screen.findAllByText('£2,204.42');
    expect(screen.getByText('£2,980.42 / mo')).toBeTruthy();

    fireEvent.change(screen.getByLabelText('Service charge'), {
      target: { value: '200' },
    });

    expect(screen.getByText('£2,995.42 / mo')).toBeTruthy();
    expect(screen.getByText('Buyer inputs and HomeScout defaults')).toBeTruthy();
  });

  it('supports the scoped light and dark themes', () => {
    const { container } = render(<App />);

    const app = container.querySelector('.homescout-app');

    expect(app?.getAttribute('data-theme')).toBe('light');

    fireEvent.click(screen.getByLabelText('Switch to dark theme'));

    expect(app?.getAttribute('data-theme')).toBe('dark');
  });
});
