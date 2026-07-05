# Listing Capture — PDF Extraction Pipeline — Design

**Status:** Planned — the capture slice that feeds
[Listing Model + Comparison Spine](./listing-decision-pack-plan.md). Turns a user-provided document
(a saved listing page, an EPC, a brochure, a survey) into a draft `Listing` the user confirms — no
scraping.

**Owning context:** [[Phased Learning And Build Plan]] Phase 2 (the "Image/PDF input" course
feature). Consumes the `Listing` model from the comparison spine; precedes per-user persistence.

## Why

The comparison spine takes structured `Listing`s, but a user has a PDF, not JSON. This slice is the
on-ramp: **upload → extract → confirm → `Listing`**. It is the terms-safe capture path — the user
provides the document, so HomeScout never scrapes a portal (see [[Plan Divergence]]) — and it fills
the exact facts listings hide: an EPC gives the rating and floor area, a brochure gives tenure and
service charge.

The hard part is accuracy. A saved page renders some facts as **graphics** (a Rightmove EPC badge is
an image; `pdftotext` cannot see it) and portals **disagree with themselves** (a Zoopla listing said
EPC "C" in its header and "B" in its bullets). So the system is built to **not trust any single
extractor, and never to assert an unconfirmed fact.**

## Principles

- **Extraction proposes; the user ratifies.** No extracted value becomes a `Listing` (or reaches the
  agent) until the user confirms it. The system need not be perfect to be trustworthy.
- **Verify against sources of truth, don't trust the listing.** EPC, council tax band, and geocode
  come from authoritative registers, not the document.
- **Unknown over guess.** Absent facts return `null`, never a fabricated value — a made-up service
  charge is worse than a blank, for a due-diligence tool.
- **Untrusted content.** Document text/images are data; the extractor ignores any instructions
  embedded in them (prompt-injection safe). Extracted facts go into the user's private case file;
  HomeScout does not re-serve a listing's prose or photos.

## The pipeline

`text → vision → register cross-check → confidence → confirm.`

1. **Text** — `ITextDocumentReader` pulls raw text (a .NET PDF library, e.g. PdfPig).
   `IListingFactParser` maps the spec block + labelled lines (`Council Tax Band: E`, `TENURE
   Freehold`) to candidate fields — deterministic where possible, an LLM pass for the free-text
   description. Cheap, fast, catches most labelled facts.
2. **Vision** — `IVisionExtractor` renders pages to images (a PDFium-based rasteriser) and asks a
   Foundry multimodal deployment to extract the same fields. This reads the graphics text cannot: EPC
   badges, floorplan dimensions, spec tiles. For a saved web page it is often the stronger reader.
   Fills gaps and gives a second opinion on the text layer.
3. **Register cross-check** — for facts with an open source of truth, verify by address/postcode
   rather than trust the document:
   - `IEpcRegistryClient` — gov.uk EPC register → authoritative EPC rating + floor area.
   - `ICouncilTaxBandService` — VOA band → band + derived monthly £ (band + council).
   - `IPostcodeGeocoder` — postcodes.io → lat/long (shared with [[Area Evidence Map]]).
4. **Merge + confidence** — reconcile candidates into one draft and score each field:
   - **High** — an authoritative register value, or text and vision agree, or a clearly-labelled
     text field.
   - **Medium** — a single source only.
   - **Low** — sources conflict (surfaced as a conflict), or a vision-only inference.
   Precedence: `register > text∧vision agreement > labelled text > vision-only`. Conflicts (the
   C-vs-B EPC) are recorded, not silently resolved.
5. **Confirm** — the endpoint returns the draft `Listing` plus per-field provenance/confidence and any
   conflicts. The confirm screen shows each field with its source, pre-flags Low/conflicting fields,
   and lets the user edit. The confirmed `Listing` is what the client sends to `POST /api/comparison`.

**Seam-first:** every stage is an interface with an offline fake (in the test project), so the
orchestration is tested without the PDF library, the model, or the external APIs; the real adapters
are implemented last and verified with `[Category("External")]` live tests. The fakes prove the
shape, not the dependency.

## Contracts — `Shared/Contracts`

```
POST /api/listings/extract   (multipart/form-data: 1–4 files [+ optional sourceUrl, mode hint])
    -> ListingExtractionResult
```

One call = one property's document(s) (e.g. a brochure + its EPC merged into one draft). To compare,
the client extracts each property, the user confirms each, then posts the confirmed set to
`POST /api/comparison`.

```csharp
public enum FieldProvenance { Text, Vision, Register, None }   // None = not found
public enum FieldConfidence { High, Medium, Low }

public record FieldExtraction(string Field, FieldProvenance Source, FieldConfidence Confidence);
public record ConflictingValue(string Value, FieldProvenance Source);
public record ExtractionConflict(string Field, IReadOnlyList<ConflictingValue> Values);

public record ListingExtractionResult(
    Listing Draft,                                  // the Listing shape, unconfirmed (fields nullable)
    IReadOnlyList<FieldExtraction> Fields,          // where each populated field came from + confidence
    IReadOnlyList<ExtractionConflict> Conflicts,    // e.g. EPC "C" vs "B"
    IReadOnlyList<string> Notes);                   // e.g. "EPC read from a graphic via vision — confirm against the certificate"
```

`Draft` reuses the existing `Listing`; `Fields`/`Conflicts` are the sidecar the confirm UI needs.
Expected failures (no file, unsupported type, too large, all extractors empty) → FluentResults →
ProblemDetails. Service: `IListingExtractor.ExtractAsync(documents, ct) : Task<Result<ListingExtractionResult>>`
in `API.Service/Listings/`.

### `Listing` model extension (amends [Listing Model + Comparison Spine](./listing-decision-pack-plan.md))

The two real PDFs showed the model needs these — all **optional and additive**, so the comparison
spine is unaffected:

- `CouncilTaxBand` (A–H) — listings give a **band**, not a monthly £; keep `MonthlyCouncilTax` as the
  derived/override value.
- `PropertyType` (e.g. "Detached Bungalow", "Flat"), `Bathrooms`, `Receptions` — reliably present,
  useful in a comparison.
- `PriceQualifier` (Guide, OffersOver, OffersInRegionOf, FixedPrice, Poa) — so "Guide Price £500,000"
  is not treated as a firm figure.
- `AddressLine` — the street + number the description exposes, so geocoding is precise even when the
  portal hides the full postcode.

## Slice order (each ships tests)

1. **Model extension** + the extraction DTOs.
2. **Text pipeline** + `POST /api/listings/extract` returning a text-only draft — fully offline-testable.
3. **Register adapters** (EPC / VOA / postcodes.io) behind interfaces — fakes offline, real verified
   with `[External]`.
4. **Vision layer** (Foundry multimodal) behind an interface — fake offline, real verified with `[External]`.
5. **Merge + confidence + conflicts**.
6. **Eval set + harness** (below).

## Eval set — how we prove accuracy (not hope)

Per "verify, don't assume," extraction quality is **measured**, reusing the Evaluator tool project +
eval discipline that already covers the copilot.

- **Corpus** — a growing set of real listing PDFs with hand-labelled ground-truth facts (JSONL, like
  the copilot datasets). Seeds: the two worked examples — the Rightmove for-sale bungalow (the
  EPC-as-graphic case) and the Zoopla rent flat (the self-conflicting EPC case). Grow with more sites
  (OnTheMarket), a leasehold flat with a service charge, and a scanned/OCR case.
- **Metrics, per field:**
  - **Precision** — of the values we asserted, how many match ground truth.
  - **Recall / miss rate** — of the ground-truth facts, how many we captured.
  - **Hallucination rate** — fields asserted that are not in the document (the dangerous one for due
    diligence; target near-zero).
  - **Confidence calibration** — are `High` fields right materially more often than `Low`.
- **Harness** — the text-layer eval runs **offline in the PR gate** (deterministic corpus). Vision +
  register evals need the model/APIs, so they are `[Category("External")]` on the nightly schedule (a
  third-party outage must not block merges).
- **Acceptance thresholds** (initial, tightened over time): high-confidence field precision ≥ ~95%;
  hallucination rate ≤ ~1%; per-field recall tracked and raised by adding the vision/register
  fallback where text alone is weak (EPC).

## Verification / Acceptance criteria

- [ ] `POST /api/listings/extract` turns 1–4 PDFs of one property into a draft `Listing` + per-field
      provenance/confidence + conflicts.
- [ ] Facts rendered as graphics (EPC badge, floorplan) are caught by the vision layer; EPC / band /
      geocode are cross-checked against registers.
- [ ] Conflicts (EPC C vs B) are surfaced, not silently resolved; absent facts are `null`, never
      fabricated.
- [ ] No extracted value is used or sent to the agent until the user confirms it; instructions
      embedded in a document are ignored.
- [ ] Every stage sits behind an interface with an offline fake; real adapters verified with
      `[External]` live tests.
- [ ] Eval set with per-field precision / recall / hallucination-rate — text-layer in the gate,
      vision/register on schedule.
- [ ] Plan and code use identical names; `scripts/quality-gate.sh` clean (drift 0 fail).

## Related

[Listing Model + Comparison Spine](./listing-decision-pack-plan.md) · [[Feature Coverage]] ·
[[Area Evidence Map]] · [[Market Landscape And Product Lessons]] ·
[Rental Cost Estimator](./rental-cost-estimator-plan.md)
