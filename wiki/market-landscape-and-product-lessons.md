# Market Landscape And Product Lessons

This page captures the UK property/renting products reviewed while shaping HomeScout's
accelerator direction. The goal is not to copy competitors. The goal is to understand what users
already expect, what is missing, and which patterns HomeScout can adapt in its own evidence-backed
decision-pack workflow.

HomeScout's target gap:

> A renter/buyer due-diligence workflow that turns saved listings into an evidence-backed decision
> pack covering hidden costs, area context, missing information, red flags, viewing questions, and
> follow-up Q&A.

## Summary Finding

There are many adjacent products, but the market appears fragmented:

- Portals own discovery and saved listings.
- AI search products improve discovery.
- Data/reporting tools serve professionals.
- Area tools explain postcodes.
- Rental platforms handle referencing, onboarding, deposits, and tenancy workflows.
- Moving platforms match buyers with conveyancers, surveyors, brokers, and removals.

HomeScout should not claim that "nothing exists". The stronger claim is:

> Existing products focus on search, data, or transactions. HomeScout focuses on the decision moment
> between finding a listing and committing to a viewing, application, deposit, or offer.

## Portals

### Rightmove

Source: [Rightmove](https://www.rightmove.co.uk/)

What it does:

- UK's dominant property portal for buying, renting, sold prices, agents, mortgages, guides, and
  saved-property workflows.
- Public homepage exposes "AI Search" and account features for saving properties, alerts, and
  enquiries.
- Offers mortgage-related journeys, valuation, affordability, house-price, and guide content.

What HomeScout can learn:

- Users already expect saved properties, alerts, and a familiar listing-first workflow.
- Search is not the wedge; Rightmove already owns attention and listing supply.
- A browser-extension or link-import flow must respect terms and avoid unsupported scraping.

What to include in HomeScout:

- Import from portal link or manual listing entry.
- Saved shortlist cards.
- "Analyse this listing" rather than "replace Rightmove".
- Deep links back to source listings.
- Evidence provenance and terms-aware import notes.

What not to compete on first:

- Full marketplace search.
- Agent advertising network.
- Property portal inventory.

### Zoopla

Source: [Zoopla](https://www.zoopla.co.uk/)

What it does:

- Search for buying/renting, house prices, valuations, "My Home", affordability, mortgages, agents,
  and travel-time search.
- Homepage uses "Just ask Zoopla" language, suggesting conversational or guided search positioning.

What HomeScout can learn:

- Consumers already expect estimates, valuations, affordability, and area discovery in the same
  property journey.
- The word "ask" is becoming normal in property search, so HomeScout needs a sharper decision-pack
  workflow rather than generic chat.

What to include in HomeScout:

- Clear distinction between listing facts, estimates, and assumptions.
- Affordability/cost assumptions surfaced in plain language.
- Travel-time comparison as a first-class dimension.
- User-owned shortlist rather than a single address lookup.

### OnTheMarket

Source: [OnTheMarket](https://www.onthemarket.com/)

What it does:

- Property portal for sale and rent, alerts, sold prices, instant valuation, rent checker, and
  agent discovery.
- "Help Me Choose", keywords, and travel-time search support more guided discovery.
- "Only With Us" gives users a reason to check the portal directly.

What HomeScout can learn:

- Guided selection is already a user expectation.
- Keywords and preference capture help reduce search overload.
- Alerts and "serious property search" framing are strong conversion hooks.

What to include in HomeScout:

- Preference capture: commute, budget, schools, furnishing, outdoor space, risk tolerance.
- A decision score or recommendation should be explainable, not a black-box "best" label.
- Listing-comparison questions generated from user preferences.

## AI Search And Discovery Products

### Jitty

Source: [Jitty](https://jitty.com/)

What it does:

- AI-powered UK property search.
- Lets users search by natural language, commute time, style/character, price per square foot, and
  unusual property features.
- Messaging is "find the exact home you're looking for".

What HomeScout can learn:

- AI search is a live competitor category.
- Lifestyle language and natural search make property discovery feel less rigid.
- Price transparency and price per square foot are important comparison primitives.

What to include in HomeScout:

- Price per square foot / square metre when floor area is available.
- Commute-time comparison.
- User preference extraction from natural language.
- "Why this may or may not fit you" explanations.

How to differentiate:

- Jitty is primarily discovery. HomeScout should own due diligence after the user has shortlisted
  homes.

### HomeFinder AI

Source: [HomeFinder AI](https://www.homefinder.ai/)

What it does:

- London-focused home-finding service with AI/human concierge positioning.
- Emphasises off-market homes, personalised suggestions, and one point of contact.
- More like a buying-agent/search concierge than a self-serve decision-pack tool.

What HomeScout can learn:

- High-intent buyers value hand-holding and curated recommendations.
- "One point of contact" reduces the cognitive load of speaking to many agents.
- Off-market access is a strong but partnership-heavy differentiator.

What to include in HomeScout:

- Concierge-like guided questions without promising human service.
- "Ask better questions before contacting agents" as a lightweight version of a buying-agent
  workflow.
- Optional expert-review pathway later, but not in the first MVP.

How to differentiate:

- HomeScout can be self-serve, renter-friendly, and listing-agnostic, not only a London/off-market
  buyer concierge.

## Property Data And Report Platforms

### Sprift

Source: [Sprift](https://sprift.com/)

What it does:

- Property data platform for professionals: estate/letting agents, surveyors, mortgage
  professionals, conveyancers, investors, developers, and API users.
- Provides dashboards, property reports, comparables, prospecting, and API/data products.
- Claims 300+ data points on over 30 million UK residential properties.

What HomeScout can learn:

- Property reports and data aggregation are valuable and already proven in B2B.
- Professional users need credibility, speed, comparables, and early risk detection.
- White-labelled reports are a commercial model.

What to include in HomeScout:

- Evidence pack/export as a first-class artifact.
- Data provenance for every claim.
- Comparable/sold-price context.
- Early risk flags.
- Optional B2B white-label report mode later.

How to differentiate:

- Sprift is professional/B2B. HomeScout should start as a renter/buyer decision assistant and may
  later partner with data providers rather than rebuild every dataset.

### Crystal Roof

Source: [Crystal Roof](https://crystalroof.co.uk/)

What it does:

- Area/postcode research for people deciding where to buy or rent.
- Covers demographics, affluence, crime, noise, transport, amenities, schools, and environment.
- Emphasises unbiased facts before committing.

What HomeScout can learn:

- Area context is a major user pain, not a side feature.
- "Unfiltered truth" and "on your side" are useful trust signals.
- Street/postcode-level evidence feels more actionable than broad regional summaries.

What to include in HomeScout:

- Area evidence panel per listing.
- Commute, schools, amenities, flood/environment, and noise where data is available.
- Avoid simplistic "safe/unsafe area" labels; use caveated evidence and comparisons.
- A "questions this area raises" section in the decision pack.

How to differentiate:

- Crystal Roof is area-first. HomeScout should combine area evidence with listing facts, true cost,
  missing data, and shortlist comparison.

### Property Log

Source: [Property Log](https://www.propertylog.net/)

What it does:

- Chrome extension for tracking Rightmove price changes and relisted properties.
- Focused, lightweight, and useful for price transparency.
- Explicitly states it is not affiliated with Rightmove.

What HomeScout can learn:

- A narrow browser-extension utility can win user trust if it solves one painful job.
- Price-change history is a valuable signal for buyers and renters.
- Extensions are a good validation path, but affiliation/terms clarity matters.

What to include in HomeScout:

- Price-change/relisted-property field where available.
- Browser-extension demo later: "Analyse with HomeScout".
- Clear source and affiliation disclaimers.

## Transaction And Journey Platforms

### OneDome

Source: [OneDome](https://www.onedome.com/)

What it does:

- End-to-end homebuying service: property search plus mortgage, legal work, surveys, buyer
  protection, and a dedicated property moving assistant.
- Sells a bundled HomeBuyer Service with fixed pricing and fall-through protection.

What HomeScout can learn:

- The buying process is stressful because users must coordinate disconnected professionals.
- Bundled, guided workflows are commercially valuable.
- Post-offer support is a distinct product category.

What to include in HomeScout:

- Pre-offer/pre-application decision pack as the upstream step before services like mortgage,
  conveyancing, and surveys.
- Partner handoff slots: broker, surveyor, conveyancer, relocation adviser.
- Fall-through and missing-information risk education without pretending to replace professional
  advice.

How to differentiate:

- OneDome helps manage the transaction after a user is ready to buy. HomeScout should help users
  decide which listings are worth pursuing before they commit.

### OpenRent

Source: [OpenRent](https://www.openrent.co.uk/)

What it does:

- Rental marketplace connecting private landlords and tenants.
- Emphasises no admin fees, no dead listings, and rent/deposit protection.
- Also supports landlord/tenant processes such as contracts, referencing, deposits, and rent
  collection.

What HomeScout can learn:

- Renters care about safety, fees, dead listings, and deposit/rent protection.
- "Safer, faster, cheaper" is strong renter messaging.
- Rental workflows have sharp pre-commitment moments: viewing, holding deposit, referencing,
  contract, move-in money.

What to include in HomeScout:

- Renter-specific red flags and missing-info checks.
- Upfront cost summary: rent, deposit, holding deposit, first month, council tax, estimated bills.
- Questions before paying a holding deposit.
- Link to official guidance when discussing rights/fees.

How to differentiate:

- OpenRent is a marketplace/transaction flow. HomeScout can analyse listings from any source and
  help renters compare before choosing where to apply.

## Renting Infrastructure And Referencing

### Canopy

Source: [Canopy](https://www.canopy.rent/)

What it does:

- RentPassport, renter readiness, referencing, and moving services for renters, agents, and
  landlords.
- Positions itself around "better renting".

What HomeScout can learn:

- "Renter readiness" is a real category.
- Renters may value a portable profile or readiness checklist before applying.

What to include in HomeScout:

- Renter readiness checklist in the decision pack.
- "Before you apply" section: documents, deposit, guarantor, affordability assumptions, questions.
- Optional future export that prepares users for referencing without becoming a referencing product.

### Goodlord

Source: [Goodlord](https://www.goodlord.com/)

What it does:

- Letting-agent platform covering referencing, rent protection, tenant payments, contracts,
  tenancy progression, utilities, and compliance.
- Uses AI for parts of identity/financial processing while keeping human control.

What HomeScout can learn:

- Lettings software buyers care about compliance, admin reduction, fraud, payments, and smooth
  progression.
- "AI-enhanced, human-controlled" is a useful trust framing.

What to include in HomeScout:

- Human-checkable evidence and assumptions.
- Agent-facing pilot option: better-qualified applicants and fewer repetitive questions.
- Compliance-aware language; avoid tenancy/legal advice.

How to differentiate:

- Goodlord is post-application/agent operations. HomeScout can sit before application as a renter
  decision and preparation tool.

### RentProfile

Source: [RentProfile](https://www.rentprofile.co/)

What it does:

- Onboarding, tenant referencing, Right to Rent, rent guarantee, landlord AML, digital signing, and
  tenancy progression.
- Pricing and workflow are agent/landlord operational rather than consumer comparison.

What HomeScout can learn:

- Referencing and onboarding are measurable workflows with clear conversion/time-to-complete value.
- Fraud, identity, Right to Rent, and deposit registration matter after a renter chooses a property.

What to include in HomeScout:

- Early warning checklist: verify agent/landlord, avoid paying before viewing/verification, ask
  about deposit scheme, check permitted fees.
- Integration/pilot idea: send a decision-pack summary into referencing/onboarding after the user
  decides to apply.

## Reviews, Guides, And Service-Matching

### HomeViews

Source: [HomeViews](https://www.homeviews.com/)

What it does:

- Verified resident reviews for new-build homes to rent and buy.
- Strongest around developments, resident experience, developer/building management, and locality.

What HomeScout can learn:

- Resident reviews are a powerful evidence type that raw listing data cannot replace.
- "Verified resident" signals trust.

What to include in HomeScout:

- Future evidence source: resident reviews where available.
- User notes and post-viewing reflections in the shortlist.
- "What residents mention" section if using a licensed/reliable review source.

### Konnect You / Compare My Move

Source: [Konnect You](https://www.konnectyou.com/)

What it does:

- Matches users with verified property-service professionals: conveyancers, surveyors, mortgage
  brokers, removals, and related services.
- Publishes cost guides and calculators reviewed by experts.

What HomeScout can learn:

- Buyers/renters need trusted next-step services after deciding.
- Expert-reviewed guides help build trust.
- Lead generation can be a business model if user value comes first.

What to include in HomeScout:

- "Next professional to speak to" suggestions after the decision pack.
- Optional partner marketplace later.
- Expert-reviewed guidance pages for common questions.
- Do not push partner leads before the user has enough value/trust.

### Reallymoving

Source: [Reallymoving](https://www.reallymoving.com/)

What it does:

- Quote comparison for conveyancing, surveyors, removals, and other moving services.
- A downstream moving-service marketplace.

What HomeScout can learn:

- Once a buyer chooses a property, the next jobs are survey, conveyancing, mortgage, and removals.
- Service matching is adjacent but not the core wedge.

What to include in HomeScout:

- Buyer decision pack can end with "likely next checks" rather than direct advice.
- Later monetisation: referrals to surveyors, conveyancers, brokers, and removals.

## Secondary Search Portals

### Homesearch

Source: [Homesearch](https://www.homesearch.co.uk/)

What it does:

- UK property search site with sale and rental listings from estate agencies.
- Focuses on fresh listings, property database, and smart filters.

What HomeScout can learn:

- There are smaller search portals and agent-data businesses beyond the big three.
- Smaller platforms may be more realistic early partners than Rightmove/Zoopla.

What to include in HomeScout:

- Partnership path for smaller portals: decision-pack add-on that increases listing engagement.
- Avoid requiring one specific portal source.

## Generic ChatGPT Risk

ChatGPT can already help a user compare pasted listings. HomeScout's defence is that it is not just
a chat prompt.

HomeScout must be:

- structured workflow: listing capture, shortlist, comparison, decision pack
- evidence layer: every claim has source/provenance
- domain guardrails: not mortgage advice, not legal advice, not tenancy advice
- repeatable output: consistent cost assumptions and checks
- shareable artifact: report users can send to a partner, parent, broker, agent, or adviser
- data model: properties, assumptions, evidence, missing fields, risks, user preferences, notes

Pitch line:

> ChatGPT is a general assistant. HomeScout is the product, workflow, data layer, and trust layer for
> housing decisions.

## Feature Backlog Inspired By The Market

Near-term MVP:

- Manual listing entry and paste-link capture.
- Listing fact extraction with user confirmation.
- Renter/buyer mode.
- True monthly cost estimate.
- Upfront cost summary.
- Missing-information checklist.
- Side-by-side comparison.
- Evidence panel.
- Viewing/application/offer questions.
- Shareable decision pack.

Differentiators to prioritise:

- "What is missing from this listing?" score.
- "What should I ask before viewing/applying/offering?" section.
- Hidden-cost comparison.
- Area evidence without simplistic labels.
- Source-linked public data.
- User preference fit explanation.
- Renter-first pack for fast validation.

Later:

- Browser extension: "Analyse with HomeScout".
- Price-change/relisted-property tracking where allowed.
- Resident-review evidence integration.
- White-label agent/relocation/broker report.
- Partner handoff to mortgage broker, surveyor, conveyancer, relocation adviser, or tenant-support
  organisation.
- Data-provider partnerships for property reports and comparables.

## Competitive Positioning

Avoid:

> We are an AI property search engine.

Use:

> HomeScout is the evidence-backed decision layer between finding a listing and committing to it.

More detailed:

> Portals help users find homes. Data platforms help professionals understand properties. Rental
> platforms help agents and tenants complete applications. HomeScout helps renters and buyers compare
> shortlisted homes, spot hidden costs, understand local context, and ask better questions before
> they commit.

## Open Research Questions

- Which first user segment has the strongest pull: London renters, international movers,
  first-time buyers, or relocation clients?
- Will users pay directly for a decision pack, or is partner distribution stronger?
- Which public datasets can be used reliably and legally for MVP evidence?
- How much manual confirmation is acceptable before the product feels useful?
- Which evidence dimensions matter most: true cost, commute, area, schools, safety, lease/rental
  terms, price history, or missing information?
- Which B2B partner feels the pain first: small letting agents, mortgage brokers, relocation
  advisers, or smaller portals?

## Related Pages

- [[Accelerator Product Direction]]
- [[Product Brief]]
- [[Feature Coverage]]
- [[RAG Architecture]]
