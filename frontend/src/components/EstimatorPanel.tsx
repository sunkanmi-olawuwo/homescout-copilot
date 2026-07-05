import type { Dispatch, SetStateAction } from 'react';
import type { BaseRate, MortgageEstimateRequest, MortgageEstimateResult } from '../types';
import { currency0, currency2, pct1 } from '../lib/format';
import { MetricRow } from './MetricRow';
import { RangeField } from './RangeField';

// The mortgage cost estimator (right-rail Estimator tab): user-driven inputs → /api/mortgage/estimate,
// with the +3% stress payment, LTV, base-rate context, assumptions and the not-mortgage-advice caveat.
export function EstimatorPanel(props: {
  request: MortgageEstimateRequest;
  setRequest: Dispatch<SetStateAction<MortgageEstimateRequest>>;
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
