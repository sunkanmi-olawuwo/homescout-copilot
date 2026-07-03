# API Vertical Slices + Validated Options — Plan (RagLab parity)

Restructure HomeScout's API to RagLab's ergonomics: **vertical slices** (Carter +
MediatR), a **validated-options** convention, and **feature/Settings folders** for easy
navigation — **without changing behaviour**. The existing contract/endpoint tests are the
behaviour-lock (they must keep passing unedited).

**Status: implemented.** Carter 10 + MediatR 12.5.0 (pinned free/Apache-2.0) +
FluentValidation 12; endpoints moved to `Features/{Status,Comparison,Mortgage,Copilot}/`
Carter modules → MediatR handlers; `BaseRateOptions`/`FoundryOptions` use the
validated-options convention. All 33 contract/BDD/endpoint tests passed **unedited** and
the Aspire boot test is green. Two divergences from this plan (recorded in Plan
Divergence): (1) the validated-options helper lives in **`.API.Service/Settings/`** (not
`.Shared`), keeping `.Shared` as pure contracts; (2) request-level FluentValidation
behaviour was **not** added — the existing in-handler checks (mortgage `Result.Fail`
→ 400; copilot empty → 400, unconfigured → 503) preserve behaviour; a MediatR validation
pipeline is an easy follow-up.

## Why

Today all endpoints are inline in `Program.cs` and `.API.Service` is a flat file list;
options are bound by hand with no validation-on-start. RagLab organises the same concerns
into self-contained `Features/<X>/` slices + a `Settings/` convention, so a feature (its
route, handler, DTOs, validation) lives in one place and bad config fails fast.

## Decision (locked)

**Full RagLab parity:** Carter (endpoint modules, auto-discovered) + MediatR
(commands/queries/handlers) + FluentValidation + a validated-options convention. Keep
HomeScout's project **roles/names** (`.API` = host, `.API.Service` = application layer,
`.Shared` = wire contracts, `.Functional` = Result→HTTP). This differs from
RagLab's project split (where `.API` is the feature library and `.API.Service` is the
host) — an intentional, recorded divergence; we adopt the *internal* organisation, not the
project-role inversion.

## Target structure

```
dotnet/src/HomeScoutCopilot.API/                 (host)
  Program.cs                                     (thin: ServiceDefaults, AddCarter,
                                                  AddMediatR, AddValidatedOptions, JSON,
                                                  DI; app.MapCarter())
  Features/
    Status/StatusEndpoints.cs                    (GET /api/status  → GetStatus query)
    Comparison/ComparisonEndpoints.cs            (GET /api/comparison/sample)
    Mortgage/
      MortgageEndpoints.cs                       (POST /api/mortgage/estimate,
                                                  GET /api/mortgage/base-rate)
      EstimateMortgage.cs                        (command + handler → IMortgageCostEstimator)
      GetBaseRate.cs                             (query + handler → IBaseRateProvider)
      EstimateMortgageValidator.cs               (FluentValidation on the request)
    Copilot/
      CopilotEndpoints.cs                        (POST /api/copilot/ask)
      AskCopilot.cs                              (command + handler → IHomeScoutAgentGateway)
      AskCopilotValidator.cs
dotnet/src/HomeScoutCopilot.API.Service/          (application/domain layer — logic unchanged)
  Settings/
    BaseRateOptions.cs                            (+ IValidatedOptions + validator)
    FoundryOptions.cs                             (+ IValidatedOptions + validator)
  (existing services: MortgageCostEstimator, BankOfEnglandBaseRateProvider,
   FoundryAgentGateway, HomeScoutAgentTools, IHomeScoutService — optionally grouped into
   Mortgage/ Copilot/ subfolders)
dotnet/src/HomeScoutCopilot.Shared/
  Settings/                                       (the small validated-options helper:
    IValidatedOptions.cs, ValidatedOptionsFactory.cs, ValidatedOptions.cs)
  Contracts/                                      (wire DTOs — shared with the client; stay here)
```

Wire DTOs (`MortgageEstimateRequest`, `CopilotRequest`, …) **stay in
`.Shared`** because the typed client and tests consume them — RagLab
co-locates DTOs in the feature, but our client shares them, so only the MediatR
command/query wrappers + validators live in the feature folder.

## Patterns

**Endpoint (Carter module, thin):**
```csharp
public sealed class MortgageEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mortgage").WithTags("Mortgage");
        group.MapPost("/estimate", (IMediator m, MortgageEstimateRequest req)
                => m.Send(new EstimateMortgageCommand(req)))
            .WithName("EstimateMortgage").WithSummary("Estimate the monthly mortgage cost")
            .Produces<MortgageEstimateResult>().ProducesProblem(StatusCodes.Status400BadRequest);
        // GET /base-rate → GetBaseRateQuery ...
    }
}
```

**Handler (MediatR → the existing service; returns Result mapped via `.ToHttpResult()`):**
```csharp
public sealed record EstimateMortgageCommand(MortgageEstimateRequest Request) : IRequest<IResult>;

public sealed class EstimateMortgageHandler(IMortgageCostEstimator estimator)
    : IRequestHandler<EstimateMortgageCommand, IResult>
{
    public Task<IResult> Handle(EstimateMortgageCommand c, CancellationToken ct)
        => Task.FromResult(estimator.Estimate(c.Request).ToHttpResult());
}
```

**Validation:** FluentValidation validators per feature, run by a MediatR pipeline
behaviour (or an endpoint filter) → 400 ProblemDetails. (The estimator's own
FluentResults validation stays as the domain guard; the request validator catches shape
errors early.)

**Options:** each options class implements `IValidatedOptions<T>` (self-declares its
section name + a FluentValidation validator); a `AddValidatedOptions<T>()` helper binds
the section and validates on start, so bad config fails fast.
```csharp
public sealed class FoundryOptions : IValidatedOptions<FoundryOptions>
{
    public static string SectionName => "Foundry";
    public string ProjectEndpoint { get; set; } = "";
    public string ModelDeploymentName { get; set; } = "chat";
    public IValidator<FoundryOptions> GetValidator() => new Validator();
    private sealed class Validator : AbstractValidator<FoundryOptions> { /* rules */ }
}
```

## Feature inventory (current endpoints → slices)

| Slice | Routes | Handler → service |
| --- | --- | --- |
| Status | `GET /api/status` | `GetStatus` → `IHomeScoutService` |
| Comparison | `GET /api/comparison/sample` | `GetComparisonSample` → `IHomeScoutService` |
| Mortgage | `POST /api/mortgage/estimate`, `GET /api/mortgage/base-rate` | `IMortgageCostEstimator`, `IBaseRateProvider` |
| Copilot | `POST /api/copilot/ask` | `IHomeScoutAgentGateway` (behind config; 503 preserved) |

## Packages

- **Carter** (endpoint modules; MIT) — current stable.
- **FluentValidation** (Apache-2.0) — current stable.
- **MediatR** — ⚠️ **licensing gotcha**: MediatR v13+ requires a commercial licence
  (Lucky Penny, 2025); **v12.x is Apache-2.0 (free)**. Decision at implementation: pin
  **MediatR 12.x** (free), buy a v13+ licence, or use a free alternative (e.g. the
  `Mediator` source generator). Confirm what RagLab pins and match unless the licence
  cost is unwanted. Recommendation: **pin MediatR 12.x** for now.
- (Optional) **Asp.Versioning.Http** for `.HasApiVersion(...)` — RagLab has it; adopt only
  if we want versioned routes. Deferred by default.

## Migration steps (behaviour-locked; one gated PR, small commits)

1. Add the **validated-options** helper (`.Shared/Settings/`); migrate
   `BaseRateOptions` + `FoundryOptions` to `.API.Service/Settings/` with validators; swap
   `Program.cs` manual binds for `AddValidatedOptions<T>()`.
2. Add **MediatR** + **Carter** + **FluentValidation** to `.API`; register in `Program.cs`
   (`AddMediatR`, `AddCarter`, validation behaviour) and `app.MapCarter()`.
3. Move each endpoint into a `Features/<X>/` **Carter module + MediatR handler**, one slice
   at a time (Status → Comparison → Mortgage → Copilot). Keep the `/api` paths identical.
4. Delete the inline endpoint maps + manual option binds from `Program.cs` (now thin).
5. (Optional) group `.API.Service` services into `Mortgage/`, `Copilot/` subfolders.

## Verification

- **Behaviour-lock:** `ApiContractTests` (`/api/status`, `/api/comparison/sample`), the
  `MortgageEstimate.feature` BDD, and `CopilotEndpointTests` (200/503/400) must pass
  **unedited** — proving routes/shapes/behaviour are unchanged.
- Options: a startup test (or the existing app boot) confirms `AddValidatedOptions`
  validates; an invalid config fails fast.
- `scripts/quality-gate.sh` green; drift 0 fail.

## Acceptance criteria

- Every endpoint lives in a `Features/<X>/` Carter module delegating to a MediatR handler;
  `Program.cs` no longer maps routes directly.
- `BaseRateOptions` + `FoundryOptions` use the validated-options convention (validate-on-start).
- All existing contract/BDD/endpoint tests pass unedited; gate green.
- MediatR pinned to a licence-clear version.

## Divergence note

Project **roles/names** stay as HomeScout's (`.API` host, `.API.Service` app layer) rather
than RagLab's inverted split — record in [[Plan Divergence]] when implemented.
