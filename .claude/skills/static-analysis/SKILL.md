---
name: static-analysis
description: Run HomeScout's local static analysis — cyclomatic complexity (Lizard, C#/TS), .NET code smells (JetBrains InspectCode), and GitHub Actions linting (actionlint). Use when asked to measure complexity, find overly complex methods, check maintainability, detect code smells / unused code / redundant usings, lint workflows, or run static analysis. Replaces the CodeQL scanning that went dormant when the repo went private.
---

# HomeScout Static Analysis

This is the **Claude-facing front door** to HomeScout's static analysis. The capability itself
lives in [`scripts/static-analysis.sh`](../../../scripts/static-analysis.sh) so that CI and every
agent (Codex/Copilot via `AGENTS.md`) share one implementation. Prefer running the script; this
page explains what it does, how to read the output, and the fallbacks.

## Run it

```bash
scripts/static-analysis.sh all          # complexity + InspectCode + actionlint (advisory)
scripts/static-analysis.sh complexity   # Lizard only (fast, no build)
scripts/static-analysis.sh inspect      # JetBrains InspectCode only (builds the solution)
scripts/static-analysis.sh actions      # actionlint only (fast)
scripts/static-analysis.sh all --strict # exit non-zero if any surface has findings
```

Posture is **advisory**: findings are reported but never fail the run (so it is safe in the
quality gate). Machine-readable reports land in `artifacts/static-analysis/` (gitignored):
`lizard-csharp.csv`, `lizard-typescript.csv`, `inspectcode.sarif`.

## What each surface covers

| Surface | Tool | Notes |
|---|---|---|
| C# + TypeScript complexity | [Lizard](https://github.com/terryyin/lizard) | Cyclomatic complexity, NLOC, params. Default gate = CCN>15; strict candidates = CCN>10. |
| .NET code smells | JetBrains InspectCode (`jb`) | Unused/redundant code, naming, null-contract issues. SARIF output. Supports the `.slnx` solution. |
| GitHub Actions workflows | [actionlint](https://github.com/rhysd/actionlint) | Workflow syntax + shellcheck of `run:` blocks + missing-permissions class. |
| Frontend smells + complexity | ESLint | Runs via `pnpm run lint` (not this script) — `complexity`/`max-depth`/`max-lines-per-function`/`max-params` are wired as **warnings** in `frontend/eslint.config.js`. |

## Prerequisites (each tool self-skips with an install hint if absent)

```bash
pipx install lizard                                        # or: pip install lizard
dotnet tool install -g JetBrains.ReSharper.GlobalTools     # provides `jb`
brew install actionlint                                    # or `go install github.com/rhysd/actionlint/cmd/actionlint@latest`
```

## Interpreting results

**Complexity (Lizard).** Columns: NLOC, CCN (cyclomatic complexity), token, PARAM, length.

| CCN | Risk | Action |
|---|---|---|
| 1–10 | Low/moderate | Fine |
| 11–15 | High | Refactor candidate — extract helpers, simplify conditionals |
| 16+ | Over the default gate | Prioritise refactoring |

The `*global*` pseudo-function Lizard emits for file scope is filtered out of the candidate
list as noise. Present warnings first, group by file, sort by CCN, and offer to refactor the
worst offenders.

**InspectCode (SARIF).** Group by severity then file; include the `ruleId` (e.g.
`RedundantUsingDirective`, `UnusedMember.Global`). If there are many low-value suggestions,
summarise the top rules by count and offer to apply the safe fixes. To change a rule's severity,
add a solution `.DotSettings` layer next to the `.slnx` rather than suppressing inline.

**actionlint.** Fix workflow issues directly — most are real (undefined `needs`, bad `if:`
expressions, missing `permissions`, shellcheck findings in `run:` blocks).

## CI

`.github/workflows/static-analysis.yml` runs this script on every PR as an **advisory** job:
it prints findings to the log + step summary and uploads `artifacts/static-analysis/` as the
`static-analysis-reports` artifact. It never blocks a merge. SARIF is **not** uploaded to GitHub
code scanning because that needs GitHub Advanced Security (unavailable on this free private repo —
the same limitation that made `codeql.yml` dormant). If the repo is made public again, CodeQL
reactivates alongside this workflow.

## Fallbacks

- **`jb` cannot open the solution** — InspectCode supports `.slnx` as of 2026.1, so this should
  not happen. If a future toolchain regresses, point `jb inspectcode` at a project glob or
  generate a temporary `.sln`, and note it in `wiki/static-analysis.md`.
- **Lizard TypeScript parse gaps** — Lizard's TS support is good for complexity but not a full
  type-aware analysis; ESLint (`pnpm run lint`) is the authoritative frontend smell checker.

## Cleanup

Reports live under the gitignored `artifacts/static-analysis/`; no cleanup is required. Remove
the directory if you want a clean slate: `rm -rf artifacts/static-analysis`.
