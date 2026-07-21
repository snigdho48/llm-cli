# LLM CLI — Eval Harness

Eval-driven development (EDD) for production readiness. Run before and after every phase.

## Quick start

```powershell
# Full harness (build + test + smoke)
.\scripts\harness.ps1

# Strict mode (fails on warnings)
.\scripts\harness.ps1 -Strict

# Continuous verification loop (every 5 minutes)
.\scripts\loop-verify.ps1 -IntervalMinutes 5
```

## Eval types

| Type | Location | Purpose |
|------|----------|---------|
| Capability | `evals/capability/` | New feature pass/fail criteria |
| Regression | `evals/baseline.json` | Must-not-break checklist |
| Human | Manual | Cursor + inference check before a release |

## Metrics

- **pass@1** — first harness run passes (required for capability evals)
- **pass^3** — three consecutive harness passes (required for release sign-off)

## Adding an eval

1. Create `evals/capability/<feature>.md` with success criteria
2. Add check to `scripts/harness.ps1` if automatable
3. Update `evals/baseline.json` regression list
4. Run `.\scripts\harness.ps1` before release sign-off
