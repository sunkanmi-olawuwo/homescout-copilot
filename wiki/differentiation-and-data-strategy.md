# Differentiation & Data Strategy

The existential question for HomeScout: **a general chatbot can already produce a listing
comparison.** Paste two listings into ChatGPT and it will give you a table. So "a comparison" is not
the product. This page is the durable reference for what actually makes HomeScout different, and the
data-acquisition stance that makes that difference possible — so product decisions can point back to it.

The rule this page exists to enforce: **be a step above a general chatbot in everything we do.** Not
occasionally — in every surface, every feature, every output.

## The moat: what a chatbot can't do for a £400k decision

We will not win on raw reasoning — it is the same class of model. The moat is the things a chat
fundamentally cannot do when the cost of being confidently wrong is enormous:

1. **Verified facts, not plausible text.** A chatbot will confidently hallucinate a floor area,
   misread an EPC, or invent a service charge. HomeScout facts are **verified against authoritative
   sources** (gov.uk EPC register, VOA, Land Registry, postcodes.io) and carry **provenance** — the
   user can see where each fact came from and how sure we are. Grounding is the product.
2. **Deterministic, auditable calculations.** £/ft², Tenant Fees Act deposit caps, the +3% stress
   test — these must be right *every time* and be explainable. A chatbot's arithmetic is inconsistent
   and unauditable; our estimators are deterministic, repeatable, and ship their assumptions.
3. **A systematic "what's missing" workflow.** A chatbot answers what you *ask*. HomeScout runs a
   due-diligence *process*: it scores completeness and tells you what the listing omitted and what to
   ask the agent — the thing buyers do badly in their heads across a dozen tabs.
4. **A durable, structured, shareable artifact — plus a private case file.** A chatbot gives you a
   throwaway message in a transcript. HomeScout produces a **decision pack**: a structured surface you
   can save, share (partner, parent, broker), and that grows as you add documents and preferences.
   Retrieval over the user's own case file (EPCs, surveys, floorplans, notes) is memory a chat window
   doesn't have.
5. **Trust by design.** HomeScout deliberately gives **no safe/unsafe verdict and no product
   recommendation**, always caveated and evidence-linked (see [[Overview]] safety boundary). In a
   regulated-adjacent domain, that restraint and explainability is a feature and a brand, not a
   limitation.

One line: **a chatbot is a plausible-sounding generalist; HomeScout is a grounded, verifiable,
repeatable due-diligence system for one high-stakes decision.**

## Not just a table — every surface is a step above chat

The comparison output is a **decision card, not a markdown table**: a highlights strip that names the
non-obvious insight (*"best value per space — £347/ft² vs £405"*, even though its sticker price is
higher), completeness bars, provenance on every fact, and actionable *"ask the agent for…"* prompts.
That card is the reference standard for what "above a chatbot" looks like — follow
[[Frontend Design Guidelines]].

The same bar applies to every feature:

- **Capture** — a measured, eval'd extraction pipeline (text → vision → register cross-check →
  confidence → confirm), not "paste a PDF into a chatbot and hope"
  (see [Listing Capture](__plans/03-backend/listing-capture-extraction-plan.md)).
- **Comparison** — the decision card above, with verified metrics
  (see [Listing Model + Comparison Spine](__plans/03-backend/listing-decision-pack-plan.md)).
- **Area evidence** — a source-linked, provenance-tagged map/list, never "the model thinks there's a
  school nearby" (see [[Area Evidence Map]]).
- **Copilot** — grounded tool calls with an evidence trail and caveats, not free-form prose.

### The litmus test

Before shipping any surface, ask: **"Could a general chatbot with browsing do this just as well?"**
If yes, we have not gone a step above — add grounding, verification, determinism, persistence, or a
visual decision-surface until the answer is no.

## Data acquisition: clean by construction — and it *is* the moat

The clean way to get data and the differentiating way to get data are **the same path**. That is the
key strategic insight of this page.

**Sources we use (clean and differentiating):**

- **User-provided documents** — the user saves/uploads their own listing page, EPC, brochure, or
  survey. They have the right to their own document; HomeScout never touches the portal.
- **Authoritative registers** — gov.uk EPC register, VOA council tax bands, Land Registry, postcodes.io.
  These are open/licensed and are the source of the "verified" claim.
- **Licensed / official feeds** for scale later — Rightmove/Zoopla partner feeds, or licensed
  providers (Sprift, PriceHubble, PropertyData). Partner rather than rebuild datasets
  (see [[Market Landscape And Product Lessons]]).
- **Browser extension** — reads a listing in the user's *own authorised session*, no server scraping.

**What we do not do: scrape portals — ours or via a third party.** Using a scraping service (Apify,
`web2pdfconvert`, a headless-Chrome converter) does not change the legal character — it is the same
automated access against the portal's terms, with a vendor in the middle who explicitly pushes the
liability back to us. The UK exposure stacks: **terms-of-service breach** (the direct route to a
cease-and-desist or IP block), the **UK/EU database right** over the compiled listings, **copyright**
on descriptions and photos, **bot-protection circumvention**, and **UK GDPR** over personal data on
the page. It also does not even work reliably (bot-walls), and it adds cost, a third-party dependency,
and data-sharing.

Crucially, scraping is **doubly wrong for us**: it creates legal risk *and* erodes the moat — a scraper
yields the same *unverified, plausible* data a chatbot already has, not the register-verified grounding
that differentiates us. So the temptation to scrape for "seamlessness" trades away the two things that
matter most.

> Not legal advice. Scraping law is genuinely contested and jurisdiction-dependent; if a
> data-acquisition strategy becomes core, get real counsel. The stance above is a deliberate
> risk-and-strategy choice, not a claim of legal certainty.

## Related

[[Market Landscape And Product Lessons]] · [[Accelerator Product Direction]] · [[Overview]] ·
[[Area Evidence Map]] · [[Frontend Design Guidelines]] ·
[Listing Model + Comparison Spine](__plans/03-backend/listing-decision-pack-plan.md) ·
[Listing Capture — PDF Extraction Pipeline](__plans/03-backend/listing-capture-extraction-plan.md)
