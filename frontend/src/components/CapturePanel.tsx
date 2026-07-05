import { useState } from 'react';
import type {
  FieldExtraction,
  Listing,
  ListingExtractionResult,
} from '../types';
import { extractListing } from '../api/client';

const TENURES = ['Freehold', 'Leasehold', 'ShareOfFreehold'] as const;
const FURNISHINGS = ['Furnished', 'PartFurnished', 'Unfurnished', 'AtTenantChoice'] as const;
const BANDS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'] as const;

// Small helpers keep the confirm form declarative (and under the complexity budget): input values are
// always strings, empty selects/text map back to null, and "can add" is a single named check.
const str = (v: string | number | null | undefined): string => (v == null ? '' : String(v));
const orNull = (v: string): string | null => (v === '' ? null : v);
const canConfirm = (d: Listing): boolean =>
  d.label.trim().length > 0 && ((d.mode === 'Buy' ? d.price : d.monthlyRent) ?? 0) > 0;

// Capture flow: upload a property's PDF(s) → extract → confirm/edit the draft before it joins the
// comparison. Extraction proposes (with provenance + confidence badges); the user ratifies. Nothing is
// added until the user confirms.
export function CapturePanel(props: { onAdd: (listing: Listing) => void; onCancel: () => void }) {
  const [result, setResult] = useState<ListingExtractionResult | null>(null);

  return result === null ? (
    <UploadStep onExtracted={setResult} onCancel={props.onCancel} />
  ) : (
    <ConfirmStep result={result} onAdd={props.onAdd} onBack={() => setResult(null)} />
  );
}

function UploadStep(props: { onExtracted: (r: ListingExtractionResult) => void; onCancel: () => void }) {
  const [files, setFiles] = useState<File[]>([]);
  const [sourceUrl, setSourceUrl] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const extract = async () => {
    if (files.length === 0 || busy) return;
    setBusy(true);
    setError(null);
    try {
      props.onExtracted(await extractListing(files, sourceUrl.trim() || undefined));
    } catch {
      setError('Could not read that file. Save the listing page as a PDF (or upload the EPC/brochure) and try again.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="capture-panel">
      <header className="panel-heading">
        <h2>Add a listing</h2>
        <p>Save the listing page as a PDF, or upload the EPC or brochure — HomeScout reads the facts for you to confirm. Your document, never scraped.</p>
      </header>

      <label className="capture-drop">
        <input
          type="file"
          accept="application/pdf"
          multiple
          onChange={(e) => setFiles(Array.from(e.target.files ?? []).slice(0, 4))}
        />
        <span>{files.length > 0 ? files.map((f) => f.name).join(', ') : 'Choose PDF(s) — up to 4 for one property'}</span>
      </label>

      <label className="capture-field">
        <span>Source link (optional)</span>
        <input type="url" value={sourceUrl} placeholder="https://…" onChange={(e) => setSourceUrl(e.target.value)} />
      </label>

      {error ? <p className="capture-error" role="status">{error}</p> : null}

      <div className="capture-actions">
        <button type="button" className="capture-primary" disabled={files.length === 0 || busy} onClick={() => void extract()}>
          {busy ? 'Reading…' : 'Extract facts'}
        </button>
        <button type="button" onClick={props.onCancel}>Cancel</button>
      </div>
    </div>
  );
}

function ConfirmStep(props: { result: ListingExtractionResult; onAdd: (l: Listing) => void; onBack: () => void }) {
  const [draft, setDraft] = useState<Listing>(props.result.draft);
  const confidence = new Map(props.result.fields.map((f) => [f.field, f]));

  const set = <K extends keyof Listing>(key: K, value: Listing[K]) => setDraft((d) => ({ ...d, [key]: value }));
  const num = (v: string): number | null => (v.trim() === '' ? null : Number(v));
  const isBuy = draft.mode === 'Buy';
  const canAdd = canConfirm(draft);

  return (
    <div className="capture-panel">
      <header className="panel-heading">
        <h2>Confirm the facts</h2>
        <p>Extraction proposes — you decide. Badges show where each fact came from; edit anything that's wrong before adding.</p>
      </header>

      <div className="confirm-grid">
        <Field label="Label" badge={confidence.get('Postcode')}>
          <input value={draft.label} onChange={(e) => set('label', e.target.value)} />
        </Field>
        <Field label="Type">
          <select value={draft.mode} onChange={(e) => set('mode', e.target.value as Listing['mode'])}>
            <option value="Buy">Buy</option>
            <option value="Rent">Rent</option>
          </select>
        </Field>
        <Field label="Postcode" badge={confidence.get('Postcode')}>
          <input value={draft.postcode} onChange={(e) => set('postcode', e.target.value)} />
        </Field>
        {isBuy ? (
          <Field label="Price (£)" badge={confidence.get('Price')}>
            <input type="number" value={str(draft.price)} onChange={(e) => set('price', num(e.target.value))} />
          </Field>
        ) : (
          <Field label="Rent (£/mo)" badge={confidence.get('MonthlyRent')}>
            <input type="number" value={str(draft.monthlyRent)} onChange={(e) => set('monthlyRent', num(e.target.value))} />
          </Field>
        )}
        <Field label="Bedrooms" badge={confidence.get('Bedrooms')}>
          <input type="number" value={str(draft.bedrooms)} onChange={(e) => set('bedrooms', num(e.target.value))} />
        </Field>
        <Field label="Floor area" badge={confidence.get('FloorArea')}>
          <div className="confirm-inline">
            <input type="number" value={str(draft.floorArea)} onChange={(e) => set('floorArea', num(e.target.value))} />
            <select value={draft.areaUnit ?? 'SquareFeet'} onChange={(e) => set('areaUnit', e.target.value as Listing['areaUnit'])}>
              <option value="SquareFeet">ft²</option>
              <option value="SquareMetres">m²</option>
            </select>
          </div>
        </Field>
        <Field label="EPC" badge={confidence.get('EpcRating')}>
          <input value={str(draft.epcRating)} maxLength={1} onChange={(e) => set('epcRating', orNull(e.target.value.toUpperCase()))} />
        </Field>
        <Field label="Council tax band" badge={confidence.get('CouncilTaxBand')}>
          <select value={str(draft.councilTaxBand)} onChange={(e) => set('councilTaxBand', orNull(e.target.value) as Listing['councilTaxBand'])}>
            <option value="">—</option>
            {BANDS.map((b) => <option key={b} value={b}>{b}</option>)}
          </select>
        </Field>
        {isBuy ? (
          <Field label="Tenure" badge={confidence.get('Tenure')}>
            <select value={str(draft.tenure)} onChange={(e) => set('tenure', orNull(e.target.value) as Listing['tenure'])}>
              <option value="">—</option>
              {TENURES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </Field>
        ) : (
          <Field label="Furnishing" badge={confidence.get('Furnishing')}>
            <select value={str(draft.furnishing)} onChange={(e) => set('furnishing', orNull(e.target.value) as Listing['furnishing'])}>
              <option value="">—</option>
              {FURNISHINGS.map((f) => <option key={f} value={f}>{f}</option>)}
            </select>
          </Field>
        )}
      </div>

      {props.result.notes.length > 0 ? (
        <ul className="confirm-notes">
          {props.result.notes.map((n) => <li key={n}>{n}</li>)}
        </ul>
      ) : null}

      <div className="capture-actions">
        <button type="button" className="capture-primary" disabled={!canAdd} onClick={() => props.onAdd(draft)}>
          Add to comparison
        </button>
        <button type="button" onClick={props.onBack}>Back</button>
      </div>
    </div>
  );
}

function Field(props: { label: string; badge?: FieldExtraction; children: React.ReactNode }) {
  return (
    <label className="confirm-field">
      <span className="confirm-label">
        {props.label}
        {props.badge ? <span className={`conf-chip conf-${props.badge.confidence.toLowerCase()}`}>{props.badge.confidence}</span> : null}
      </span>
      {props.children}
    </label>
  );
}
