import type { Dispatch, SetStateAction } from 'react';
import type { BaseRate, EvidenceItem, MortgageEstimateRequest, MortgageEstimateResult, RightTab } from '../types';
import { EstimatorPanel } from './EstimatorPanel';
import { EvidencePanel } from './EvidencePanel';

// The right-hand aside: tabbed between the copilot answer's Evidence trail and the Mortgage estimator.
export function EvidenceRail(props: {
  rightTab: RightTab;
  onSelectTab: (tab: RightTab) => void;
  evidence: EvidenceItem[];
  request: MortgageEstimateRequest;
  setRequest: Dispatch<SetStateAction<MortgageEstimateRequest>>;
  depositPercent: number;
  estimate: MortgageEstimateResult | null;
  estimateError: string | null;
  baseRate: BaseRate | null;
}) {
  const { rightTab, onSelectTab, evidence, request, setRequest, depositPercent, estimate, estimateError, baseRate } = props;

  return (
    <aside className="evidence-rail" aria-label="Evidence and estimator">
      <div className="rail-tabs" role="tablist" aria-label="Right panel views">
        <button type="button" role="tab" aria-selected={rightTab === 'evidence'} onClick={() => onSelectTab('evidence')}>
          Evidence
        </button>
        <button type="button" role="tab" aria-selected={rightTab === 'estimator'} onClick={() => onSelectTab('estimator')}>
          Estimator
        </button>
      </div>

      {rightTab === 'evidence' ? (
        <EvidencePanel evidence={evidence} />
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
  );
}
