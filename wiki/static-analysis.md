# Static Analysis

HomeScout runs static analysis **locally**, driven by one script that every agent and CI share.
It replaces the CodeQL scanning that went dormant when the repo went private (CodeQL result
upload needs GitHub Advanced Security, which is not available on free private repositories — see
`.github/workflows/codeql.yml`). Rather than a scan we cannot read, we run tools whose output we
read directly.

## Source of truth

[`scripts/static-analysis.sh`](../scripts/static-analysis.sh) is the single implementation. It is
invoked by:

- **Humans / any agent** — run it directly.
- **CI** — `.github/workflows/static-analysis.yml` on every PR.
- **Claude** — the `static-analysis` skill (`.claude/skills/static-analysis/`) wraps it.
- **Codex / Copilot** — referenced from [`AGENTS.md`](../AGENTS.md) (canonical agent instructions).

Keeping the logic in a script — not in the Claude skill — is deliberate: a `SKILL.md` is a
Claude-only construct, so if the capability lived there, other agents could not use it. The skill
is just Claude's front door to the shared script, the same pattern `scripts/quality-gate.sh` uses.

## Surfaces and tools

| Surface | Tool | Install |
|---|---|---|
| C# + TypeScript cyclomatic complexity | [Lizard](https://github.com/terryyin/lizard) | `pipx install lizard` |
| .NET code smells (unused/redundant code, naming, null-contract) | JetBrains InspectCode (`jb`) | `dotnet tool install -g JetBrains.ReSharper.GlobalTools` |
| GitHub Actions workflows | [actionlint](https://github.com/rhysd/actionlint) | `brew install actionlint` |
| Frontend smells + complexity budget | ESLint (`pnpm run lint`) | already in `frontend/` |

InspectCode supports the `.slnx` solution format (verified against `dotnet/HomeScoutCopilot.slnx`
with ReSharper GlobalTools 2026.1). The frontend complexity budget lives in
`frontend/eslint.config.js` as `complexity` / `max-depth` / `max-lines-per-function` /
`max-params`, wired as **warnings** so it surfaces without failing `frontend-ci`.

## Usage

```bash
scripts/static-analysis.sh all          # everything (advisory)
scripts/static-analysis.sh complexity   # Lizard only (fast)
scripts/static-analysis.sh inspect      # InspectCode only (builds the solution)
scripts/static-analysis.sh actions      # actionlint only
scripts/static-analysis.sh all --strict # non-zero exit if any surface has findings
```

Machine-readable reports land in the gitignored `artifacts/static-analysis/`
(`lizard-*.csv`, `inspectcode.sarif`).

## Posture: advisory

Findings are **reported but never fail** the run (the script exits 0 unless `--strict`). This is
intentional:

- It is safe inside `scripts/quality-gate.sh` (added as an advisory step, not folded into the
  pass/fail decision).
- The CI job (`static-analysis.yml`) prints findings to the job log + step summary and uploads the
  reports as the `static-analysis-reports` artifact, but does not block a merge.
- SARIF is **not** uploaded to GitHub code scanning (that needs GitHub Advanced Security, the same
  limitation that made CodeQL dormant on this private repo).

Tighten to blocking later — set thresholds and use `--strict` in CI, and flip the ESLint
complexity rules from `warn` to `error` — once the baseline is comfortably under budget.

## Current baseline (2026-07-05) — clean

The first run's findings were all triaged and cleared, so the tool now starts from zero:

- **Complexity:** 0 over the default gate. The two C# offenders were refactored — the Evaluator
  `switch` (CCN 27) split into per-verb handlers, and `RecordAuthenticatedUserAsync` (CCN 17) had
  its claim-resolution chains extracted into helpers. Frontend: `renderInlineMarkdown` split into
  inline matchers, `EstimatorPanel` extracted an `EstimateResult` subcomponent, and `App` moved its
  workspace grid into `WorkspaceBody`. All under budget.
- **InspectCode:** 0 at WARNING+. ReSharper `cleanupcode` (redundancy-only profile in
  `dotnet/HomeScoutCopilot.slnx.DotSettings`) removed 36 redundant usings/qualifiers; two genuine
  Aspire-scaffold nits were fixed in code; the rest were intentional conventions or false positives,
  accepted via `dotnet/.editorconfig` (see below).
- **actionlint:** clean.

## Accepted conventions (why some inspections are configured off)

These decisions live in `dotnet/.editorconfig` (InspectCode reads `resharper_*_highlighting`), each a
deliberate call that a finding is *not* a defect — not an ignored pile. Genuine issues are fixed in code.

- **`CheckNamespace` — off.** HomeScout uses one root namespace per project; folders organise files.
  The API endpoints project happens to mirror folders, but that is not a required convention, and
  Aspire's `ServiceDefaults` intentionally uses `namespace Microsoft.Extensions.Hosting` so its
  extension methods are discoverable. Mass-renaming 37 files (26 in `API.Service`) plus their 64
  consumers to satisfy a cosmetic advisory finding is disproportionate.
- **`NotAccessedPositionalProperty.Global` — off.** Records in `*.Shared.Contracts` and the evaluator
  dataset are (de)serialization DTOs; their positional properties are read across the JSON boundary
  (API responses, the React client), which the in-solution reachability analysis cannot see.
- **Nullable-contract + redundant-`!` inspections — off in `tests/` and `tools/` only** (active in
  product `src/`). Test fixtures use the `= null!` late-init pattern with defensive teardown guards,
  and live/external test helpers use redundant null-forgiving (`!`) after their own env-var guards.

The **frontend** `max-lines-per-function` rule was dropped (cyclomatic `complexity`, `max-depth`, and
`max-params` remain): raw line count is a poor signal for verbose-but-simple JSX view components and
test bodies. Severity mechanism note: prefer `.editorconfig` (`resharper_*_highlighting`) over the
solution `.DotSettings` for portability.

## Related

- [[Testing Strategy]] — test framework and verification patterns.
- [[Coding Conventions]] — the conventions these tools help enforce.
- [[Log]] — where baseline changes are recorded.
