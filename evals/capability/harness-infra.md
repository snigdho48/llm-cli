# CAPABILITY EVAL: harness-infra

**Phase:** 0  
**Status:** ACTIVE

## Task

Establish eval harness and verification loop infrastructure.

## Success criteria

- [ ] `evals/` directory exists with baseline.json
- [ ] `scripts/harness.ps1` runs build + test + smoke
- [ ] `scripts/loop-verify.ps1` supports interval verification
- [ ] Core docs exist (`ARCHITECTURE`, `COMMANDS`, `MODELS`, `RELEASE`)

## Grader

```powershell
.\scripts\harness.ps1
# Expected: HARNESS: ALL PASS
```

## pass@1 target

100% on first run after Phase 0 complete.
