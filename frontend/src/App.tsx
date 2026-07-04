import { useEffect, useState } from 'react';
import './App.css';

type Theme = 'light' | 'dark';
type RepaymentType = 'Repayment' | 'InterestOnly';
type Provenance = 'Live' | 'Cache' | 'Fallback';
type MainTab = 'conversation' | 'comparison';
type RightTab = 'evidence' | 'estimator';

interface MortgageEstimateRequest {
  propertyPrice: number;
  deposit: number;
  annualInterestRatePercent: number;
  termYears: number;
  repaymentType: RepaymentType;
}

interface MortgageStressTest {
  ratePercent: number;
  monthlyPayment: number;
}

interface MortgageEstimateResult {
  loan: number;
  ltvPercent: number;
  monthlyPayment: number;
  totalRepayment: number | null;
  totalInterest: number;
  stressTest: MortgageStressTest;
  assumptions: string[];
  caveats: string[];
}

interface BaseRate {
  ratePercent: number;
  effectiveDate: string;
  provenance: Provenance;
  source: string;
  note: string;
}

const savedComparisons = [
  { name: 'Greenwich vs Croydon', meta: 'Commute · schools · monthly cost', age: 'edited just now · 2 areas', active: true },
  { name: 'Reading family homes', meta: 'Parks · rail · price context', age: '3 days ago · 3 areas', active: false },
  { name: 'Canary Wharf flats', meta: 'Service charge · commute', age: '1 week ago · 2 areas', active: false },
];

const startPrompts = [
  { title: 'Compare SE10 vs CR0', body: '2-bed flat, on commute, schools, parks, crime context & monthly cost' },
  { title: 'What would this cost me monthly?', body: 'Ownership cost on your own rate, with a +3% stress test', opensEstimator: true },
  { title: 'What should I ask at the viewing?', body: 'Questions worth asking for each area, grounded in the data' },
  { title: 'Upload a listing, EPC or survey', body: 'Extract facts to feed the comparison', upload: true },
];

const gbp0 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 });
const gbp2 = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP', minimumFractionDigits: 2, maximumFractionDigits: 2 });
const pct1 = new Intl.NumberFormat('en-GB', { maximumFractionDigits: 1 });

const initialRequest: MortgageEstimateRequest = {
  propertyPrice: 465_000,
  deposit: 92_500,
  annualInterestRatePercent: 5.1,
  termYears: 25,
  repaymentType: 'Repayment',
};

function currency0(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp0.format(value);
}
function currency2(value: number | null | undefined) {
  return value === null || value === undefined || Number.isNaN(value) ? 'Missing' : gbp2.format(value);
}

async function readJson<T>(url: string, options?: RequestInit, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { ...options, signal });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }
  return (await response.json()) as T;
}

function useViewport() {
  const [width, setWidth] = useState(() => (typeof window === 'undefined' ? 1280 : window.innerWidth));
  useEffect(() => {
    const onResize = () => setWidth(window.innerWidth);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return width;
}

function App() {
  const [theme, setTheme] = useState<Theme>('light');
  const [mainTab, setMainTab] = useState<MainTab>('conversation');
  const [rightTab, setRightTab] = useState<RightTab>('evidence');
  const [navOpen, setNavOpen] = useState(false);

  const [request, setRequest] = useState<MortgageEstimateRequest>(initialRequest);
  const [estimate, setEstimate] = useState<MortgageEstimateResult | null>(null);
  const [estimateError, setEstimateError] = useState<string | null>(null);
  const [baseRate, setBaseRate] = useState<BaseRate | null>(null);
  const [copilotNotice, setCopilotNotice] = useState<string | null>(null);

  const viewport = useViewport();
  const isMobile = viewport < 760;

  useEffect(() => {
    const controller = new AbortController();
    void readJson<BaseRate>('/api/mortgage/base-rate', undefined, controller.signal)
      .then((data) => setBaseRate(data))
      .catch(() => {
        if (!controller.signal.aborted) setBaseRate(null);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void readJson<MortgageEstimateResult>(
      '/api/mortgage/estimate',
      { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request) },
      controller.signal,
    )
      .then((data) => {
        setEstimate(data);
        setEstimateError(null);
      })
      .catch((error: Error) => {
        if (controller.signal.aborted) return;
        setEstimate(null);
        setEstimateError(error.message);
      });
    return () => controller.abort();
  }, [request]);

  const depositPercent = request.propertyPrice > 0 ? Math.round((request.deposit / request.propertyPrice) * 100) : 0;

  const openEstimator = () => {
    setRightTab('estimator');
    setCopilotNotice(null);
    if (isMobile) setMainTab('comparison');
  };

  const askCopilot = async (message: string) => {
    if (!message.trim()) return;
    try {
      const response = await fetch('/api/copilot/ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message }),
      });
      if (response.status === 503) {
        setCopilotNotice('HomeScout’s copilot isn’t connected yet — the reasoning agent is still being provisioned. Meanwhile, the Estimator is live: open it from the panel.');
        return;
      }
      if (!response.ok) {
        setCopilotNotice(`Copilot request failed (${response.status}). The Estimator remains available.`);
        return;
      }
      setCopilotNotice('Copilot is connected. Streaming answers arrive in a later iteration.');
    } catch {
      setCopilotNotice('Could not reach the HomeScout API. The Estimator uses the same API and will show its own status.');
    }
  };

  return (
    <div className="homescout-app" data-theme={theme} data-viewport={isMobile ? 'mobile' : 'wide'}>
      <header className="top-bar">
        {isMobile ? (
          <button
            className="icon-button nav-toggle"
            type="button"
            aria-label={navOpen ? 'Close navigation' : 'Open navigation'}
            aria-expanded={navOpen}
            onClick={() => setNavOpen((open) => !open)}
          >
            ☰
          </button>
        ) : null}
        <div className="brand-lockup" aria-label="HomeScout Copilot">
          <span className="brand-mark" aria-hidden="true">H</span>
          <div>
            <strong>HomeScout <span>Copilot</span></strong>
            <small>Greenwich vs Croydon · draft</small>
          </div>
        </div>
        <span className="decision-pill">Decision support, not advice</span>
        <div className="top-actions">
          <button className="toolbar-button" type="button">Search<kbd>⌘K</kbd></button>
          <button
            className="icon-button"
            type="button"
            aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
            title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
            onClick={() => setTheme((current) => (current === 'dark' ? 'light' : 'dark'))}
          >
            {theme === 'dark' ? '☀' : '◐'}
          </button>
          <button className="avatar-button" type="button" aria-label="Account">AO</button>
        </div>
      </header>

      <div className="workspace-grid">
        <aside
          className={`left-rail${navOpen ? ' open' : ''}`}
          aria-label="Saved comparisons and tools"
          aria-hidden={isMobile && !navOpen}
        >
          <button className="new-comparison" type="button">+ New comparison</button>
          <label className="rail-search">
            <span aria-hidden="true">⌕</span>
            <input type="search" placeholder="Filter saved searches" aria-label="Filter saved searches" />
          </label>
          <section aria-labelledby="saved-comparisons-heading">
            <h2 id="saved-comparisons-heading">Saved comparisons</h2>
            <div className="saved-list">
              {savedComparisons.map((comparison) => (
                <button
                  type="button"
                  className={`saved-item${comparison.active ? ' active' : ''}`}
                  key={comparison.name}
                >
                  <strong>{comparison.name}</strong>
                  <span>{comparison.meta}</span>
                  <small>{comparison.age}</small>
                </button>
              ))}
            </div>
          </section>
          <nav className="rail-footer" aria-label="Workspace">
            <button type="button">Case file<span className="badge">2</span></button>
            <button type="button">Preferences<span className="badge">5</span></button>
            <button type="button">Settings</button>
          </nav>
        </aside>
        {isMobile && navOpen ? (
          <button className="rail-scrim" type="button" aria-label="Close navigation" onClick={() => setNavOpen(false)} />
        ) : null}

        <main className="main-workspace" aria-label="HomeScout workspace">
          <div className="workspace-tabs" role="tablist" aria-label="Workspace views">
            <button type="button" role="tab" aria-selected={mainTab === 'conversation'} onClick={() => setMainTab('conversation')}>
              Conversation
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mainTab === 'comparison'}
              onClick={() => {
                setMainTab('comparison');
                if (isMobile) setRightTab('estimator');
              }}
            >
              {isMobile ? 'Estimator' : 'Comparison'}
            </button>
          </div>

          {mainTab === 'conversation' || !isMobile ? (
            <section className="conversation" aria-label="HomeScout copilot">
              <span className="status-pill"><i aria-hidden="true" />Copilot ready · public-data tools connected</span>
              <h1>Compare areas and properties, with the evidence shown.</h1>
              <p className="conversation-lead">
                Ask HomeScout to compare two or three postcodes or listings. It reasons over public data, your
                uploads and saved preferences — and every figure keeps its source, freshness and assumptions.
                This is decision support, never mortgage advice.
              </p>
              <p className="eyebrow">Start with</p>
              <div className="start-grid">
                {startPrompts.map((prompt) => (
                  <button
                    type="button"
                    className={`start-card${prompt.upload ? ' upload' : ''}`}
                    key={prompt.title}
                    onClick={prompt.opensEstimator ? openEstimator : undefined}
                  >
                    <strong>{prompt.title}</strong>
                    <span>{prompt.body}</span>
                  </button>
                ))}
              </div>
              {copilotNotice ? (
                <div className="copilot-notice" role="status">{copilotNotice}</div>
              ) : null}
              <form
                className="composer"
                onSubmit={(event) => {
                  event.preventDefault();
                  const input = event.currentTarget.elements.namedItem('copilot-message') as HTMLInputElement;
                  void askCopilot(input.value);
                  input.value = '';
                }}
              >
                <input
                  name="copilot-message"
                  type="text"
                  aria-label="Ask HomeScout"
                  placeholder="Ask about an area, add a property, or estimate cost…"
                />
                <button type="submit" aria-label="Send">→</button>
              </form>
              <p className="caveat-inline">
                Estimates and public-data summaries — <strong>not mortgage advice</strong>. Speak to a qualified
                adviser before deciding.
              </p>
            </section>
          ) : null}
        </main>

        {(!isMobile || mainTab === 'comparison') ? (
          <aside className="evidence-rail" aria-label="Evidence and estimator">
            <div className="rail-tabs" role="tablist" aria-label="Right panel views">
              <button type="button" role="tab" aria-selected={rightTab === 'evidence'} onClick={() => setRightTab('evidence')}>
                Evidence
              </button>
              <button type="button" role="tab" aria-selected={rightTab === 'estimator'} onClick={() => setRightTab('estimator')}>
                Estimator
              </button>
            </div>

            {rightTab === 'evidence' ? (
              <div className="evidence-empty" role="note">
                <span className="evidence-mark" aria-hidden="true">▤</span>
                <strong>Evidence appears here</strong>
                <p>Ask a question and HomeScout will show every fact, estimate, assumption and gap — each with its source and freshness.</p>
              </div>
            ) : (
              <EstimatorPanel
                request={request}
                setRequest={setRequest}
                depositPercent={depositPercent}
                estimate={estimate}
                estimateError={estimateError}
                baseRate={baseRate}
              />
            )}
          </aside>
        ) : null}
      </div>
    </div>
  );
}

function EstimatorPanel(props: {
  request: MortgageEstimateRequest;
  setRequest: React.Dispatch<React.SetStateAction<MortgageEstimateRequest>>;
  depositPercent: number;
  estimate: MortgageEstimateResult | null;
  estimateError: string | null;
  baseRate: BaseRate | null;
}) {
  const { request, setRequest, depositPercent, estimate, estimateError, baseRate } = props;

  const update = (key: keyof MortgageEstimateRequest, value: string | number) =>
    setRequest((current) => ({
      ...current,
      [key]: key === 'repaymentType' ? value : Number(value),
    }));

  const caveats = estimate?.caveats.length
    ? estimate.caveats
    : ['This is an estimate, not mortgage advice — speak to a qualified adviser before deciding.'];

  return (
    <section className="estimator-panel" aria-label="Mortgage cost estimator">
      <header className="panel-heading">
        <h2>Mortgage cost estimator</h2>
        <p>Uses <strong>your own rate</strong>, not a recommended product. SE10 · Greenwich.</p>
      </header>

      <div className="panel-fields">
        <RangeField label="Property price" value={currency0(request.propertyPrice)} min={150_000} max={900_000} step={5_000} raw={request.propertyPrice} onChange={(v) => update('propertyPrice', v)} />
        <RangeField label="Deposit" value={`${currency0(request.deposit)} · ${depositPercent}%`} min={0} max={Math.max(request.propertyPrice, 1)} step={2_500} raw={request.deposit} onChange={(v) => update('deposit', v)} />
        <RangeField label="Interest rate (your figure)" value={`${request.annualInterestRatePercent}%`} min={1} max={10} step={0.1} raw={request.annualInterestRatePercent} onChange={(v) => update('annualInterestRatePercent', v)} />
        <RangeField label="Term" value={`${request.termYears} yrs`} min={5} max={40} step={1} raw={request.termYears} onChange={(v) => update('termYears', v)} />
        <fieldset className="segmented-control">
          <legend>Repayment type</legend>
          <div className="segmented-options">
            <button type="button" aria-pressed={request.repaymentType === 'Repayment'} onClick={() => update('repaymentType', 'Repayment')}>Repayment</button>
            <button type="button" aria-pressed={request.repaymentType === 'InterestOnly'} onClick={() => update('repaymentType', 'InterestOnly')}>Interest-only</button>
          </div>
        </fieldset>
      </div>

      {estimateError ? (
        <div className="empty-state" role="status">
          The mortgage API could not be reached from this preview. Connect the HomeScout API service to calculate this estimate.
        </div>
      ) : (
        <>
          <div className="payment-card">
            <span>Monthly payment<span className="kind-chip estimate">estimate</span></span>
            <strong>{estimate === null ? 'Loading…' : currency2(estimate.monthlyPayment)}</strong>
          </div>
          <dl className="metric-rows">
            <MetricRow label="Loan amount" value={currency0(estimate?.loan)} />
            <MetricRow label="Loan-to-value" value={estimate === null ? 'Missing' : `${pct1.format(estimate.ltvPercent)}%`} />
            <MetricRow label="Total interest" value={currency0(estimate?.totalInterest)} />
            <MetricRow label="Total repayable" value={estimate?.totalRepayment == null ? '—' : currency0(estimate.totalRepayment)} />
          </dl>
          <div className="stress-row">
            <span>+3% stress payment</span>
            <strong>{currency0(estimate?.stressTest.monthlyPayment)}</strong>
          </div>
        </>
      )}

      <div className="base-rate-line">
        <span className={`provenance ${baseRate?.provenance.toLowerCase() ?? 'missing'}`}>{baseRate?.provenance ?? 'Missing'}</span>
        {baseRate === null ? 'BoE base rate unavailable' : `BoE base rate ${baseRate.ratePercent}% — context only`}
      </div>

      <section className="assumption-block" aria-labelledby="assumptions-heading">
        <h3 id="assumptions-heading">Assumptions</h3>
        <ul>
          {(estimate?.assumptions ?? ['Mortgage calculation comes from /api/mortgage/estimate.']).map((assumption) => (
            <li key={assumption}>{assumption}</li>
          ))}
        </ul>
      </section>

      <div className="caveat">
        {caveats.map((caveat) => (
          <p key={caveat}>{caveat}</p>
        ))}
      </div>
    </section>
  );
}

function RangeField(props: {
  label: string;
  value: string;
  min: number;
  max: number;
  step: number;
  raw: number;
  onChange: (value: string) => void;
}) {
  return (
    <label className="range-field">
      <span>{props.label}<strong>{props.value}</strong></span>
      <input type="range" min={props.min} max={props.max} step={props.step} value={props.raw} onChange={(event) => props.onChange(event.target.value)} />
    </label>
  );
}

function MetricRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="metric-row">
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

export default App;
