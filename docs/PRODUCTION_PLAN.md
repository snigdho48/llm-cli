# Production Plan — LLM CLI

**Goal:** Ship a production-ready CLI installable via `winget install llm-cli`, with eval-driven development (harness engineering) and continuous verification loops (loop engineering).

**Target hardware:** HP EliteBook 840 G8 · i5-1145G7 · 32 GB · Iris Xe · Qwen2.5-Coder 7B

**Methodology:**
- **Harness engineering** — define pass/fail evals BEFORE each phase; run `scripts/harness.ps1` after every change
- **Loop engineering** — `scripts/loop-verify.ps1` re-runs harness on interval until all evals pass

---

## Production readiness criteria

| Gate | Eval ID | Pass condition |
|------|---------|----------------|
| Build | `REG-BUILD` | `dotnet build` → 0 errors, 0 warnings |
| Unit tests | `REG-TEST` | `dotnet test` → 100% pass |
| Smoke CLI | `CAP-SMOKE` | `help`, `version`, `model search` exit 0 |
| Harness | `REG-HARNESS` | `scripts/harness.ps1` → all checks PASS |
| Docs | `CAP-DOCS` | All files in `docs/` index present |
| Install | `CAP-INSTALL` | `scripts/install.ps1` publishes exe |
| CI | `CAP-CI` | GitHub Actions green on push |

**Metrics:** regression evals require pass^3 (3 consecutive harness passes). Capability evals require pass@1.

---

## Phase 0 — Harness & loop infrastructure

**Status:** COMPLETE (pending Phase 7 human sign-off)

### Eval definition

```markdown
[CAPABILITY EVAL: harness-infra]
- [ ] evals/ directory with baseline + capability specs
- [ ] scripts/harness.ps1 runs build + test + smoke
- [ ] scripts/loop-verify.ps1 runs harness on interval
- [ ] docs/PRODUCTION_PLAN.md tracks phase status
```

### Deliverables
- `evals/README.md`, `evals/baseline.json`, `evals/capability/*.md`
- `scripts/harness.ps1`, `scripts/loop-verify.ps1`

---

## Phase 1 — Production hardening

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: prod-hardening]
- [ ] Semantic versioning in csproj (1.0.0)
- [ ] Commands return exit codes (0=ok, 1=fail)
- [ ] Dead stub files removed
- [ ] Config migration for missing fields on load
```

### Deliverables
- `Directory.Build.props` or `LLM.CLI.csproj` version
- `ICommand` returns `Task<int>`
- Clean `src/LLM.CLI/{Config,Doctor,Model}/` stubs

---

## Phase 2 — Runtime install from GitHub

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: runtime-install]
- [ ] llm runtime install downloads win-vulkan-x64 release
- [ ] Extracts to <workspace>/runtimes/llama.cpp/current/
- [ ] Updates config.runtime.executablePath
- [ ] Falls back message if no matching asset
- [ ] Unit test: asset name resolution (no network)
```

### Deliverables
- `RuntimeInstallService.cs`
- `runtime install` subcommand

---

## Phase 3 — Cursor auto-config

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: cursor-write]
- [ ] llm cursor --write patches or creates settings snippet file
- [ ] llm cursor --json outputs machine-readable config
- [ ] Detects %APPDATA%\Cursor\User\settings.json on Windows
```

### Deliverables
- Enhanced `CursorCommand` + `CursorSettingsService`

---

## Phase 4 — Benchmark harness

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: bench]
- [ ] llm bench runs timed inference against running server
- [ ] Reports tokens/sec estimate
- [ ] Suggests profile (cpu-only vs iris-xe-coding)
- [ ] Works offline in test with mocked timing logic
```

### Deliverables
- `BenchService`, `BenchCommand`

---

## Phase 5 — CI/CD pipeline

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: ci]
- [ ] .github/workflows/ci.yml on push/PR
- [ ] Runs dotnet build, test, harness (smoke only in CI)
- [ ] Windows runner
```

### Deliverables
- `.github/workflows/ci.yml`

---

## Phase 6 — Release packaging

**Status:** PENDING

### Eval definition

```markdown
[CAPABILITY EVAL: release]
- [ ] scripts/release.ps1 — publish + zip
- [ ] docs/RELEASE.md — release checklist
- [ ] winget manifest stub in packaging/winget/
- [ ] CHANGELOG [1.0.0] section
```

### Deliverables
- `scripts/release.ps1`, `docs/RELEASE.md`, `packaging/winget/LLM.CLI.yaml`

---

## Phase 7 — v1.0 production sign-off

**Status:** PENDING

### Final regression eval (pass^3)

```markdown
[REGRESSION EVAL: v1.0-signoff]
Run scripts/harness.ps1 three consecutive times:
  Run 1: ALL PASS
  Run 2: ALL PASS
  Run 3: ALL PASS

Manual human eval (HUMAN REVIEW):
  - [ ] llm setup on clean machine
  - [ ] llm serve + Cursor connection
  - [ ] llm chat response < 60s on Qwen 7B
Risk: LOW (local CLI, no secrets in repo)
```

---

## Loop engineering schedule

| Loop | Interval | Command | Purpose |
|------|----------|---------|---------|
| Verify | 5 min (dev) | `scripts/loop-verify.ps1 -IntervalMinutes 5` | Catch regressions during active dev |
| Nightly | 24h | CI scheduled workflow | Long-term stability |
| Pre-release | On demand | `scripts/harness.ps1 -Strict` | Release gate |

---

## Feature backlog (post-v1.0)

| Feature | Version | Priority |
|---------|---------|----------|
| HuggingFace search API (live) | v1.8 | Done (`--live`) |
| Multi-GPU detect/select (Intel/NVIDIA/AMD) | v1.9 | Done (`llm gpu`) |
| Windows service (`llm serve --daemon`) | v2.1 | Done (Task Scheduler) |
| Plugin system | v2.1 | Low |
| Core CommandRouter migration | v2.4 | Low |
| OpenVINO runtime provider | v3.0 | Low |
| `brew install llm-cli` | v2.3 | Medium |

---

## Execution log

| Phase | Started | Completed | Harness |
|-------|---------|-----------|---------|
| 0 | 2026-07-21 | 2026-07-21 | PASS |
| 1 | 2026-07-21 | 2026-07-21 | PASS |
| 2 | 2026-07-21 | 2026-07-21 | PASS |
| 3 | 2026-07-21 | 2026-07-21 | PASS |
| 4 | 2026-07-21 | 2026-07-21 | PASS |
| 5 | 2026-07-21 | 2026-07-21 | PASS |
| 6 | 2026-07-21 | 2026-07-21 | PASS |
| 7 | 2026-07-21 | 2026-07-21 | PASS (setup on D:\AI) |
| 8 | 2026-07-21 | 2026-07-21 | PASS (HF live + daemon) |

_Update this table as phases complete._

---

## Phase 8 — Live Hub search + daemon (post-v1.0)

**Status:** COMPLETE

### Deliverables
- `llm model search --live` — Hugging Face Hub API
- `llm model files <org/repo>` — list GGUF files
- `llm daemon install|uninstall|status` — Task Scheduler auto-start
- `llm serve --daemon` — start + register auto-start
- Config migration for incomplete/old `config.json`
