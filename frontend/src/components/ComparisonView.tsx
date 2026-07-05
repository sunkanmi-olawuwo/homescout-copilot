import { useEffect, useState } from 'react';
import type { ComparisonResult, Listing } from '../types';
import { compareListings } from '../api/client';
import { CapturePanel } from './CapturePanel';
import { ComparisonCard } from './ComparisonCard';

// The compare workspace: add 2–4 listings (via PDF capture + confirm), then see the real side-by-side
// — price per ft², indicative monthly cost, a completeness score, and what to ask the agent. Evidence
// and gaps, never a verdict. This is the "a step above a chatbot" surface.
export function ComparisonView() {
  const [listings, setListings] = useState<Listing[]>([]);
  const [result, setResult] = useState<ComparisonResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [capturing, setCapturing] = useState(false);

  useEffect(() => {
    if (listings.length < 2) {
      setResult(null);
      setError(null);
      return;
    }
    const controller = new AbortController();
    compareListings(listings, controller.signal)
      .then((r) => { setResult(r); setError(null); })
      .catch((e: Error) => { if (!controller.signal.aborted) { setResult(null); setError(e.message); } });
    return () => controller.abort();
  }, [listings]);

  const add = (listing: Listing) => {
    setListings((current) => [...current, listing].slice(0, 4));
    setCapturing(false);
  };
  const remove = (index: number) => setListings((current) => current.filter((_, i) => i !== index));

  return (
    <section className="comparison-view" aria-label="Property comparison">
      <div className="comparison-bar">
        <div className="comparison-title">
          <h1>Compare properties</h1>
          <p>{listings.length}/4 added — {listings.length < 2 ? 'add at least two to compare' : 'evidence and gaps, not a verdict'}</p>
        </div>
        {listings.length < 4 && !capturing ? (
          <button type="button" className="comparison-add" onClick={() => setCapturing(true)}>+ Add listing</button>
        ) : null}
      </div>

      {listings.length > 0 ? (
        <ul className="comparison-pills">
          {listings.map((l, i) => (
            <li key={`${l.label}-${i}`}>
              {l.label}
              <button type="button" aria-label={`Remove ${l.label}`} onClick={() => remove(i)}>×</button>
            </li>
          ))}
        </ul>
      ) : null}

      {capturing ? <CapturePanel onAdd={add} onCancel={() => setCapturing(false)} /> : null}

      {error ? <p className="comparison-error" role="status">Couldn’t build the comparison: {error}</p> : null}

      {!capturing && listings.length < 2 ? (
        <div className="empty-state comparison-empty">
          Add at least two listings to compare. Upload a saved listing page, EPC or brochure — HomeScout
          extracts the facts for you to confirm, then shows price per ft², true monthly cost, and what each
          listing is missing.
        </div>
      ) : null}

      {result ? (
        <>
          {result.highlights.length > 0 ? (
            <ul className="comparison-highlights">
              {result.highlights.map((h) => <li key={h}>{h}</li>)}
            </ul>
          ) : null}

          <div className="comparison-grid">
            {result.listings.map((item, i) => <ComparisonCard key={`${item.listing.label}-${i}`} item={item} />)}
          </div>

          {result.assumptions.length > 0 ? (
            <section className="assumption-block" aria-labelledby="comparison-assumptions">
              <h3 id="comparison-assumptions">Assumptions</h3>
              <ul>{result.assumptions.map((a) => <li key={a}>{a}</li>)}</ul>
            </section>
          ) : null}

          <div className="caveat">
            {result.caveats.map((c) => <p key={c}>{c}</p>)}
          </div>
        </>
      ) : null}
    </section>
  );
}
