import { useEffect, useMemo, useState } from 'react';
import './App.css';

type Theme = 'light' | 'dark';
type RepaymentType = 'Repayment' | 'InterestOnly';
type Provenance = 'Live' | 'Cache' | 'Fallback';
type FigureKind = 'fact' | 'estimate' | 'assumption' | 'missing';

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

interface RunningCosts {
  serviceCharge: number;
  councilTax: number;
  buildingsInsurance: number;
  maintenanceReserve: number;
}

interface EvidenceItem {
  label: string;
  value: string;
  kind: FigureKind;
  source: string;
  provenance?: Provenance;
}

const savedComparisons = [
  {
    name: 'Greenwich vs Croydon',
    meta: 'Commute, schools, budget',
    age: 'today',
  },
  {
    name: 'Reading family homes',
    meta: 'Parks, rail, price context',
    age: '3 days ago',
  },
  {
    name: 'Canary Wharf flats',
    meta: 'Service charge, commute',
    age: '1 week ago',
  },
];

const formatter = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'GBP',
  maximumFractionDigits: 0,
});

const preciseFormatter = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'GBP',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const percentFormatter = new Intl.NumberFormat('en-GB', {
  maximumFractionDigits: 1,
});

const initialEstimateRequest: MortgageEstimateRequest = {
  propertyPrice: 465_000,
  deposit: 92_500,
  annualInterestRatePercent: 5.1,
  termYears: 25,
  repaymentType: 'Repayment',
};

const initialRunningCosts: RunningCosts = {
  serviceCharge: 185,
  councilTax: 168,
  buildingsInsurance: 35,
  maintenanceReserve: 388,
};

function currency(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return 'Missing';
  }

  return formatter.format(value);
}

function preciseCurrency(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return 'Missing';
  }

  return preciseFormatter.format(value);
}

async function readJson<T>(
  url: string,
  options?: RequestInit,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(url, { ...options, signal });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}

function App() {
  const [theme, setTheme] = useState<Theme>('light');
  const [request, setRequest] = useState<MortgageEstimateRequest>(
    initialEstimateRequest,
  );
  const [runningCosts, setRunningCosts] =
    useState<RunningCosts>(initialRunningCosts);
  const [estimate, setEstimate] = useState<MortgageEstimateResult | null>(null);
  const [baseRate, setBaseRate] = useState<BaseRate | null>(null);
  const [estimateError, setEstimateError] = useState<string | null>(null);
  const [baseRateError, setBaseRateError] = useState<string | null>(null);
  const [loadingEstimate, setLoadingEstimate] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    void readJson<BaseRate>(
      '/api/mortgage/base-rate',
      undefined,
      controller.signal,
    )
      .then((data) => {
        setBaseRate(data);
        setBaseRateError(null);
      })
      .catch((error: Error) => {
        if (controller.signal.aborted) {
          return;
        }

        setBaseRate(null);
        setBaseRateError(error.message);
      });

    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    setLoadingEstimate(true);

    void readJson<MortgageEstimateResult>(
      '/api/mortgage/estimate',
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      },
      controller.signal,
    )
      .then((data) => {
        setEstimate(data);
        setEstimateError(null);
      })
      .catch((error: Error) => {
        if (controller.signal.aborted) {
          return;
        }

        setEstimate(null);
        setEstimateError(error.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoadingEstimate(false);
        }
      });

    return () => controller.abort();
  }, [request]);

  const monthlyRunningCosts = useMemo(
    () =>
      runningCosts.serviceCharge +
      runningCosts.councilTax +
      runningCosts.buildingsInsurance +
      runningCosts.maintenanceReserve,
    [runningCosts],
  );

  const ownershipTotal =
    estimate === null ? null : estimate.monthlyPayment + monthlyRunningCosts;

  const evidence: EvidenceItem[] = [
    {
      label: 'Monthly mortgage payment',
      value:
        estimate === null ? 'Awaiting API estimate' : preciseCurrency(estimate.monthlyPayment),
      kind: estimate === null ? 'missing' : 'estimate',
      source: '/api/mortgage/estimate',
      provenance: estimate === null ? undefined : 'Live',
    },
    {
      label: 'Loan to value',
      value:
        estimate === null
          ? 'Awaiting API estimate'
          : `${percentFormatter.format(estimate.ltvPercent)}%`,
      kind: estimate === null ? 'missing' : 'estimate',
      source: '/api/mortgage/estimate',
      provenance: estimate === null ? undefined : 'Live',
    },
    {
      label: 'Running costs',
      value: `${currency(monthlyRunningCosts)} / mo`,
      kind: 'assumption',
      source: 'Buyer inputs and HomeScout defaults',
    },
    {
      label: 'BoE base rate',
      value: baseRate === null ? 'Unavailable' : `${baseRate.ratePercent}%`,
      kind: baseRate === null ? 'missing' : 'fact',
      source: baseRate?.source ?? '/api/mortgage/base-rate',
      provenance: baseRate?.provenance,
    },
  ];

  const caveats =
    estimate?.caveats.length ? estimate.caveats : ['This is an estimate, not mortgage advice. Speak to a qualified mortgage adviser before deciding.'];

  const updateRequest = (
    key: keyof MortgageEstimateRequest,
    value: string | number,
  ) => {
    setRequest((current) => ({
      ...current,
      [key]:
        key === 'repaymentType'
          ? value
          : Number.isInteger(current[key] as number)
            ? Math.round(Number(value))
            : Number(value),
    }));
  };

  const updateRunningCost = (key: keyof RunningCosts, value: string) => {
    setRunningCosts((current) => ({
      ...current,
      [key]: Math.max(0, Number(value)),
    }));
  };

  return (
    <div className="homescout-app" data-theme={theme}>
      <header className="top-bar">
        <div className="brand-lockup" aria-label="HomeScout Copilot">
          <span className="brand-mark" aria-hidden="true">
            H
          </span>
          <div>
            <strong>
              HomeScout <span>Copilot</span>
            </strong>
            <small>Greenwich vs Croydon · draft</small>
          </div>
        </div>
        <span className="decision-pill">Decision support, not advice</span>
        <div className="top-actions">
          <button className="toolbar-button" type="button">
            Search
            <kbd>⌘K</kbd>
          </button>
          <button
            className="icon-button"
            type="button"
            aria-label={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
            onClick={() =>
              setTheme((current) => (current === 'dark' ? 'light' : 'dark'))
            }
            title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`}
          >
            {theme === 'dark' ? '☀' : '◐'}
          </button>
          <button className="avatar-button" type="button" aria-label="Account">
            AO
          </button>
        </div>
      </header>

      <div className="workspace-grid">
        <aside className="left-rail" aria-label="Saved comparisons and tools">
          <button className="new-comparison" type="button">
            + New comparison
          </button>
          <nav className="primary-nav" aria-label="Workspace tools">
            <button type="button">Area comparison</button>
            <button className="active" type="button">
              Mortgage cost estimator
            </button>
            <button type="button">Settings</button>
          </nav>
          <section aria-labelledby="saved-comparisons-heading">
            <h2 id="saved-comparisons-heading">Saved comparisons</h2>
            <div className="saved-list">
              {savedComparisons.map((comparison) => (
                <button type="button" className="saved-item" key={comparison.name}>
                  <strong>{comparison.name}</strong>
                  <span>{comparison.meta}</span>
                  <small>{comparison.age}</small>
                </button>
              ))}
            </div>
          </section>
        </aside>

        <main className="main-workspace" aria-label="Mortgage cost estimator">
          <div className="workspace-tabs" role="tablist" aria-label="Workspace views">
            <button type="button" role="tab" aria-selected="false">
              Copilot
            </button>
            <button type="button" role="tab" aria-selected="true">
              Mortgage estimate
            </button>
            <button type="button" role="tab" aria-selected="false">
              Comparison canvas
            </button>
          </div>

          <section className="estimator-surface" aria-labelledby="estimator-heading">
            <div className="surface-heading">
              <div>
                <p className="eyebrow">API-backed cost view</p>
                <h1 id="estimator-heading">Mortgage cost estimator</h1>
                <p>
                  Uses your own quoted or assumed rate. HomeScout does not
                  recommend mortgage products.
                </p>
              </div>
              <div className="api-state" data-state={estimateError ? 'error' : 'ready'}>
                {estimateError ? 'API unavailable' : loadingEstimate ? 'Updating' : 'Live API'}
              </div>
            </div>

            <div className="estimator-grid">
              <form className="input-stack" aria-label="Mortgage inputs">
                <MoneyInput
                  label="Property price"
                  value={request.propertyPrice}
                  min={150_000}
                  max={900_000}
                  step={5_000}
                  onChange={(value) => updateRequest('propertyPrice', value)}
                />
                <MoneyInput
                  label="Deposit"
                  value={request.deposit}
                  min={0}
                  max={Math.max(request.propertyPrice, 1)}
                  step={2_500}
                  onChange={(value) => updateRequest('deposit', value)}
                />
                <SliderInput
                  label="Interest rate"
                  suffix="%"
                  value={request.annualInterestRatePercent}
                  min={1}
                  max={10}
                  step={0.1}
                  onChange={(value) =>
                    updateRequest('annualInterestRatePercent', value)
                  }
                />
                <SliderInput
                  label="Term"
                  suffix="years"
                  value={request.termYears}
                  min={5}
                  max={40}
                  step={1}
                  onChange={(value) => updateRequest('termYears', value)}
                />
                <fieldset className="segmented-control">
                  <legend>Repayment type</legend>
                  <button
                    type="button"
                    aria-pressed={request.repaymentType === 'Repayment'}
                    onClick={() => updateRequest('repaymentType', 'Repayment')}
                  >
                    Repayment
                  </button>
                  <button
                    type="button"
                    aria-pressed={request.repaymentType === 'InterestOnly'}
                    onClick={() => updateRequest('repaymentType', 'InterestOnly')}
                  >
                    Interest-only
                  </button>
                </fieldset>
              </form>

              <section className="result-panel" aria-label="Mortgage estimate result">
                {estimateError ? (
                  <div className="empty-state" role="status">
                    The mortgage API could not be reached from this preview.
                    Connect the HomeScout API service to calculate this estimate.
                  </div>
                ) : (
                  <>
                    <div className="hero-figure">
                      <span>Estimated monthly mortgage</span>
                      <strong>
                        {estimate === null
                          ? 'Loading'
                          : preciseCurrency(estimate.monthlyPayment)}
                      </strong>
                    </div>
                    <div className="figure-grid">
                      <MetricCard
                        label="Loan amount"
                        value={currency(estimate?.loan)}
                        kind={estimate === null ? 'missing' : 'estimate'}
                        provenance={estimate === null ? undefined : 'Live'}
                      />
                      <MetricCard
                        label="LTV"
                        value={
                          estimate === null
                            ? 'Missing'
                            : `${percentFormatter.format(estimate.ltvPercent)}%`
                        }
                        kind={estimate === null ? 'missing' : 'estimate'}
                        provenance={estimate === null ? undefined : 'Live'}
                      />
                      <MetricCard
                        label={`Stress test at ${
                          estimate === null
                            ? '+3%'
                            : `${estimate.stressTest.ratePercent}%`
                        }`}
                        value={preciseCurrency(estimate?.stressTest.monthlyPayment)}
                        kind={estimate === null ? 'missing' : 'estimate'}
                        provenance={estimate === null ? undefined : 'Live'}
                      />
                      <MetricCard
                        label="Total interest"
                        value={preciseCurrency(estimate?.totalInterest)}
                        kind={estimate === null ? 'missing' : 'estimate'}
                        provenance={estimate === null ? undefined : 'Live'}
                      />
                    </div>
                  </>
                )}
              </section>
            </div>

            <section className="running-costs" aria-labelledby="running-costs-heading">
              <div>
                <p className="eyebrow">Ownership context</p>
                <h2 id="running-costs-heading">Running costs</h2>
              </div>
              <div className="running-grid">
                <NumberInput
                  label="Service charge"
                  value={runningCosts.serviceCharge}
                  onChange={(value) => updateRunningCost('serviceCharge', value)}
                />
                <NumberInput
                  label="Council tax"
                  value={runningCosts.councilTax}
                  onChange={(value) => updateRunningCost('councilTax', value)}
                />
                <NumberInput
                  label="Buildings insurance"
                  value={runningCosts.buildingsInsurance}
                  onChange={(value) =>
                    updateRunningCost('buildingsInsurance', value)
                  }
                />
                <NumberInput
                  label="Maintenance reserve"
                  value={runningCosts.maintenanceReserve}
                  onChange={(value) =>
                    updateRunningCost('maintenanceReserve', value)
                  }
                />
              </div>
              <div className="ownership-total">
                <span>Total monthly ownership estimate</span>
                <strong>
                  {ownershipTotal === null
                    ? 'Awaiting mortgage API'
                    : `${preciseCurrency(ownershipTotal)} / mo`}
                </strong>
              </div>
            </section>
          </section>
        </main>

        <aside className="evidence-rail" aria-label="Evidence panel">
          <div className="rail-tabs" role="tablist" aria-label="Right panel views">
            <button type="button" role="tab" aria-selected="true">
              Evidence
            </button>
            <button type="button" role="tab" aria-selected="false">
              Uploads
            </button>
          </div>
          <EvidenceList items={evidence} />

          <section className="assumption-block" aria-labelledby="assumptions-heading">
            <h2 id="assumptions-heading">Assumptions</h2>
            <ul>
              {(estimate?.assumptions ?? [
                'Mortgage calculation comes from /api/mortgage/estimate.',
                'Running costs are editable buyer inputs.',
              ]).map((assumption) => (
                <li key={assumption}>{assumption}</li>
              ))}
            </ul>
          </section>

          <section className="base-rate-card" aria-label="Base rate reference">
            <div>
              <span
                className={`provenance ${baseRate?.provenance.toLowerCase() ?? 'missing'}`}
              >
                {baseRate?.provenance ?? 'Missing'}
              </span>
              <strong>
                {baseRate === null
                  ? 'Base rate unavailable'
                  : `BoE base rate ${baseRate.ratePercent}%`}
              </strong>
            </div>
            <p>
              {baseRate === null
                ? `Could not reach /api/mortgage/base-rate${
                    baseRateError ? ` (${baseRateError})` : ''
                  }.`
                : `${baseRate.note} Effective ${baseRate.effectiveDate}. Context only.`}
            </p>
          </section>

          <div className="caveat">
            {caveats.map((caveat) => (
              <p key={caveat}>{caveat}</p>
            ))}
          </div>
        </aside>
      </div>
    </div>
  );
}

function MoneyInput(props: {
  label: string;
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (value: string) => void;
}) {
  return (
    <label className="range-field">
      <span>
        {props.label}
        <strong>{currency(props.value)}</strong>
      </span>
      <input
        type="range"
        min={props.min}
        max={props.max}
        step={props.step}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </label>
  );
}

function SliderInput(props: {
  label: string;
  suffix: string;
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (value: string) => void;
}) {
  return (
    <label className="range-field">
      <span>
        {props.label}
        <strong>
          {props.value} {props.suffix}
        </strong>
      </span>
      <input
        type="range"
        min={props.min}
        max={props.max}
        step={props.step}
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </label>
  );
}

function NumberInput(props: {
  label: string;
  value: number;
  onChange: (value: string) => void;
}) {
  return (
    <label className="number-field">
      <span>{props.label}</span>
      <div>
        <span>£</span>
        <input
          aria-label={props.label}
          type="number"
          min="0"
          step="1"
          value={props.value}
          onChange={(event) => props.onChange(event.target.value)}
        />
      </div>
    </label>
  );
}

function MetricCard(props: {
  label: string;
  value: string;
  kind: FigureKind;
  provenance?: Provenance;
}) {
  return (
    <div className="metric-card">
      <span>{props.label}</span>
      <strong>{props.value}</strong>
      <div className="chip-row">
        <span className={`kind-chip ${props.kind}`}>{props.kind}</span>
        {props.provenance ? (
          <span className={`provenance ${props.provenance.toLowerCase()}`}>
            {props.provenance}
          </span>
        ) : null}
      </div>
    </div>
  );
}

function EvidenceList({ items }: { items: EvidenceItem[] }) {
  return (
    <section className="evidence-list" aria-labelledby="evidence-heading">
      <h2 id="evidence-heading">Evidence</h2>
      {items.map((item) => (
        <article className="evidence-item" key={item.label}>
          <div>
            <span className={`kind-chip ${item.kind}`}>{item.kind}</span>
            {item.provenance ? (
              <span className={`provenance ${item.provenance.toLowerCase()}`}>
                {item.provenance}
              </span>
            ) : null}
          </div>
          <strong>{item.label}</strong>
          <p>{item.value}</p>
          <small>{item.source}</small>
        </article>
      ))}
    </section>
  );
}

export default App;
