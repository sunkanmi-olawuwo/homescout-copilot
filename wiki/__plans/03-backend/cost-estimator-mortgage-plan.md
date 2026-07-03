# Mortgage Cost Estimator — Design (MVP)

Design for HomeScout's first deterministic capability: a **mortgage-only** monthly
cost estimator. This is the "deterministic tool before agent complexity" step in
the [phased learning and build plan](../00-roadmap/phased-learning-build-plan.md)
(Phase 2) and the recommended first tool in the
[video roadmap](../00-roadmap/video-implementation-roadmap.md).

Scope for MVP is **mortgage repayment only**. Council tax, insurance, service
charge/ground rent, stamp duty, and energy are deliberately out of scope here and
tracked as the future full "cost of ownership" composition.

## Product framing & safety boundary

HomeScout is a due-diligence assistant, **not** a regulated mortgage adviser. The
FCA regulates *advice* — a **personal recommendation to enter into a specific
regulated mortgage contract** (FCA Handbook MCOB 4.7A / 4.8). A **generic
illustrative calculator** that computes "if you borrow £X at Y% over Z years, the
monthly repayment is about £M" using a **user-supplied rate** and **no named
product** is *information*, not advice — the same thing the government-backed Money
and Pensions Service (MaPS / MoneyHelper) publishes openly.

To stay firmly on the information side:

- Never reference or recommend a specific lender or mortgage product.
- The interest rate is always a **user input** (or an explicit, clearly-labelled
  assumption) — never a rate we "recommend."
- Always return the result **with its assumptions and caveats**, and the line
  "This is an estimate, not mortgage advice — speak to a qualified mortgage adviser."
- Present output as an illustration/estimate, never as suitability or affordability
  approval.

## The calculation

Standard loan **amortisation** — the same formula MaPS/MoneyHelper and lenders use.

```
loan (P)          = propertyPrice − deposit
monthlyRate (i)   = annualRatePercent / 100 / 12
numberOfPayments  = termYears × 12   (n)

Repayment (capital + interest):
  monthlyPayment  = P · i · (1 + i)^n / ((1 + i)^n − 1)
  edge case i = 0 → monthlyPayment = P / n

Interest-only:
  monthlyPayment  = P · i

Derived:
  totalRepayment  = monthlyPayment · n          (repayment type)
  totalInterest   = totalRepayment − P          (repayment type)
                  = P · i · n                    (interest-only, over the term)
  ltvPercent      = P / propertyPrice × 100
```

Conventions (mirroring the MaPS Algorithmic Transparency Record):

- Monthly rate is the **nominal annual rate ÷ 12** (not an effective/AER
  conversion). Documented simplification; real lenders may use daily interest.
- Round money outputs to **2 decimal places**.
- **Stress test:** also compute the monthly payment at **rate + 3 percentage
  points** (MaPS does this) so buyers see rate-rise exposure.

### Worked example (illustrative — the unit test pins exact values)

`price £300,000, deposit £30,000 (10%), rate 4.5%, term 25y, repayment`
→ loan £270,000, i = 0.00375, n = 300 → monthly ≈ **£1,500.72**,
total interest ≈ £180,216, stress (+3% → 7.5%) ≈ £1,995/month.

## Inputs and outputs

Contracts live in `HomeScoutCopilot.Shared.Application`; names here are the source
of truth and must match the code (plan-sync rule).

Request — `MortgageEstimateRequest`:

| Field | Type | Notes |
| --- | --- | --- |
| `PropertyPrice` | decimal | > 0 |
| `Deposit` | decimal | ≥ 0 and < `PropertyPrice`; default 0 |
| `AnnualInterestRatePercent` | decimal | ≥ 0; sane upper bound (e.g. ≤ 25) |
| `TermYears` | int | 1–40 |
| `RepaymentType` | enum | `Repayment` \| `InterestOnly` |

Response — `MortgageEstimateResult`:

| Field | Type | Notes |
| --- | --- | --- |
| `Loan` | decimal | `PropertyPrice − Deposit` |
| `LtvPercent` | decimal | 2dp |
| `MonthlyPayment` | decimal | 2dp |
| `TotalRepayment` | decimal | repayment type only |
| `TotalInterest` | decimal | 2dp |
| `StressTest` | `{ RatePercent, MonthlyPayment }` | rate + 3pp |
| `Assumptions` | string[] | see below |
| `Caveats` | string[] | incl. the "not mortgage advice" line |

Assumptions surfaced in every response (from MaPS): the rate is constant for the
whole term; payments are monthly and on time; no fees, taxes, insurance, or other
ownership costs are included; no overpayments or early repayment.

## Validation (expected failures → FluentResults → ProblemDetails)

Return `Result.Fail` (not exceptions) with a specific message per rule; the
endpoint maps them to `400 ProblemDetails` via `.ToHttpResult()`:

- `PropertyPrice` ≤ 0
- `Deposit` < 0 or `Deposit` ≥ `PropertyPrice`
- `AnnualInterestRatePercent` < 0 or above the sane cap
- `TermYears` outside 1–40
- unknown `RepaymentType`

## Architecture placement

- **`HomeScoutCopilot.API.Service`** — `IMortgageCostEstimator` (pure, deterministic,
  no network) behind the existing `IHomeScoutService` boundary. This is where the
  formula and validation live.
- **`HomeScoutCopilot.Shared.Application`** — the request/result DTOs above.
- **`HomeScoutCopilot.Functional`** — existing `Result → ProblemDetails` mapping.
- **`HomeScoutCopilot.API`** — endpoint `POST /api/mortgage/estimate` (thin: bind →
  service → `.ToHttpResult()`). Composable into a future comparison workflow.
- **`HomeScoutCopilot.API.Client`** — `EstimateMortgageAsync(request)` typed method.

No external calls in the estimator itself: it is fully offline and deterministic.
The BoE base rate is surfaced **separately** as *rate context only* via
`IBaseRateProvider` / `GET /api/mortgage/base-rate` — a live Bank of England
Interactive-Database fetch (series `IUDBEDR`), cached ~1 day, with a resilient
fallback to a configured last-known value; it never throws and is never used as a
default the estimator computes with (base rate ≠ product rate). Implemented ahead of
the estimator; see [[Component Architecture]] and [[Endpoint Summary]]. The live
fetch is **verified end-to-end** (real BoE call through the wired app) and kept
verified by the nightly `external-checks.yml` workflow.

## Testing

- **Unit** (`HomeScoutCopilot.Functional.Test` / a new service test project or the
  API.Test unit section):
  - Known-value vectors (e.g. the worked example) to the penny with the defined
    rounding.
  - `i = 0` edge case → `P / n`.
  - Interest-only path.
  - Monotonicity sanity (higher rate/term ⇒ higher payment/interest).
  - Each validation failure returns `Result.Fail` with the expected message.
- **BDD** (`HomeScoutCopilot.API.Test`, Reqnroll): a `MortgageEstimate.feature`
  scenario — e.g. *"Given a £300,000 property with a 10% deposit at 4.5% over 25
  years, When I estimate the monthly mortgage cost, Then the monthly payment is
  about £1,500 and the result is labelled not mortgage advice."*
- No live network in any test.

## Out of scope (MVP) / future

- Full monthly cost of ownership: council tax (VOA band + local authority rates),
  buildings insurance, service charge / ground rent (from the case file), energy
  (EPC / Ofgem).
- Upfront costs: SDLT (HMRC) / LBTT (Revenue Scotland) / LTT (Welsh Revenue
  Authority), legal, survey, arrangement fees.
- APRC, product comparison, variable/offset rates, daily-interest accrual.
- Statutory rates (SDLT, council tax) are data that changes — when those phases
  land they must be **verified against the authoritative source at implementation
  time**, per the engineering standard, not hardcoded from memory.

## Sources

- MaPS Mortgage Repayment Calculator — methodology (amortisation, monthly rate,
  2dp rounding, assumptions, +3% stress): GOV.UK Algorithmic Transparency Record —
  https://www.gov.uk/algorithmic-transparency-records/money-and-pensions-service-mortgage-repayment-calculator
- MoneyHelper mortgage calculators (consumer-facing reference):
  https://www.moneyhelper.org.uk/en/homes/buying-a-home/mortgage-calculator
- Interest-only vs repayment (MoneyHelper):
  https://www.moneyhelper.org.uk/en/homes/buying-a-home/mortgage-repayment-options
- FCA Handbook MCOB 4 — advised vs non-advised/execution-only (advice = personal
  recommendation on a specific regulated mortgage contract):
  https://handbook.fca.org.uk/handbook/mcob4
- Bank of England Bank Rate (context only): https://www.bankofengland.co.uk/monetary-policy/the-interest-rate-bank-rate
