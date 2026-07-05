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

## Current baseline (2026-07-05)

- **Complexity:** two C# functions over the default gate (CCN>15) — the Evaluator argument
  `switch` (CCN 27) and `Program.RecordAuthenticatedUserAsync` (CCN 17); frontend TS clean at
  default thresholds. ESLint flags `App`, `EstimatorPanel`, and `renderInlineMarkdown` as
  complexity-budget warnings.
- **InspectCode:** ~108 WARNING+ suggestions, mostly redundant usings / namespace / nullable-
  contract hints — advisory cleanup candidates, not defects.
- **actionlint:** all workflows clean.

## Related

- [[Testing Strategy]] — test framework and verification patterns.
- [[Coding Conventions]] — the conventions these tools help enforce.
- [[Log]] — where baseline changes are recorded.
