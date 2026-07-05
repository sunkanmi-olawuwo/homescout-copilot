# Accelerator Product Direction

HomeScout can grow from a technical copilot prototype into an accelerator-ready product by focusing on
property decision support, not property search.

## Product Thesis

HomeScout helps renters and buyers avoid bad property decisions by turning property listings into
evidence-backed decision packs.

The product should not try to replace Rightmove, Zoopla, OnTheMarket, or estate-agent portals. Those
services help people discover homes. HomeScout should help people decide whether a home is worth
viewing, applying for, offering on, or walking away from.

Simple positioning:

> HomeScout turns property listings into evidence-backed decision packs for renters and buyers,
> helping people spot hidden costs, compare areas, and ask better questions before committing.

## Core User Flow

1. A user finds a home on Rightmove, Zoopla, OnTheMarket, or an estate-agent site.
2. The user pastes the listing link into HomeScout, uploads a listing document, or manually enters
   the key details.
3. HomeScout reads or helps extract important details:
   price or rent, postcode, bedrooms, tenure, EPC, council tax, service charge, deposit, floor area,
   availability, furnishing, bills, and agent notes.
4. HomeScout adds the property to a shortlist.
5. The user adds another property.
6. HomeScout compares the homes and answers questions such as:
   - Which one is better value?
   - What costs am I missing?
   - What should I ask at the viewing?
   - Is this area better for commute, schools, and local context?

The product should always distinguish between listing claims, public-data evidence, user-provided
assumptions, and estimates.

## Buyer Use Case

Buyer decision packs should help users understand:

- purchase price and estimated monthly cost
- deposit and mortgage assumptions
- leasehold/freehold status
- service charge and ground rent
- EPC and likely energy-cost implications
- council tax
- comparable sold-price context
- commute and area trade-offs
- flood, planning, and local-risk context where data is available
- missing listing information
- questions to ask the agent or solicitor

The buyer angle is commercially meaningful, but it is more sensitive because it touches mortgage,
legal, and due-diligence decisions. HomeScout must stay clear that it is not a regulated mortgage
adviser, solicitor, surveyor, or safety authority.

## Renter Use Case

Renting may be the strongest first wedge because renter decisions are faster, more frequent, and
often made with incomplete information.

Rental decision packs should help users understand:

- monthly rent
- deposit and upfront cost
- council tax
- estimated bills
- EPC and energy-cost context
- furnishing and availability
- tenancy notes
- commute comparison
- area context
- missing information
- common rental red flags
- questions to ask before paying a holding deposit

Example renter questions:

- Which flat is better value after bills and commute?
- What should I ask before paying a holding deposit?
- Is this listing missing anything important?
- Compare these three rentals for commute, safety, bills, and space.

## What This Is Not

HomeScout should not be pitched as generic "AI property search". Large portals either already have,
or can build, conversational search and natural-language filtering.

HomeScout's wedge is due diligence after discovery:

- not "find me a home"
- but "help me understand whether this home is a good decision"

That distinction matters for accelerator positioning and for conversations with potential partners.

## Potential Customers And Channels

HomeScout can start consumer-first and later expand into B2B channels.

Potential first users:

- renters comparing multiple flats
- first-time buyers comparing listings
- people relocating to a new area
- couples or families trying to make a shared housing decision

Potential commercial channels:

- mortgage brokers who want to engage buyers before application
- estate agents who want better-qualified buyer or renter leads
- relocation advisers
- buying agents
- tenant-support or renter-advice services
- smaller portals and proptech platforms
- larger portals later, once demand is proven

The most realistic early route is not selling to Rightmove or Zoopla first. They are large, have
internal product teams, and may already be building broad AI search features. A better first route is
to prove user demand with renters or buyers, then approach agents, brokers, relocation services, or
smaller proptech partners.

## Accelerator-Ready Framing

HomeScout is accelerator-worthy as a direction, but it needs sharper validation before it becomes a
fully proven startup.

Accelerator one-liner:

> HomeScout helps UK renters and homebuyers avoid costly property mistakes by turning listings into
> evidence-backed decision packs covering hidden costs, commute, local context, red flags, and
> viewing questions.

Problem:

> People make expensive housing decisions from incomplete listings, scattered browser tabs,
> spreadsheets, and guesswork.

Solution:

> HomeScout turns listings into clear comparison packs with evidence and questions to ask before
> committing.

Why now:

> Housing costs are high, renting is competitive, listings are incomplete, and AI can now structure
> messy property information into useful, evidence-linked decision support.

Why HomeScout can win:

- focused on due diligence, not generic search
- evidence-backed, not just chatbot prose
- works across portals and uploaded documents
- useful before viewings, applications, deposits, or offers
- expands naturally from renters to buyers
- can later partner with brokers, agents, relocation services, and portals

## MVP Scope

The first accelerator-grade MVP should do one thing well:

> Paste or enter two to three listings, then generate a comparison pack.

The MVP should include:

- listing capture by URL, document upload, or manual entry
- structured listing facts
- side-by-side comparison
- estimated true monthly cost
- missing-information flags
- evidence trail
- viewing questions
- copilot Q&A over the shortlist
- shareable or exportable decision pack

Avoid overbuilding early:

- no complex account system unless needed for testing
- no direct portal partnership dependency
- no unsupported scraping commitment
- no regulated mortgage or legal advice
- no broad marketplace/search replacement

## Current Implementation Status

This strategy is being realised incrementally against the plans in `wiki/__plans/`. What already
exists in the codebase (see [[Feature Coverage]] for the authoritative list):

- **Estimated true monthly cost — shipped for both tenures.** The buyer mortgage estimator and the
  renter cost estimator (`POST /api/mortgage/estimate`, `POST /api/rental/estimate`) both compute
  deterministic figures with assumptions and caveats, exposed to the copilot as tools and rendered
  in the evidence panel. The renter estimator applies the Tenant Fees Act 2019 deposit/upfront caps.
  Plan: [[Rental Cost Estimator — Design]].
- **Copilot that serves renters and buyers.** Prompt `homescout.v3.md` routes to the right tool by
  tenure and keeps the tenure-appropriate caveat (not mortgage advice / not tenancy advice).

Still to build for the MVP decision pack: listing capture (URL/upload/manual), structured listing
facts, side-by-side comparison, missing-information flags, viewing questions, and shareable export.

## Validation Plan

The next validation goal is to prove that real users feel enough pain to use HomeScout before
viewings, rental applications, deposits, or purchase offers.

Target validation:

- test with 20-30 renters or first-time buyers
- ask users to bring real listings they are considering
- watch them use HomeScout on those listings
- measure whether HomeScout found something they missed
- ask whether they would use it before a viewing
- ask whether they would pay for a report or subscription
- ask whether they would share it with a partner, flatmate, parent, broker, or agent

Useful proof points for accelerator applications:

- Tested with 25 renters or buyers.
- 18 said they would use it before viewings.
- 12 said it found something they missed.
- 8 said they would pay for a comparison pack.
- Users compared 3 listings in minutes instead of juggling tabs and spreadsheets.

The exact numbers can change; the important thing is to collect real evidence instead of relying on
the idea alone.

## Business Model Options

Likely early models:

- consumer report: small one-off fee per decision pack
- consumer subscription: monthly fee while actively searching
- freemium: free simple comparison, paid export or deeper report
- B2B agent/broker tool: branded reports for clients
- partner lead generation: free user tool, paid partner introductions

The simplest accelerator story is:

> Start consumer-first with renters and buyers, then partner with brokers, agents, relocation
> advisers, and property platforms once usage is proven.

## Thirty-Day Plan

Week 1:

- sharpen the MVP around renters first
- create a simple landing page and waitlist
- prepare one high-quality sample comparison report
- support manual listing entry so the demo does not depend on unreliable link import

Week 2:

- test with 10 real renters
- use their real listings
- improve the decision-pack format
- identify the most valuable sections

Week 3:

- test with 15 more users
- collect quotes and simple metrics
- add share/export for the decision pack
- refine pricing hypotheses

Week 4:

- make a short demo video
- create a concise pitch deck
- apply to accelerators
- contact 5-10 brokers, agents, relocation advisers, or renter communities for pilots

## Repository And IP Posture

The repository should stay private while the product direction, prompts, roadmap, architecture, and
accelerator narrative are being shaped.

Public materials can include:

- landing page
- short demo video
- screenshots
- pitch deck
- waitlist form

Private materials should include:

- source code
- prompts
- evaluation approach
- roadmap
- architecture
- product strategy notes
- raw user research

Before sharing with accelerators or partners, run a secret/config review. Private repositories are
not a substitute for removing committed secrets.

## Related Pages

- [[Product Brief]]
- [[Feature Coverage]]
- [[HomeScout Accelerator Shortlist]]
- [[Market Landscape And Product Lessons]]
- [[Component Architecture]]
- [[RAG Architecture]]
- [[Work Tracks]]
