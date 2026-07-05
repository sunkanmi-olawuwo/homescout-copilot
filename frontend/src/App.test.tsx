import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';

const estimateBody = {
  loan: 372500,
  ltvPercent: 80.1,
  monthlyPayment: 2204.42,
  totalRepayment: 661326,
  totalInterest: 288826,
  stressTest: { ratePercent: 8.1, monthlyPayment: 2924.82 },
  assumptions: ['Repayment mortgage at 5.1% over 25 years.', 'Payments are monthly and on time.'],
  caveats: ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'],
};

const baseRateBody = {
  ratePercent: 4.25,
  effectiveDate: '2026-06-18',
  provenance: 'Live',
  source: 'Bank of England',
  note: 'Bank Rate is provided for orientation only.',
};

const copilotAnswer = {
  text: [
    '## Greenwich cost context',
    '',
    'For this Greenwich flat, the **estimated monthly repayment** is £2,204.42 and the `loan-to-value` is 80.1%.',
    '',
    '- Treat the base rate as context, not a product recommendation.',
    '- Review the [Bank of England](https://www.bankofengland.co.uk) source.',
    '- Ignore [unsafe](javascript:alert(1)) <script>alert("x")</script>.',
  ].join('\n'),
  toolCalls: [
    { name: 'estimate_mortgage', summary: 'Calculated repayment mortgage costs.' },
    { name: 'get_base_rate', summary: 'Checked Bank of England context.' },
  ],
  evidence: [
    {
      label: 'Monthly mortgage payment',
      value: '£2,204.42',
      kind: 'estimate',
      source: '/api/mortgage/estimate',
      provenance: 'Live',
    },
    {
      label: 'BoE base rate',
      value: '4.25%',
      kind: 'fact',
      source: 'Bank of England',
      provenance: 'Cache',
    },
  ],
  assumptions: ['Repayment mortgage at 5.1% over 25 years.'],
  caveats: ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'],
};

const copilotAnswerWithMarkdownCaveat = {
  ...copilotAnswer,
  text: [
    '**Estimated monthly repayment: £2,204.42**',
    '',
    '## Next steps',
    '- Check the assumptions before relying on this.',
    '',
    'This is an estimate, not mortgage advice — speak to a qualified mortgage adviser.',
  ].join('\n'),
  caveats: [],
};

function jsonResponse(body: unknown) {
  return { ok: true, status: 200, statusText: 'OK', json: () => Promise.resolve(body) } as Response;
}
function statusResponse(status: number) {
  return { ok: false, status, statusText: '', json: () => Promise.resolve({}) } as Response;
}
function emptyResponse(status = 204) {
  return { ok: status >= 200 && status < 300, status, statusText: 'No Content', json: () => Promise.resolve({}) } as Response;
}

describe('App workspace', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: RequestInfo | URL) => {
        const url = String(input);
        if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
        if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
        if (url.endsWith('/api/copilot/ask')) return Promise.resolve(statusResponse(503));
        if (url.endsWith('/api/copilot/session/reset')) return Promise.resolve(emptyResponse());
        return Promise.reject(new Error(`Unexpected request: ${url}`));
      }),
    );
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it('leads with the copilot conversation as the main surface', () => {
    render(<App />);

    expect(screen.getByLabelText('HomeScout Copilot')).toBeTruthy();
    expect(screen.getByRole('button', { name: /Greenwich vs Croydon/ })).toBeTruthy();
    expect(screen.getByRole('heading', { name: /Compare areas and properties/i })).toBeTruthy();
    // Estimator lives in the right rail, not the main surface.
    expect(screen.getByRole('tab', { name: 'Estimator' })).toBeTruthy();
    expect(screen.getByText(/not mortgage advice/i)).toBeTruthy();
  });

  it('shows the API-backed estimator in the right rail', async () => {
    render(<App />);

    fireEvent.click(screen.getByRole('tab', { name: 'Estimator' }));

    expect((await screen.findAllByText('£2,204.42')).length).toBeGreaterThan(0);
    expect(screen.getByText('£372,500')).toBeTruthy();
    expect(screen.getByText('80.1%')).toBeTruthy();
    expect(screen.getByText(/BoE base rate 4.25%/)).toBeTruthy();
    expect(screen.getAllByText('Live').length).toBeGreaterThan(0);
    // Total repayable is now surfaced (design parity), plus the +3% stress payment.
    expect(screen.getByText('Total repayable')).toBeTruthy();
    expect(screen.getByText('+3% stress payment')).toBeTruthy();
  });

  it('posts the typed mortgage estimate request (string enum) to the seam', async () => {
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

    fireEvent.click(screen.getByRole('tab', { name: 'Estimator' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Interest-only' }));

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

  it('degrades gracefully when the copilot is not provisioned (503)', async () => {
    render(<App />);

    fireEvent.change(screen.getByLabelText('Ask HomeScout'), { target: { value: 'Compare SE10 vs CR0' } });
    fireEvent.submit(screen.getByLabelText('Ask HomeScout').closest('form') as HTMLFormElement);

    expect(await screen.findByText(/isn.t connected yet/i)).toBeTruthy();
  });

  it('posts composer questions to the copilot seam and renders answer evidence', async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
      if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
      if (url.endsWith('/api/copilot/ask')) return Promise.resolve(jsonResponse(copilotAnswer));
      if (url.endsWith('/api/copilot/session/reset')) return Promise.resolve(emptyResponse());
      return Promise.reject(new Error(`Unexpected request: ${url}`));
    });

    const { container } = render(<App />);

    fireEvent.change(screen.getByLabelText('Ask HomeScout'), { target: { value: 'What would this cost monthly?' } });
    fireEvent.submit(screen.getByLabelText('Ask HomeScout').closest('form') as HTMLFormElement);

    expect(await screen.findByRole('heading', { name: 'Greenwich cost context' })).toBeTruthy();
    expect(screen.getByText('estimated monthly repayment')).toBeTruthy();
    expect(screen.getByText('loan-to-value')).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Bank of England' }).getAttribute('href')).toBe('https://www.bankofengland.co.uk');
    expect(screen.queryByRole('link', { name: 'unsafe' })).toBeNull();
    expect(container.querySelector('script')).toBeNull();
    expect(container.querySelector('.conversation.active')).toBeTruthy();
    expect(screen.getByText('estimate_mortgage')).toBeTruthy();
    expect(screen.getByText('Calculated repayment mortgage costs.')).toBeTruthy();
    expect(screen.getByText('Evidence trail')).toBeTruthy();
    expect(screen.getByText('Monthly mortgage payment')).toBeTruthy();
    expect(screen.getByText('/api/mortgage/estimate')).toBeTruthy();
    expect(screen.getAllByText('estimate').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Cache').length).toBeGreaterThan(0);

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/copilot/ask' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).message === 'What would this cost monthly?',
        ),
      ).toBe(true);
    });
  });

  it('promotes a trailing markdown caveat into the caveat callout when structured caveats are empty', async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
      if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
      if (url.endsWith('/api/copilot/ask')) return Promise.resolve(jsonResponse(copilotAnswerWithMarkdownCaveat));
      if (url.endsWith('/api/copilot/session/reset')) return Promise.resolve(emptyResponse());
      return Promise.reject(new Error(`Unexpected request: ${url}`));
    });

    const { container } = render(<App />);

    fireEvent.change(screen.getByLabelText('Ask HomeScout'), { target: { value: 'Estimate this listing' } });
    fireEvent.submit(screen.getByLabelText('Ask HomeScout').closest('form') as HTMLFormElement);

    expect(await screen.findByRole('heading', { name: 'Next steps' })).toBeTruthy();
    const caveat = await screen.findByText('This is an estimate, not mortgage advice — speak to a qualified mortgage adviser.');
    expect(caveat.closest('.answer-caveats')).toBeTruthy();
    expect(container.querySelector('.answer-markdown')?.textContent).not.toContain('not mortgage advice');
  });

  it('starts a new conversation through the session reset endpoint and clears visible state', async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
      if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
      if (url.endsWith('/api/copilot/ask')) return Promise.resolve(jsonResponse(copilotAnswer));
      if (url.endsWith('/api/copilot/session/reset')) return Promise.resolve(emptyResponse());
      return Promise.reject(new Error(`Unexpected request: ${url}`));
    });

    const { container } = render(<App />);

    fireEvent.change(screen.getByLabelText('Ask HomeScout'), { target: { value: 'What would this cost monthly?' } });
    fireEvent.submit(screen.getByLabelText('Ask HomeScout').closest('form') as HTMLFormElement);

    expect(await screen.findByRole('heading', { name: 'Greenwich cost context' })).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name: 'New conversation' }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) => String(url) === '/api/copilot/session/reset' && init?.method === 'POST',
        ),
      ).toBe(true);
    });

    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Greenwich cost context' })).toBeNull());
    expect(screen.getByRole('heading', { name: /Compare areas and properties/i })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'New conversation' })).toBeNull();
    expect(screen.getByText('Evidence appears here')).toBeTruthy();
    expect(container.querySelector('.conversation.active')).toBeNull();
  });

  it('posts start-with cards to the copilot seam', async () => {
    const fetchMock = vi.mocked(fetch);
    fetchMock.mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith('/api/mortgage/base-rate')) return Promise.resolve(jsonResponse(baseRateBody));
      if (url.endsWith('/api/mortgage/estimate')) return Promise.resolve(jsonResponse(estimateBody));
      if (url.endsWith('/api/copilot/ask')) return Promise.resolve(jsonResponse(copilotAnswer));
      if (url.endsWith('/api/copilot/session/reset')) return Promise.resolve(emptyResponse());
      return Promise.reject(new Error(`Unexpected request: ${url}`));
    });

    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: /Compare SE10 vs CR0/i }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(
          ([url, init]) =>
            String(url) === '/api/copilot/ask' &&
            init?.method === 'POST' &&
            JSON.parse(String(init.body)).message.startsWith('Compare SE10 vs CR0.'),
        ),
      ).toBe(true);
    });
  });

  it('supports the scoped light and dark themes', () => {
    const { container } = render(<App />);
    const app = container.querySelector('.homescout-app');

    expect(app?.getAttribute('data-theme')).toBe('light');
    fireEvent.click(screen.getByLabelText('Switch to dark theme'));
    expect(app?.getAttribute('data-theme')).toBe('dark');
  });
});
