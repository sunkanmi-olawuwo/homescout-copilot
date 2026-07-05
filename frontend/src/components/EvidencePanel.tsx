import type { EvidenceItem } from '../types';

// The evidence trail: every figure from the copilot answer with its kind, provenance and source.
// Shows an empty-state prompt until an answer produces evidence.
export function EvidencePanel({ evidence }: { evidence: EvidenceItem[] }) {
  if (!evidence.length) {
    return (
      <div className="evidence-empty" role="note">
        <span className="evidence-mark" aria-hidden="true">▤</span>
        <strong>Evidence appears here</strong>
        <p>Ask a question and HomeScout will show every fact, estimate, assumption and gap — each with its source and freshness.</p>
      </div>
    );
  }

  return (
    <section className="evidence-panel" aria-label="Evidence trail">
      <header className="panel-heading">
        <h2>Evidence trail</h2>
        <p>Every figure from the copilot answer keeps its status, provenance and source.</p>
      </header>
      <div className="evidence-list">
        {evidence.map((item) => (
          <article className="evidence-item" key={`${item.label}-${item.value}-${item.source}`}>
            <div className="evidence-item-head">
              <span className={`kind-chip ${item.kind}`}>{item.kind}</span>
              <span className={`provenance ${item.provenance?.toLowerCase() ?? 'missing'}`}>
                {item.provenance ?? 'Missing'}
              </span>
            </div>
            <strong>{item.value}</strong>
            <span>{item.label}</span>
            <small>{item.source}</small>
          </article>
        ))}
      </div>
    </section>
  );
}
