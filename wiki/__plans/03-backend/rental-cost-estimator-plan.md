# Rental Cost Estimator Plan

**Status:** In progress — the first **renter** feature, extending HomeScout from buying-only to
**renting and buying** ([[Accelerator Product Direction]]: renting is the strongest first wedge).

## Why

The [[Accelerator Product Direction]] MVP calls for an "estimated true monthly cost" for renters.
This mirrors the existing **mortgage cost estimator** — a deterministic, offline, FluentResults
calculator surfaced as a copilot tool + REST endpoint + structured evidence. The rental estimator is
the *second instance* of that proven pattern; ~90% of the pipeline (tool → evidence → copilot →
threads → eval) is reused unchanged.

## Deterministic renter figures (what makes this reliable)

- **Tenancy deposit cap** — Tenant Fees Act 2019 (England): **5 weeks' rent** if annual rent
  < £50,000, else **6 weeks**. Pure calc from rent.
- **Holding deposit cap** — **1 week's rent** (typically credited toward the first month/deposit).
- **First month's rent** — upfront.
- **Weekly rent** — monthly × 12 / 52 (the legal basis for the caps).
- **Council tax** — deterministic given the band (band × the council's published band rate). Taken
  as a monthly **input** for now; a VOA band-by-postcode lookup is a later public-data integration
  (seam-first, like the BoE base rate). Flagged as *missing* when not provided.
- **Bills** — user-provided **estimate** (not sourced); included when given, flagged as an
  assumption/missing otherwise.

Nation caveat: caps are England (Tenant Fees Act 2019). Wales is similar; Scotland/NI differ — stated
in the caveats.

## Contract

- `RentalCostRequest(MonthlyRent, MonthlyCouncilTax?, EstimatedMonthlyBills?)` — rent required;
  council tax + bills optional (null ⇒ excluded from the monthly total and flagged).
- `RentalCostResult(WeeklyRent, DepositWeeks, TenancyDeposit, HoldingDeposit, FirstMonthRent,
  UpfrontCost, TotalMonthlyCost, Assumptions, Caveats)` where `UpfrontCost = FirstMonthRent +
  TenancyDeposit` and `TotalMonthlyCost = MonthlyRent + councilTax + bills`.

## Slice (mirrors the mortgage estimator)

1. **Contracts** — `Shared/Contracts/RentalContracts.cs`.
2. **Service** — `API.Service/Rental/RentalCostEstimator.cs` (`IRentalCostEstimator`, deterministic,
   FluentResults validation) + comprehensive offline unit tests.
3. **REST endpoint** — `POST /api/rental/estimate` (mirrors `/api/mortgage/estimate`).
4. **Copilot tool** — `estimate_rental_cost` in `HomeScoutAgentTools` (+ registered in DI /
   `CopilotGatewayFactory`).
5. **Evidence** — `CopilotEvidenceBuilder`: add `EstimateRentalCostToolName => FromRentalEstimate`
   (the `toolName switch` is the extension point). Monthly total + upfront + deposit as `Estimate`.
6. **Prompt v3** — teach the agent it serves **renters and buyers**, when to use
   `estimate_rental_cost` vs `estimate_mortgage`, and the renter guardrails.
7. **Guardrails** — generalise "not mortgage advice" → also **"not tenancy/letting advice"** (the
   `SafetyEvaluators` disclaimer check accepts either); keep "no safe/unsafe verdict" (both).
8. **Eval dataset** — add renter cases (rental cost, deposit, "which flat is better value after
   bills", adversarial "is this a good landlord?" probe).

## Guardrails / not-advice

- Not letting/tenancy/legal advice; not a regulated adviser.
- Deposit caps cite the Tenant Fees Act with the nation caveat.
- Bills are the user's own estimates, not sourced.
- No "good/bad landlord" or safe/unsafe verdict.

## Deferred (deterministic follow-ons, same pattern)

- **Buyer stamp duty** (SDLT/LBTT/LTT) + **Land Registry fee** — the deterministic *buyer* hidden
  costs; the strongest next buyer figures.
- **Council tax VOA lookup** (band by postcode) — turns council tax from input to sourced fact.
- **Bills-from-EPC** energy-cost estimate.

## References

- [[Accelerator Product Direction]] · [[Mortgage Cost Estimator — Design]] · [[GenAIOps Tooling Plan]]
- Tenant Fees Act 2019 (deposit/holding-deposit caps).
